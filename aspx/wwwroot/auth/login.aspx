<%@ Page language="C#" %>
<%@ Import Namespace="AuthUtilities" %>

<script runat="server">
    private void Login() {
        var authCookieHandler = new AuthUtilities.AuthCookieHandler();
        authCookieHandler.SetAuthCookie(Request, Response);
    }
</script>

<%
Login();
%>
