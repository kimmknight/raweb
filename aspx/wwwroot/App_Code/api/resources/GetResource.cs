using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;

namespace RAWebServer.Api
{
  public partial class ResourceController : ApiController
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="path">relative path to the rdp file, or the name of the registry key</param>
    /// <param name="from">rdp or registry</param>
    /// <returns></returns>
    [HttpGet]
    [Route("{*path}")]
    [RequireAuthentication]
    public IHttpActionResult GetImage(string path, string from = "rdp")
    {
      int permissionHttpStatus = 200;
      bool hasPermission = false;

      // ensure the parameters are valid formats
      if (from != "rdp" && from != "registry")
      {
        throw new ArgumentException("Parameter 'from' must be either 'rdp' or 'registry'.");
      }

      // get authentication information
      var authCookieHandler = new AuthUtilities.AuthCookieHandler();
      var userInfo = authCookieHandler.GetUserInformationSafe(HttpContext.Current.Request);

      // if it is an RDP file, serve it from the file system
      if (from == "rdp")
      {
        string root = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
        string filePath = Path.Combine(root, string.Format("{0}", path));
        if (!filePath.EndsWith(".rdp", StringComparison.OrdinalIgnoreCase))
        {
          filePath += ".rdp";
        }
        if (!File.Exists(filePath))
        {
          return ResponseMessage(Request.CreateErrorResponse(
            HttpStatusCode.NotFound,
            "The specified RDP file does not exist."
          ));
        }

        // check that the user has permission to access the RDP file
        hasPermission = FileSystemUtilities.Reader.CanAccessPath(filePath, userInfo, out permissionHttpStatus);
        if (!hasPermission)
        {
          return ResponseMessage(Request.CreateResponse((HttpStatusCode)permissionHttpStatus));
        }

        // serve the RDP file
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new ByteArrayContent(File.ReadAllBytes(filePath));
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-rdp");
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = Path.GetFileName(filePath) };
        return ResponseMessage(response);
      }

      // ensure the path is a valid registry key name
      if (path.Contains("\\") || path.Contains("/"))
      {
        return BadRequest("When 'from' is 'registry', 'path' must be the name of the registry key, not a file path.");
      }

      // check that the user has permission Wto access the remoteapp in the registry
      hasPermission = RegistryUtilities.Reader.CanAccessRemoteApp(path, userInfo, out permissionHttpStatus);
      if (!hasPermission)
      {
        return ResponseMessage(Request.CreateResponse((HttpStatusCode)permissionHttpStatus));
      }

      // construct an RDP file from the values in the registry and serve it
      string rdpFileContents = RegistryUtilities.Reader.ConstructRdpFileFromRegistry(path);
      var response2 = new HttpResponseMessage(HttpStatusCode.OK);
      response2.Content = new StringContent(rdpFileContents);
      response2.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-rdp");
      response2.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = path + ".rdp" };
      return ResponseMessage(response2); ;
    }
  }
}
