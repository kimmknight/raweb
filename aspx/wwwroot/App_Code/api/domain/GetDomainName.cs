using RAWebServer.Utilities;
using System.Web.Http;

namespace RAWebServer.Api
{

  public partial class DomainInfoController : ApiController
  {
    [HttpGet]
    [Route("")]
    public IHttpActionResult GetDomainName()
    {
      string domain = SignOn.GetDomainName();
      return Ok(new { domain });
    }
  }
}
