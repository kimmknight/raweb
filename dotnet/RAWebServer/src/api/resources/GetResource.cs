using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using Microsoft.Win32;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

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
      // ensure the parameters are valid formats
      if (from != "rdp" && from != "registry" && from != "mr" && from != "registryDesktop") {
        throw new ArgumentException("Parameter 'from' must be either 'rdp', 'mr', 'registry', or 'registryDesktop'.");
      }

      // if the path starts with App_Data/, remove that part
      if (path.StartsWith("App_Data/", StringComparison.OrdinalIgnoreCase)) {
        path = path.Substring("App_Data/".Length);
      }

      // get authentication information
      var userInfo = UserInformation.FromHttpRequestSafe(HttpContext.Current.Request);

      int permissionHttpStatus;
      bool hasPermission;
      // if it is an RDP file, serve it from the file system
      if (from == "rdp") {
        var root = Constants.AppDataFolderPath;
        var filePath = Path.Combine(root, string.Format("{0}", path));
        if (!filePath.EndsWith(".rdp", StringComparison.OrdinalIgnoreCase)) {
          filePath += ".rdp";
        }
        if (!File.Exists(filePath)) {
          return ResponseMessage(Request.CreateErrorResponse(
            HttpStatusCode.NotFound,
            "The specified RDP file does not exist."
          ));
        }

        // check that the user has permission to access the RDP file
        hasPermission = FileAccessInfo.CanAccessPath(filePath, userInfo, out permissionHttpStatus);
        if (!hasPermission) {
          return ResponseMessage(Request.CreateResponse((HttpStatusCode)permissionHttpStatus));
        }

        // serve the RDP file
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new ByteArrayContent(File.ReadAllBytes(filePath));
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-rdp");
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = Path.GetFileName(filePath) };
        return ResponseMessage(response);
      }

      // if it is a managed .resource file, serve it from the file system
      if (from == "mr") {
        var rootedPath = Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, path));
        var resource = ManagedFileResource.FromResourceFile(rootedPath);
        if (resource == null) {
          return ResponseMessage(Request.CreateErrorResponse(
            HttpStatusCode.NotFound,
            "The specified managed resource file does not exist."
          ));
        }

        // check that the user has permission to access the managed resource file
        hasPermission = resource.SecurityDescriptor == null ||
                        resource.SecurityDescriptor.GetAllowedSids().Any(sid => userInfo.Sid == sid.ToString() || userInfo.Groups.Any(g => g.Sid == sid.ToString()));
        if (!hasPermission) {
          return ResponseMessage(Request.CreateResponse((HttpStatusCode)403));
        }

        // serve the RDP file
        var response1 = new HttpResponseMessage(HttpStatusCode.OK);
        response1.Content = new StringContent(resource.ToRdpFileStringBuilder().ToString());
        response1.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-rdp");
        response1.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = Path.GetFileNameWithoutExtension(path) + ".rdp" };
        return ResponseMessage(response1);
      }

      // if it is a registry desktop, construct the RDP file from the registry
      if (from == "registryDesktop") {
        // ensure the path is a valid registry key name
        if (path.Contains("\\") || path.Contains("/")) {
          return BadRequest("When 'from' is 'registryDesktop', 'path' must be the name of the registry key, not a file path.");
        }

        var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
        var centralizedPublishingCollectionName = AppId.ToCollectionName();
        if (!supportsCentralizedPublishing) {
          throw new Exception("Centralized Publishing is not enabled on this server.");
        }

        var desktopResource = SystemDesktop.FromRegistry(centralizedPublishingCollectionName, path);

        // check that the user has permission to access the remoteapp in the registry
        var registryKey = Registry.LocalMachine.OpenSubKey(desktopResource.collectionDesktopsRegistryPath + "\\" + path);
        hasPermission = RegistryReader.CanAccessRemoteApp(registryKey, userInfo, out permissionHttpStatus);
        if (!hasPermission) {
          return ResponseMessage(Request.CreateResponse((HttpStatusCode)permissionHttpStatus));
        }

        // construct an RDP file from the values in the registry and serve it
        var rdpFileContents3 = RegistryReader.ConstructRdpFileFromRegistry(path, isDesktop: true);
        var response3 = new HttpResponseMessage(HttpStatusCode.OK);
        response3.Content = new StringContent(rdpFileContents3);
        response3.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-rdp");
        response3.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = path + ".rdp" };
        return ResponseMessage(response3);
      }

      // ensure the path is a valid registry key name
      if (path.Contains("\\") || path.Contains("/")) {
        return BadRequest("When 'from' is 'registry', 'path' must be the name of the registry key, not a file path.");
      }

      // check that the user has permission to access the remoteapp in the registry
      hasPermission = RegistryReader.CanAccessRemoteApp(path, userInfo, out permissionHttpStatus);
      if (!hasPermission) {
        return ResponseMessage(Request.CreateResponse((HttpStatusCode)permissionHttpStatus));
      }

      // construct an RDP file from the values in the registry and serve it
      var rdpFileContents = RegistryReader.ConstructRdpFileFromRegistry(path);
      var response2 = new HttpResponseMessage(HttpStatusCode.OK);
      response2.Content = new StringContent(rdpFileContents);
      response2.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-rdp");
      response2.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = path + ".rdp" };
      return ResponseMessage(response2);
    }
  }
}
