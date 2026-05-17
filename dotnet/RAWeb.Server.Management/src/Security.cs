using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Security.Principal;

namespace RAWeb.Server.Management;

/// <summary>
/// A resolved security identifier (SID) for a user or group.
/// </summary>
/// <param name="sid"></param>
/// <param name="domain"></param>
/// <param name="userName"></param>
/// <param name="displayName"></param>
/// <param name="userPrincipalName"></param>
/// <param name="principalKind"></param>
[DataContract]
public class ResolvedSecurityIdentifier(string sid, string domain, string userName, string displayName, string userPrincipalName, PrincipalKind principalKind) {
  [DataMember] public string Sid { get; set; } = sid;
  [DataMember] public string Domain { get; set; } = domain;
  [DataMember] public string UserPrincipalName { get; set; } = userPrincipalName;
  [DataMember] public string UserName { get; set; } = userName;
  [DataMember] public string DisplayName { get; set; } = displayName;
  [DataMember] public PrincipalKind PrincipalKind { get; set; } = principalKind;
  [DataMember]
  public string ExpandedDisplayName {
    get {
      return ToString();
    }
  }
  public override string ToString() {
    if (!string.IsNullOrEmpty(UserPrincipalName)) {
      if (!string.IsNullOrEmpty(DisplayName)) {
        return DisplayName + " (" + UserPrincipalName + ")";
      }
      else {
        return UserPrincipalName;
      }
    }
    else {
      if (!string.IsNullOrEmpty(DisplayName)) {
        return DisplayName + " (" + (Domain ?? Environment.MachineName) + "\\" + UserName + ")";
      }
      else {
        return Domain + "\\" + UserName;
      }
    }
  }

  /// <summary>
  /// Resolves a single security identifier (SID) to its corresponding
  /// account domain, username, and display name.
  /// <br /><br />
  /// If you need to resolve multiple SIDs, use
  /// <see cref="ResolvedSecurityIdentifiers.FromSidStrings(string[], out List{string})"/>
  /// instead.
  /// It groups SIDs by their domain for more efficient lookup.
  /// </summary>
  /// <param name="sidString"></param>
  /// <returns></returns>
  public static ResolvedSecurityIdentifier? FromSidString(string sidString) {
    var resolvedSids = ResolvedSecurityIdentifiers.FromSidStrings([sidString], out _);
    if (resolvedSids.Count > 0) {
      return resolvedSids[0];
    }
    return null;
  }

  /// <summary>
  /// Resolves a SecurityIdentifier to its corresponding
  /// resolved security identifier (SID).
  /// <br /><br />
  /// See <see cref="FromSidString(string)"/> for details.
  /// <br /><br />
  /// If you need to resolve multiple SIDs, use
  /// <see cref="ResolvedSecurityIdentifiers.FromSids(SecurityIdentifier[], out List{SecurityIdentifier})"/>
  /// instead.
  /// It groups SIDs by their domain for more efficient lookup.
  /// </summary>
  /// <param name="sid"></param>
  /// <returns></returns>
  public static ResolvedSecurityIdentifier? FromSecurityIdentifier(SecurityIdentifier sid) {
    return FromSidString(sid.Value);
  }

  /// <summary>
  /// Accepts a lookup string and attempts to find the corresponding
  /// resolved security identifier (SID) for a user or group.
  /// <br /><br />
  /// The lookup string can be a username, group name, user principal name,
  /// or string SID.
  /// </summary>
  /// <param name="lookup"></param>
  /// <param name="domain"></param>
  /// <returns></returns>
  public static ResolvedSecurityIdentifier? FromLookupString(string lookup, string? domain = null) {
    // if the lookup string looks like a SID, resolve it directly
    try {
      var sid = new SecurityIdentifier(lookup);
      return FromSecurityIdentifier(sid);
    }
    catch {
    }

    var isMachineContext = domain is null || string.IsNullOrWhiteSpace(domain) || domain.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);

