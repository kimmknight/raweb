using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;

namespace RAWeb.Server.Utilities;

public class UserInformation {
  public string Username { get; set; }
  public string Domain { get; set; }
  public string Sid { get; set; }
  public string? FullName { get; set; }
  public GroupInformation[] Groups { get; set; }
  public bool IsAnonymousUser {
    get {
      return Sid == "S-1-4-447-1";
    }
  }
  public bool IsRemoteDesktopUser {
    get {
      return Groups.Any(g => g.Sid == "S-1-5-32-555");
    }
  }
  public bool IsLocalAdministrator {
    get {
      return Groups.Any(g => g.Sid == "S-1-5-32-544");
    }
  }

  public UserInformation(string sid, string username, string domain, string? fullName, GroupInformation[] groups) {
    Sid = sid;
    Username = username;
    Domain = domain;

    if (string.IsNullOrEmpty(fullName)) {
      FullName = username; // default to username if full name is not provided
    }
    else {
      FullName = fullName;
    }

    Groups = groups;
  }

  public UserInformation(string sid, string username, string domain) {
    Sid = sid;
    Username = username;
    Domain = domain;
    FullName = username;
    Groups = [];
  }

  public class GroupRetrievalException : Exception {
    public GroupRetrievalException(string message) : base(message) { }
    public GroupRetrievalException(string message, Exception innerException) : base(message, innerException) { }
  }

  // see: https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/manage/understand-special-identities-groups#everyone
  public static readonly GroupInformation[] IncludedSpecialIdentityGroups = [
            new GroupInformation("Everyone", "S-1-1-0"), // all authenticated and guest users are part of Everyone
            new GroupInformation("Authenticated Users", "S-1-5-11"), // all authenticated users are implicitly a member of this group
        ];

  // see: https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/manage/understand-special-identities-groups
  // and https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/manage/understand-security-identifiers
  public static readonly GroupInformation[] ExcludedSpecialIdentityGroups = [
            new GroupInformation("Anonymous Logon", "S-1-5-7"),
            new GroupInformation("Attested Key Property", "S-1-18-6"),
            new GroupInformation("Authentication Authority Asserted Identity", "S-1-18-1"),
            new GroupInformation("Batch", "S-1-5-3"),
            new GroupInformation("Console Logon", "S-1-2-1"),
            new GroupInformation("Creator Group", "S-1-3-1"),
            new GroupInformation("Creator Owner", "S-1-3-0"),
            new GroupInformation("Dialup", "S-1-5-1"),
            new GroupInformation("Digest Authentication", "S-1-5-64-21"),
            new GroupInformation("Enterprise Domain Controllers", "S-1-5-9"),
            // new GroupInformation("Enterprise Read-only Domain Controllers", "S-1-5-21-<RootDomain>-498"),
            new GroupInformation("Fresh Public Key Identity", "S-1-18-3"),
            new GroupInformation("Interactive", "S-1-5-4"),
            new GroupInformation("IUSR", "S-1-5-17"),
            new GroupInformation("Key Trust", "S-1-18-4"),
            new GroupInformation("Local Service", "S-1-5-19"),
            new GroupInformation("LocalSystem", "S-1-5-18"),
            new GroupInformation("Local account", "S-1-5-113"),
            new GroupInformation("Local account and member of Administrators group", "S-1-5-114"),
            new GroupInformation("MFA Key Property", "S-1-18-5"),
            new GroupInformation("Network", "S-1-5-2"),
            new GroupInformation("Network Service", "S-1-5-20"),
            new GroupInformation("NTLM Authentication", "S-1-5-64-10"),
            new GroupInformation("Other Organization", "S-1-5-1000"),
            new GroupInformation("Owner Rights", "S-1-3-4"),
            new GroupInformation("Principal Self", "S-1-5-10"),
            new GroupInformation("Proxy", "S-1-5-8"),
            // new GroupInformation("Read-only Domain Controllers", "S-1-5-21-<domain>-521"),
            new GroupInformation("Remote Interactive Logon", "S-1-5-14"),
            new GroupInformation("Restricted", "S-1-5-12"),
            new GroupInformation("SChannel Authentication", "S-1-5-64-14"),
            new GroupInformation("Service", "S-1-5-6"),
            new GroupInformation("Service Asserted Identity", "S-1-18-2"),
            new GroupInformation("Terminal Server User", "S-1-5-13"),
            new GroupInformation("This Organization", "S-1-5-15"),
            new GroupInformation("Window Manager\\Window Manager Group", "S-1-5-90")
        ];

