using System.Web.Http;
using RAWebServer.Utilities;

namespace RAWebServer.Api {
  public partial class DomainInfoController : ApiController {
    [HttpGet]
    [Route("")]
    public IHttpActionResult GetDomainName() {
      var domain = SignOn.GetDomainName();
      return Ok(new { domain });
    }
  }
}
