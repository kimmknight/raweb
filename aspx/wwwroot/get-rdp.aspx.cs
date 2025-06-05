using AuthUtilities;
using FileSystemUtilities;
using RegistryUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;

public partial class GetRDP : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        int permissionHttpStatus = 200;
        bool hasPermission = false;

        // retrieve the query string parameters
        string path = Request.QueryString["path"]; // relative path to the rdp file, or the name of the registry key
        string from = Request.QueryString["from"]; // rdp or registry

        // ensure the parameters are valid formats
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(from))
        {
            throw new ArgumentException("Parameters 'path' and 'from' are required.");
        }
        if (from != "rdp" && from != "registry")
        {
            throw new ArgumentException("Parameter 'from' must be either 'rdp' or 'registry'.");
        }

        // get authentication information
        AuthCookieHandler authCookieHandler = new AuthCookieHandler();
        UserInformation userInfo = authCookieHandler.GetUserInformationSafe(Request);

        // refuse to serve RDP files if the user is not authenticated
        if (userInfo == null)
        {
            Response.StatusCode = 401;
            Response.End();
            return;
        }

        // if it is an RDP file, serve it from the file system
        if (from == "rdp")
        {
            string filePath = Server.MapPath("~/" + path);
            if (!File.Exists(filePath))
            {
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Response.StatusCode = 404;
                    HttpContext.Current.Response.End();
                }
                throw new FileNotFoundException("The specified RDP file does not exist.", filePath);
            }

            // check that the user has permission to access the RDP file
            hasPermission = FileSystemUtilities.Reader.CanAccessPath(filePath, userInfo, out permissionHttpStatus);
            if (!hasPermission)
            {
                Response.StatusCode = permissionHttpStatus;
                Response.End();
                return;
            }

            // serve the RDP file
            Response.ContentType = "application/x-rdp";
            Response.WriteFile(filePath);
            Response.End();
            return;
        }

        // check that the user has permission to access the remoteapp in the registry
        hasPermission = RegistryUtilities.Reader.CanAccessRemoteApp(path, userInfo, out permissionHttpStatus);
        if (!hasPermission)
        {
            Response.StatusCode = permissionHttpStatus;
            Response.End();
        }

        // construct an RDP file from the values in the registry and serve it
        string rdpFileContents = RegistryUtilities.Reader.ConstructRdpFileFromRegistry(path);
        Response.ContentType = "application/x-rdp";
        Response.AddHeader("Content-Disposition", "attachment; filename=\"" + path + ".rdp\"");
        Response.Write(rdpFileContents.ToString());
        Response.End();
    }
}