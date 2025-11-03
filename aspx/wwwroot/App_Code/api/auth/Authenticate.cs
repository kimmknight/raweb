using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using RAWeb.Server.Utilities;

namespace RAWebServer.Api {
  public partial class AuthController : ApiController {
    public class ValidateCredentialsBody {
      public string Username { get; set; }
      public string Password { get; set; }
    }

    [HttpPost]
    [Route("authenticate")]
    public IHttpActionResult Authenticate([FromBody] ValidateCredentialsBody body) {
      if (ShouldAuthenticateAnonymously(body.Username)) {
        var ticket = AuthTicket.FromUserInformation(s_anonUserInfo);
        return CreateAuthCookieResponse("anonymous", "RAWEB", ticket);
      }

      var credentials = new ParsedCredentialsBody(body.Username, body.Password);

      try {
        // check if the username and password are valid for the domain
        using (var userToken = SignIn.ValidateCredentials(credentials.Username, credentials.Password, credentials.Domain)) {
          var ticket = AuthTicket.FromLogonToken(userToken.DangerousGetHandle());
          return CreateAuthCookieResponse(credentials.Username, credentials.Domain, ticket);
        }
      }
      catch (ValidateCredentialsException ex) {
        return Content(HttpStatusCode.OK, new {
          success = false,
          error = ex.Message,
          domain = credentials.Domain
        });
      }
    }

    private bool ShouldAuthenticateAnonymously(string username) {
      var anonSetting = PoliciesManager.RawPolicies["App.Auth.Anonymous"];
      return anonSetting == "always" || (anonSetting == "allow" && username == "RAWEB\\anonymous");
    }

    private static readonly UserInformation s_anonUserInfo = new UserInformation("S-1-4-447-1", "anonymous", "RAWEB", "Anonymous User", new GroupInformation[0]);

    private class ParsedCredentialsBody {
      public string Domain { get; set; }
      public string Username { get; set; }
      public string Password { get; set; }

      public ParsedCredentialsBody(string username, string password) {
        Password = password;

        // if the username contains a domain, split it to get the username and domain separately
        if (username.Contains("\\")) {
          var parts = username.Split(new[] { '\\' }, 2);
          Domain = parts[0]; // the part before the backslash is the domain
          Username = parts[1]; // the part after the backslash is the username
        }
        else {
          Domain = SignIn.GetDomainName();
          Username = username;
        }
      }
    }

    private IHttpActionResult CreateAuthCookieResponse(string username, string domain, AuthTicket ticket) {
      var cookiePath = System.Web.VirtualPathUtility.ToAbsolute("~/"); // set the path to the application root
      var cookie = ticket.ToCookie(cookiePath);

      var cookieHeader = new CookieHeaderValue(cookie.Name, cookie.Value) {
        Path = cookie.Path,
        HttpOnly = cookie.HttpOnly,
        Secure = cookie.Secure,
        Expires = cookie.Expires == DateTime.MinValue ? (DateTimeOffset?)null : cookie.Expires
      };

      var response = new HttpResponseMessage(HttpStatusCode.OK);
      response.Headers.AddCookies(new[] { cookieHeader });
      response.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new {
        success = true,
        username = username,
        domain = domain
      }));

      return ResponseMessage(response);
    }
  }
}
