<%@ Application Language="C#" %>
<%@ Import Namespace="System.Web.Http" %>

<script runat="server">
    void Application_Start(object sender, EventArgs e)
    {
        RAWeb.Server.Utilities.AppId.Initialize();
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        GlobalConfiguration.Configure(RAWebServer.Api.WebApi.Register);
    }
</script>