  /// <summary>
  /// A predefined UserInformation object representing an anonymous user.
  /// </summary>
  public static readonly UserInformation AnonymousUser = new("S-1-4-447-1", "anonymous", "RAWEB", "Anonymous User", IncludedSpecialIdentityGroups);

  public override string ToString() {
    var str = new StringBuilder();

    str.Append("Username: ").Append(Username).Append("\n");

    str.Append("Domain: ").Append(Domain).Append("\n");

    str.Append("Groups: ");
    if (Groups != null && Groups.Length > 0) {
      foreach (var group in Groups) {
        str.Append("\n").Append("  - ").Append(group.Name).Append(" (").Append(group.Sid).Append(")");
      }
    }
    else {
      str.Append("None");
    }

    return str.ToString();
  }

  /// <summary>
  /// Determines if the specified username and domain correspond to an anonymous account.
  /// </summary>
  /// <param name="username"></param>
  /// <param name="domain"></param>
  /// <returns></returns>
  private static bool IsAnonymousAccount(string username, string domain) {
    return (domain == "NT AUTHORITY" && username == "IUSR") || (domain == "IIS APPPOOL" && username == "raweb") || (domain == "RAWEB" && username == "anonymous");
  }

  /// <summary>
  /// Creates a UserInformation object from a username and domain.
  /// <br /><br />
  /// For local machine accounts, we look up the user's SID, full name,
  /// and local group memberships via <see cref="NetUserInformation"/>.
  /// <br /><br />
  /// For domain accounts, we connect to the domain controller via LDAP using the current
  /// process's credentials (usually the IIS application pool) and retrieve the user's SID,
  /// full name, and transitive group memberships via the <c>tokenGroups</c> constructed
  /// attribute, supplemented with local machine groups.
  /// </summary>
  /// <param name="username">The SAM account name of the user.</param>
  /// <param name="domain">The NetBIOS domain name, or the machine name for local accounts.</param>
  /// <returns>A <see cref="UserInformation"/> object, or null if the user is not found.</returns>
  /// <exception cref="Exception">Thrown if the LDAP query fails for a domain account.</exception>
  /// <exception cref="GroupRetrievalException">Thrown if group retrieval fails.</exception>
  public static UserInformation? FromPrincipal(string username, string domain) {
    // if the account is the anonymous account, return those details
    if (IsAnonymousAccount(username, domain)) {
      return AnonymousUser;
    }

    // treat an empty domain or the machine name as a local account
    var domainIsMachine = string.IsNullOrEmpty(domain) ||
        domain.Trim().Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
    domain = domainIsMachine ? Environment.MachineName : domain;

    string userSid;
    string? fullName;
    GroupInformation[] groups;

    if (domainIsMachine) {
      // look up the local account
      var sid = NetUserInformation.GetSidFromAccountName(username);
      if (sid is null) {
        return null;
      }
      userSid = sid.ToString();

      // attempt to get the display name, falling back to the username if it is unavailable
      try {
        fullName = NetUserInformation.GetFullName(null, username);
      }
      catch {
        fullName = username;
      }

      // enumerate local machine groups and apply Windows special identity group rules
      groups = ApplySpecialGroupRules(NetUserInformation.GetLocalGroupMemberships(sid));
    }
    else {
      // look up the domain account via LDAP
      (userSid, fullName, groups) = GetDomainUserInfo(username, domain);
    }

    var userInfo = new UserInformation(userSid, username, domain, fullName, groups);

    // update the cache with the user information
    if (PoliciesManager.RawPolicies["UserCache.Enabled"] == "true") {
      var dbHelper = new UserCacheDatabaseHelper();
      dbHelper.StoreUser(userInfo);
    }

    return userInfo;
  }

