<%@ Application Language="C#" %>
<%@ Import Namespace="System.Web.Http" %>

<script runat="server">
    void Application_Start(object sender, EventArgs e)
    {
        RAWeb.Server.Utilities.AppId.Initialize();
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        GlobalConfiguration.Configure(RAWebServer.Api.WebApi.Register);
    }
    void Application_Stop(object sender, EventArgs e)
    {
        RAWeb.Server.Utilities.Guacd.Stop();
        RAWeb.Server.Utilities.Guacd.UninstallGuacd();
    }
</script>
