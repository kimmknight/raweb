using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web.Http;


namespace RAWebServer.Api {
  public partial class PoliciesController : ApiController {
    [HttpGet]
    [Route("")]
    [RequireLocalAdministrator]
    public IHttpActionResult GetAppSettings() {
      var appSettings = System.Configuration.ConfigurationManager.AppSettings;
      var settingsDict = new Dictionary<string, string>();
      foreach (var key in appSettings.AllKeys) {
        settingsDict.Add(key, appSettings[key]);
      }

      return Ok(settingsDict);
    }
  }
}