  /// <summary>
  /// Retrieves the SID, display name, and group memberships for a domain account.
  /// <br /><br />
  /// This method connects to the domain controller via LDAP using the current process
  /// credentials (usualy the IIS application pool identity) and searches for the user
  /// by <c>sAMAccountName</c>. Domain groups are resolved using the <c>tokenGroups</c>
  /// constructed attribute for transitive membership. Local machine groups are then resolved via
  /// <see cref="NetUserInformation.GetLocalGroupMemberships"/>, checking both the user SID
  /// and all domain group SIDs to capture nested membership.
  /// </summary>
  /// <param name="username">The SAM account name of the user.</param>
  /// <param name="domain">The domain to connect to.</param>
  /// <returns>A tuple of (SID string, full name, resolved group array).</returns>
  /// <exception cref="Exception">Thrown if the user is not found in the domain.</exception>
  /// <exception cref="GroupRetrievalException">Thrown if group retrieval fails.</exception>
  private static (string sid, string? fullName, GroupInformation[] groups) GetDomainUserInfo(string username, string domain) {
    // open a connection to a domain controller and get the distinguished
    // name of the connection's root domain so we can use it for searching
    using var connection = Management.LdapHelpers.OpenLdapConnection(domain);
    var domainDN = Management.LdapHelpers.GetDefaultNamingContext(connection);

    // search for the user by sAMAccountName (the short DOMAIN\username form without the domain prefix)
    var searchReq = new SearchRequest(
        domainDN,
        $"(&(objectClass=user)(sAMAccountName={Management.LdapHelpers.LdapEscapeFilter(username)}))",
        SearchScope.Subtree,
        // the attributes we need to build the UserInformation object
        "objectSid", "displayName", "distinguishedName"
    );
    var searchResp = (SearchResponse)connection.SendRequest(searchReq);
    if (searchResp.Entries.Count == 0) {
      throw new Exception($"User '{username}' not found in domain '{domain}'.");
    }

    // extract the SID, distinguished name, and display name from the matched entry
    var entry = searchResp.Entries[0];
    var sidBytes = (byte[])entry.Attributes["objectSid"].GetValues(typeof(byte[]))[0];
    var userSid = new SecurityIdentifier(sidBytes, 0).ToString();
    var userDN = (string)entry.Attributes["distinguishedName"][0];
    var fullName = entry.Attributes["displayName"]?.Count > 0
        ? (string?)entry.Attributes["displayName"][0]
        : null;

    // get all domain groups (transitive) via the tokenGroups constructed attribute
    var domainGroups = GetTokenGroups(connection, userDN);

    // get local machine groups, passing domain group SIDs so nested membership
    // (e.g. "Domain Users" added to the local "Users" group) is also detected
    var userSidObj = new SecurityIdentifier(userSid);
    var domainGroupSids = domainGroups.Select(g => new SecurityIdentifier(g.Sid)).ToArray();
    var localGroups = NetUserInformation.GetLocalGroupMemberships(userSidObj, domainGroupSids);

    // merge domain and local groups, remove duplicates, then apply special identity group rules
    var all = domainGroups.Concat(localGroups).GroupBy(g => g.Sid).Select(g => g.First()).ToArray();
    return (userSid, fullName, ApplySpecialGroupRules(all));
  }

