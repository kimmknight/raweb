<%@ Page language="C#" %>
<%@ Import Namespace="System.Web.Security" %>
<script runat="server">
//useful fields: https://learn.microsoft.com/en-us/dotnet/api/system.web.httprequest.logonuseridentity?view=netframework-4.8.1
private string getLoginToken() {
    FormsAuthenticationTicket tkt;
    string token;
    string username = Request.LogonUserIdentity.Name;
    bool isPersistent = false;
    HttpCookie ck;
    tkt = new FormsAuthenticationTicket(1, username, DateTime.Now, DateTime.Now.AddMinutes(30), isPersistent , "");
    token = FormsAuthentication.Encrypt(tkt);
    return token;
}
</script>
<%
HttpContext.Current.Response.ContentType = "application/x-msts-webfeed-login; charset=utf-8";
HttpContext.Current.Response.Write(getLoginToken());
%>
