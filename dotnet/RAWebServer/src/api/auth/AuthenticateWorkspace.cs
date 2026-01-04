using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using RAWeb.Server.Utilities;

namespace RAWebServer.Api {
  public partial class AuthController : ApiController {
    [HttpGet]
    [ConditionalAuthorize] // require authorization from IIS unless App.Auth.Anonymous is set to always -- Windows Authentication must be enabled
    [Route("authenticate-workspace")]
    [Route("~/auth/loginfeed.aspx")]
    public IHttpActionResult AuthenticateWorkspace() {
      // check if workspace authentication is blocked via policy
      var blockWorkspaceAuth = PoliciesManager.RawPolicies["WorkspaceAuth.Block"] == "true";
      if (blockWorkspaceAuth) {
        return Content(HttpStatusCode.Forbidden, new {
          success = false,
          error = "Workspace client authentication is blocked by policy."
        });
      }

      if (ShouldAuthenticateAnonymously(null)) {
        var anonTicket = AuthTicket.FromUserInformation(UserInformation.AnonymousUser);
        return CreateWorkspaceAuthResponse(anonTicket);
      }

      var ticket = AuthTicket.FromHttpRequestIdentity(HttpContext.Current.Request);
      return CreateWorkspaceAuthResponse(ticket);
    }

    private IHttpActionResult CreateWorkspaceAuthResponse(AuthTicket ticket) {
      var response = new HttpResponseMessage(HttpStatusCode.OK);
      response.Content = new StringContent(ticket.ToEncryptedToken());
      response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-msts-webfeed-login") {
        CharSet = "utf-8"
      };
      return ResponseMessage(response);
    }
  }
}
