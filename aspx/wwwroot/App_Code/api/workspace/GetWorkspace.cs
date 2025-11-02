using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using RAWebServer.Utilities;

namespace RAWebServer.Api {
  public partial class WorkspaceController : ApiController {

    [HttpGet]
    [Route("")]
    [Route("~/webfeed.aspx")]
    [RequireAuthentication(RedirectUrl = "~/api/auth/authenticate-workspace")]
    public IHttpActionResult GetWorkspace(string terminalServer = "", string mergeTerminalServers = "0") {
      var request = HttpContext.Current.Request;

      var authCookieHandler = new AuthCookieHandler();
      var userInfo = authCookieHandler.GetUserInformationSafe(HttpContext.Current.Request);

      var authTicket = authCookieHandler.GetAuthTicket(HttpContext.Current.Request);

      var schemaVersion = WorkspaceBuilder.SchemaVersion.v1;
      var acceptHeader = request.Headers["accept"];
      if (!string.IsNullOrEmpty(acceptHeader)) {
        acceptHeader = acceptHeader.ToLowerInvariant();
        if (acceptHeader.Contains("radc_schema_version=2.0")) {
          schemaVersion = WorkspaceBuilder.SchemaVersion.v2;
        }
        else if (acceptHeader.Contains("radc_schema_version=2.1")) {
          schemaVersion = WorkspaceBuilder.SchemaVersion.v2_1;
        }
      }

      var resourcesFolder = "resources";
      var multiuserResourcesFolder = "multiuser-resources";
      var workspaceXml = new WorkspaceBuilder(
        schemaVersion,
        userInfo,
        HttpContext.Current.Request.Url.Host,
        mergeTerminalServers == "1",
        terminalServer
      ).GetWorkspaceXmlString(resourcesFolder, multiuserResourcesFolder);

      var contentType = schemaVersion >= WorkspaceBuilder.SchemaVersion.v2 ? "application/x-msts-radc+xml" : "text/xml";

      var response = new HttpResponseMessage(HttpStatusCode.OK) {
        Content = new StringContent(workspaceXml, Encoding.UTF8, contentType)
      };

      return ResponseMessage(response);
    }
  }
}
