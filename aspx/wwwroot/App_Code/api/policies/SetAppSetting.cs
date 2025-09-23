using System.Web.Http;


namespace RAWebServer.Api
{
  public partial class PoliciesController : ApiController
  {
    [HttpPost]
    [Route("{key}")]
    [RequireLocalAdministrator]
    public IHttpActionResult SetAppSetting(string key, [FromBody] string value)
    {
      bool shouldRemove = string.IsNullOrEmpty(value);

      System.Configuration.Configuration config = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~");

      if (shouldRemove)
      {
        if (config.AppSettings.Settings[key] != null)
        {
          config.AppSettings.Settings.Remove(key);
        }
      }
      else
      {
        if (config.AppSettings.Settings[key] == null)
        {
          config.AppSettings.Settings.Add(key, value);
        }
        else
        {
          config.AppSettings.Settings[key].Value = value;
        }
      }

      config.Save(System.Configuration.ConfigurationSaveMode.Modified);
      return Ok();
    }
  }
}