    if (isMachineContext) {
      // resolve local account via NTAccount translation
      try {
        var ntAccount = new NTAccount(Environment.MachineName, lookup);
        var sid = (SecurityIdentifier)ntAccount.Translate(typeof(SecurityIdentifier));
        return FromSecurityIdentifier(sid);
      }
      catch {
        return null;
      }
    }

    // resolve domain account via LDAP
    try {
      using var connection = LdapHelpers.OpenLdapConnection(domain!);

      // get the distinguished name of the connection's root domain
      var domainDN = LdapHelpers.GetDefaultNamingContext(connection);

      // search the domain:
      // - sAMAccountName: DOMAIN\username or groupname (note that there is no prefix for group names)
      // - userPrincipalName: username@domain.tld
      // - distinguishedName: CN=...,OU=...,DC=...
      // - cn: the leftmost CN component of the distinguished name
      string[] searchAttributes = ["sAMAccountName", "userPrincipalName", "distinguishedName", "cn"];
      foreach (var attr in searchAttributes) {
        // prepare a request
        var filter = $"({attr}={LdapHelpers.LdapEscapeFilter(lookup)})";
        var req = new SearchRequest(
                    domainDN,
                    filter,
                    SearchScope.Subtree,
                    // the attributes that we want to retrieve for a matched entry
                    "objectSid", "sAMAccountName", "displayName", "userPrincipalName", "objectClass"
                  );

        // get the requested attributes exiting early if there are no matches
        var resp = (SearchResponse)connection.SendRequest(req);
        if (resp.Entries.Count == 0) {
          continue;
        }

        var entry = resp.Entries[0]; // we want the best match (the first returned entry)

        // resolve the security identifier to a SecurityIdentifier instance
        var sidBytes = (byte[])entry.Attributes["objectSid"].GetValues(typeof(byte[]))[0];
        var resolvedSid = new SecurityIdentifier(sidBytes, 0);

        // try to resolve a nice display name, prefering displayName,
        // then the commonName, and as a last resort, the input lookup string
        var displayName = lookup;
        if (entry.Attributes["displayName"]?.Count > 0) {
          displayName = (string)entry.Attributes["displayName"][0];
        }
        else if (entry.Attributes["cn"]?.Count > 0) {
          displayName = (string)entry.Attributes["cn"][0];
        }

        var kind = entry.ToPrincipalKind();
        var name = entry.Attributes["sAMAccountName"]?.Count > 0 ? (string)entry.Attributes["sAMAccountName"][0] : lookup;
        var userPrincipalName = entry.Attributes["userPrincipalName"]?.Count > 0 ? (string)entry.Attributes["userPrincipalName"][0] : "";

        return new ResolvedSecurityIdentifier(
            sid: resolvedSid.Value,
            domain: domain!,
            userName: name,
            displayName: displayName,
            userPrincipalName: userPrincipalName,
            principalKind: kind
        );
      }
    }
    catch {
    }

    return null;
  }
}

/// <summary>
/// A collection of resolved security identifiers (SIDs).
/// </summary>
public class ResolvedSecurityIdentifiers : Collection<ResolvedSecurityIdentifier> {
  public ResolvedSecurityIdentifiers() {
  }
  public ResolvedSecurityIdentifiers(IList<ResolvedSecurityIdentifier> apps) {
    foreach (var app in apps) {
      Add(app);
    }
  }

