using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
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

            // get the user principal
            UserPrincipal user = UserPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName, domain + "\\" + username);

            // if the user is not found, return null early
            if (user == null)
            {
                return null;
            }

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
                // log the exception if needed
                System.Diagnostics.Debug.WriteLine("Error getting user information: " + ex.Message);
                return null; // return null if an error occurs
            }
        }
    }

    public class UserInformation
    {
        public string Username { get; set; }
        public string Domain { get; set; }
        public string FullName { get; set; }
        public GroupInformation[] Groups { get; set; }

        public UserInformation(string username, string domain, string fullName, GroupInformation[] groups)
        {
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

        public UserInformation(string username, string domain)
        {
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
}
