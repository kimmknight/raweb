using System;
using System.Collections.Generic;
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
            if (domain == "NT AUTHORITY" && username == "IUSR")
            {
                return new UserInformation("S-1-5-17", username, domain, "Anonymous User", new GroupInformation[0]);
            }

            // attempt to get the latest user information using principal contexts, but fall back to the cache if an error occurs
            try
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

                // get the group SIDs and names
                PrincipalSearchResult<Principal> groupsResult = user.GetGroups();
                List<string> groupSIDs = new List<string>();
                List<string> groupNames = new List<string>();
                List<GroupInformation> groupInformation = new List<GroupInformation>();
                foreach (var maybeGroup in groupsResult)
                {
                    // continue to the next group if the group is null or not a GroupPrincipal
                    if (maybeGroup == null || !(maybeGroup is GroupPrincipal))
                    {
                        continue;
                    }


                    groupSIDs.Add(maybeGroup.Sid.ToString());
                    groupNames.Add(maybeGroup.Name);
                    groupInformation.Add(new GroupInformation((GroupPrincipal)maybeGroup));
                }

                // if the pricipal context is for a remote domain,
                // we also need to get the local machine groups
                if (!domainIsMachine)
                {
                    // get the localmachine pricipal context
                    PrincipalContext localMachineContext = new PrincipalContext(ContextType.Machine);

                    // get the local groups (all groups on the local machine)
                    // this is necessary because the user may be a member of local groups, including 'Users' and 'Administrators'
                    var searcher = new PrincipalSearcher(new GroupPrincipal(localMachineContext));
                    foreach (GroupPrincipal group
                        in searcher.FindAll().OfType<GroupPrincipal>())
                    {
                        // continue to the next group if the group is null
                        if (group == null)
                        {
                            continue;
                        }

                        // check if the user's SID or group SIDs are members of this group
                        // (e.g., the domain user is a member of the Domain Users group, which is usually a member of the local Users group)
                        bool userOrUserGroupIsMemberOfThisGroup = group.GetMembers().Any(m => m.Sid.Equals(user.Sid) || groupSIDs.Contains(m.Sid.ToString()));
                        if (!userOrUserGroupIsMemberOfThisGroup)
                        {
                            // if the user or user's groups are not members of this group, skip it
                            continue;
                        }

                        // add the group SID and name to the lists
                        groupSIDs.Add(group.Sid.ToString());
                        groupNames.Add(group.Name);
                        groupInformation.Add(new GroupInformation(group));
                    }
                }

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
            catch (Exception ex)
            {
                // fall back to the cache if an error occurs and the user cache is enabled
                // (e.g., the principal context for the domain cannot currently be accessed)
                if (System.Configuration.ConfigurationManager.AppSettings["UserCache.Enabled"] == "true")
                {
                    var dbHelper = new UserCacheDatabaseHelper();
                    UserInformation cachedUserInfo = dbHelper.GetUser(null, username, domain);
                    return cachedUserInfo;
                }
                return null;
            }
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
                return this.Sid == "S-1-5-17";
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

        public GroupInformation(string name, string sid)
        {
            Name = name;
            Sid = sid;
        }

        public GroupInformation(GroupPrincipal groupPrincipal)
        {
            if (groupPrincipal == null)
            {
                throw new ArgumentNullException("groupPrincipal", "GroupPrincipal cannot be null.");
            }

            Name = groupPrincipal.Name;
            Sid = groupPrincipal.Sid.ToString();
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
                    default:
                        return Tuple.Create(false, "An unknown error occurred: " + errorCode);
                }
            }
        }
    }
}
