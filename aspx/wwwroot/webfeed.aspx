<%@ Page language="C#" explicit="true" %>
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
    public string Root()
    {
        string DocPath = HttpContext.Current.Request.ServerVariables["PATH_INFO"];
        string[] aPath = ("/" + DocPath).Split('/');
        string Root = DocPath.Substring(0, (DocPath.Length - aPath[aPath.GetUpperBound(0)].Length));
        return Root;
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
      Response.Redirect("auth/loginfeed.aspx");
  }
  else {
      string [] authUserGroups = getAuthenticatedUserGroups();
      HttpContext.Current.Response.ContentType = "text/xml; charset=utf-8";
      string ServerName = System.Net.Dns.GetHostName();
      string datetime = DateTime.Now.Year.ToString() + "-" + (DateTime.Now.Month + 100).ToString().Substring(1, 2) + "-" + (DateTime.Now.Day + 100).ToString().Substring(1, 2) + "T" + (DateTime.Now.Hour + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Minute + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Second + 100).ToString().Substring(1, 2) + ".0Z";

      HttpContext.Current.Response.Write("<ResourceCollection PubDate=\"" + datetime + "\" SchemaVersion=\"1.1\" xmlns=\"http://schemas.microsoft.com/ts/2007/05/tswf\">" + "\r\n");
      HttpContext.Current.Response.Write("<Publisher LastUpdated=\"" + datetime + "\" Name=\"" + ServerName + "\" ID=\"" + ServerName + "\" Description=\"\">" + "\r\n");
      HttpContext.Current.Response.Write("<Resources>" + "\r\n");

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
      foreach (string eachfile in allfiles)
      {
         string extfile = eachfile.Substring(eachfile.Length - 4, 4);
         if (extfile.ToLower() == ".rdp")
         {
            if (!(GetRDPvalue(eachfile, "full address:s:") == ""))
            {
               string basefilename = eachfile.Substring(Whichfolder.Length, eachfile.Length - Whichfolder.Length - 4);

               string appalias = GetRDPvalue(eachfile, "remoteapplicationprogram:s:");
               string apptitle = GetRDPvalue(eachfile, "remoteapplicationname:s:");
               string appicon = basefilename + ".ico";
               string appicon32 = basefilename + ".png";
               string apprdpfile = basefilename + ".rdp";
               if(basefilename.IndexOf("\\")>0) {
                    basefilename = basefilename.Substring(basefilename.LastIndexOf("\\")+1);
               }
               string appresourceid = appalias;
               string appftastring = GetRDPvalue(eachfile, "remoteapplicationfileextensions:s:");
               string appfulladdress = GetRDPvalue(eachfile, "full address:s:");
               string rdptype = "RemoteApp";
               if (appalias == "")
               {
                  rdptype = "Desktop";
                  appalias = basefilename;
                  apptitle = basefilename;
                  appresourceid = basefilename;
               }
               else
               {
                  rdptype = "RemoteApp";
               }
               DateTime filedatetimeraw = System.IO.File.GetLastWriteTime(eachfile);
               string filedatetime = DateTime.Now.Year.ToString() + "-" + (filedatetimeraw.Month + 100).ToString().Substring(1,2) + "-" + (filedatetimeraw.Day + 100).ToString().Substring(1,2) + "T" + (filedatetimeraw.Hour + 100).ToString().Substring(1,2) + ":" + (filedatetimeraw.Minute + 100).ToString().Substring(1,2) + ":" + (filedatetimeraw.Second + 100).ToString().Substring(1,2) + ".0Z";
               HttpContext.Current.Response.Write("<Resource ID=\"" + appresourceid + "\" Alias=\"" + appalias + "\" Title=\"" + apptitle + "\" LastUpdated=\"" + filedatetime + "\" Type=\"" + rdptype + "\">" + "\r\n");
               HttpContext.Current.Response.Write("<Icons>" + "\r\n");
               if (System.IO.File.Exists(HttpContext.Current.Server.MapPath("icon/" + appicon))) {
                  HttpContext.Current.Response.Write("<IconRaw FileType=\"Ico\" FileURL=\"" + Root() + "icon/" + appicon.Replace("\\","/") + "\" />" + "\r\n");
               }
               if (System.IO.File.Exists(HttpContext.Current.Server.MapPath("icon32/" + appicon32)))
               {
                  HttpContext.Current.Response.Write("<Icon32 Dimensions=\"32x32\" FileType=\"Png\" FileURL=\"" + Root() + "icon32/" + appicon32.Replace("\\","/") + "\" />" + "\r\n");
               }
               HttpContext.Current.Response.Write("</Icons>" + "\r\n");
               if (appftastring != "")
               {
                  HttpContext.Current.Response.Write("<FileExtensions>" + "\r\n");
                  string[] appftaarray = appftastring.Split(',');
                  foreach(string filetype in appftaarray)
                  {
                     string docicon = basefilename.Replace("\\","/")  + filetype + ".ico";
                     HttpContext.Current.Response.Write("<FileExtension Name=\"" + filetype + "\" PrimaryHandler=\"True\">" + "\r\n");
                     HttpContext.Current.Response.Write("<FileAssociationIcons>" + "\r\n");
                     HttpContext.Current.Response.Write("<IconRaw FileType=\"Ico\" FileURL=\"" + Root() + "icon/" + docicon.Replace("\\","/") + "\" />" + "\r\n");
                     HttpContext.Current.Response.Write("</FileAssociationIcons>" + "\r\n");
                     HttpContext.Current.Response.Write("</FileExtension>" + "\r\n");
                  }
                  HttpContext.Current.Response.Write("</FileExtensions>" + "\r\n");
               }
               else
               {
                  HttpContext.Current.Response.Write("<FileExtensions />" + "\r\n");
               }
               HttpContext.Current.Response.Write("<HostingTerminalServers>" + "\r\n");
               HttpContext.Current.Response.Write("<HostingTerminalServer>" + "\r\n");
               HttpContext.Current.Response.Write("<ResourceFile FileExtension=\".rdp\" URL=\"" + Root() + "rdp/" + apprdpfile.Replace("\\","/") + "\" />" + "\r\n");
               HttpContext.Current.Response.Write("<TerminalServerRef Ref=\"" + ServerName + "\" />" + "\r\n");
               HttpContext.Current.Response.Write("</HostingTerminalServer>" + "\r\n");
               HttpContext.Current.Response.Write("</HostingTerminalServers>" + "\r\n");
               HttpContext.Current.Response.Write("</Resource>" + "\r\n");
            }
         }
      }
      HttpContext.Current.Response.Write("</Resources>" + "\r\n");
      HttpContext.Current.Response.Write("<TerminalServers>" + "\r\n");
      HttpContext.Current.Response.Write("<TerminalServer ID=\"" + ServerName + "\" Name=\"" + ServerName + "\" LastUpdated=\"" + datetime + "\" />" + "\r\n");
      HttpContext.Current.Response.Write("</TerminalServers>" + "\r\n");
      HttpContext.Current.Response.Write("</Publisher>" + "\r\n");
      HttpContext.Current.Response.Write("</ResourceCollection>" + "\r\n");
  }
%>
