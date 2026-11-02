using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace RAWeb.Server.Utilities;

/// <summary>
/// Determines which domains users may sign in with: the local machine, the machine's own domain,
/// and the domains that the machine's domain trusts.
/// <br /><br />
/// Sign-in and password change requests include a domain name from the user. RAWeb connects to that
/// domain (LDAP, NetUserChangePassword) with the server's own credentials, so an arbitrary domain name
/// could make the server authenticate to a host that an attacker controls.
/// Every domain name from a request must be checked with <see cref="IsAllowedLogonDomain(string?)"/>
/// before RAWeb contacts that domain.
/// </summary>
/// <remarks>
/// <para>
/// The machine's own domain is read from the local LSA policy, so it is allowed even when no domain controller
/// can be reached.
/// </para>
/// <para>
/// Trusted domains are enumerated with DsEnumerateDomainTrusts, which Netlogon answers from a cache that expires
/// after five minutes. After that, Netlogon asks a domain controller. If no domain controller can be reached,
/// trusted domains are not allowed until one can be reached again.
/// </para>
/// </remarks>
public static class TrustedDomains {
  /// <summary>
  /// Whether a domain name from a sign-in or password change request may be used.
  /// <br /><br />
  /// Allowed values are an empty domain, ".", and the machine name (all of which mean the local machine);
  /// the machine's own domain; and domains that the machine's domain trusts. Both NetBIOS and DNS names
  /// are accepted. The comparison ignores case and a trailing dot.
  /// </summary>
  public static bool IsAllowedLogonDomain(string? domain) {
    if (IsLocalMachineDomain(domain)) {
      return true;
    }

    var normalized = NormalizeDomainName(domain!);

    // the default domain and the machine's own domain do not require a domain controller
    if (IsAllowedLogonDomain(normalized, GetOwnDomainNames())) {
      return true;
    }

    // a domain that the machine's domain trusts (requires Netlogon's cache or a domain controller)
    try {
      var (trustedDomainNames, forestTrustRoots) = EnumerateTrustedDomains();
      if (IsAllowedLogonDomain(normalized, trustedDomainNames)) {
        return true;
      }

      // a forest trust only lists the other forest's root domain, so check the domains inside that forest too
      if (forestTrustRoots.Count > 0) {
        var domainController = GetPrimaryDomainControllerName();
        foreach (var forestRoot in forestTrustRoots) {
          try {
            if (IsAllowedLogonDomain(normalized, GetForestTrustDomainNames(domainController, forestRoot))) {
              return true;
            }
          }
          catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Could not read the domains in trusted forest {forestRoot}: {ex.Message}");
          }
        }
      }
    }
    catch (Exception ex) {
      System.Diagnostics.Debug.WriteLine("Could not enumerate trusted domains: " + ex.Message);
    }

