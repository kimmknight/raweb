using System.Web.Http;
using RAWeb.Server.Utilities;

namespace RAWebServer.Api {
  public partial class DomainInfoController : ApiController {
    [HttpGet]
    [Route("")]
    public IHttpActionResult GetDomainName() {
      var domain = SignIn.GetDomainName();
      return Ok(new { domain });
    }
  }
}