  /// <summary>
  /// Resolves a list of security identifiers (SIDs) to their corresponding
  /// account domains, usernames, and display names.
  /// <br /><br />
  /// SIDs are grouped by domain so that each domain requires only one LDAP
  /// connection. Local machine SIDs are resolved via NTAccount translation.
  /// </summary>
  /// <param name="sids"></param>
  /// <param name="invalidOrUnfoundSids"></param>
  /// <returns></returns>
  public static ResolvedSecurityIdentifiers FromSidStrings(string[] sids, out List<string> invalidOrUnfoundSids) {
    var resolvedSids = new ResolvedSecurityIdentifiers();
    invalidOrUnfoundSids = [];

    // group SIDs by their domain for more efficient lookup (domain or local machine)
    var sidsByDomain = new Dictionary<string, List<SecurityIdentifier>>();
    foreach (var sid in sids) {
      try {
        var securityIdentifier = new SecurityIdentifier(sid);
        var domainName = securityIdentifier.ToNetBiosDomainName();
        if (domainName is null) {
          invalidOrUnfoundSids.Add(sid);
          continue;
        }

        if (!sidsByDomain.ContainsKey(domainName)) {
          sidsByDomain[domainName] = [];
        }

        sidsByDomain[domainName].Add(securityIdentifier);
      }
      catch {
        // ignore invalid SIDs
        invalidOrUnfoundSids.Add(sid);
      }
    }

    // prefer FQDN over NetBIOS domain name
    sidsByDomain = sidsByDomain.ToDictionary(
      // re-write the key to use the FQDN (if it can be resolved)
      keyValuePair => {
        var domain = keyValuePair.Key;
        var sampleSid = keyValuePair.Value[0];
        var fqdn = sampleSid.ToFQDN();
        return fqdn ?? domain;
      },
      // keep the existing list of SIDs that we need to resolve
      keyValuePair => keyValuePair.Value
    );

    // resolve the SIDs for each domain
    foreach (var domainEntry in sidsByDomain) {
      var isLocalMachine = Environment.MachineName.Equals(domainEntry.Key, StringComparison.OrdinalIgnoreCase);

      if (isLocalMachine) {
        // resolve local machine SIDs via NTAccount translation
        foreach (var securityIdentifier in domainEntry.Value) {
          try {
            var ntAccount = (NTAccount)securityIdentifier.Translate(typeof(NTAccount));
            var parts = ntAccount.Value.Split('\\');
            var userName = parts.Length > 1 ? parts[1] : parts[0];
            resolvedSids.Add(new ResolvedSecurityIdentifier(
                sid: securityIdentifier.Value,
                domain: Environment.MachineName,
                userName: userName,
                displayName: userName,
                userPrincipalName: "", // TODO: should this be userName@Environment.MachineName.local?
                principalKind: PrincipalKind.User
            ));
          }
          catch {
            invalidOrUnfoundSids.Add(securityIdentifier.Value);
          }
        }
      }

      // resolve domain SIDs via LDAP
      else {
        // open a connection to a domain and
        // get the distinguished name of the connection's root domain
        LdapConnection? connection = null;
        string? domainDN = null;
        try {
          connection = LdapHelpers.OpenLdapConnection(domainEntry.Key);
          domainDN = LdapHelpers.GetDefaultNamingContext(connection);
        }
        catch {
          // unable to connect to domain; mark all SIDs for this domain as unfound
          foreach (var securityIdentifier in domainEntry.Value) {
            invalidOrUnfoundSids.Add(securityIdentifier.Value);
          }
          connection?.Dispose();
          continue;
        }

        using (connection) {
          foreach (var securityIdentifier in domainEntry.Value) {
            try {
              // prepare a search filter based on the SID
              var filter = "(objectSid=" + LdapHelpers.LdapEncodeSid(securityIdentifier) + ")";

              // look for the first matching entry on the domain
              var req = new SearchRequest(
                          domainDN,
                          filter,
                          SearchScope.Subtree,
                          // the attributes that we want to retrieve for a matched entry
                          "sAMAccountName", "displayName", "userPrincipalName", "objectClass"
                        );
              var resp = (SearchResponse)connection.SendRequest(req);
              if (resp.Entries.Count == 0) {
                invalidOrUnfoundSids.Add(securityIdentifier.Value);
                continue;
              }
              var entry = resp.Entries[0];

              var kind = entry.ToPrincipalKind();
              var userName = entry.Attributes["sAMAccountName"]?.Count > 0 ? (string)entry.Attributes["sAMAccountName"][0] : securityIdentifier.Value;
              var upn = entry.Attributes["userPrincipalName"]?.Count > 0 ? (string)entry.Attributes["userPrincipalName"][0] : "";

              // try to resolve a nice display name, prefering displayName,
              // then the commonName, and as a last resort, the userName string
              var displayName = userName;
              if (entry.Attributes["displayName"]?.Count > 0) {
                displayName = (string)entry.Attributes["displayName"][0];
              }
              else if (entry.Attributes["cn"]?.Count > 0) {
                displayName = (string)entry.Attributes["cn"][0];
              }

              resolvedSids.Add(new ResolvedSecurityIdentifier(
                  sid: securityIdentifier.Value,
                  domain: domainEntry.Key,
                  userName: userName,
                  displayName: displayName,
                  userPrincipalName: upn,
                  principalKind: kind
              ));
            }
            catch {
              invalidOrUnfoundSids.Add(securityIdentifier.Value);
            }
          }
        }
      }
    }

    return resolvedSids;
  }

