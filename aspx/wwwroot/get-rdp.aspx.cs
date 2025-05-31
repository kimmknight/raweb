using AuthUtilities;
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
                throw new FileNotFoundException("The specified RDP file does not exist.", filePath);
            }

            Response.ContentType = "application/x-rdp";
            Response.WriteFile(filePath);
            Response.End();
            return;
        }

        // check that the user has permission to access the remoteapp in the registry
        bool hasPermission = Reader.CanAccessRemoteApp(path, userInfo);
        if (!hasPermission)
        {
            Response.StatusCode = 403;
            Response.End();
        }

        // construct an RDP file from the values in the registry and serve it
        string rdpFileContents = Reader.ConstructRdpFileFromRegistry(path);
        Response.ContentType = "application/x-rdp";
        Response.AddHeader("Content-Disposition", "attachment; filename=\"" + path + ".rdp\"");
        Response.Write(rdpFileContents.ToString());
        Response.End();
    }
}