  /// <summary>
  /// Retrieves the transitive group memberships for a domain user via the
  /// <c>tokenGroups</c> constructed attribute. Supports multi-domain forests.
  /// <br /><br />
  /// <c>tokenGroups</c> is computed by Active Directory and contains all direct and
  /// indirect group SIDs to which the user belongs. SIDs are
  /// grouped by <c>AccountDomainSid</c> so that each domain requires only one LDAP
  /// connection and one batch search. For groups in the user's own domain, the existing
  /// connection is reused; for groups in other forest domains, a separate connection is
  /// opened using NTAccount translation to discover the domain name. SIDs present in
  /// <see cref="ExcludedSpecialIdentityGroups"/> are skipped. Groups that cannot be
  /// resolved fall back to using the SID string as the display name.
  /// </summary>
  /// <param name="connection">An open, bound LDAP connection to the user's domain.</param>
  /// <param name="userDN">The distinguished name of the user object.</param>
  /// <param name="domainDN">
  /// The distinguished name of the user's domain, used as the base
  /// DN for group searches. If null, it will be derived from the connection.
  /// </param>  
  /// <returns>An array of <see cref="GroupInformation"/> objects.</returns>
  private static GroupInformation[] GetTokenGroups(LdapConnection connection, string userDN, string? domainDN = null) {
    // derive the home domain's base DN and SID from the connection so we can later
    // distinguish home-domain groups (reuse this connection) from foreign-domain groups
    // (open a separate connection per domain)
    domainDN ??= Management.LdapHelpers.GetDefaultNamingContext(connection);
    var domainRootReq = new SearchRequest(domainDN, "(objectClass=*)", SearchScope.Base, "objectSid");
    var domainRootResp = (SearchResponse)connection.SendRequest(domainRootReq);
    var domainSidBytes = (byte[])domainRootResp.Entries[0].Attributes["objectSid"].GetValues(typeof(byte[]))[0];
    var userDomainSid = new SecurityIdentifier(domainSidBytes, 0);

    // tokenGroups is a constructed attribute — it must be requested with scope Base
    // directly on the user object; it cannot be retrieved via a subtree search
    var req = new SearchRequest(userDN, "(objectClass=*)", SearchScope.Base, "tokenGroups");
    var resp = (SearchResponse)connection.SendRequest(req);

    if (resp.Entries.Count == 0 || !resp.Entries[0].Attributes.Contains("tokenGroups"))
      return [];

    // collect all SIDs as (raw bytes, parsed SID) pairs, filtering out excluded special identity groups
    var allSids = resp.Entries[0].Attributes["tokenGroups"]
        .GetValues(typeof(byte[]))
        .Cast<byte[]>()
        .Select(b => (bytes: b, sid: new SecurityIdentifier(b, 0)))
        .Where(x => !ExcludedSpecialIdentityGroups.Any(g => g.Sid == x.sid.Value))
        .ToList();

    // group SIDs by their domain SID so we can batch-query each domain with one connection
    var sidsByDomain = allSids.GroupBy(x => x.sid.AccountDomainSid?.Value).ToList();

    var groups = new List<GroupInformation>();

    foreach (var domainGroup in sidsByDomain) {
      var domainSidValue = domainGroup.Key;
      var groupSids = domainGroup.ToList();

      // if the domain SID is null, that means we somehow encountered
      // built-in groups that we do not have included in ExcludedSpecialIdentityGroups
      if (domainSidValue is null) {
        foreach (var (_, groupSid) in groupSids) {
          groups.Add(new GroupInformation(groupSid.Value, groupSid.Value));
        }
        continue;
      }

      // determine which LDAP connection and base DN to use for this domain's batch search:
      // reuse the existing connection for groups in the user's own domain, or open a new
      // connection for groups belonging to other domains in the forest
      LdapConnection? targetConnection = null;
      string? targetDN = null;
      var ownsConnection = false;

      if (domainSidValue == userDomainSid.Value) {
        // for the home domain, reuse the already-open connection
        targetConnection = connection;
        targetDN = domainDN;
      }
      else {
        // for a foreign domain in the forest, discover its name via NTAccount
        // translation and open a new connection
        // (windows should be able to resolve the name across the forest trust)
        string? foreignDomainName = null;
        try {
          var ntAccount = (NTAccount)groupSids[0].sid.Translate(typeof(NTAccount));
          var parts = ntAccount.Value.Split('\\');
          if (parts.Length > 1) {
            foreignDomainName = parts[0];
          }
        }
        catch { }

        // if we cannot identify the foreign domain name, we will have to
        // skip name resolution and just use the raw SIDs for this domain's groups
        if (foreignDomainName is null) {
          foreach (var (_, groupSid) in groupSids) {
            groups.Add(new GroupInformation(groupSid.Value, groupSid.Value));
          }
          continue;
        }

        // attempt to open a connection to the foreign domain so that
        // we can resolve group names
        try {
          targetConnection = Management.LdapHelpers.OpenLdapConnection(foreignDomainName);
          targetDN = Management.LdapHelpers.GetDefaultNamingContext(targetConnection);
          ownsConnection = true;
        }
        catch {
          // if we fail to connect to the foreign domain, we will have to skip name
          // resolution and just use the raw SIDs for this domain's groups
          foreach (var (_, groupSid) in groupSids) {
            groups.Add(new GroupInformation(groupSid.Value, groupSid.Value));
          }
          continue;
        }
      }

      try {
        // batch all SIDs for this domain into a single search using an OR filter,
        var sidFilters = string.Concat(groupSids.Select(x => "(objectSid=" + Management.LdapHelpers.LdapEncodeSid(x.bytes) + ")"));
        var filter = groupSids.Count == 1 ? sidFilters : $"(|{sidFilters})";

        var groupReq = new SearchRequest(
            targetDN,
            filter,
            SearchScope.Subtree,
            "objectSid", "sAMAccountName", "distinguishedName"
        );
        var groupResp = (SearchResponse)targetConnection.SendRequest(groupReq);

        // index the result entries by SID string for fast lookup by SID
        var resolvedBySid = new Dictionary<string, SearchResultEntry>();
        foreach (SearchResultEntry entry in groupResp.Entries) {
          if (entry.Attributes["objectSid"]?.Count > 0) {
            var entrySidBytes = (byte[])entry.Attributes["objectSid"].GetValues(typeof(byte[]))[0];
            var entrySid = new SecurityIdentifier(entrySidBytes, 0).Value;
            resolvedBySid[entrySid] = entry;
          }
        }

        // map each requested SID to its resolved group info, falling back to raw SID if unresolved
        foreach (var (_, groupSid) in groupSids) {
          if (resolvedBySid.TryGetValue(groupSid.Value, out var entry)) {
            var name = entry.Attributes["sAMAccountName"]?.Count > 0 ? (string?)entry.Attributes["sAMAccountName"][0] : groupSid.Value;
            var dn = entry.Attributes["distinguishedName"]?.Count > 0 ? (string?)entry.Attributes["distinguishedName"][0] : null;
            groups.Add(new GroupInformation(name, groupSid.Value, dn));
          }
          else {
            // SID exists in tokenGroups but has no matching entry from the domain
            groups.Add(new GroupInformation(groupSid.Value, groupSid.Value));
          }
        }
      }
      catch {
        // if the batch search fails, fall back to raw SIDs for this domain's groups
        foreach (var (_, groupSid) in groupSids) {
          groups.Add(new GroupInformation(groupSid.Value, groupSid.Value));
        }
      }
      finally {
        if (ownsConnection) {
          targetConnection?.Dispose();
        }
      }
    }

    return [.. groups];
  }

