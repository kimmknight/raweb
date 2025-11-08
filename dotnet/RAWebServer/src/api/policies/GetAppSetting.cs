using System.Web.Http;
using RAWeb.Server.Utilities;

namespace RAWebServer.Api {
  public partial class PoliciesController : ApiController {
    [HttpGet]
    [Route("{key}")]
    [RequireLocalAdministrator]
    public IHttpActionResult GetAppSetting(string key) {
      var value = PoliciesManager.RawPolicies[key];
      return Ok(new { key, value });
    }
  }
}
