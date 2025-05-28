<%@ Page language="C#" %>
<%@ Import Namespace="AuthUtilities" %>

<script runat="server">
    private void Login() {
        string authTicket = AuthUtilities.AuthCookieHandler.CreateAuthTicket(Request);
        HttpContext.Current.Response.ContentType = "application/x-msts-webfeed-login; charset=utf-8";
        HttpContext.Current.Response.Write(authTicket);
    }
</script>

<%
Login();
%>