  /// <summary>
  /// Resolves a list of security identifiers (SIDs) to their corresponding
  /// account domains, usernames, and display names.
  /// </summary>
  /// <param name="sids"></param>
  /// <param name="invalidOrUnfoundSids"></param>
  /// <returns></returns>
  public static ResolvedSecurityIdentifiers FromSids(SecurityIdentifier[] sids, out List<SecurityIdentifier> invalidOrUnfoundSids) {
    var sidStrings = sids.Select(sid => sid.Value).ToArray();
    List<string> invalidOrUnfoundSidStrings = [];
    var resolvedSids = FromSidStrings(sidStrings, out invalidOrUnfoundSidStrings);

    invalidOrUnfoundSids = [.. invalidOrUnfoundSidStrings.Select(sidString => new SecurityIdentifier(sidString))];
    return resolvedSids;
  }
}

/// <summary>
/// Indicates the kind of principal (user, group, computer, other).
/// </summary>
public enum PrincipalKind {
  User,
  Group,
  Computer,
  Other
}

public static class SecurityTransformers {
  /// <summary>
  /// Builds a <see cref="RawSecurityDescriptor"/> from collections of allowed and denied SIDs.
  /// </summary>
  /// <param name="allowedSids">
  /// A collection of tuples (Sid, Rights) representing SIDs granted access.
  /// Optional. May be <see langword="null"/>.
  /// If <c>Rights</c> is <see langword="null"/>, defaults to <see cref="FileSystemRights.ReadData"/>.
  /// </param>
  /// <param name="deniedSids">
  /// A collection of tuples (Sid, Rights) representing SIDs explicitly denied access.
  /// Optional. May be <see langword="null"/>.
  /// </param>
  /// <returns>
  /// A <see cref="RawSecurityDescriptor"/> containing a DACL with the specified allowed and denied entries.
  /// </returns>
  /// <remarks>
  /// <para>
  /// Deny access entries are inserted before allow entries.
  /// </para>
  /// <para>
  /// The created descriptor contains only a DACL; the owner, group, and SACL fields are <see langword="null"/>.
  /// </para>
  /// </remarks>
  public static RawSecurityDescriptor? SidRightsToRawSecurityDescriptor(
    IEnumerable<Tuple<string, FileSystemRights?>>? allowedSids = null,
    IEnumerable<Tuple<string, FileSystemRights?>>? deniedSids = null
  ) {
    var dacl = new RawAcl(2, 0);
    var aceIndex = 0;

    if ((deniedSids is null || !deniedSids.Any()) && (allowedSids is null || !allowedSids.Any())) {
      return null;
    }

    // add deny access control entries (ACEs) first
    if (deniedSids is not null) {
      foreach (var (sidStr, rights) in deniedSids) {
        var sid = new SecurityIdentifier(sidStr);
        var rightsValue = rights ?? FileSystemRights.ReadData;

        var ace = new CommonAce(
            AceFlags.None,
            AceQualifier.AccessDenied,
            (int)rightsValue,
            sid,
            false,
            null
        );

        dacl.InsertAce(aceIndex++, ace);
      }
    }

    // add allow access control entries (ACEs)
    if (allowedSids is not null) {
      foreach (var (sidStr, rights) in allowedSids) {
        var sid = new SecurityIdentifier(sidStr);
        var rightsValue = rights ?? FileSystemRights.ReadData;

        var ace = new CommonAce(
            AceFlags.None,
            AceQualifier.AccessAllowed,
            (int)rightsValue,
            sid,
            false,
            null
        );

        dacl.InsertAce(aceIndex++, ace);
      }
    }

    // combine ACEs into a RawSecurityDescriptor
    var descriptor = new RawSecurityDescriptor(
        ControlFlags.DiscretionaryAclPresent,
        owner: null,
        group: null,
        systemAcl: null,
        discretionaryAcl: dacl
    );

    return descriptor;
  }
}