    return false;
  }

  /// <summary>
  /// Whether a domain name is the local machine or one of the specified allowed domain names.
  /// </summary>
  public static bool IsAllowedLogonDomain(string? domain, IReadOnlySet<string> allowedDomainNames) {
    if (IsLocalMachineDomain(domain)) {
      return true;
    }

    var normalized = NormalizeDomainName(domain!);
    return normalized.Length > 0 && allowedDomainNames.Any(allowed => string.Equals(NormalizeDomainName(allowed), normalized, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Whether the domain name refers to the local machine instead of a domain.
  /// </summary>
  private static bool IsLocalMachineDomain(string? domain) {
    if (string.IsNullOrWhiteSpace(domain)) {
      return true;
    }
    var normalized = NormalizeDomainName(domain);
    return normalized == "." || normalized.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Removes surrounding whitespace and a trailing dot from a domain name.
  /// </summary>
  public static string NormalizeDomainName(string domain) {
    var trimmed = domain.Trim();
    return trimmed.Length > 1 && trimmed.EndsWith('.') ? trimmed[..^1] : trimmed;
  }

  /// <summary>
  /// Gets the default sign-in domain (<see cref="SignIn.GetDomainName"/>) and the NetBIOS and DNS names of the
  /// machine's own domain. These come from the machine's configuration and do not require a domain controller.
  /// </summary>
  private static HashSet<string> GetOwnDomainNames() {
    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    AddName(names, SignIn.GetDomainName());

    var primaryDomain = GetPrimaryDomain();
    if (primaryDomain is not null) {
      AddName(names, primaryDomain.Value.NetbiosName);
      AddName(names, primaryDomain.Value.DnsName);
    }

    return names;
  }

  private static void AddName(HashSet<string> names, string? name) {
    if (!string.IsNullOrWhiteSpace(name)) {
      names.Add(NormalizeDomainName(name));
    }
  }

  #region Win32

  private const int DsRolePrimaryDomainInfoBasic = 1;
  private const int DsRole_RoleStandaloneWorkstation = 0;
  private const int DsRole_RoleStandaloneServer = 2;

  [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
  private static extern int DsRoleGetPrimaryDomainInformation(string? lpServer, int infoLevel, out IntPtr buffer);

  [DllImport("netapi32.dll")]
  private static extern void DsRoleFreeMemory(IntPtr buffer);

  [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
  private static extern int DsEnumerateDomainTrustsW(string? serverName, DsDomainFlags flags, out IntPtr domains, out uint domainCount);

  [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
  private static extern int DsGetDcNameW(string? computerName, string? domainName, IntPtr domainGuid, string? siteName, uint flags, out IntPtr domainControllerInfo);

  [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
  private static extern int DsGetForestTrustInformationW(string? serverName, string trustedDomainName, uint flags, out IntPtr forestTrustInfo);

  [DllImport("netapi32.dll")]
  private static extern int NetApiBufferFree(IntPtr buffer);

  /// <summary>
  /// Gets the machine's own domain from the local LSA policy or null if the machine is not joined to a domain.
  /// This does not contact a domain controller.
  /// </summary>
  private static (string? NetbiosName, string? DnsName)? GetPrimaryDomain() {
    var result = DsRoleGetPrimaryDomainInformation(null, DsRolePrimaryDomainInfoBasic, out var buffer);
    if (result != 0 || buffer == IntPtr.Zero) {
      return null;
    }

    try {
      // the first int in DSROLE_PRIMARY_DOMAIN_INFO_BASIC is MachineRole
      var machineRole = Marshal.ReadInt32(buffer, 0);
      if (machineRole == DsRole_RoleStandaloneWorkstation || machineRole == DsRole_RoleStandaloneServer) {
        return null;
      }

      // DSROLE_PRIMARY_DOMAIN_INFO_BASIC also has pointers to NetbiosDomainName and DnsForestName strings
      var netbiosName = Marshal.PtrToStringUni(Marshal.ReadIntPtr(buffer, 8));
      var dnsName = Marshal.PtrToStringUni(Marshal.ReadIntPtr(buffer, 8 + IntPtr.Size));
      return (netbiosName, dnsName);
    }
    finally {
      DsRoleFreeMemory(buffer);
    }
  }

  /// <summary>
  /// Enumerates the domains in the machine's forest and the domains that the machine's domain directly trusts.
  /// Also returns the root domains of trusted forests, whose other domains are not included in the enumeration.
  /// </summary>
  /// <exception cref="System.ComponentModel.Win32Exception">When the trusts cannot be enumerated.</exception>
  private static (HashSet<string> DomainNames, List<string> ForestTrustRoots) EnumerateTrustedDomains() {
    if (GetPrimaryDomain() is null) {
      return ([], []); // a machine that is not joined to a domain has no trusted domains
    }

    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var forestTrustRoots = new List<string>();

    foreach (var trust in ReadDomainTrusts()) {
      // users can sign in from domains in this forest and from domains that this domain trusts,
      // but not from domains that only trust this domain (inbound-only trusts)
      var usersCanSignIn = (trust.Flags & (DsDomainFlags.InForest | DsDomainFlags.DirectOutbound | DsDomainFlags.Primary)) != 0;
      if (!usersCanSignIn) {
        continue;
      }

      AddName(names, trust.NetbiosName);
      AddName(names, trust.DnsName);

      // a forest trust only lists the other forest's root domain, so the caller must look up the rest of that forest
      var isForestTrust = (trust.Flags & DsDomainFlags.DirectOutbound) != 0 && (trust.Attributes & TrustAttributes.ForestTransitive) != 0;
      var forestRoot = trust.DnsName ?? trust.NetbiosName;
      if (isForestTrust && !string.IsNullOrEmpty(forestRoot)) {
        forestTrustRoots.Add(forestRoot);
      }
    }

    return (names, forestTrustRoots);
  }

  /// <summary>
  /// A domain returned by DsEnumerateDomainTrusts.
  /// </summary>
  private sealed record DomainTrust(string? NetbiosName, string? DnsName, DsDomainFlags Flags, TrustAttributes Attributes);

  /// <summary>
  /// Calls DsEnumerateDomainTrusts and copies the results into <see cref="DomainTrust"/> records.
  /// Inbound-only trusts are not requested.
  /// </summary>
  /// <exception cref="System.ComponentModel.Win32Exception">When the trusts cannot be enumerated.</exception>
  private static List<DomainTrust> ReadDomainTrusts() {
    var requestedFlags = DsDomainFlags.InForest | DsDomainFlags.DirectOutbound | DsDomainFlags.Primary;
    var result = DsEnumerateDomainTrustsW(null, requestedFlags, out var buffer, out var count);
    if (result != 0) {
      throw new System.ComponentModel.Win32Exception(result);
    }

    try {
      // the buffer is an array of DS_DOMAIN_TRUSTSW structures
      var trusts = new List<DomainTrust>();
      var size = Marshal.SizeOf<DS_DOMAIN_TRUSTSW>();
      for (var i = 0; i < count; i++) {
        var entry = Marshal.PtrToStructure<DS_DOMAIN_TRUSTSW>(IntPtr.Add(buffer, i * size));
        trusts.Add(new DomainTrust(
          Marshal.PtrToStringUni(entry.NetbiosDomainName),
          Marshal.PtrToStringUni(entry.DnsDomainName),
          entry.Flags,
          entry.TrustAttributes
        ));
      }
      return trusts;
    }
    finally {
      NetApiBufferFree(buffer);
    }
  }

  /// <summary>
  /// Gets the name of a domain controller in the machine's domain (e.g., \\dc1.contoso.com).
  /// </summary>
  private static string? GetPrimaryDomainControllerName() {
    var result = DsGetDcNameW(null, null, IntPtr.Zero, null, 0, out var info);
    if (result != 0 || info == IntPtr.Zero) {
      return null;
    }
    try {
      // DOMAIN_CONTROLLER_INFOW starts with DomainControllerName
      return Marshal.PtrToStringUni(Marshal.ReadIntPtr(info, 0));
    }
    finally {
      NetApiBufferFree(info);
    }
  }

  /// <summary>
  /// Gets the NetBIOS and DNS names of the domains in a trusted forest.
  /// <br /><br />
  /// Domains whose SIDs were disabled (by an administrator or by a conflict with another forest) are skipped
  /// because their users cannot sign in. Disabled NetBIOS names are skipped, but the domain's DNS name is still used.
  /// </summary>
  /// <exception cref="System.ComponentModel.Win32Exception">When the forest trust information cannot be read.</exception>
  private static HashSet<string> GetForestTrustDomainNames(string? domainController, string forestRoot) {
    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var domain in ReadForestTrustDomains(domainController, forestRoot)) {
      var sidDisabled = (domain.Flags & (ForestTrustRecordFlags.SidDisabledAdmin | ForestTrustRecordFlags.SidDisabledConflict)) != 0;
      if (sidDisabled) {
        continue;
      }

      AddName(names, domain.DnsName);

      var netbiosDisabled = (domain.Flags & (ForestTrustRecordFlags.NetbiosDisabledAdmin | ForestTrustRecordFlags.NetbiosDisabledConflict)) != 0;
      if (!netbiosDisabled) {
        AddName(names, domain.NetbiosName);
      }
    }

    return names;
  }

  /// <summary>
  /// A domain in a trusted forest.
  /// </summary>
  private sealed record ForestTrustDomain(string? DnsName, string? NetbiosName, ForestTrustRecordFlags Flags);

  /// <summary>
  /// Calls DsGetForestTrustInformation and copies its domain records into <see cref="ForestTrustDomain"/> records.
  /// </summary>
  /// <param name="domainController">A domain controller in the machine's domain, which stores the forest trust information.</param>
  /// <param name="forestRoot">The root domain of the trusted forest.</param>
  /// <exception cref="System.ComponentModel.Win32Exception">When the forest trust information cannot be read.</exception>
  private static List<ForestTrustDomain> ReadForestTrustDomains(string? domainController, string forestRoot) {
    var result = DsGetForestTrustInformationW(domainController, forestRoot, 0, out var buffer);
    if (result != 0) {
      throw new System.ComponentModel.Win32Exception(result);
    }

    try {
      var domains = new List<ForestTrustDomain>();

      // the buffer is an LSA_FOREST_TRUST_INFORMATION structure, which points to an array of pointers to records
      var info = Marshal.PtrToStructure<LSA_FOREST_TRUST_INFORMATION>(buffer);
      for (var i = 0; i < info.RecordCount; i++) {
        var recordPointer = Marshal.ReadIntPtr(info.Entries, i * IntPtr.Size);
        var record = Marshal.PtrToStructure<LSA_FOREST_TRUST_RECORD_HEADER>(recordPointer);
        if (record.ForestTrustType != LsaForestTrustRecordType.DomainInfo) {
          continue;
        }

        // the record's data comes right after the header, and it is an LSA_FOREST_TRUST_DOMAIN_INFO for domain records
        var dataPointer = IntPtr.Add(recordPointer, Marshal.SizeOf<LSA_FOREST_TRUST_RECORD_HEADER>());
        var domainInfo = Marshal.PtrToStructure<LSA_FOREST_TRUST_DOMAIN_INFO>(dataPointer);
        domains.Add(new ForestTrustDomain(
          domainInfo.DnsName.ToManagedString(),
          domainInfo.NetbiosName.ToManagedString(),
          record.Flags
        ));
      }

      return domains;
    }
    finally {
      NetApiBufferFree(buffer);
    }
  }

  /// <summary>
  /// Flags for DsEnumerateDomainTrusts (the DS_DOMAIN_* values in dsgetdc.h).
  /// </summary>
  [Flags]
  private enum DsDomainFlags : uint {
    InForest = 0x0001,
    DirectOutbound = 0x0002,
    TreeRoot = 0x0004,
    Primary = 0x0008,
    NativeMode = 0x0010,
    DirectInbound = 0x0020,
  }

  /// <summary>
  /// The TRUST_ATTRIBUTE_* values in ntsecapi.h that RAWeb uses.
  /// </summary>
  [Flags]
  private enum TrustAttributes : uint {
    ForestTransitive = 0x0008,
  }

  /// <summary>
  /// LSA_FOREST_TRUST_RECORD_TYPE in ntsecapi.h.
  /// </summary>
  private enum LsaForestTrustRecordType : int {
    TopLevelName = 0,
    TopLevelNameEx = 1,
    DomainInfo = 2,
  }

  /// <summary>
  /// The flags of a domain record in the forest trust information (LSA_SID_DISABLED_* and LSA_NB_DISABLED_* in ntsecapi.h).
  /// </summary>
  [Flags]
  private enum ForestTrustRecordFlags : uint {
    SidDisabledAdmin = 0x1,
    SidDisabledConflict = 0x2,
    NetbiosDisabledAdmin = 0x4,
    NetbiosDisabledConflict = 0x8,
  }

  /// <summary>
  /// DS_DOMAIN_TRUSTSW in dsgetdc.h.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  private struct DS_DOMAIN_TRUSTSW {
    public IntPtr NetbiosDomainName;
    public IntPtr DnsDomainName;
    public DsDomainFlags Flags;
    public uint ParentIndex;
    public uint TrustType;
    public TrustAttributes TrustAttributes;
    public IntPtr DomainSid;
    public Guid DomainGuid;
  }

  /// <summary>
  /// LSA_FOREST_TRUST_INFORMATION in ntsecapi.h.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  private struct LSA_FOREST_TRUST_INFORMATION {
    public uint RecordCount;
    public IntPtr Entries;
  }

  /// <summary>
  /// The fields of LSA_FOREST_TRUST_RECORD in ntsecapi.h that come before its ForestTrustData union.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  private struct LSA_FOREST_TRUST_RECORD_HEADER {
    public ForestTrustRecordFlags Flags;
    public LsaForestTrustRecordType ForestTrustType;
    public long Time;
  }

  /// <summary>
  /// LSA_FOREST_TRUST_DOMAIN_INFO in ntsecapi.h.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  private struct LSA_FOREST_TRUST_DOMAIN_INFO {
    public IntPtr Sid;
    public LSA_UNICODE_STRING DnsName;
    public LSA_UNICODE_STRING NetbiosName;
  }

  /// <summary>
  /// LSA_UNICODE_STRING in ntsecapi.h. The string is not null-terminated, and Length is in bytes.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  private struct LSA_UNICODE_STRING {
    public ushort Length;
    public ushort MaximumLength;
    public IntPtr Buffer;

    public readonly string? ToManagedString() {
      return Buffer == IntPtr.Zero ? null : Marshal.PtrToStringUni(Buffer, Length / 2);
    }
  }

  #endregion
}