  /// <summary>
  /// Ensures that <see cref="IncludedSpecialIdentityGroups"/> are present in the group
  /// list and that <see cref="ExcludedSpecialIdentityGroups"/> are removed from it.
  /// </summary>
  /// <param name="groups">The raw group array to normalize.</param>
  /// <returns>A new array with the special identity group rules applied.</returns>
  private static GroupInformation[] ApplySpecialGroupRules(GroupInformation[] groups) {
    var list = groups.ToList();

    // add any included special identity groups that are not already in the list
    // (e.g. "Everyone" and "Authenticated Users" which Windows adds implicitly)
    foreach (var g in IncludedSpecialIdentityGroups) {
      if (!list.Any(x => x.Sid == g.Sid)) list.Add(g);
    }

    // remove any excluded special identity groups
    // (e.g. logon session groups like "Interactive" and "Console Logon")
    foreach (var g in ExcludedSpecialIdentityGroups) {
      list.RemoveAll(x => x.Sid == g.Sid);
    }

    return [.. list];
  }

  /// <summary>
  /// Creates a UserInformation object from a match in the user cache.
  /// <br /><br />
  /// </summary>
  /// <param name="username"></param>
  /// <param name="domain"></param>
  /// <param name="maxAgeSeconds"></param>
  /// <returns></returns>
  private static UserInformation? FromUserCache(string username, string domain, int? maxAgeSeconds = null) {
    var userCacheIsEnabled = PoliciesManager.RawPolicies["UserCache.Enabled"] == "true";
    if (!userCacheIsEnabled) {
      return null;
    }

    var dbHelper = new UserCacheDatabaseHelper();
    var cachedUserInfo = dbHelper.GetUser(null, username, domain, maxAgeSeconds);

    if (cachedUserInfo != null) {
      return cachedUserInfo;
    }

    return null;
  }

