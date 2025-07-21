using AuthUtilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.DirectoryServices.AccountManagement;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.Services.Protocols;

[WebService(Namespace = "https://raweb.app/AuthService")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
[ScriptService]
public class AuthService : WebService
{
    [WebMethod]
    [ScriptMethod(UseHttpGet = true)]
    public bool CheckLoginPageForAnonymousAuthentication(string loginPageUrl)
    {
        return AuthUtilities.SignOn.CheckLoginPageForAnonymousAuthentication(loginPageUrl);
    }

    [WebMethod]
    [ScriptMethod]
    public string ValidateCredentials(string username, string password)
    {
        // if the username contains a domain, split it to get the username and domain separately
        string domain = null;
        if (username.Contains("\\"))
        {
            string[] parts = username.Split(new[] { '\\' }, 2);
            domain = parts[0]; // the part before the backslash is the domain
            username = parts[1]; // the part after the backslash is the username
        }
        else
        {
            domain = AuthUtilities.SignOn.GetDomainName();
        }

        // check if the username and password are valid for the domain
        var result = AuthUtilities.SignOn.ValidateCredentials(username, password, domain);
        var credentialsAreValid = result.Item1;
        var errorMessage = result.Item2;

        if (credentialsAreValid)
        {
            return new JavaScriptSerializer().Serialize(new { success = true, username = username, domain = domain });
        }
        else
        {
            return new JavaScriptSerializer().Serialize(new { success = false, error = result.Item2, domain = domain });
        }



    }

    [WebMethod]
    [ScriptMethod(UseHttpGet = true)]
    public string GetDomainName()
    {
        return AuthUtilities.SignOn.GetDomainName();
    }
}
