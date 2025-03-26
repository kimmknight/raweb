<%@ Page Language="C#" AutoEventWireup="true" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>RemoteApps - Signing out</title>
</head>
<body>
    <form id="form1" runat="server">
        <script runat="server">
            protected void Page_Load(object sender, EventArgs e)
            {
                // clear server-side session
                Session.Clear();
                Session.Abandon();

                // expire session cookie on client
                if (Request.Cookies["ASP.NET_SessionId"] != null)
                {
                    Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.Now.AddDays(-1);
                }

                // optionally, expire auth cookie too (if using FormsAuthentication)
                if (Request.Cookies[".ASPXAUTH"] != null)
                {
                    Response.Cookies[".ASPXAUTH"].Expires = DateTime.Now.AddDays(-1);
                }

                // redirect to login page
                Response.Redirect("~/app/login.aspx", true);
            }
        </script>
    </form>
</body>
</html>