public static class SecurityIdentifierExtensions {
  /// <summary>
  /// Gets the NetBIOS domain name for a given SecurityIdentifier (SID).
  /// </summary>
  /// <param name="sid"></param>
  /// <returns></returns>
  public static string? ToNetBiosDomainName(this SecurityIdentifier sid) {
    if (sid.IsWellKnown()) {
      return Environment.MachineName;
    }

    // extract the domain portion of the SID
    var accountDomainSid = sid.AccountDomainSid;
    if (accountDomainSid is null) {
      return Environment.MachineName;
    }

    // check if it matches the local machine's SID
    var adminNTA = new NTAccount($"{Environment.MachineName}\\Administrator");
    var localAdminSid = (SecurityIdentifier)adminNTA.Translate(typeof(SecurityIdentifier));
    var localDomainSid = localAdminSid.AccountDomainSid;
    if (localDomainSid is not null && accountDomainSid.Equals(localDomainSid)) {
      return Environment.MachineName;
    }

    // try to translate the base SID to get its domain name
    try {
      var translated = sid.Translate(typeof(NTAccount)).Value;
      var netbiosDomainName = translated.Split('\\')[0];
      return netbiosDomainName;
    }
    catch {
      return null;
    }
  }

  /// <summary>
  /// Gets the fully-qualified domain name for a given SecurityIdentifier (SID)
  /// by connecting to the domain controller via LDAP and reading <c>defaultNamingContext</c>
  /// from the RootDSE.
  /// </summary>
  /// <param name="sid"></param>
  /// <returns></returns>
  public static string? ToFQDN(this SecurityIdentifier sid) {
    var netbiosDomainName = sid.ToNetBiosDomainName();
    if (netbiosDomainName is null || netbiosDomainName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)) {
      return null;
    }

    try {
      using var connection = LdapHelpers.OpenLdapConnection(netbiosDomainName);
      var req = new SearchRequest("", "(objectClass=*)", SearchScope.Base, "defaultNamingContext");
      var resp = (SearchResponse)connection.SendRequest(req);
      var dn = (string)resp.Entries[0].Attributes["defaultNamingContext"][0];
      // convert "DC=domain,DC=tld" to "domain.tld"
      return string.Join(".", dn.Split(',')
          .Select(p => p.Trim())
          .Where(p => p.StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
          .Select(p => p.Substring(3)));
    }
    catch {
      return null;
    }
  }

  /// <summary>
  /// Determines whether a given SecurityIdentifier (SID) is a well-known SID.
  /// </summary>
  /// <param name="sid"></param>
  /// <returns></returns>
  public static bool IsWellKnown(this SecurityIdentifier sid) {
    foreach (WellKnownSidType type in Enum.GetValues(typeof(WellKnownSidType))) {
      if (sid.IsWellKnown(type))
        return true;
    }
    return false;
  }
}

