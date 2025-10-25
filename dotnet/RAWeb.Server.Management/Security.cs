using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices.AccountManagement;
using System.Linq;
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
  /// Resolves a Principal to its corresponding
  /// resolved security identifier (SID).
  /// <br /><br />
  /// If you need to resolve multiple Principals, use
  /// <see cref="ResolvedSecurityIdentifiers.FromPrincipals(Principal[])"/>
  /// instead.
  /// </summary>
  /// <param name="principal"></param>
  /// <returns></returns>
  public static ResolvedSecurityIdentifier FromPrincipal(Principal principal) {
    return new ResolvedSecurityIdentifier(
        sid: principal.Sid.Value,
        domain: principal.Context.Name,
        userName: principal.SamAccountName,
        displayName: principal.DisplayName,
        userPrincipalName: principal.UserPrincipalName,
        principalKind: principal.ToPrincipalKind()
    );
  }

  /// <summary>
  /// Resolves a SecurityIdentifier to its corresponding
  /// resolved security identifier (SID).
  /// <br /><br />
  /// See <see cref="FromSidString(string)"/> for details.
  /// <br /><br />
  /// If you need to resolve multiple SIDs, use
  /// <see cref="ResolvedSecurityIdentifiers.FromSids(SecurityDescriptor[], out List{SecurityDescriptor})"/>
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
    var isMachineContext = domain is null || string.IsNullOrWhiteSpace(domain) || domain.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);

    PrincipalContext principalContext;
    if (isMachineContext) {
      principalContext = new PrincipalContext(ContextType.Machine);
    }
    else {
      principalContext = new PrincipalContext(ContextType.Domain, domain);
    }

    try {
      // attempt to find by SID first
      var sid = new SecurityIdentifier(lookup);
      var resolved = FromSecurityIdentifier(sid);
      if (resolved == null) {
        return null;
      }
      return resolved;
    }
    catch {
    }

    // attempt to find by other identity types
    Principal? principal;
    if (isMachineContext) {
      principal = Principal.FindByIdentity(principalContext, IdentityType.SamAccountName, lookup)
        ?? Principal.FindByIdentity(principalContext, IdentityType.Name, lookup);
    }
    else {
      principal = Principal.FindByIdentity(principalContext, IdentityType.SamAccountName, lookup)
        ?? Principal.FindByIdentity(principalContext, IdentityType.UserPrincipalName, lookup)
        ?? Principal.FindByIdentity(principalContext, IdentityType.Name, lookup);
    }
    if (principal == null) {
      return null;
    }

    // resolve the SID from the found principal
    var resolvedPrincipal = FromPrincipal(principal);
    if (resolvedPrincipal == null) {
      return null;
    }

    principalContext.Dispose();
    return resolvedPrincipal;
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
        var domainSid = securityIdentifier.AccountDomainSid;
        var domainName = securityIdentifier.ToDomainName();
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

    // resolve the SIDs for each domain
    foreach (var domainEntry in sidsByDomain) {

      PrincipalContext context;
      if (Environment.MachineName.Equals(domainEntry.Key, StringComparison.OrdinalIgnoreCase)) {
        context = new PrincipalContext(ContextType.Machine, domainEntry.Key);
      }
      else {
        try {
          context = new PrincipalContext(ContextType.Domain, domainEntry.Key);
        }
        catch {
          // unable to create PrincipalContext for domain; skip all SIDs for this domain
          foreach (var securityIdentifier in domainEntry.Value) {
            invalidOrUnfoundSids.Add(securityIdentifier.Value);
          }
          continue;
        }
      }

      // resolve SIDs using the PrincipalContext
      foreach (var securityIdentifier in domainEntry.Value) {
        try {
          var principal = Principal.FindByIdentity(context, IdentityType.Sid, securityIdentifier.Value);
          if (principal is null) {
            invalidOrUnfoundSids.Add(securityIdentifier.Value);
            continue;
          }

          var resolvedSid = ResolvedSecurityIdentifier.FromPrincipal(principal);
          if (resolvedSid is null) {
            invalidOrUnfoundSids.Add(securityIdentifier.Value);
            continue;
          }

          resolvedSids.Add(resolvedSid);
        }
        catch {
          invalidOrUnfoundSids.Add(securityIdentifier.Value);
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

  /// <summary>
  /// Resolves a list of Principals to their corresponding
  /// resolved security identifiers (SIDs).
  /// </summary>
  /// <param name="principals"></param>
  /// <returns></returns>
  public static ResolvedSecurityIdentifiers FromPrincipals(Principal[] principals) {
    var resolvedSids = new ResolvedSecurityIdentifiers();

    foreach (var principal in principals) {
      var resolvedSid = ResolvedSecurityIdentifier.FromPrincipal(principal);
      resolvedSids.Add(resolvedSid);
    }

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

public static class PrincipalExtensions {
  /// <summary>
  /// Gets the PrincipalKind for a given Principal.
  /// </summary>
  /// <param name="principal"></param>
  /// <returns></returns>
  public static PrincipalKind ToPrincipalKind(this Principal principal) {
    if (principal is UserPrincipal)
      return PrincipalKind.User;

    if (principal is GroupPrincipal)
      return PrincipalKind.Group;

    if (principal is ComputerPrincipal)
      return PrincipalKind.Computer;

    return PrincipalKind.Other;
  }
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

    if (deniedSids is null && allowedSids is null) {
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
  /// Gets the domain name for a given SecurityIdentifier (SID).
  /// </summary>
  /// <param name="sid"></param>
  /// <returns></returns>
  public static string? ToDomainName(this SecurityIdentifier sid) {
    if (sid.IsWellKnown()) {
      return Environment.MachineName;
    }

    // extract the domain portion of the SID
    var accountDomainSid = sid.AccountDomainSid;
    if (accountDomainSid is null) {
      return Environment.MachineName;
    }

    // check if it matches the local machine's SID
    var localDomainSid = WindowsIdentity.GetCurrent().User?.AccountDomainSid;
    if (localDomainSid is not null &&
        accountDomainSid.Equals(localDomainSid)) {
      return Environment.MachineName;
    }

    // try to translate the base SID to get its domain name
    try {
      var translated = accountDomainSid.Translate(typeof(NTAccount)).Value;
      return translated.Split('\\')[0];
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
