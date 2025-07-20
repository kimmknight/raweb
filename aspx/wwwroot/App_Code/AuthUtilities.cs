using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
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
            string username = request.LogonUserIdentity.Name;
            DateTime issueDate = DateTime.Now;
            DateTime expirationDate = DateTime.Now.AddMinutes(30);
            bool isPersistent = false;
            string userData = "";

            FormsAuthenticationTicket tkt = new FormsAuthenticationTicket(version, username, issueDate, expirationDate, isPersistent, userData);
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

            return new UserInformation(
                userSid,
                username,
                domain,
                fullName,
                groupInformation.ToArray()
            );
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
                return Environment.MachineName; // eeturn the machine name if the domain is not found
            }
            catch (Exception ex)
            {
                return Environment.MachineName;
            }
        }

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
        public static Tuple<bool, string, PrincipalContext> ValidateCredentials(string username, string password, string domain)
        {
            if (string.IsNullOrEmpty(domain) || domain.Trim() == Environment.MachineName)
            {
                try
                {
                    PrincipalContext pc = new PrincipalContext(ContextType.Machine);
                    return Tuple.Create(pc.ValidateCredentials(username, password), (string)null, pc);
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred while validating credentials against the local machine.", ex);
                    return Tuple.Create(false, Resources.WebResources.Login_LocalMachineError, (PrincipalContext)null);
                }
            }

            try
            {
                PrincipalContext pc = new PrincipalContext(ContextType.Domain, domain);
                return Tuple.Create(pc.ValidateCredentials(username, password), (string)null, pc);
            }
            catch (Exception ex)
            {
                return Tuple.Create(false, Resources.WebResources.Login_UnfoundDomain, (PrincipalContext)null);
            }
        }
    }


}
