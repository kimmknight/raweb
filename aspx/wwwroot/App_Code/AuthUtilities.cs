using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Security;

namespace AuthUtilities
{
    public class AuthCookieHandler
    {
        public static string cookieName;

        public AuthCookieHandler(string name = ".ASPXAUTH")
        {
            cookieName = name;
        }

        public static string CreateAuthTicket(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request", "HttpRequest cannot be null.");
            }

            int version = 1;
            // useful fields: https://learn.microsoft.com/en-us/dotnet/api/system.web.httprequest.logonuseridentity?view=netframework-4.8.1

            string userSid = request.LogonUserIdentity.User.Value;
            string username = request.LogonUserIdentity.Name.Split('\\').Last(); // get the username from the LogonUserIdentity, which is in DOMAIN\username format
            string domain = request.LogonUserIdentity.Name.Contains("\\") ? request.LogonUserIdentity.Name.Split('\\')[0] : Environment.MachineName; // get the domain from the username, or use machine name if no domain

            // parse the groups from the LogonUserIdentity
            IdentityReferenceCollection groups = request.LogonUserIdentity.Groups;
            List<GroupInformation> groupInformation = new List<GroupInformation>();
            foreach (IdentityReference group in groups)
            {
                string groupSid = group.Value;
                string displayName = groupSid;

                // Attempt to translate the SID to an NTAccount (e.g., DOMAIN\GroupName)
                try
                {
                    NTAccount ntAccount = (NTAccount)group.Translate(typeof(NTAccount));
                    displayName = ntAccount.Value.Split('\\').Last(); // Get the group name from the NTAccount
                }
                catch (IdentityNotMappedException)
                {
                    // identity cannot be mapped - use SID as display name
                }
                catch (System.SystemException)
                {
                    // cannot communicate with the domain controller - use SID as display name
                }

                groupInformation.Add(new GroupInformation(displayName, groupSid));
            }

            string groupsString = string.Join(", ", groupInformation.Select(g => g.Name + " (" + g.Sid + ")"));

            DateTime issueDate = DateTime.Now;
            DateTime expirationDate = DateTime.Now.AddMinutes(30);
            bool isPersistent = false;
            string userData = "";

            if (System.Configuration.ConfigurationManager.AppSettings["UserCache.Enabled"] == "true")
            {
                var dbHelper = new UserCacheDatabaseHelper();
                dbHelper.StoreUser(userSid, username, domain, username, groupInformation);
            }

            FormsAuthenticationTicket tkt = new FormsAuthenticationTicket(version, domain + "\\" + username, issueDate, expirationDate, isPersistent, userData);
            string token = FormsAuthentication.Encrypt(tkt);
            return token;
        }

        public void SetAuthCookie(HttpRequest request, HttpResponse response)
        {
            string authTicket = CreateAuthTicket(request);
            if (string.IsNullOrEmpty(authTicket))
            {
                throw new Exception("Failed to create authentication ticket.");
            }

            SetAuthCookie(authTicket, response);
        }

        public void SetAuthCookie(string cookieValue, HttpResponse response)
        {
            string combinedCookieNameAndValue = cookieName + "=" + cookieValue;

            if (response == null)
            {
                throw new ArgumentNullException("response", "HttpResponse cannot be null.");
            }

            // if the cookie name+value length is greater than or equal to 4096 bytes,
            // end with an exception
            if (combinedCookieNameAndValue.Length >= 4096)
            {
                throw new Exception("Cookie name and value length exceeds 4096 bytes.");
            }

            // create a cookie and add it to the response
            HttpCookie authCookie = new HttpCookie(cookieName, cookieValue);
            authCookie.Path = FormsAuthentication.FormsCookiePath;
            response.Cookies.Add(authCookie);
            return;
        }

        public FormsAuthenticationTicket GetAuthTicket(HttpRequest request)
        {
            string cookieValue = string.Empty;

            // get the cookie value from the request
            if (request == null)
            {
                throw new ArgumentNullException("request", "HttpRequest cannot be null.");
            }
            if (request.Cookies == null)
            {
                throw new ArgumentNullException("request.Cookies", "Cookies collection cannot be null.");
            }
            if (request.Cookies[cookieName] == null)
            {
                // if the cookie does not exist, return null
                return null;
            }

            // if the cookie exists, get its value
            cookieValue = request.Cookies[cookieName].Value;

            // decrypt the value and return it
            try
            {
                // decrypt may throw an exception if cookieValue is invalid
                FormsAuthenticationTicket authTicket = FormsAuthentication.Decrypt(cookieValue);
                return authTicket;
            }
            catch
            {
                return null;
            }
        }