public static class SecurityDescriptorExtensions {
  /// <summary>
  /// Gets the list of allowed ACEs from a RawSecurityDescriptor.
  /// <br /><br />
  /// If you need to find which SIDs are allowed AND NOT DENIED,
  /// use <see cref="GetAllowedSids(RawSecurityDescriptor)"/> instead.
  /// </summary>
  /// <param name="securityDescriptor"></param>
  /// <param name="requiredRights"></param>
  /// <returns></returns>
  public static List<CommonAce> GetAccessAllowedAces(this RawSecurityDescriptor securityDescriptor, FileSystemRights? requiredRights = null) {
    if (securityDescriptor.DiscretionaryAcl is null) {
      return [];
    }

    return securityDescriptor.DiscretionaryAcl
      .OfType<CommonAce>()
      .Where(ace => {
        var isAllowedAce = ace.AceType == AceType.AccessAllowed;
        var hasRequiredRights = !requiredRights.HasValue || (ace.AccessMask & (int)requiredRights) == (int)requiredRights;
        return isAllowedAce && hasRequiredRights;
      })
      .ToList();
  }

  /// <summary>
  /// Gets the list of denied ACEs from a RawSecurityDescriptor.
  /// </summary>
  /// <param name="securityDescriptor"></param>
  /// <returns></returns>
  public static List<CommonAce> GetAccessDeniedAces(this RawSecurityDescriptor securityDescriptor, FileSystemRights? requiredRights = null) {
    if (securityDescriptor.DiscretionaryAcl is null) {
      return [];
    }

    return securityDescriptor.DiscretionaryAcl
      .OfType<CommonAce>()
      .Where(ace => {
        var isAllowedAce = ace.AceType == AceType.AccessDenied;
        var hasRequiredRights = !requiredRights.HasValue || (ace.AccessMask & (int)requiredRights) == (int)requiredRights;
        return isAllowedAce && hasRequiredRights;
      })
      .ToList();
  }

  /// <summary>
  /// Gets the list of allowed SIDs from a RawSecurityDescriptor,
  /// excluding any that are also explicitly denied.
  /// </summary>
  /// <param name="securityDescriptor"></param>
  /// <param name="requiredRights"></param>
  /// <returns></returns>
  public static List<SecurityIdentifier> GetAllowedSids(this RawSecurityDescriptor securityDescriptor, FileSystemRights? requiredRights = null) {
    // since we need to exclude denined SIDs, get the denied ACEs first
    var deniedAces = securityDescriptor.GetAccessDeniedAces();

    // get the allowed ACEs that are not also denied
    return securityDescriptor
      .GetAccessAllowedAces(requiredRights)
      .Select(ace => {
        // only include if not also denied
        if (!deniedAces.Any(deniedAce => deniedAce.SecurityIdentifier.Equals(ace.SecurityIdentifier))) {
          return ace.SecurityIdentifier;
        }
        return null;
      })
      // filter out nulls
      .Where(sid => sid != null)
      .ToList()!;
  }

  /// <summary>
  /// Gets the list of explicitly allowed SIDs from a RawSecurityDescriptor.
  /// </summary>
  /// <param name="securityDescriptor"></param>
  /// <param name="requiredRights"></param>
  /// <returns></returns>
  public static List<SecurityIdentifier> GetExplicitlyAllowedSids(this RawSecurityDescriptor securityDescriptor, FileSystemRights? requiredRights = null) {
    return [.. securityDescriptor.GetAccessAllowedAces(requiredRights).Select(ace => ace.SecurityIdentifier)];
  }

