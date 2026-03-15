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
      public string ReturnUrl { get; set; } // nullable
    }

    [HttpPost]
    [Route("authenticate")]
    public IHttpActionResult Authenticate([FromBody] ValidateCredentialsBody body) {
      if (ShouldAuthenticateAnonymously(body.Username)) {
        var ticket = AuthTicket.FromUserInformation(UserInformation.AnonymousUser);
        return CreateAuthCookieResponse("anonymous", "RAWEB", ticket);
      }

      var credentials = new ParsedCredentialsBody(body.Username, body.Password);

      try {
        // check if the username and password are valid for the domain
        using (var userToken = SignIn.ValidateCredentials(credentials.Username, credentials.Password, credentials.Domain)) {
          var ticket = AuthTicket.FromLogonToken(userToken.DangerousGetHandle());

          // update the the credentials to have the correct case for username and domain
          var userInfo = UserInformation.FromDownLevelLogonName(ticket.Name);
          credentials.Username = userInfo.Username;
          credentials.Domain = userInfo.Domain;

          // if MFA is enabled, redirect to the MFA prompt instead of setting the auth cookie directly
          var mfaResult = TriggerMultiFactorAuthenticationPrompt(credentials, body.ReturnUrl);
          if (mfaResult != null) {
            return mfaResult;
          }

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

    /// <summary>
    /// Reads the response from Duo MFA and creates an authentication cookie if
    /// the response indicates a successful authentication and contains a valid
    /// state (which should be an absolute URL to redirect to after setting the auth cookie).
    /// </summary>
    /// <param name="state"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("duo/callback")]
    public IHttpActionResult DuoCallback([FromUri] string state, [FromUri] string code) {
      var domain = state.Split('‾')[0]; // the user's domain is prepended to the state with a delimiter

      try {
        var duoPolicy = PoliciesManager.GetDuoMfaPolicyForDomain(domain);
        var redirectPath = System.Web.VirtualPathUtility.ToAbsolute("~" + duoPolicy.RedirectPath); // append the path prefix for RAWeb in IIS
        var duoAuth = new DuoAuth(duoPolicy.ClientId, duoPolicy.SecretKey, duoPolicy.Hostname, redirectPath);

        var result = duoAuth.VerifyResponse(code, state);
        var userInfo = UserInformation.FromDownLevelLogonName(domain + "\\" + result.Username);
        var ticket = AuthTicket.FromUserInformation(userInfo);

        return CreateAuthCookieResponse(userInfo.Username, userInfo.Domain, ticket, result.ReturnUrl);
      }
      catch (Exception ex) {
        return Content(HttpStatusCode.OK, ex.Message);
      }
    }

    /// <summary>
    /// Reads the response from LoginTC MFA and creates an authentication cookie if
    /// the response indicates a successful authentication and contains a valid
    /// state (which should be an absolute URL to redirect to after setting the auth cookie).
    /// </summary>
    /// <param name="state"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("logintc/callback")]
    public IHttpActionResult LoginTCCallback([FromUri] string state, [FromUri] string code = null, [FromUri] string error = null, [FromUri] string error_description = null) {

      var domain = state.Split('‾')[0]; // the user's domain is prepended to the state with a delimiter

      try {
        var loginTcPolicy = PoliciesManager.GetLoginTCMfaPolicyForDomain(domain);
        var redirectPath = System.Web.VirtualPathUtility.ToAbsolute("~" + loginTcPolicy.RedirectPath); // append the path prefix for RAWeb in IIS
        var loginTcAuth = new LoginTCAuth(loginTcPolicy.ClientId, loginTcPolicy.SecretKey, loginTcPolicy.Hostname, redirectPath);

        if (error == "access_denied" && error_description == "User cancelled authentication") {
          var returnUrlPart = state.Split('‾')[1]; // the return URL is appended to the state with a delimiter

          try {
            // redirect to the logout page
            var returnUri = new UriBuilder(returnUrlPart) {
              Path = System.Web.VirtualPathUtility.ToAbsolute("~/logoff")
            }.Uri;
            var response = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect);
            response.Headers.Location = returnUri;
            return ResponseMessage(response);
          }
          catch (UriFormatException) {
            // if the return URL is not a valid absolute URI, use the regular error response instead of redirecting
          }
        }

        if (error != null) {
          return Content(HttpStatusCode.OK, error_description);
        }

        var result = loginTcAuth.VerifyResponse(code, state);
        var userInfo = UserInformation.FromDownLevelLogonName(domain + "\\" + result.Username);
        var ticket = AuthTicket.FromUserInformation(userInfo);

        return CreateAuthCookieResponse(userInfo.Username, userInfo.Domain, ticket, result.ReturnUrl);
      }
      catch (Exception ex) {
        return Content(HttpStatusCode.OK, ex.Message);
      }
    }

    /// <summary>
    /// Triggers the multi-factor authentication prompt if at least one MFA provider is configured.
    /// <br /><br />
    /// If no MFA providers are configured, this method returns null.
    /// </summary>
    /// <param name="credentials"></param>
    /// <param name="ReturnUrl">The absolute URL that the web app should go to after MFA is completed.</param>
    /// <returns></returns>
    private IHttpActionResult TriggerMultiFactorAuthenticationPrompt(ParsedCredentialsBody credentials, string returnUrl) {
      // if Duo MFA is enabled, redirect to Duo authorization endpoint
      var duoPolicy = PoliciesManager.GetDuoMfaPolicyForDomain(credentials.Domain, credentials.Username);
      if (duoPolicy != null) {
        try {
          var redirectPath = System.Web.VirtualPathUtility.ToAbsolute("~" + duoPolicy.RedirectPath); // append the path prefix for RAWeb in IIS
          var duoAuth = new DuoAuth(duoPolicy.ClientId, duoPolicy.SecretKey, duoPolicy.Hostname, redirectPath);
          duoAuth.DoHealthCheck();
          var redirectUrl = duoAuth.GetRequestAuthorizationEndpoint(credentials.Domain, credentials.Username, returnUrl);
          return Content(HttpStatusCode.OK, new {
            success = true,
            username = credentials.Username,
            mfa_redirect = redirectUrl,
            domain = credentials.Domain
          });
        }
        catch (Exception ex) {
          System.Diagnostics.Debug.WriteLine("Duo health check or authorization request failed: " + ex.Message);
          return Content(HttpStatusCode.OK, new {
            success = false,
            error = ex.Message,
            domain = credentials.Domain
          });
        }
      }

      // if LoginTC MFA is enabled, redirect to LoginTC authorization endpoint
      var loginTcPolicy = PoliciesManager.GetLoginTCMfaPolicyForDomain(credentials.Domain, credentials.Username);
      if (loginTcPolicy != null) {
        try {
          var redirectPath = System.Web.VirtualPathUtility.ToAbsolute("~" + loginTcPolicy.RedirectPath); // append the path prefix for RAWeb in IIS
          var loginTcAuth = new LoginTCAuth(loginTcPolicy.ClientId, loginTcPolicy.SecretKey, loginTcPolicy.Hostname, redirectPath);
          loginTcAuth.DoPing();
          var redirectUrl = loginTcAuth.GetRequestAuthorizationEndpoint(credentials.Domain, credentials.Username, returnUrl);
          return Content(HttpStatusCode.OK, new {
            success = true,
            username = credentials.Username,
            mfa_redirect = redirectUrl,
            domain = credentials.Domain
          });
        }
        catch (Exception ex) {
          System.Diagnostics.Debug.WriteLine("LoginTC ping or authorization request failed: " + ex.Message);
          return Content(HttpStatusCode.OK, new {
            success = false,
            error = ex.Message,
            domain = credentials.Domain
          });
        }
      }

      return null;
    }

    private bool ShouldAuthenticateAnonymously(string username) {
      var anonSetting = PoliciesManager.RawPolicies["App.Auth.Anonymous"];
      return anonSetting == "always" || (anonSetting == "allow" && username == "RAWEB\\anonymous");
    }

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

    private IHttpActionResult CreateAuthCookieResponse(string username, string domain, AuthTicket ticket, string returnUrl = null) {
      var cookiePath = System.Web.VirtualPathUtility.ToAbsolute("~/"); // set the path to the application root
      var cookie = ticket.ToCookie(cookiePath);

      var cookieHeader = new CookieHeaderValue(cookie.Name, cookie.Value) {
        Path = cookie.Path,
        HttpOnly = cookie.HttpOnly,
        Secure = cookie.Secure,
        Expires = cookie.Expires == DateTime.MinValue ? (DateTimeOffset?)null : cookie.Expires
      };

      var responseStatus = string.IsNullOrEmpty(returnUrl) ? HttpStatusCode.OK : HttpStatusCode.Found;

      var response = new HttpResponseMessage(responseStatus);
      response.Headers.AddCookies(new[] { cookieHeader });
      response.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new {
        success = true,
        username = username,
        domain = domain
      }));

      if (!string.IsNullOrEmpty(returnUrl)) {
        response.Headers.Location = new Uri(returnUrl, UriKind.RelativeOrAbsolute);
      }

      return ResponseMessage(response);
    }

  }
}