  /// <summary>
  /// Creates a UserInformation object directly from a <see cref="WindowsIdentity"/> without
  /// any network or LDAP calls.
  /// <br /><br />
  /// Group membership is read from the identity's access token, which Windows populates
  /// at logon time. Full name is resolved via NetAPI only for local accounts; domain accounts
  /// fall back to the SAM account name to avoid a DC round-trip.
  /// </summary>
  /// <remarks>
  /// This method is intended for use only in the desktop app. Servers should continue to
  /// use <see cref="FromDownLevelLogonName"/> or <see cref="FromPrincipal"/>.
  /// </remarks>
  /// <returns>A <see cref="UserInformation"/> object, or null if the identity has no user SID.</returns>
  public static UserInformation? FromWindowsIdentity(WindowsIdentity identity) {
    if (identity?.User is null) {
      return null;
    }

    var parts = identity.Name.Split('\\');
    var username = parts.Length > 1 ? parts[1] : parts[0];
    var domain = parts.Length > 1 ? parts[0] : Environment.MachineName;

    if (IsAnonymousAccount(username, domain)) {
      return AnonymousUser;
    }

    var userSid = identity.User.Value;

    var domainIsMachine = domain.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
    string? fullName = null;
    if (domainIsMachine) {
      try {
        fullName = NetUserInformation.GetFullName(null, username);
      }
      catch {
        fullName = username;
      }
    }

    // identity token already contains all group SIDs (domain and local)
    var groupInformation = (identity.Groups ?? Enumerable.Empty<IdentityReference>())
      .Cast<SecurityIdentifier>()
      .Where(s => !ExcludedSpecialIdentityGroups.Any(g => g.Sid == s.Value))
      .Select(s => new GroupInformation(s.Value, s.Value))
      .ToList();

    // check the local machine for whether the user is a local administrator
    // and add the local Administrators group if needed
    if (!groupInformation.Any(g => g.Sid == "S-1-5-32-544")) {
      if (NetUserInformation.IsUserLocalAdministrator(userSid)) {
        groupInformation.Add(new GroupInformation("S-1-5-32-544"));
      }
    }

    return new UserInformation(userSid, username, domain, fullName ?? username, ApplySpecialGroupRules([.. groupInformation]));
  }

