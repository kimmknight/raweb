using System.Web.Http;
using RAWebServer.Utilities;

namespace RAWebServer.Api {
  public partial class AuthController : ApiController {
    public class ValidateCredentialsBody {
      public string Username { get; set; }
      public string Password { get; set; }
    }

    [HttpPost]
    [Route("validate")]
    public IHttpActionResult ValidateCredentials([FromBody] ValidateCredentialsBody body) {
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

      // check if the username and password are valid for the domain
      var result = SignOn.ValidateCredentials(username, password, domain);
      var credentialsAreValid = result.Item1;
      var errorMessage = result.Item2;

      if (credentialsAreValid) {
        return Ok(new { success = true, username = username, domain = domain });
      }
      else {
        return Ok(new { success = false, error = result.Item2, domain = domain });
      }
    }
  }
}
