<%@ Application Language="C#" %>
<%@ Import Namespace="System.Web.Http" %>

<script runat="server">
    void Application_Start(object sender, EventArgs e)
    {
        RAWeb.Server.Utilities.AppId.Initialize();
        GlobalConfiguration.Configure(RAWebServer.Api.WebApi.Register);
    }
</script>
