<%@ Page language="C#" %>
<%@ Import Namespace="System.Web.Security" %>
<script runat="server">
//useful fields: https://learn.microsoft.com/en-us/dotnet/api/system.web.httprequest.logonuseridentity?view=netframework-4.8.1
private void Login() {
    FormsAuthenticationTicket tkt;
    string cookiestr;
    string username = Request.LogonUserIdentity.Name;
    bool isPersistent = false;
    HttpCookie ck;
    tkt = new FormsAuthenticationTicket(1, username, DateTime.Now, DateTime.Now.AddMinutes(30), isPersistent , "");
    cookiestr = FormsAuthentication.Encrypt(tkt);
    ck = new HttpCookie(".ASPXAUTH", cookiestr);
    ck.Path = FormsAuthentication.FormsCookiePath;
    Response.Cookies.Add(ck);

    Response.Redirect(FormsAuthentication.GetRedirectUrl(username, isPersistent));

}
</script>
<%
Login();
%>
