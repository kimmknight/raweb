<%@ Page language="C#" explicit="true"  %>
<script runat="server">
    public string GetRDPvalue(string eachfile, string valuename)
    {
        string GetRDPvalue = "";
        string fileline = "";

        string theName = "";
        int valuenamelen = 0;
        System.IO.StreamReader contentfile = new System.IO.StreamReader(eachfile);
        valuenamelen = valuename.Length;
        while ((fileline = contentfile.ReadLine()) != null)
        {
            if (fileline.Length >= valuenamelen)
            {
                string filelineleft = fileline.Substring(0, valuenamelen);
                if ((filelineleft.ToLower() == valuename))
                {
                    theName = fileline.Substring(valuenamelen, fileline.Length - valuenamelen);
                }
            }
        }
        GetRDPvalue = theName.Replace("|", "");
        return GetRDPvalue;
    }

    public string getAuthenticatedUser() {
        HttpCookie authCookie = HttpContext.Current.Request.Cookies[".ASPXAUTH"];
        if(authCookie == null || authCookie.Value == "") return "";
        try {
            // Decrypt may throw an exception if authCookie.Value is total gargbage
            FormsAuthenticationTicket authTicket = FormsAuthentication.Decrypt(authCookie.Value);
            if(authTicket==null) {
                return "";
            }
            return authTicket.Name;
        }
        catch {
            return "";
        }
    }

    public string[] getAuthenticatedUserGroups() {
        HttpCookie authCookie = HttpContext.Current.Request.Cookies[".ASPXAUTH"];
        if(authCookie == null || authCookie.Value == "") return new string[0];
        try {
            // Decrypt may throw an exception if authCookie.Value is total gargbage
            FormsAuthenticationTicket authTicket = FormsAuthentication.Decrypt(authCookie.Value);
            if(authTicket==null) {
                return new string[0];
            }
            string [] sids = authTicket.UserData.Split(',');
            string [] groups = new string[sids.Length];
            for(int pos=0;pos<sids.Length;pos++) {
                string group = new System.Security.Principal.SecurityIdentifier(sids[pos].ToString()).Translate(typeof(System.Security.Principal.NTAccount)).ToString();
                if(group.IndexOf("\\")>0) {
                    string authDomain = group.Substring(0,group.IndexOf("\\"));
                    if(HttpContext.Current.Server.MachineName == authDomain || authDomain == "BUILTIN")
                        group = group.Substring(authDomain.Length+1);
                }
                groups.SetValue(group,pos);
            }
            return groups;
        }
        catch {
            return new string[0];
        }
    }
</script>
<%
  string authUser = getAuthenticatedUser();
  if(authUser=="") {
     Response.Redirect("auth/login.aspx?ReturnUrl="+Uri.EscapeUriString(HttpContext.Current.Request.Url.AbsolutePath));
  }
  else {
    string [] authUserGroups = getAuthenticatedUserGroups();
%>
<html>
<head>
<title>
RAWeb - Remote Applications
</title>
<style type="text/css">
a:link {color:#444444;text-decoration:none;}
a:visited {color:#444444;text-decoration:none;}
a:hover{color:#000000;text-decoration:underline;}
a:active {color:#000000;text-decoration:underline;}
   #apptile
{
width:150px;
height:135px;
text-align:center;
vertical-align:bottom;
border-style:solid;
border-width:0px;
float:left;
font-size:14px;
font-weight:bold;
}
h1 {
    font-family:Arial, Helvetica, sans-serif;
    font-size: 30px;
    font-style: italic;
    color:rgb(0,0,0)
}
body
{
font-family:Arial,sans-serif;
}
</style>
<link rel="shortcut icon" href="favicon.ico">
</head>
<body>
<div style="text-align:left;"><h1>Remote<font style="color:rgb(100,100,100)">Apps</font></h1></div><br>
<% 
   string appname = "";
   string basefilename = "";
   string pngname = "";
   string pngpath = "";

   string Whichfolder = HttpContext.Current.Server.MapPath("rdp\\");
   string UserFolder = HttpContext.Current.Server.MapPath("rdp\\user\\")+authUser;
   string[] alluserfiles = System.IO.Directory.GetFiles(Whichfolder);
   string[] userfiles = new string[0];
   if(System.IO.Directory.Exists(UserFolder))
      userfiles = System.IO.Directory.GetFiles(UserFolder);

   string[] allfiles = new string[alluserfiles.Length + userfiles.Length];
   Array.Copy(alluserfiles,allfiles,alluserfiles.Length);
   Array.Copy(userfiles,0,allfiles,alluserfiles.Length,userfiles.Length);

   foreach(string group in authUserGroups) {
      string groupdir = HttpContext.Current.Server.MapPath("rdp\\group\\") + group;
      if( System.IO.Directory.Exists(groupdir) ) {
         string[] groupfiles = System.IO.Directory.GetFiles(groupdir);
         int oldSize = allfiles.Length;
         Array.Resize(ref allfiles,allfiles.Length + groupfiles.Length);
         Array.Copy(groupfiles,0,allfiles,oldSize,groupfiles.Length);
      }
   }

   foreach(string eachfile in allfiles)
   {
      string extfile = eachfile.Substring(eachfile.Length - 4, 4);       
      if (extfile.ToLower() == ".rdp")
      {
         if (!(GetRDPvalue(eachfile,"full address:s:") == ""))
         {
            appname = GetRDPvalue(eachfile, "remoteapplicationname:s:");
            basefilename = eachfile.Substring(Whichfolder.Length, eachfile.Length - Whichfolder.Length - 4);
            string webfilename = eachfile.Replace("\\","/");
            if (appname == "")
            {
               appname = basefilename;
               if(appname.IndexOf("\\")>0) {
                    appname = basefilename.Substring(basefilename.LastIndexOf("\\")+1);
               }
            }
            pngname = basefilename + ".png";
            if (System.IO.File.Exists(HttpContext.Current.Server.MapPath("png\\" + pngname)))
            {
               pngpath = "png/" + pngname;
            }
            else
            {
               pngpath = "rdpicon.png";
            }
            HttpContext.Current.Response.Write("<div id=apptile>");
            HttpContext.Current.Response.Write("<a href=\"" + "rdp/" + webfilename.Substring(Whichfolder.Length, webfilename.Length - Whichfolder.Length) + "\"><img border=0 height=64 width=64 src=\"" + pngpath.Replace("\\","/") + "\"><br>" + appname + "</a>");
            HttpContext.Current.Response.Write("</div>");
         }
      }
   }
%>
</body>
</html>
<%
  }
%>
