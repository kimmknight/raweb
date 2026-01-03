using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using RAWeb.Server.Utilities;
using static RAWeb.Server.Utilities.ResourceContentsResolver;

namespace RAWebServer.Api {
  public partial class ResourceController : ApiController {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path">relative path to the rdp file, or the name of the registry key</param>
    /// <param name="from">rdp or registry</param>
    /// <returns></returns>
    [HttpGet]
    [Route("{*path}")]
    [Route("~/get-rdp.aspx")]
    [RequireAuthentication]
    public IHttpActionResult GetResource(string path, string from = "rdp") {
      // get authentication information
      var userInfo = UserInformation.FromHttpRequestSafe(HttpContext.Current.Request);

      // resolve the resource based on the parameters
      var resolved = ResolveResource(userInfo, path, from);

      // if the resource resolution failed, return the appropriate error response
      // (can fail due to permissions or invalid parameters)
      if (resolved is FailedResourceResult failed) {
        if (failed.ErrorMessage != null) {
          return ResponseMessage(Request.CreateErrorResponse(failed.PermissionHttpStatus, failed.ErrorMessage));
        }
        else {
          return ResponseMessage(Request.CreateResponse(failed.PermissionHttpStatus));
        }
      }

      // if the resource was resolved successfully, return the RDP file
      if (resolved is ResolvedResourceResult resolvedResult) {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(resolvedResult.RdpFileContents);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-rdp");
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = resolvedResult.FileName };
        return ResponseMessage(response);
      }

      throw new Exception("Unrecognized ResourceResult type.");
    }
  }
}
