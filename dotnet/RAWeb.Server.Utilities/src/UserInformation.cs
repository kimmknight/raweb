using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
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

  /// <summary>
  /// Gets the local group memberships for a user.
  /// </summary>
  /// <param name="de">The directory entry for the user.</param>
  /// <param name="userSid">The sid of the user in string form.</param>
  /// <param name="userGroupsSids">The optional array of string sids representing groups that the user belongs to. Use this when searching local groups after finding domain groups.</param>
  /// <returns>A list of group information</returns>
  /// <exception cref="ArgumentNullException"></exception>
  private static List<GroupInformation> GetLocalGroupMemberships(DirectoryEntry de, string userSid, string[]? userGroupsSids = null) {
    if (de == null) {
      throw new ArgumentNullException(nameof(de), "DirectoryEntry cannot be null.");
    }
    if (string.IsNullOrEmpty(userSid)) {
      throw new ArgumentNullException(nameof(userSid), "User SID cannot be null or empty.");
    }
    if (userGroupsSids == null) {
      userGroupsSids = [];
    }

    // seach the local machine for groups that contain the user's SID
    var localGroups = new List<GroupInformation>();
    var localMachinePath = "WinNT://" + Environment.MachineName + ",computer";
    try {
      using (var machineEntry = new DirectoryEntry(localMachinePath)) {
        foreach (DirectoryEntry machineChildEntry in machineEntry.Children) {
          // skip entries that are not groups
          if (machineChildEntry.SchemaClassName != "Group") {
            continue;
          }

          // skip if there are no members of the group
          var members = machineChildEntry.Invoke("Members") as System.Collections.IEnumerable;
          if (members == null || !members.Cast<object>().Any()) {
            continue;
          }

          // get the sid of the group
          if (machineChildEntry.Properties["objectSid"].Value is not byte[] groupSidBytes) continue;
          var groupSid = new SecurityIdentifier(groupSidBytes, 0).ToString();

          // check the SIDs of each member in the group (this gets user and group SIDs)
          foreach (var member in members) {
            using (var memberEntry = new DirectoryEntry(member)) {

              if (memberEntry.Properties["objectSid"].Value is not byte[] sidBytes) continue;
              var groupMemberSid = new SecurityIdentifier(sidBytes, 0).ToString();

              // add the group to the list if:
              // - the group member SID matches the user's SID (the user is a member of the group)
              // - the group member SID is in the user's groups SIDs (the user is a member of a group that is a member of this group)
              if (groupMemberSid == userSid || userGroupsSids.Contains(groupMemberSid)) {
                localGroups.Add(new GroupInformation(machineChildEntry.Name, groupSid));
              }
            }
          }
        }
      }
    }
    catch (Exception ex) {
      throw new GroupRetrievalException("Failed to retrieve local group memberships for user with SID: " + userSid, ex);
    }

    // ensure that the local groups include the special identity groups that Windows typically adds
    foreach (var specialGroup in IncludedSpecialIdentityGroups) {
      if (!localGroups.Any(g => g.Sid == specialGroup.Sid)) {
        localGroups.Add(specialGroup);
      }
    }

    // ensure that excluded special identity groups are not included in the local groups
    foreach (var excludedGroup in ExcludedSpecialIdentityGroups) {
      localGroups.RemoveAll(g => g.Sid == excludedGroup.Sid);
    }

    return localGroups;
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

  /// <summary>
  /// Searches a directory entry that represents a domain for groups that match the specified filter.
  /// </summary>
  /// <param name="searchRoot">A DirectoryEntry. It must be for a domain.</param>
  /// <param name="filter">A filter that can be used with a DirectorySearcher.</param>
  /// <returns>A list of found groups.</returns>
  /// <exception cref="ArgumentNullException"></exception>
  /// <exception cref="Exception"></exception>
  /// <exception cref="GroupRetrievalException"></exception>
  private static List<GroupInformation> FindDomainGroups(DirectoryEntry searchRoot, string filter) {
    if (searchRoot == null) {
      throw new ArgumentNullException(nameof(searchRoot), "DirectoryEntry cannot be null.");
    }
    if (string.IsNullOrEmpty(filter)) {
      throw new ArgumentNullException(nameof(filter), "Filter cannot be null or empty.");
    }

    var propertiesToLoad = new[] { "msDS-PrincipalName", "objectSid", "distinguishedName" };

    try {
      var foundGroups = new List<GroupInformation>();
      var directorySearcher = new DirectorySearcher(searchRoot, filter, propertiesToLoad);

      using (var results = directorySearcher.FindAll()) {
        foreach (SearchResult result in results) {
          // get the group name and SID from the properties
          var groupName = result.Properties["msDS-PrincipalName"][0].ToString();
          var groupDistinguishedName = result.Properties["distinguishedName"][0].ToString();
          var groupSidBytes = (byte[])result.Properties["objectSid"][0];
          var groupSid = new SecurityIdentifier(groupSidBytes, 0).ToString();

          // add the group to the found groups
          foundGroups.Add(new GroupInformation(groupName, groupSid, groupDistinguishedName));
        }
      }

      return foundGroups;
    }
    catch (Exception ex) {
      var domain = searchRoot?.Properties?["distinguishedName"]?.Value as string ?? "unknown";
      throw new GroupRetrievalException($"Failed to find groups in domain '{domain}' with filter '{filter}'", ex);
    }
  }

  /// <summary>
  /// Gets all groups for a user across all domains in the forest.
  /// <br />
  /// This method find all domains in the forest of the user's domain and then
  /// searches for groups where the user is a direct member or an indirect member via group membership.
  /// <br />
  /// This method also searches for group membership in externally trusted domains found in
  /// the Foreign Security Principals container.
  /// <br />
  /// Each domain in the forest (or each trusted foreign domain) is wrapped in a try-catch block
  /// to ensure that if one domain fails to be queried, it does not affect the others.
  /// <br />
  /// Queries will fail if the application pool is not running with credentials that are accepted
  /// by the domain.
  /// </summary>
  /// <param name="de"></param>
  /// <param name="userSid"></param>
  /// <returns>A list of found groups.</returns>
  /// <exception cref="ArgumentNullException"></exception>
  /// <exception cref="Exception"></exception>
  private static List<GroupInformation> GetUserGroupsForAllDomains(DirectoryEntry de, string userSid) {
    if (de == null) {
      throw new ArgumentNullException(nameof(de), "DirectoryEntry cannot be null.");
    }
    if (string.IsNullOrEmpty(userSid)) {
      throw new ArgumentNullException(nameof(userSid), "User SID cannot be null or empty.");
    }

    // track the found groups (may include netbios names in the group names)
    var foundGroups = new List<GroupInformation>();
    var searchedDomains = new HashSet<string>();

    // ensure the properties we want are loaded
    de.RefreshCache(["canonicalName", "objectSid", "distinguishedName", "primaryGroupID"]);
    var userCanonicalName = de.Properties["canonicalName"].Value as string;
    var userDistinguishedName = de.Properties["distinguishedName"].Value as string;
    var primaryGroupId = de.Properties["primaryGroupID"].Value as int?;

    // extract the user's domain from the canonicalName property and use it to get the domain and forest
    if (userCanonicalName is null || string.IsNullOrWhiteSpace(userCanonicalName)) {
      throw new Exception("Canonical name is not available for the user's directory entry.");
    }
    var domainName = userCanonicalName.Split('/')[0];
    var domainDirectoryContext = new DirectoryContext(DirectoryContextType.Domain, domainName);
    var userDomain = System.DirectoryServices.ActiveDirectory.Domain.GetDomain(domainDirectoryContext);
    var forest = userDomain.Forest;

    // this may fail if the raweb application pool is not running with credentials
    // that can query the domains in the forest
    try {
      using (var searchRoot = new DirectoryEntry("LDAP://" + userDomain.Name)) {
        // construct the user's primary group SID
        searchRoot.RefreshCache(["objectSid"]);
        if (searchRoot.Properties["objectSid"].Value is not byte[] objectSidBytes) {
          throw new Exception("Could not retrieve objectSid from the domain directory entry.");
        }
        var domainSid = new SecurityIdentifier(objectSidBytes, 0).ToString();
        var userPrimaryGroupSid = domainSid + "-" + primaryGroupId;

        // search for the primary group using the primary group SID
        // and add it to the found groups
        var filter = "(&(objectClass=group)(objectSid=" + userPrimaryGroupSid + "))";
        var found = FindDomainGroups(searchRoot, filter);
        foundGroups.AddRange(found);

        // search domains in the user's domain forest
        forest.Domains
            .Cast<Domain>()
            .Where(domain => !searchedDomains.Contains(domain.Name))
            .ToList()
            .ForEach(domain => {
              // add this domain to the searched domains so we do not search it again
              searchedDomains.Add(domain.Name);

              // search the directory for groups where the user is a direct member
              // and add them to the found groups
              filter = "(&(objectClass=group)(member=" + userDistinguishedName + "))";
              found = FindDomainGroups(searchRoot, filter);
              foundGroups.AddRange(found);

              // search the directory for groups where the user is an indirect member via group membership
              // and add them to the found groups
              var groupDistinguishedNames = foundGroups
                              .Select(g => g.EscapedDN)
                              .Where(dn => !string.IsNullOrEmpty(dn))
                              .ToArray();
              if (groupDistinguishedNames.Length > 0) {
                filter = "(&(objectClass=group)(|" + string.Join("", groupDistinguishedNames.Select(dn => "(member=" + dn + ")")) + "))";
                found = FindDomainGroups(searchRoot, filter);
                foundGroups.AddRange(found);
              }
            });
      }
    }
    catch (Exception ex) {
      if (ex is GroupRetrievalException) {
        throw;
      }

      var lastCheckedDomain = searchedDomains.LastOrDefault() ?? domainName;
      throw new GroupRetrievalException("Failed to retrieve user groups from domain in forest: " + lastCheckedDomain, ex);
    }

    // also search any externally trusted domains from Foreign Security Principals
    var trusts = forest.GetAllTrustRelationships();
    var groupsFoundInTrusts = trusts
        .Cast<TrustRelationshipInformation>()
        .Where(trust => trust.TargetName is not null && !searchedDomains.Contains(trust.TargetName)) // do not search domains we have already searched
        .Where(trust => trust.TrustDirection != TrustDirection.Outbound) // ignore outbound trusts
        .ToList()
        .SelectMany(trust => {
          var foundGroupsInTrust = new List<GroupInformation>();

          // this will fail if the raweb application pool is not running with credentials
          // that have access to this domain from the foreign security principals
          try {
            using (var searchRoot = new DirectoryEntry("LDAP://" + trust.TargetName)) {
              // construct the distinguished name for the foreign security principal
              searchRoot.RefreshCache(["distinguishedName"]);
              var domainDistinguishedName = searchRoot.Properties["distinguishedName"].Value as string;
              var foreignSecurityPrincipalDistinguishedName = "CN=" + userSid + ",CN=ForeignSecurityPrincipals," + domainDistinguishedName;

              // search for groups where the user is a direct member
              var filter = "(&(objectClass=group)(member=" + foreignSecurityPrincipalDistinguishedName + "))";
              var found = FindDomainGroups(searchRoot, filter);
              foundGroupsInTrust.AddRange(found);

              // search  for groups where the user is an indirect member via group membership
              var groupDistinguishedNames = foundGroups
                          .Select(g => g.EscapedDN)
                          .Where(dn => !string.IsNullOrEmpty(dn))
                          .ToArray();
              if (groupDistinguishedNames.Length > 0) {
                filter = "(&(objectClass=group)(|" + string.Join("", groupDistinguishedNames.Select(dn => "(member=" + dn + ")")) + "))";
                found = FindDomainGroups(searchRoot, filter);
                foundGroupsInTrust.AddRange(found);
              }
            }
          }
          catch (Exception ex) {
            if (ex is GroupRetrievalException) {
              throw;
            }

            throw new GroupRetrievalException("Failed to retrieve user groups from externally trusted domain: " + trust.TargetName, ex);
          }

          return foundGroupsInTrust;
        })
        .ToList();

    foundGroups = foundGroups
        // add groups found in foreign security principals
        .Concat(groupsFoundInTrusts)
        // remove the domain names from the group names
        .Select(g => {
          // remove the domain name from the group name if it exists
          var groupName = g.Name;
          if (groupName is not null && groupName.Contains("\\")) {
            groupName = groupName.Split('\\').Last();
          }
          return new GroupInformation(groupName, g.Sid, g.DN);
        })
        .ToList();

    return foundGroups;
  }

  /// <summary>
  /// A helper method to get all groups for a user.
  /// <br />
  /// If the user is from the local machine, it will enumerate local machine groups.
  /// <br />
  /// If the user is from a domain, it will search all domains in the forest for groups
  /// where the user is a direct member or an indirect member via group membership.
  /// See GetUserGroupsForAllDomains for more details.
  /// </summary>
  /// <param name="user">A user principal</param>
  /// <returns>A list of found groups.</returns>
  /// <exception cref="NullReferenceException"></exception>
  /// <exception cref="ArgumentNullException"></exception>
  /// <exception cref="GroupRetrievalException"></exception>
  /// <exception cref="Exception"></exception>
  public static List<GroupInformation> GetAllUserGroups(UserPrincipal user) {
    if (user.GetUnderlyingObject() is not DirectoryEntry de) {
      throw new NullReferenceException("DirectoryEntry (de) cannot be null.");
    }

    var userSid = user.Sid.ToString();

    // if the user is from the local machine instead of a domain, we need
    // to enumerate the local machine groups to find which groups contain
    // the user's SID
    var isLocalMachineUser = de.Path.StartsWith("WinNT://", StringComparison.OrdinalIgnoreCase);
    if (isLocalMachineUser) {
      var localGroups = GetLocalGroupMemberships(de, userSid);
      return localGroups;
    }

    // otherwise, we need to get the user's groups from the domain
    // and then also find local machine groups the contain the user's sid OR the user's groups SIDs
    var foundDomainGroups = GetUserGroupsForAllDomains(de, userSid);
    var foundLocalGroups = GetLocalGroupMemberships(de, userSid, foundDomainGroups.Select(g => g.Sid).ToArray());
    var allGroups = foundDomainGroups.Concat(foundLocalGroups);

    // remove duplicate groups by SID
    allGroups = allGroups.GroupBy(g => g.Sid).Select(g => g.First());

    return allGroups.ToList();
  }

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
  /// This method creates a UserPrincipal for the specified username and domain,
  /// and then retrieves the user's SID, full name, and group memberships.
  /// via a PrincipalSearcher.
  /// </summary>
  /// <param name="username"></param>
  /// <param name="domain"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentNullException"></exception>
  /// <exception cref="ArgumentException"></exception>
  /// <exception cref="Exception"></exception>
  /// <exception cref="GroupRetrievalException"></exception>
  public static UserInformation? FromPrincipal(string username, string domain) {
    // if the account is the anonymous account, return those details
    if (IsAnonymousAccount(username, domain)) {
      return AnonymousUser;
    }

    // get the principal context for the domain or machine
    var domainIsMachine = string.IsNullOrEmpty(domain) || domain.Trim() == Environment.MachineName;
    PrincipalContext principalContext;
    if (domainIsMachine) {
      // if the domain is empty or the same as the machine name, use the machine context
      domain = Environment.MachineName;
      principalContext = new PrincipalContext(ContextType.Machine);
    }
    else {
      // if the domain is specified, use the domain context
      principalContext = new PrincipalContext(ContextType.Domain, domain);
    }

    // get the user principal (PrincipalSearcher is much faster than UserPrincipal.FindByIdentity)
    var user = new UserPrincipal(principalContext) {
      SamAccountName = username
    };
    var userSearcher = new PrincipalSearcher(user);
    user = userSearcher.FindOne() as UserPrincipal;

    // if the user is not found, return null early
    if (user == null) {
      return null;
    }

    // get the user SID
    var userSid = user.Sid.ToString();

    // get the full name of the user
    var fullName = user.DisplayName ?? user.Name ?? user.SamAccountName;

    // get all groups of which the user is a member (checks all domains and local machine groups)
    var groupInformation = GetAllUserGroups(user); // may raise exceptions

    // clean up
    if (principalContext != null) {
      try {
        principalContext.Dispose();
      }
      catch (Exception ex) {
        // log the exception if needed
        System.Diagnostics.Debug.WriteLine("Error disposing PrincipalContext: " + ex.Message);
      }
    }
    if (user != null) {
      try {
        user.Dispose();
      }
      catch (Exception ex) {
        // log the exception if needed
        System.Diagnostics.Debug.WriteLine("Error disposing UserPrincipal: " + ex.Message);
      }
    }

    var userInfo = new UserInformation(
        userSid,
        username,
        domain,
        fullName,
        [.. groupInformation]
    );

    // update the cache with the user information
    if (PoliciesManager.RawPolicies["UserCache.Enabled"] == "true") {
      var dbHelper = new UserCacheDatabaseHelper();
      dbHelper.StoreUser(userInfo);
    }

    return userInfo;
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

    // otherwise, attempt to get the latest user information using principal contexts,
    // but fall back to the cache with no staleness restrictions if an error occurs
    // TODO: if we ever enable the user cache by default, we should not bypass the stale check and instead suggest that those who need something similar set their UserCache.StaleWhileRevalidate value to a massive number
    try {
      var userInfo = FromPrincipal(username, domain);
      return userInfo;
    }
    catch (Exception) {
      // fall back to the cache if an error occurs and the user cache is enabled
      // (e.g., the principal context for the domain cannot currently be accessed)
      cachedUserInfo = FromUserCache(username, domain, 315576000); // 10 years max age to effectively disable staleness
      return cachedUserInfo;
    }
  }

#if NET462
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
  public static UserInformation? FromHttpRequest(System.Web.HttpRequest request) {
    if (request == null) {
      throw new ArgumentNullException("request", "HttpRequest cannot be null.");
    }

    var authTicket = AuthTicket.FromHttpRequestCookie(request);
    if (authTicket == null) {
      return null;
    }

    // use a request-based cache to avoid repeated lookups during the same request
    var context = request.RequestContext.HttpContext;
    const string contextKey = "UserInformation";

    // if the user information is already in the request context, return it
    if (context.Items[contextKey] is UserInformation) {
      return context.Items[contextKey] as UserInformation;
    }

    var userInfo = FromDownLevelLogonName(authTicket.Name);
    if (userInfo != null) {
      context.Items[contextKey] = userInfo; // store in request context
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
  public static UserInformation? FromHttpRequestSafe(System.Web.HttpRequest request) {
    try {
      return FromHttpRequest(request);
    }
    catch (Exception) {
      return null; // return null if an error occurs
    }
  }
#else
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

    // use a request-based cache to avoid repeated lookups during the same request
    const string contextKey = "UserInformation";

    // if the user information is already in the request context, return it
    if (request.HttpContext.Items[contextKey] is UserInformation) {
      return request.HttpContext.Items[contextKey] as UserInformation;
    }

    var userInfo = FromDownLevelLogonName(authTicket.Name);
    if (userInfo != null) {
      request.HttpContext.Items[contextKey] = userInfo; // store in request context
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
#endif
}
