using System.Web.Http;

namespace RAWebServer.Api
{
  public static class WebApi
  {
    public static void Register(HttpConfiguration config)
    {
      // enable attribute routing
      config.MapHttpAttributeRoutes();

      // disable XML formatting
      config.Formatters.Remove(config.Formatters.XmlFormatter);
    }
  }
}
