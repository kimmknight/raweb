using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Net;
using System.Web.Http;
using RAWeb.Server.Utilities;
using RAWebServer.Utilities;

namespace RAWebServer.Api {
  public partial class AuthController : ApiController {
    public class ChangePasswordBody {
      public string Username { get; set; }
      public string OldPassword { get; set; }
      public string NewPassword { get; set; }
    }

    [HttpPost]
    [Route("change-password")]
    public IHttpActionResult ChangePassword([FromBody] ChangePasswordBody body) {
      if (PoliciesManager.RawPolicies["PasswordChange.Enabled"] == "false") {
        return Content(HttpStatusCode.Unauthorized, new { success = false, error = "Password change is disabled." });
      }

      // if the username contains a domain, split it to get the username and domain separately
      string domain;
      var username = body.Username;
      if (username.Contains("\\")) {
        var parts = body.Username.Split(new[] { '\\' }, 2);
        domain = parts[0]; // the part before the backslash is the domain
        username = parts[1]; // the part after the backslash is the username
      }
      else {
        domain = SignOn.GetDomainName();
      }

      if (string.IsNullOrEmpty(username)) {
        return Content(
          HttpStatusCode.BadRequest,
          new { success = false, error = "Username must be provided.", domain = domain }
        );
      }

      // attempt to change the credentials for the user
      var result = ChangeCredentials(username, body.OldPassword, body.NewPassword, domain);
      var success = result.Item1;
      var errorMessage = result.Item2;

      if (success) {
        return Ok(new { success = true, username = username, domain = domain });
      }
      else {
        return Content(
          HttpStatusCode.BadRequest,
          new { success = false, error = errorMessage, domain = domain }
        );
      }
    }

    public static Tuple<bool, string> ChangeCredentials(string username, string oldPassword, string newPassword, string domain) {
      if (domain.Trim() == Environment.MachineName) {
        domain = null; // for local machine
      }

      string entryUrl;

      // if the user is on the local machine, we can use the WinNT provider to change the password
      if (string.IsNullOrEmpty(domain)) {
        entryUrl = "WinNT://" + Environment.MachineName + "/" + username + ",user";
      }
      // othwerwise, we need to find the user's distinguished name in the domain
      // so we can use the LDAP provider to change the password
      else {
        string userDistinguishedName = null;
        var ldapPath = "LDAP://" + domain;
        try {

          using (var searchRoot = new DirectoryEntry(ldapPath)) {
            using (var searcher = new DirectorySearcher(searchRoot)) {
              searcher.Filter = "(&(objectClass=user)(sAMAccountName=" + username + "))";
              searcher.PropertiesToLoad.Add("distinguishedName");

              var result = searcher.FindOne();
              if (result != null && result.Properties.Contains("distinguishedName")) {
                userDistinguishedName = result.Properties["distinguishedName"][0].ToString();
              }
            }
          }
        }
        catch (Exception) {
          return Tuple.Create(false, "The domain cannot be accessed.");
        }

        if (string.IsNullOrEmpty(userDistinguishedName)) {
          return Tuple.Create(false, "User could not be found in the domain: " + domain);
        }

        entryUrl = "LDAP://" + domain + "/" + userDistinguishedName;
      }

      // get the user's directory entry and then attempt to change the password
      using (var user = new DirectoryEntry(entryUrl)) {
        // if the user is not found, throw an exception
        if (user == null) {
          return Tuple.Create(false, "The user could not be found.");
        }

        // change the password
        {
          try {
            user.Invoke("ChangePassword", new object[] { oldPassword, newPassword });
            return Tuple.Create(true, (string)null);
          }
          catch (System.Reflection.TargetInvocationException ex) {
            // if the password change fails, return false with an error message
            if (ex.InnerException != null) {
              // if there is a constraint violation, try the PrincipalContext method
              if (ex.InnerException is DirectoryServicesCOMException) {
                try {
                  if (string.IsNullOrEmpty(domain)) {
                    using (var pc = new PrincipalContext(ContextType.Machine))
                    using (var userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, username)) {
                      userPrincipal.ChangePassword(oldPassword, newPassword);
                      userPrincipal.Save();
                      return Tuple.Create(true, (string)null);
                    }
                  }
                  else {
                    using (var pc = new PrincipalContext(ContextType.Domain, domain ?? Environment.MachineName))
                    using (var userPrincipal = UserPrincipal.FindByIdentity(pc, IdentityType.SamAccountName, username)) {
                      userPrincipal.ChangePassword(oldPassword, newPassword);
                      userPrincipal.Save();
                      return Tuple.Create(true, (string)null);
                    }
                  }
                }
                catch (Exception pEx) {
                  return Tuple.Create(false, pEx.Message);
                }
              }
              return Tuple.Create(false, ex.InnerException.Message);
            }
            throw; // rethrow if there is no inner exception - we don't know what went wrong
          }
          catch (Exception ex) {
            return Tuple.Create(false, ex.Message);
          }
        }
      }
    }
  }
}
