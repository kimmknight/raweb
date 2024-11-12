<%@ Page language="C#" %>
<%@ Import Namespace="System.Web.Security" %>
<script runat="server">
//useful fields: https://learn.microsoft.com/en-us/dotnet/api/system.web.httprequest.logonuseridentity?view=netframework-4.8.1
private void Login() {
    FormsAuthenticationTicket tkt;
    string cookiestr;
    string username = Request.LogonUserIdentity.Name;
    if(username.IndexOf("\\")>0) {
        string authDomain = username.Substring(0,username.IndexOf("\\"));
        if(HttpContext.Current.Server.MachineName == authDomain)
            username = username.Substring(authDomain.Length+1);
    }
    bool isPersistent = false;
    HttpCookie ck;
    string groupSids = "";
    System.Security.Principal.IdentityReferenceCollection groups = Request.LogonUserIdentity.Groups;
    foreach(System.Security.Principal.IdentityReference sid in groups) {
        groupSids+=(groupSids.Length>0?",":"")+sid.ToString();
    }
    tkt = new FormsAuthenticationTicket(1, username, DateTime.Now, DateTime.Now.AddMinutes(30), isPersistent , groupSids);
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
