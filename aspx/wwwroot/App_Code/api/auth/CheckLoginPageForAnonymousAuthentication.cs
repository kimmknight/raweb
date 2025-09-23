using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Web.Http;

namespace RAWebServer.Api {
  public partial class AuthController : ApiController {

    /// <summary>
    /// Checks if anaonymous authentication is enabled for the login URL.
    /// <br />
    /// This method sends a request to the login page URL and checks if it returns a successful response.
    /// </summary>
    /// <param name="loginPageUrl">The full URL to the login page. The origin must have the correct domain and port.</param>
    /// <returns>Whether anonymous authentication is enabled, or an error when HttpContext is unavailable</returns>
    [HttpGet]
    [Route("check-login-page-for-anonymous-authentication")]
    public IHttpActionResult CheckLoginPageForAnonymousAuthentication(string loginPageUrl) {
      // this method will fail if we do not have access the a web request context
      if (System.Web.HttpContext.Current == null) {
        return BadRequest("No HTTP context");
      }

      try {
        // accept all certificates since some users may not install the self-signed
        // certificate on all devices or use a globally trusted certificate
        ServicePointManager.ServerCertificateValidationCallback =
          delegate (object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            return true;
          };

        var request = (HttpWebRequest)WebRequest.Create(loginPageUrl);
        using (var response = (HttpWebResponse)request.GetResponse()) {
          return Ok(new { skip = true });
        }
      }
      catch (Exception) {
        return Ok(new { skip = false });
      }
    }
  }
}
