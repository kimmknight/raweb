<%@ Page language="C#" %>
<%@ Import Namespace="RAWebServer.Utilities" %>

<script runat="server">
    private void Login() {
        var authCookieHandler = new AuthCookieHandler();
        authCookieHandler.SetAuthCookie(Request, Response);
    }
</script>

<%
Login();
%>
