using System.Web.Http;
using RAWeb.Server.Utilities;


namespace RAWebServer.Api {
  public partial class PoliciesController : ApiController {
    [HttpGet]
    [Route("")]
    [RequireLocalAdministrator]
    public IHttpActionResult GetAppSettings() {
      return Ok(PoliciesManager.RawPolicies.Value);
    }
  }
}
