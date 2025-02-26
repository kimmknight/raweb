<%@ Page language="C#" %>
<%@ Import Namespace="System.Web.Security" %>
<script runat="server">

private string [] sidToDomainMap;
private string [] knownSidPrefixes;
private string getDomainMap(string sidPrefix) {
    int num = Array.IndexOf(knownSidPrefixes,sidPrefix);
    if(num<0) {
        knownSidPrefixes = knownSidPrefixes.Append(sidPrefix).ToArray();
        sidToDomainMap = sidToDomainMap.Append("P" + knownSidPrefixes.Length+"-").ToArray();
        return sidToDomainMap.GetValue(knownSidPrefixes.Length-1).ToString();
    }
    return sidToDomainMap.GetValue(num).ToString();
}
//useful fields: https://learn.microsoft.com/en-us/dotnet/api/system.web.httprequest.logonuseridentity?view=netframework-4.8.1
private string getLoginToken() {
    FormsAuthenticationTicket tkt;
    string token;
    string username = Request.LogonUserIdentity.Name;
    string groupSids = "";
    string mappings = "";
    knownSidPrefixes = new String[0];
    sidToDomainMap = new String[0];
    System.Security.Principal.IdentityReferenceCollection groups = Request.LogonUserIdentity.Groups;
    foreach(System.Security.Principal.IdentityReference sid in groups) {
        string strSid = sid.ToString();
        if(strSid.StartsWith("S-1-5-21-")) {
            string mapped = getDomainMap(strSid.Substring(0,strSid.LastIndexOf("-")));
            strSid = mapped + strSid.Substring(strSid.LastIndexOf("-")+1);
        }
        groupSids+=(groupSids.Length>0?",":"")+strSid;
    }
    for(int i=0;i<knownSidPrefixes.Length;i++) {
        mappings += (mappings.Length>0?",":"") + knownSidPrefixes.GetValue(i).ToString().Substring(9)+"="+sidToDomainMap.GetValue(i).ToString();
    }
    bool isPersistent = false;
    string tokenset = mappings + ";" + groupSids;
    while (tokenset.Length>2048) {
        tokenset = tokenset.Substring(0,2048);
        tokenset = tokenset.Substring(tokenset.LastIndexOf(','));
    }
    tkt = new FormsAuthenticationTicket(1, username, DateTime.Now, DateTime.Now.AddMinutes(30), isPersistent , tokenset);
    token = FormsAuthentication.Encrypt(tkt);
    return token;
}

private void Login() {
    HttpCookie ck;
    ck = new HttpCookie(".ASPXAUTH", getLoginToken());
    ck.Path = FormsAuthentication.FormsCookiePath;
    Response.Cookies.Add(ck);

    Response.Redirect(FormsAuthentication.GetRedirectUrl("",false));

}
</script>
<%
Login();
%>
