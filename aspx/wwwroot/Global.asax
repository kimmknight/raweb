<%@ Application Language="C#" %>
<%@ Import Namespace="System" %>
<%@ Import Namespace="System.Web" %>
<%@ Import Namespace="System.Web.Http" %>

<script runat="server">
    void Application_Start(object sender, EventArgs e)
    {
        GlobalConfiguration.Configure(RAWebServer.Api.WebApi.Register);
    }
</script>
