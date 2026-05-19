using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using RAWeb.Server.Utilities;

namespace RAWebServer.Api {
  public partial class InjectFolderFilesController : ApiController {
    [HttpGet]
    [Route("file/{*relativeFilePath}")]
    [RequireAuthentication]
    public IHttpActionResult GetImage(string relativeFilePath) {
      // get authentication information
      var userInfo = UserInformation.FromHttpRequestSafe(HttpContext.Current.Request);

      if (string.IsNullOrEmpty(relativeFilePath) || string.IsNullOrEmpty(relativeFilePath)) {
        return BadRequest("Missing relative file path parameter.");
      }

      string rootedFilePath;
      if (relativeFilePath == "index.js" || relativeFilePath == "index.css") {
        rootedFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "inject", relativeFilePath);
      }
      else {
        rootedFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "inject", "filestore", relativeFilePath);
      }
      if (!File.Exists(rootedFilePath)) {
        return NotFound();
      }

      // check whether the user has access to the file
      var hasPermission = FileAccessInfo.CanAccessPath(rootedFilePath, userInfo, out var permissionHttpStatus);
      if (!hasPermission) {
        return ResponseMessage(Request.CreateResponse(permissionHttpStatus));
      }

      // serve the file with the correct content type
      var fileBytes = File.ReadAllBytes(rootedFilePath);
      var contentType = MimeMapping.GetMimeMapping(rootedFilePath);
      var result = new HttpResponseMessage(HttpStatusCode.OK) {
        Content = new ByteArrayContent(fileBytes)
      };
      result.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
      return ResponseMessage(result);
    }
  }
}
