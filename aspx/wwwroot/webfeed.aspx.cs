using System;
using System.Linq;
using System.Web;
using RAWebServer.Utilities;

public partial class GetWorkspace : System.Web.UI.Page {
    protected void Page_Load(object sender, EventArgs e) {
        var userInfo = new AuthCookieHandler().GetUserInformationSafe(Request);
        var schemaVersion = WorkspaceBuilder.SchemaVersion.v1;
        var searchParams = HttpUtility.ParseQueryString(HttpContext.Current.Request.Url.Query);
        var resolver = new AliasResolver();

        if (userInfo == null) {
            Response.Redirect("auth/loginfeed.aspx");
        }

        else {
            if (HttpContext.Current.Request.Headers.GetValues("accept").FirstOrDefault().ToLower().Contains("radc_schema_version=2.0")) {
                schemaVersion = WorkspaceBuilder.SchemaVersion.v2;
            }
            else if (HttpContext.Current.Request.Headers.GetValues("accept").FirstOrDefault().ToLower().Contains("radc_schema_version=2.1")) {
                schemaVersion = WorkspaceBuilder.SchemaVersion.v2_1;
            }

            // process resources
            var resourcesFolder = "App_Data/resources";
            var multiuserResourcesFolder = "App_Data/multiuser-resources";
            var workspaceBuilder = new WorkspaceBuilder(
                schemaVersion,
                userInfo,
                HttpContext.Current.Request.Url.Host,
                searchParams["mergeTerminalServers"] == "1",
                searchParams["terminalServer"]
            );
            HttpContext.Current.Response.ContentType = schemaVersion >= WorkspaceBuilder.SchemaVersion.v2 ? "application/x-msts-radc+xml; charset=utf-8" : "text/xml; charset=utf-8";
            HttpContext.Current.Response.Write(workspaceBuilder.GetWorkspaceXmlString(resourcesFolder, multiuserResourcesFolder));
            HttpContext.Current.Response.End();
            return;
        }
    }
}
