using System.Web.Http;
using RAWeb.Server.Utilities;


namespace RAWebServer.Api {
  public partial class PoliciesController : ApiController {
    [HttpPost]
    [Route("{key}")]
    [RequireLocalAdministrator]
    public IHttpActionResult SetAppSetting(string key, [FromBody] string value) {
      PoliciesManager.Set(key, value);
      return Ok();
    }
  }
}
