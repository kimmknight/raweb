using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Web.Http;


namespace RAWebServer.Api
{
  public partial class PoliciesController : ApiController
  {
    [HttpGet]
    [Route("")]
    [RequireLocalAdministrator]
    public IHttpActionResult GetAppSettings()
    {
      NameValueCollection appSettings = System.Configuration.ConfigurationManager.AppSettings;
      var settingsDict = new Dictionary<string, string>();
      foreach (string key in appSettings.AllKeys)
      {
        settingsDict.Add(key, appSettings[key]);
      }

      return Ok(settingsDict);
    }
  }
}
