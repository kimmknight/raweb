using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using RAWebServer.Utilities;

namespace RAWebServer.Api {
  public partial class AuthController : ApiController {
    [HttpPost]
    [Route("authenticate")]
    public IHttpActionResult Authenticate([FromBody] ValidateCredentialsBody body) {
      var forceAnon = System.Configuration.ConfigurationManager.AppSettings["App.Auth.Anonymous"] == "always" ||
                      (System.Configuration.ConfigurationManager.AppSettings["App.Auth.Anonymous"] == "allow" && body.Username == "RAWEB\\anonymous");
      if (forceAnon) {
        var userInfo = new UserInformation("S-1-4-447-1", "anonymous", "RAWEB", "Anonymous User", new GroupInformation[0]);
        var anonEncryptedToken = AuthCookieHandler.CreateAuthTicket(userInfo);
        return CreateAuthCookieResponse("anonymous", "RAWEB", anonEncryptedToken);
      }

      var username = body.Username;
      var password = body.Password;

      // if the username contains a domain, split it to get the username and domain separately
      string domain;
      if (username.Contains("\\")) {
        var parts = username.Split(new[] { '\\' }, 2);
        domain = parts[0]; // the part before the backslash is the domain
        username = parts[1]; // the part after the backslash is the username
      }
      else {
        domain = SignOn.GetDomainName();
      }

      try {
        // check if the username and password are valid for the domain
        using (var userToken = SignOn.ValidateCredentials(username, password, domain)) {
          var encryptedToken = AuthCookieHandler.CreateAuthTicket(userToken.DangerousGetHandle());
          return CreateAuthCookieResponse(username, domain, encryptedToken);
        }
      }
      catch (ValidateCredentialsException ex) {
        return Content(HttpStatusCode.Unauthorized, new {
          success = false,
          error = ex.Message,
          domain = domain
        });
      }
    }

    private IHttpActionResult CreateAuthCookieResponse(string username, string domain, string encryptedToken) {
      var authCookieHandler = new AuthCookieHandler();
      var cookie = authCookieHandler.CreateAuthTicketCookie(encryptedToken);

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
