using System.Web.Http;

namespace RAWebServer.Api
{

  public partial class DomainInfoController : ApiController
  {
    [HttpGet]
    [Route("")]
    public IHttpActionResult GetDomainName()
    {
      string domain = AuthUtilities.SignOn.GetDomainName();
      return Ok(new { domain });
    }
  }
}
