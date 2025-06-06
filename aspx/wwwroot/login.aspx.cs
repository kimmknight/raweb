using AliasUtilities;
using System;
using System.DirectoryServices.AccountManagement;
using System.Web;
using System.Web.Security;
using System.Web.Services;
using System.Diagnostics;

public partial class Login : System.Web.UI.Page
{
    // public property for the domain so that it can be accessed in the .aspx file if needed
    public string Domain { get; set; }

    // make the alias resolver available to the page
    public AliasUtilities.AliasResolver resolver = new AliasUtilities.AliasResolver();

    protected void Page_Load(object sender, EventArgs e)
    {
        // prevent client-side caching
        Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
        Response.Cache.SetNoStore();
        Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

        // set the title of the security error info bar
        // (we set it here because it mixes resource strings with static text)
        InfoBarSecurityError5003.Title =
            Resources.WebResources.SecError5003_Title +
            "<span style='font-size: 13px; font-weight: 400; opacity: 0.7; position: absolute; top: -12px; right: 6px;'>" +
            Resources.WebResources.SecError5003 +
            "</span>";

        if (!IsPostBack)
        {
            // on first load, store the return URL in a hidden field
            // so that we can redirect the user back to it after a successful login
            ReturnUrl.Value = string.Empty;
            if (Request.QueryString["ReturnUrl"] != null)
            {
                ReturnUrl.Value = Request.QueryString["ReturnUrl"];
            }
        }

        if (IsPostBack)
        {

            string username = Request.Form["username"];
            string password = Request.Form["password"];

            // if the username contains a domain, split it to get the username and domain separately
            if (username.Contains("\\"))
            {
                string[] parts = username.Split(new[] { '\\' }, 2);
                Domain = parts[0]; // the part before the backslash is the domain
                username = parts[1]; // the part after the backslash is the username
            }
            else
            {
                Domain = GetDomainName();
                Username.Value = Domain + "\\" + username; // update the HTML text field to include the domain
            }

            // check if the username and password are valid for the domain
            var result = ValidateUser(username, password, Domain);
            var credentialsAreValid = result.Item1;
            var errorMessage = result.Item2;
            var principalContext = result.Item3;
            if (credentialsAreValid)
            {
                // get the correct case username
                UserPrincipal user = UserPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName, Domain + "\\" + username);
                if (user != null)
                {
                    username = user.SamAccountName;
                }

                // click the AuthenticateButton to trigger the login
                string clickScript = "document.addEventListener('DOMContentLoaded', function() { authenticateUser('" + Domain + "\\\\" + username + "', '" + password + "', '" + ReturnUrl.Value + "'); });";
                ClientScript.RegisterStartupScript(this.GetType(), "clickAuthenticateButton", clickScript, true);
            }
            else
            {
                InfoBarCritical1.Message = errorMessage != null ? System.Web.HttpUtility.HtmlEncode(errorMessage) : Resources.WebResources.Login_IncorrectUsernameOrPassword;
                InfoBarCritical1.Visible = true;
            }

            if (principalContext != null)
            {
                // dispose of the PrincipalContext to free up resources
                try
                {
                    principalContext.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error disposing PrincipalContext: " + ex.Message);
                }
            }
        }
    }

    private Tuple<bool, string, PrincipalContext> ValidateUser(string username, string password, string domain)
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

    private static string GetDomainName()
    {
        try
        {
            return System.DirectoryServices.ActiveDirectory.Domain.GetComputerDomain().Name;
        }
        catch (System.DirectoryServices.ActiveDirectory.ActiveDirectoryObjectNotFoundException)
        {
            return Environment.MachineName; // Return the machine name if the domain is not found
        }
        catch (Exception ex)
        {
            // Handle other exceptions (e.g., network issues)
            return Environment.MachineName;
        }
    }

    [WebMethod]
    public static bool CheckLoginPageForAnonymousAuthentication(string loginPageUrl)
    {
        if (HttpContext.Current == null) return false;

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
        return false;
    }
}