        public UserInformation GetUserInformation(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request", "HttpRequest cannot be null.");
            }

            FormsAuthenticationTicket authTicket = GetAuthTicket(request);
            if (authTicket == null)
            {
                return null;
            }

            // use a request-based cache to avoid repeated lookups during the same request
            var context = request.RequestContext.HttpContext;
            const string contextKey = "UserInformation";

            // if the user information is already in the request context, return it
            if (context.Items[contextKey] is UserInformation)
            {
                return context.Items[contextKey] as UserInformation;
            }

            // get the username and domain from the auth ticket (we used DOMAIN\username for ticket name)
            string[] parts = authTicket.Name.Split('\\');
            string username = parts.Length > 1 ? parts[1] : parts[0]; // the part after the backslash is the username
            string domain = parts.Length > 1 ? parts[0] : Environment.MachineName; // the part before the backslash is the domain, or use machine name if no domain

            // throw an exception if username or domain is null or empty
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(domain))
            {
                throw new ArgumentException("Username or domain cannot be null or empty.");
            }

            // if the account is the anonymous account, return those details
            if ((domain == "NT AUTHORITY" && username == "IUSR") || (domain == "IIS APPPOOL" && username == "raweb"))
            {
                var userInfo = new UserInformation("S-1-4-447-1", username, domain, "Anonymous User", new GroupInformation[0]);
                context.Items[contextKey] = userInfo; // store in request context
                return userInfo;
            }

            // if the user cache is enabled, attempt to get the user from the cache first,
            // but only if the user information is not stale
            if (System.Configuration.ConfigurationManager.AppSettings["UserCache.Enabled"] == "true")
            {
                var dbHelper = new UserCacheDatabaseHelper();
                UserInformation cachedUserInfo = dbHelper.GetUser(null, username, domain);

                if (cachedUserInfo != null)
                {
                    // store in request context
                    context.Items[contextKey] = cachedUserInfo;

                    // return the cached user information immediately
                    return cachedUserInfo;
                }
            }

            // otherwise, attempt to get the latest user information using principal contexts,
            // but fall back to the cache with no staleness restrictions if an error occurs
            // TODO: if we ever enable the user cache by default, we should not bypass the stale check and instead suggest that those who need something similar set their UserCache.StaleWhileRevalidate value to a massive number
            try
            {
                var userInfo = GetUserInformationFromPrincipalContext(username, domain);

                // store the user information in the request context
                context.Items[contextKey] = userInfo;

                return userInfo;
            }
            catch (Exception ex)
            {
                // fall back to the cache if an error occurs and the user cache is enabled
                // (e.g., the principal context for the domain cannot currently be accessed)
                if (System.Configuration.ConfigurationManager.AppSettings["UserCache.Enabled"] == "true")
                {
                    var dbHelper = new UserCacheDatabaseHelper();
                    UserInformation cachedUserInfo = dbHelper.GetUser(null, username, domain, 315576000); // 10 years max age to effectively disable staleness
                    context.Items[contextKey] = cachedUserInfo; // store in request context
                    return cachedUserInfo;
                }
                return null;
            }
        }

        public UserInformation GetUserInformationFromPrincipalContext(string username, string domain)
        {
            // get the principal context for the domain or machine
            bool domainIsMachine = string.IsNullOrEmpty(domain) || domain.Trim() == Environment.MachineName;
            PrincipalContext principalContext;
            if (domainIsMachine)
            {
                // if the domain is empty or the same as the machine name, use the machine context
                domain = Environment.MachineName;
                principalContext = new PrincipalContext(ContextType.Machine);
            }
            else
            {
                // if the domain is specified, use the domain context
                principalContext = new PrincipalContext(ContextType.Domain, domain);
            }

            // get the user principal (PrincipalSearcher is much faster than UserPrincipal.FindByIdentity)
            var user = new UserPrincipal(principalContext);
            user.SamAccountName = username;
            var userSearcher = new PrincipalSearcher(user);
            user = userSearcher.FindOne() as UserPrincipal;

            // if the user is not found, return null early
            if (user == null)
            {
                return null;
            }

            // get the user SID
            string userSid = user.Sid.ToString();

            // get the full name of the user
            string fullName = user.DisplayName ?? user.Name ?? user.SamAccountName;

            // get all groups of which the user is a member (checks all domains and local machine groups)
            var groupInformation = UserInformation.GetAllUserGroups(user);

            // clean up
            if (principalContext != null)
            {
                try
                {
                    principalContext.Dispose();
                }
                catch (Exception ex)
                {
                    // log the exception if needed
                    System.Diagnostics.Debug.WriteLine("Error disposing PrincipalContext: " + ex.Message);
                }
            }
            if (user != null)
            {
                try
                {
                    user.Dispose();
                }
                catch (Exception ex)
                {
                    // log the exception if needed
                    System.Diagnostics.Debug.WriteLine("Error disposing UserPrincipal: " + ex.Message);
                }
            }

            var userInfo = new UserInformation(
                userSid,
                username,
                domain,
                fullName,
                groupInformation.ToArray()
            );

            // update the cache with the user information
            if (System.Configuration.ConfigurationManager.AppSettings["UserCache.Enabled"] == "true")
            {
                var dbHelper = new UserCacheDatabaseHelper();
                dbHelper.StoreUser(userInfo);
            }

            return userInfo;
        }

        public UserInformation GetUserInformationSafe(HttpRequest request)
        {
            try
            {
                return GetUserInformation(request);
            }
            catch (Exception ex)
            {
                return null; // return null if an error occurs
            }
        }
    }

    public class UserInformation
    {
        public string Username { get; set; }
        public string Domain { get; set; }
        public string Sid { get; set; }
        public string FullName { get; set; }
        public GroupInformation[] Groups { get; set; }
        public bool IsAnonymousUser
        {
            get
            {
                return this.Sid == "S-1-4-447-1";
            }
        }
        public bool IsRemoteDesktopUser
        {
            get
            {
                return this.Groups.Any(g => g.Sid == "S-1-5-32-555");
            }
        }
        public bool IsLocalAdministrator
        {
            get
            {
                return this.Groups.Any(g => g.Sid == "S-1-5-32-544");
            }
        }

        public UserInformation(string sid, string username, string domain, string fullName, GroupInformation[] groups)
        {
            Sid = sid;
            Username = username;
            Domain = domain;

            if (string.IsNullOrEmpty(fullName))
            {
                FullName = username; // default to username if full name is not provided
            }
            else
            {
                FullName = fullName;
            }

            Groups = groups;
        }

        public UserInformation(string sid, string username, string domain)
        {
            Sid = sid;
            Username = username;
            Domain = domain;
            FullName = username;
            Groups = new GroupInformation[0];
        }

        /// <summary>
        /// Gets the local group memberships for a user.
        /// </summary>
        /// <param name="de">The directory entry for the user.</param>
        /// <param name="userSid">The sid of the user in string form.</param>
        /// <param name="userGroupsSids">The optional array of string sids representing groups that the user belongs to. Use this when searching local groups after finding domain groups.</param>
        /// <returns>A list of group information</returns>
        /// <exception cref="ArgumentNullException"></exception>
        private static List<GroupInformation> GetLocalGroupMemberships(DirectoryEntry de, string userSid, string[] userGroupsSids = null)
        {
            if (de == null)
            {
                throw new ArgumentNullException("de", "DirectoryEntry cannot be null.");
            }
            if (string.IsNullOrEmpty(userSid))
            {
                throw new ArgumentNullException("userSid", "User SID cannot be null or empty.");
            }
            if (userGroupsSids == null)
            {
                userGroupsSids = new string[0];
            }

            // seach the local machine for groups that contain the user's SID
            List<GroupInformation> localGroups = new List<GroupInformation>();
            string localMachinePath = "WinNT://" + Environment.MachineName + ",computer";
            try
            {
                using (var machineEntry = new DirectoryEntry(localMachinePath))
                {
                    foreach (DirectoryEntry machineChildEntry in machineEntry.Children)
                    {
                        // skip entries that are not groups
                        if (machineChildEntry.SchemaClassName != "Group")
                        {
                            continue;
                        }

                        // skip if there are no members of the group
                        var members = machineChildEntry.Invoke("Members") as System.Collections.IEnumerable;
                        if (members == null || !members.Cast<object>().Any())
                        {
                            continue;
                        }

                        // get the sid of the group
                        byte[] groupSidBytes = (byte[])machineChildEntry.Properties["objectSid"].Value;
                        var groupSid = new SecurityIdentifier(groupSidBytes, 0).ToString();

                        // check the SIDs of each member in the group (this gets user and group SIDs)
                        foreach (object member in members)
                        {
                            using (DirectoryEntry memberEntry = new DirectoryEntry(member))
                            {

                                byte[] sidBytes = (byte[])memberEntry.Properties["objectSid"].Value;
                                var groupMemberSid = new SecurityIdentifier(sidBytes, 0).ToString();

                                // add the group to the list if:
                                // - the group member SID matches the user's SID (the user is a member of the group)
                                // - the group member SID is in the user's groups SIDs (the user is a member of a group that is a member of this group)
                                if (groupMemberSid == userSid || userGroupsSids.Contains(groupMemberSid))
                                {
                                    localGroups.Add(new GroupInformation(machineChildEntry.Name, groupSid));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return localGroups;
        }

        /// <summary>
        /// Searches a directory entry that represents a domain for groups that match the specified filter.
        /// </summary>
        /// <param name="searchRoot">A DirectoryEntry. It must be for a domain.</param>
        /// <param name="filter">A filter that can be used with a DirectorySearcher.</param>
        /// <returns>A list of found groups.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        private static List<GroupInformation> FindDomainGroups(DirectoryEntry searchRoot, string filter)
        {
            if (searchRoot == null)
            {
                throw new ArgumentNullException("searchRoot", "DirectoryEntry cannot be null.");
            }
            if (string.IsNullOrEmpty(filter))
            {
                throw new ArgumentNullException("filter", "Filter cannot be null or empty.");
            }

            var propertiesToLoad = new[] { "msDS-PrincipalName", "objectSid", "distinguishedName" };

            List<GroupInformation> foundGroups = new List<GroupInformation>();
            var directorySearcher = new DirectorySearcher(searchRoot, filter, propertiesToLoad);

            using (var results = directorySearcher.FindAll())
            {
                foreach (SearchResult result in results)
                {
                    // get the group name and SID from the properties
                    string groupName = result.Properties["msDS-PrincipalName"][0].ToString();
                    string groupDistinguishedName = result.Properties["distinguishedName"][0].ToString();
                    byte[] groupSidBytes = (byte[])result.Properties["objectSid"][0];
                    string groupSid = new SecurityIdentifier(groupSidBytes, 0).ToString();

                    // add the group to the found groups
                    foundGroups.Add(new GroupInformation(groupName, groupSid, groupDistinguishedName));
                }
            }

            return foundGroups;
        }

        /// <summary>
        /// Searches a directory entry that represents a domain for groups that match the specified filter.
        /// <br />
        /// Exceptions are caught and an empty list is returned instead of throwing an exception.
        /// </summary>
        /// <param name="searchRoot">A DirectoryEntry. It must be for a domain.</param>
        /// <param name="filter">A filter that can be used with a DirectorySearcher.</param>
        /// <returns>A list of found groups.</returns>
        private static List<GroupInformation> FindDomainGroupsSafe(DirectoryEntry searchRoot, string filter)
        {
            try
            {
                return FindDomainGroups(searchRoot, filter);
            }
            catch (Exception ex)
            {
                return new List<GroupInformation>();
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
        private static List<GroupInformation> GetUserGroupsForAllDomains(DirectoryEntry de, string userSid)
        {
            if (de == null)
            {
                throw new ArgumentNullException("de", "DirectoryEntry cannot be null.");
            }
            if (string.IsNullOrEmpty(userSid))
            {
                throw new ArgumentNullException("userSid", "User SID cannot be null or empty.");
            }

            // track the found groups (may include netbios names in the group names)
            List<GroupInformation> foundGroups = new List<GroupInformation>();
            var searchedDomains = new HashSet<string>();

            // ensure the properties we want are loaded
            de.RefreshCache(new[] { "canonicalName", "objectSid", "distinguishedName", "primaryGroupID" });
            string userCanonicalName = de.Properties["canonicalName"].Value as string;
            string userDistinguishedName = de.Properties["distinguishedName"].Value as string;
            int primaryGroupId = (int)de.Properties["primaryGroupID"].Value;

            // extract the user's domain from the canonicalName property and use it to get the domain and forest
            if (string.IsNullOrEmpty(userCanonicalName))
            {
                throw new Exception("Canonical name is not available for the user's directory entry.");
            }
            string domainName = userCanonicalName.Split('/')[0];
            var domainDirectoryContext = new DirectoryContext(DirectoryContextType.Domain, domainName);
            Domain userDomain = System.DirectoryServices.ActiveDirectory.Domain.GetDomain(domainDirectoryContext);
            Forest forest = userDomain.Forest;

            // this may fail if the raweb application pool is not running with credentials
            // that can query the domains in the forest
            try
            {
                using (var searchRoot = new DirectoryEntry("LDAP://" + userDomain.Name))
                {

                    // construct the user's primary group SID
                    searchRoot.RefreshCache(new[] { "objectSid" });
                    byte[] objectSidBytes = (byte[])searchRoot.Properties["objectSid"].Value;
                    string domainSid = new SecurityIdentifier(objectSidBytes, 0).ToString();
                    string userPrimaryGroupSid = domainSid + "-" + primaryGroupId;

                    // search for the primary group using the primary group SID
                    // and add it to the found groups
                    var filter = "(&(objectClass=group)(objectSid=" + userPrimaryGroupSid + "))";
                    var found = FindDomainGroupsSafe(searchRoot, filter);
                    foundGroups.AddRange(found);

                    // search domains in the user's domain forest
                    forest.Domains
                        .Cast<Domain>()
                        .Where(domain => !searchedDomains.Contains(domain.Name))
                        .ToList()
                        .ForEach(domain =>
                        {
                            // add this domain to the searched domains so we do not search it again
                            searchedDomains.Add(domain.Name);

                            // search the directory for groups where the user is a direct member
                            // and add them to the found groups
                            filter = "(&(objectClass=group)(member=" + userDistinguishedName + "))";
                            found = FindDomainGroupsSafe(searchRoot, filter);
                            foundGroups.AddRange(found);

                            // search the directory for groups where the user is an indirect member via group membership
                            // and add them to the found groups
                            string[] groupDistinguishedNames = foundGroups
                                .Select(g => g.EscapedDN)
                                .Where(dn => !string.IsNullOrEmpty(dn))
                                .ToArray();
                            if (groupDistinguishedNames.Length > 0)
                            {
                                filter = "(&(objectClass=group)(|" + string.Join("", groupDistinguishedNames.Select(dn => "(member=" + dn + ")")) + "))";
                                found = FindDomainGroupsSafe(searchRoot, filter);
                                foundGroups.AddRange(found);
                            }
                        });
                }
            }
            catch (System.Exception ex)
            {
            }

            // also search any externally trusted domains from Foreign Security Principals
            var trusts = forest.GetAllTrustRelationships();
            List<GroupInformation> groupsFoundInTrusts = trusts
                .Cast<TrustRelationshipInformation>()
                .Where(trust => !searchedDomains.Contains(trust.TargetName)) // do not search domains we have already searched
                .Where(trust => trust.TrustDirection != TrustDirection.Outbound) // ignore outbound trusts
                .ToList()
                .SelectMany(trust =>
                {
                    var foundGroupsInTrust = new List<GroupInformation>();

                    // this will fail if the raweb application pool is not running with credentials
                    // that have access to this domain from the foreign security principals
                    try
                    {
                        using (var searchRoot = new DirectoryEntry("LDAP://" + trust.TargetName))
                        {
                            // construct the distinguished name for the foreign security principal
                            searchRoot.RefreshCache(new[] { "distinguishedName" });
                            string domainDistinguishedName = searchRoot.Properties["distinguishedName"].Value as string;
                            string foreignSecurityPrincipalDistinguishedName = "CN=" + userSid + ",CN=ForeignSecurityPrincipals," + domainDistinguishedName;

                            // search for groups where the user is a direct member
                            var filter = "(&(objectClass=group)(member=" + foreignSecurityPrincipalDistinguishedName + "))";
                            var found = FindDomainGroupsSafe(searchRoot, filter);
                            foundGroupsInTrust.AddRange(found);

                            // search  for groups where the user is an indirect member via group membership
                            string[] groupDistinguishedNames = foundGroups
                                .Select(g => g.EscapedDN)
                                .Where(dn => !string.IsNullOrEmpty(dn))
                                .ToArray();
                            if (groupDistinguishedNames.Length > 0)
                            {
                                filter = "(&(objectClass=group)(|" + string.Join("", groupDistinguishedNames.Select(dn => "(member=" + dn + ")")) + "))";
                                found = FindDomainGroupsSafe(searchRoot, filter);
                                foundGroupsInTrust.AddRange(found);
                            }
                        }
                    }
                    catch (System.Exception)
                    {
                    }

                    return foundGroupsInTrust;
                })
                .ToList();

            foundGroups = foundGroups
                // add groups found in foreign security principals
                .Concat(groupsFoundInTrusts)
                // remove the domain names from the group names
                .Select(g =>
                {
                    // remove the domain name from the group name if it exists
                    string groupName = g.Name;
                    if (groupName.Contains("\\"))
                    {
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
        public static List<GroupInformation> GetAllUserGroups(UserPrincipal user)
        {
            DirectoryEntry de = user.GetUnderlyingObject() as DirectoryEntry;
            string userSid = user.Sid.ToString();

            // if the user is from the local machine instead of a domain, we need
            // to enumerate the local machine groups to find which groups contain
            // the user's SID
            bool isLocalMachineUser = de.Path.StartsWith("WinNT://", StringComparison.OrdinalIgnoreCase);
            if (isLocalMachineUser)
            {
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

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();

            str.Append("Username: ").Append(Username).Append("\n");

            str.Append("Domain: ").Append(Domain).Append("\n");

            str.Append("Groups: ");
            if (Groups != null && Groups.Length > 0)
            {
                foreach (var group in Groups)
                {
                    str.Append("\n").Append("  - ").Append(group.Name).Append(" (").Append(group.Sid).Append(")");
                }
            }
            else
            {
                str.Append("None");
            }

            return str.ToString();
        }
    }

    public class GroupInformation
    {
        public string Name { get; set; }
        public string Sid { get; set; }
        public string DN { get; set; }

        /// <summary>
        /// Escaped distinguished name for LDAP filters.
        /// </summary>
        public string EscapedDN
        {
            get
            {
                if (string.IsNullOrEmpty(DN))
                {
                    return null;
                }

                // escape the distinguished name for LDAP filters
                StringBuilder sb = new StringBuilder();
                foreach (char c in DN)
                {
                    switch (c)
                    {
                        case '*': sb.Append(@"\2A"); break;
                        case '(': sb.Append(@"\28"); break;
                        case ')': sb.Append(@"\29"); break;
                        case '\\': sb.Append(@"\5C"); break;
                        default: sb.Append(c); break;
                    }
                }
                return sb.ToString();
            }
        }

        public GroupInformation(string name, string sid, string dn = null)
        {
            Name = name;
            Sid = sid;
            DN = dn;
        }

        public GroupInformation(GroupPrincipal groupPrincipal)
        {
            if (groupPrincipal == null)
            {
                throw new ArgumentNullException("groupPrincipal", "GroupPrincipal cannot be null.");
            }

            Name = groupPrincipal.Name;
            Sid = groupPrincipal.Sid.ToString();
            DN = groupPrincipal.DistinguishedName;
        }
    }

    public static class SignOn
    {
        /// <summary>
        /// Checks if anaonymous authentication is enabled for the login URL.
        /// <br />
        /// This method sends a request to the login page URL and checks if it returns a successful response.
        /// </summary>
        /// <param name="loginPageUrl">The full URL to the login page. The origin must have the correct domain and port.</param>
        /// <returns>Whether anonymous authentication is enabled, or an error when HttpContext is unavailable</returns>
        public static bool CheckLoginPageForAnonymousAuthentication(string loginPageUrl)
        {
            if (System.Web.HttpContext.Current == null)
            {
                throw new InvalidOperationException("HttpContext is not available. This method must be called within a web request context.");
            }

            try
            {
                System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(loginPageUrl);
                using (System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the current machine's domain. If the machine is not part of a domain, it returns the machine name.
        /// If the domain cannot be accessed, likely due to the machine either not being part of the domain
        /// or the network connection between the machine and the domain controller being unavailable, the machine
        /// name will be used instead.
        /// </summary>
        /// <returns>The domain name</returns>
        public static string GetDomainName()
        {
            try
            {
                return System.DirectoryServices.ActiveDirectory.Domain.GetComputerDomain().Name;
            }
            catch (System.DirectoryServices.ActiveDirectory.ActiveDirectoryObjectNotFoundException)
            {
                // if the domain cannot be found, attempt to get the domain from the registry
                var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters");
                if (regKey == null)
                {
                    // if the registry key is not found, return the machine name
                    return Environment.MachineName;
                }

                using (regKey)
                {
                    // this either contains the machine's domain name or is empty if the machine is not part of a domain
                    string foundDomain = regKey.GetValue("Domain") as string;
                    if (string.IsNullOrEmpty(foundDomain))
                    {
                        // if the domain is not found, return the machine name
                        return Environment.MachineName;
                    }
                    return foundDomain;
                }
            }
            catch (Exception ex)
            {
                return Environment.MachineName;
            }
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(
            string lpszUsername,
            string lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            out IntPtr phToken
        );

        public const int LOGON32_LOGON_INTERACTIVE = 2;
        public const int LOGON32_PROVIDER_DEFAULT = 0;

        // see https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--1300-1699-
        public const int ERROR_LOGON_FAILURE = 1326; // incorrect username or password
        public const int ERROR_ACCOUNT_RESTRICTION = 1327; // account restrictions, such as logon hours or workstation restrictions, are preventing this user from logging on
        public const int ERROR_INVALID_LOGON_HOURS = 1328; // the user is not allowed to log on at this time
        public const int ERROR_INVALID_WORKSTATION = 1329; // the user is not allowed to log on to this workstation
        public const int ERROR_PASSWORD_EXPIRED = 1330; // the user's password has expired
        public const int ERROR_ACCOUNT_DISABLED = 1331; // the user account is disabled
        public const int ERROR_PASSWORD_MUST_CHANGE = 1907; // the user account password must change before signing in

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Validates the user credentials against the local machine or domain.
        /// <br />
        /// If the domain is not specified or is the same as the machine name, it validates against the local machine.
        /// If the domain is specified, it validates against the domain.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="domain"></param>
        /// <returns>A three-part tuple, where the first value is whether the credentials are valid, the second part is an nullable error message, and the third part is the pricipal context used for credential validation.</returns>
        public static Tuple<bool, string> ValidateCredentials(string username, string password, string domain)
        {
            if (string.IsNullOrEmpty(domain) || domain.Trim() == Environment.MachineName)
            {
                domain = "."; // for local machine
            }

            // if the user cache is not enabled, require the principal context to be accessible
            // because the GetUserInformation method will attempt to access the principal context
            // to get the user information, which will fail if the domain cannot be accessed
            // and the user cache is not enabled
            if (System.Configuration.ConfigurationManager.AppSettings["UserCache.Enabled"] != "true")
            {
                try
                {
                    // attempt to get the principal context for the domain or machine
                    PrincipalContext principalContext;
                    if (domain == ".")
                    {
                        principalContext = new PrincipalContext(ContextType.Machine);
                    }
                    else
                    {
                        principalContext = new PrincipalContext(ContextType.Domain, domain, null, ContextOptions.Negotiate | ContextOptions.Signing | ContextOptions.Sealing);
                    }

                    // dispose of the principal context once we have verified it can be accessed
                    principalContext.Dispose();
                }
                catch (Exception ex)
                {
                    return Tuple.Create(false, Resources.WebResources.Login_UnfoundDomain);
                }
            }

            IntPtr userToken = IntPtr.Zero;
            if (LogonUser(username, domain, password, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, out userToken))
            {
                CloseHandle(userToken);
                return Tuple.Create(true, (string)null);
            }
            else
            {
                int errorCode = Marshal.GetLastWin32Error();
                switch (errorCode)
                {
                    case ERROR_LOGON_FAILURE:

                        // check if the domain can be resolved
                        if (domain != ".")
                        {
                            try
                            {
                                Domain.GetDomain(new DirectoryContext(DirectoryContextType.Domain, domain));
                            }
                            catch (ActiveDirectoryObjectNotFoundException)
                            {
                                return Tuple.Create(false, Resources.WebResources.Login_UnfoundDomain);
                            }
                        }

                        return Tuple.Create(false, (string)null);
                    case ERROR_ACCOUNT_RESTRICTION:
                        return Tuple.Create(false, Resources.WebResources.Login_AccountRestrictionError);
                    case ERROR_INVALID_LOGON_HOURS:
                        return Tuple.Create(false, Resources.WebResources.Login_InvalidLogonHoursError);
                    case ERROR_INVALID_WORKSTATION:
                        return Tuple.Create(false, Resources.WebResources.Login_InvalidWorkstationError);
                    case ERROR_PASSWORD_EXPIRED:
                        return Tuple.Create(false, Resources.WebResources.Login_PasswordExpiredError);
                    case ERROR_ACCOUNT_DISABLED:
                        return Tuple.Create(false, Resources.WebResources.Login_AccountDisabledError);
                    case ERROR_PASSWORD_MUST_CHANGE:
                        return Tuple.Create(false, Resources.WebResources.Login_PasswordMustChange);
                    default:
                        return Tuple.Create(false, "An unknown error occurred: " + errorCode);
                }
            }
        }

        [DllImport("Netapi32.dll", SetLastError = true)]
        public static extern int NetUserChangePassword(
            [In] string domainname,
            [In] string username,
            [In] string oldpassword,
            [In] string newpassword
        );
        public static Tuple<bool, string> ChangeCredentials(string username, string oldPassword, string newPassword, string domain)
        {
            if (domain.Trim() == Environment.MachineName)
            {
                domain = null; // for local machine
            }

            string entryUrl = null;

            // if the user is on the local machine, we can use the WinNT provider to change the password
            if (string.IsNullOrEmpty(domain))
            {
                entryUrl = "WinNT://" + Environment.MachineName + "/" + username + ",user";
            }
            // othwerwise, we need to find the user's distinguished name in the domain
            // so we can use the LDAP provider to change the password
            else
            {
                string userDistinguishedName = null;
                string ldapPath = "LDAP://" + domain;
                try
                {

                    using (DirectoryEntry searchRoot = new DirectoryEntry(ldapPath))
                    {
                        using (DirectorySearcher searcher = new DirectorySearcher(searchRoot))
                        {
                            searcher.Filter = "(&(objectClass=user)(sAMAccountName=" + username + "))";
                            searcher.PropertiesToLoad.Add("distinguishedName");

                            SearchResult result = searcher.FindOne();
                            if (result != null && result.Properties.Contains("distinguishedName"))
                            {
                                userDistinguishedName = result.Properties["distinguishedName"][0].ToString();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return Tuple.Create(false, "The domain cannot be accessed.");
                }

                if (string.IsNullOrEmpty(userDistinguishedName))
                {
                    return Tuple.Create(false, "User could not be found in the domain: " + domain);
                }

                entryUrl = "LDAP://" + domain + "/" + userDistinguishedName;
            }

            // get the user's directory entry and then attempt to change the password
            using (DirectoryEntry user = new DirectoryEntry(entryUrl))
            {
                // if the user is not found, throw an exception
                if (user == null)
                {
                    return Tuple.Create(false, "The user could not be found.");
                }

                // change the password
                {
                    try
                    {
                        user.Invoke("ChangePassword", new object[] { oldPassword, newPassword });
                        user.CommitChanges();
                        return Tuple.Create(true, (string)null);
                    }
                    catch (System.Reflection.TargetInvocationException ex)
                    {
                        // if the password change fails, return false with an error message
                        if (ex.InnerException != null)
                        {
                            // if there is a constraint violation, try the PrincipalContext method
                            if (ex.InnerException is System.DirectoryServices.DirectoryServicesCOMException)
                            {
                                try
                                {
                                    if (string.IsNullOrEmpty(domain))
                                    {
                                        using (var pc = new PrincipalContext(ContextType.Machine))
                                        using (var userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, username))
                                        {
                                            userPrincipal.ChangePassword(oldPassword, newPassword);
                                            userPrincipal.Save();
                                            return Tuple.Create(true, (string)null);
                                        }
                                    }
                                    else
                                    {
                                        using (var pc = new PrincipalContext(ContextType.Domain, domain ?? Environment.MachineName))
                                        using (var userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, username))
                                        {
                                            userPrincipal.ChangePassword(oldPassword, newPassword);
                                            userPrincipal.Save();
                                            return Tuple.Create(true, (string)null);
                                        }
                                    }
                                }
                                catch (Exception pEx)
                                {
                                    return Tuple.Create(false, pEx.Message);
                                }
                            }
                            return Tuple.Create(false, ex.InnerException.Message);
                        }
                        throw ex; // rethrow if there is no inner exception - we don't know what went wrong
                    }
                    catch (Exception ex)
                    {
                        return Tuple.Create(false, ex.Message);
                    }
                }
            }
        }
    }
}