  /// <summary>
  /// Gets the list of explicitly denied SIDs from a RawSecurityDescriptor.
  /// </summary>
  /// <param name="securityDescriptor"></param>
  /// <param name="requiredRights"></param>
  /// <returns></returns>
  public static List<SecurityIdentifier> GetExplicitlyDeniedSids(this RawSecurityDescriptor securityDescriptor, FileSystemRights? requiredRights = null) {
    return [.. securityDescriptor.GetAccessDeniedAces(requiredRights).Select(ace => ace.SecurityIdentifier)];
  }
}

// Shared LDAP helpers used by ResolvedSecurityIdentifier and SecurityIdentifierExtensions
public static class LdapHelpers {
  /// <summary>
  /// Opens an LDAP connection to the specified domain using the current
  /// process's credentials (usually the IIS application pool identity).
  /// </summary>
  /// <param name="domain"></param>
  /// <returns></returns>
  public static LdapConnection OpenLdapConnection(string domain, TimeSpan? timeout = null) {
    var identifier = new LdapDirectoryIdentifier(domain, 389);
    var connection = new LdapConnection(identifier, CredentialCache.DefaultNetworkCredentials, AuthType.Negotiate);
    connection.SessionOptions.ProtocolVersion = 3;
    connection.Timeout = timeout ?? TimeSpan.FromSeconds(10);
    connection.Bind();
    return connection;
  }

  /// <summary>
  /// Gets the distinguished name of the default naming context for the connected domain by
  /// querying the RootDSE (Root Directory Service Agent Specific Entry) for the
  /// <code>defaultNamingContext</code> attribute.
  /// </summary>
  /// <param name="connection"></param>
  /// <returns></returns>
  public static string GetDefaultNamingContext(LdapConnection connection) {
    var req = new SearchRequest("", "(objectClass=*)", SearchScope.Base, "defaultNamingContext");
    var resp = (SearchResponse)connection.SendRequest(req);
    return (string)resp.Entries[0].Attributes["defaultNamingContext"][0];
  }

  /// <summary>
  /// Encodes a byte array SID into the escaped hexadecimal format required for LDAP filters.
  /// </summary>
  /// <param name="bytes"></param>
  /// <returns></returns>
  public static string LdapEncodeSid(byte[] bytes) {
    return string.Concat(bytes.Select(b => "\\" + b.ToString("x2")));
  }

  /// <summary>
  /// Encodes a SecurityIdentifier (SID) into the escaped hexadecimal format required for LDAP filters.
  /// </summary>
  /// <param name="sid"></param>
  /// <returns></returns>
  public static string LdapEncodeSid(SecurityIdentifier sid) {
    var bytes = new byte[sid.BinaryLength];
    sid.GetBinaryForm(bytes, 0);
    return LdapEncodeSid(bytes);
  }

  /// <summary>
  /// Escapes special characters in a string for use in an LDAP filter.
  /// </summary>
  /// <param name="value"></param>
  /// <returns></returns>
  public static string LdapEscapeFilter(string value) {
    return value
        .Replace("\\", "\\5c").Replace("*", "\\2a")
        .Replace("(", "\\28").Replace(")", "\\29")
        .Replace("\0", "\\00");
  }

  /// <summary>
  /// Determines the kind of principal (user, group, computer, other) represented by a SearchResultEntry.
  /// </summary>
  /// <param name="entry"></param>
  /// <returns></returns>
  internal static PrincipalKind ToPrincipalKind(this SearchResultEntry entry) {
    if (!entry.Attributes.Contains("objectClass")) {
      return PrincipalKind.Other;
    }

    var classes = entry.Attributes["objectClass"].GetValues(typeof(string)).Cast<string>().ToArray();
    if (classes.Contains("user") || classes.Contains("inetOrgPerson")) {
      return PrincipalKind.User;
    }
    if (classes.Contains("group")) {
      return PrincipalKind.Group;
    }
    if (classes.Contains("computer")) {
      return PrincipalKind.Computer;
    }
    return PrincipalKind.Other;
  }
}