  /// <summary>
  /// Creates a UserInformation object from a down-level logon name (DOMAIN\username).
  /// <br /><br />
  /// This method parses the down-level logon name to extract the domain and username,
  /// then attempts to get the user information from the user cache (if enabled and not stale),
  /// and finally falls back to querying the principal contexts if the cache is
  /// disabled, unavailable, or stale.
  /// </summary>
  /// <param name="downLevelLogonName"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentNullException"></exception>
  /// <exception cref="ArgumentException"></exception>
  public static UserInformation? FromDownLevelLogonName(string downLevelLogonName) {
    // if there is no backslash, we are unable to parse the domain and username
    if (downLevelLogonName == null || string.IsNullOrEmpty(downLevelLogonName)) {
      throw new ArgumentException("Down-level logon name cannot be null.");
    }
    if (!downLevelLogonName.Contains("\\")) {
      throw new ArgumentException("Down-level logon name must be in the format DOMAIN\\username.");
    }

    // get the username and domain from the string in the format DOMAIN\username
    var parts = downLevelLogonName.Split('\\');
    var username = parts.Length > 1 ? parts[1] : parts[0]; // the part after the backslash is the username
    var domain = parts.Length > 1 ? parts[0] : Environment.MachineName; // the part before the backslash is the domain, or use machine name if no domain

    // throw an exception if username or domain is null or empty
    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(domain)) {
      throw new ArgumentException("Username or domain cannot be null or empty.");
    }

    // if the account is any anonymous account, return the default anonymous user details
    if (IsAnonymousAccount(username, domain)) {
      return AnonymousUser;
    }

    // if the user cache is enabled, attempt to get the user from the cache first,
    // but only if the user information is not stale
    var cachedUserInfo = FromUserCache(username, domain);
    if (cachedUserInfo != null) {
      return cachedUserInfo;
    }

    // otherwise, attempt to get the latest user information using LDAP/NetAPI,
    // but fall back to the cache with no staleness restrictions if an error occurs
    // TODO: if we ever enable the user cache by default, we should not bypass the stale check and instead suggest that those who need something similar set their UserCache.StaleWhileRevalidate value to a massive number
    try {
      var userInfo = FromPrincipal(username, domain);
      return userInfo;
    }
    catch (Exception) {
      // fall back to the cache if an error occurs and the user cache is enabled
      // (e.g., the domain controller cannot currently be reached)
      cachedUserInfo = FromUserCache(username, domain, 315576000); // 10 years max age to effectively disable staleness
      return cachedUserInfo;
    }
  }

  public const string UserInformationContextKey = "UserInformation";

  /// <summary>
  /// Creates a UserInformation object from an HttpRequest.
  /// <br /><br />
  /// If a UserInformation object for the user has already been
  /// created for the current request, it will be returned from
  /// the request context cache. Conversely, if it has not yet been
  /// created, it will be created and then stored in the request
  /// context cache for future use during the same request.
  /// <br /><br />
  /// This method extracts and validates the AuthTicket from the
  /// HttpRequest cookies, then uses the down-level logon name
  /// from the AuthTicket to create the UserInformation object.
  /// See <see cref="FromDownLevelLogonName"/> for more details.
  /// </summary>
  /// <param name="request"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentNullException"></exception>
  /// <exception cref="ArgumentException"></exception>
  public static UserInformation? FromHttpRequest(Microsoft.AspNetCore.Http.HttpRequest request) {
    if (request == null) {
      throw new ArgumentNullException(nameof(request), "HttpRequest cannot be null.");
    }

    var authTicket = AuthTicket.FromHttpRequestCookie(request);
    if (authTicket == null) {
      return null;
    }

    // If the user information is already in the request context, return it.
    // This allows us to avouid repeated lookups during the same request.
    if (request.HttpContext.Items[UserInformationContextKey] is UserInformation) {
      return request.HttpContext.Items[UserInformationContextKey] as UserInformation;
    }

    var userInfo = FromDownLevelLogonName(authTicket.Name);
    if (userInfo != null) {
      request.HttpContext.Items[UserInformationContextKey] = userInfo; // store in request context
    }
    return userInfo;
  }

  /// <summary>
  /// Creates a UserInformation object from an HttpRequest
  /// or null if an error occurs.
  /// <br /><br />
  /// See <see cref="FromHttpRequest"/> for more details.
  /// </summary>
  /// <param name="request"></param>
  /// <returns></returns>
  public static UserInformation? FromHttpRequestSafe(Microsoft.AspNetCore.Http.HttpRequest request) {
    try {
      return FromHttpRequest(request);
    }
    catch (Exception) {
      return null; // return null if an error occurs
    }
  }
}
