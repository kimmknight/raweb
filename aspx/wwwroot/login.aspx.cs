using AliasUtilities;
using System;
using System.DirectoryServices.AccountManagement;
using System.Web.Security;
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
                string clickScript = "document.addEventListener('DOMContentLoaded', function() { authenticateUser('" + Domain + "\\" + username + "', '" + password + "'); });";
                ClientScript.RegisterStartupScript(this.GetType(), "clickAuthenticateButton", clickScript, true);
            }
            else
            {
                InfoBarCritical1.Message = errorMessage != null ? System.Web.HttpUtility.HtmlEncode(errorMessage) : "Incorrect username or password.";
                InfoBarCritical1.Visible = true;
            }
            principalContext.Dispose();
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
                return Tuple.Create(false, "Something went wrong while validating credentials against the local machine. Please try again later.", (PrincipalContext)null);
            }
        }

        try
        {
            PrincipalContext pc = new PrincipalContext(ContextType.Domain, domain);
            return Tuple.Create(pc.ValidateCredentials(username, password), (string)null, pc);
        }
        catch (Exception ex)
        {
            return Tuple.Create(false, "Could not find the specified domain. Please check your domain name and try again.", (PrincipalContext)null);
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
}