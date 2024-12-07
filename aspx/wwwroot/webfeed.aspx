<%@ Page language="C#" explicit="true" Debug="true" %>
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
        contentfile.Close();
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
            // Decrypt may throw an exception if authCookie.Value is total garbage
            FormsAuthenticationTicket authTicket = FormsAuthentication.Decrypt(authCookie.Value);
            if(authTicket==null) {
                return "";
            }
            
            // Strip the DOMAIN\ if it's there.
            string username = authTicket.Name;
            if(username.IndexOf("\\")>0) {
                // Grab only the username
                username = username.Split('\\')[1];
            }

            return username;
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
                    //string authDomain = group.Substring(0,group.IndexOf("\\"));
                    //if(HttpContext.Current.Server.MachineName == authDomain || authDomain == "BUILTIN")
                    //    group = group.Substring(authDomain.Length+1);

                    group = group.Split('\\')[1];
                }
                groups.SetValue(group,pos);
            }
            return groups;
        }
        catch {
            return new string[0];
        }
    }

    private string[] extraSubFolders = {};

    private void ProcessSubFolders(string directoryPath, string relativePath)
    {
        if (System.IO.Directory.Exists(directoryPath) == false)
        {
            string fullRelativePath = Root() + directoryPath;
            directoryPath = HttpContext.Current.Server.MapPath(fullRelativePath);
        }

        string[] subDirectories = System.IO.Directory.GetDirectories(directoryPath);
        foreach (string subDirectory in subDirectories)
        {
            string folderName = relativePath + "/" + System.IO.Path.GetFileName(subDirectory);
            HttpContext.Current.Response.Write("<Folder Name=\"" + folderName + "\" />" + "\r\n");
            ProcessSubFolders(subDirectory, folderName);
        } 
    }

    private void ProcessExtraSubFolders()
    {
        foreach (string folderName in extraSubFolders)
        {
            HttpContext.Current.Response.Write("<Folder Name=\"" + folderName + "\" />" + "\r\n");
        }
    }

    private void AddExtraSubFolder(string folderName)
    {
        // Add folderName to extraSubFolders if it's not already there
        if (Array.IndexOf(extraSubFolders, folderName) == -1)
        {
            extraSubFolders = extraSubFolders.Concat(new string[] { folderName }).ToArray();
        }
    }

    private void ProcessResources(string directoryPath, string relativePath, string serverName, string overrideFolder = null)
    {
        // check if directorypath is a relative path or physical path

        if (System.IO.Directory.Exists(directoryPath) == false)
        {
            string fullRelativePath = Root() + directoryPath;
            directoryPath = HttpContext.Current.Server.MapPath(fullRelativePath);
        }

        if (overrideFolder == null)
        {
            string[] subDirectories = System.IO.Directory.GetDirectories(directoryPath);
            foreach (string subDirectory in subDirectories)
            {
                string folderName = relativePath + "/" + System.IO.Path.GetFileName(subDirectory);
                ProcessResources(subDirectory, folderName, serverName);
            }
        }
        

        string[] allfiles = System.IO.Directory.GetFiles(directoryPath, "*.rdp");
        foreach (string eachfile in allfiles)
        {
            if (!(GetRDPvalue(eachfile, "full address:s:") == ""))
            {
                // get the basefilename and remove the last 4 characters (.rdp)
                string basefilename = System.IO.Path.GetFileName(eachfile).Substring(0, System.IO.Path.GetFileName(eachfile).Length - 4);

                string appalias = GetRDPvalue(eachfile, "remoteapplicationprogram:s:");
                string apptitle = GetRDPvalue(eachfile, "remoteapplicationname:s:");
                string apprdpfile = basefilename + ".rdp";
                string appresourceid = appalias;
                string appftastring = GetRDPvalue(eachfile, "remoteapplicationfileextensions:s:");
                string appfulladdress = GetRDPvalue(eachfile, "full address:s:");
                string rdptype = "RemoteApp";

                // Extract full relative path from the directoryPath (including the resources or multiuser-resources folder)
                string relativePathFull = directoryPath.Replace(HttpContext.Current.Server.MapPath(Root()), "").TrimStart('\\').TrimEnd('\\').Replace("\\", "/") + "/";

                string subFolderName = relativePath;

                if (overrideFolder != null)
                {
                    subFolderName = overrideFolder;
                }

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
                HttpContext.Current.Response.Write("<IconRaw FileType=\"Ico\" FileURL=\"" + Root() + "get-image.aspx?image=" + relativePathFull + Regex.Replace(basefilename, "^/+", "") + "&amp;format=ico\" />" + "\r\n");
                HttpContext.Current.Response.Write("<Icon32 Dimensions=\"32x32\" FileType=\"Png\" FileURL=\"" + Root() + "get-image.aspx?image=" + relativePathFull + Regex.Replace(basefilename, "^/+", "") + "&amp;format=png32\" />" + "\r\n");
                HttpContext.Current.Response.Write("</Icons>" + "\r\n");
                if (appftastring != "")
                {
                    HttpContext.Current.Response.Write("<FileExtensions>" + "\r\n");
                    string[] appftaarray = appftastring.Split(',');
                    foreach(string filetype in appftaarray)
                    {
                        string docicon = basefilename + filetype + ".ico";
                        HttpContext.Current.Response.Write("<FileExtension Name=\"" + filetype + "\" PrimaryHandler=\"True\">" + "\r\n");
                        HttpContext.Current.Response.Write("<FileAssociationIcons>" + "\r\n");
                        HttpContext.Current.Response.Write("<IconRaw FileType=\"Ico\" FileURL=\"" + Root() + "get-image.aspx?image=" + relativePathFull + Regex.Replace(docicon, "^/+", "") + "&amp;format=ico\" />" + "\r\n");
                        HttpContext.Current.Response.Write("</FileAssociationIcons>" + "\r\n");
                        HttpContext.Current.Response.Write("</FileExtension>" + "\r\n");
                    }
                    HttpContext.Current.Response.Write("</FileExtensions>" + "\r\n");
                }
                else
                {
                    HttpContext.Current.Response.Write("<FileExtensions />" + "\r\n");
                }
                HttpContext.Current.Response.Write("<Folders>" + "\r\n");
                HttpContext.Current.Response.Write("<Folder Name=\"" + subFolderName + "\" />" + "\r\n");
                HttpContext.Current.Response.Write("</Folders>" + "\r\n");
                HttpContext.Current.Response.Write("<HostingTerminalServers>" + "\r\n");
                HttpContext.Current.Response.Write("<HostingTerminalServer>" + "\r\n");
                HttpContext.Current.Response.Write("<ResourceFile FileExtension=\".rdp\" URL=\"" + Root() + relativePathFull + apprdpfile + "\" />" + "\r\n");
                HttpContext.Current.Response.Write("<TerminalServerRef Ref=\"" + serverName + "\" />" + "\r\n");
                HttpContext.Current.Response.Write("</HostingTerminalServer>" + "\r\n");
                HttpContext.Current.Response.Write("</HostingTerminalServers>" + "\r\n");
                HttpContext.Current.Response.Write("</Resource>" + "\r\n");
            }
        }
    }

    private void ProcessMultiuserResources(string directoryPath, string serverName)
    {
        if (System.IO.Directory.Exists(directoryPath) == false)
        {
            string fullRelativePath = Root() + directoryPath;
            directoryPath = HttpContext.Current.Server.MapPath(fullRelativePath);
        }

        string authUser = getAuthenticatedUser();
        string[] authUserGroups = getAuthenticatedUserGroups();

        // Process resources in basePath\\user\\ authUser

        string UserFolder = directoryPath + "\\user\\" + authUser + "\\";

        if (System.IO.Directory.Exists(UserFolder))
        {
            ProcessResources(UserFolder, "", serverName, "/" + authUser);
            AddExtraSubFolder("/" + authUser);
        }

        // Process resources in basePath\\group\\ [group name]

        foreach (string group in authUserGroups)
        {
            string GroupFolder = directoryPath + "\\group\\" + group + "\\";

            if (System.IO.Directory.Exists(GroupFolder))
            {
                ProcessResources(GroupFolder, "", serverName, "/" + group);
                AddExtraSubFolder("/" + group);
            }
        }

    }

</script>
<%
  string authUser = getAuthenticatedUser();
  if(authUser=="") {
      Response.Redirect("auth/loginfeed.aspx");
  }
  else {
      HttpContext.Current.Response.ContentType = "text/xml; charset=utf-8";
      string serverName = System.Net.Dns.GetHostName();
      string datetime = DateTime.Now.Year.ToString() + "-" + (DateTime.Now.Month + 100).ToString().Substring(1, 2) + "-" + (DateTime.Now.Day + 100).ToString().Substring(1, 2) + "T" + (DateTime.Now.Hour + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Minute + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Second + 100).ToString().Substring(1, 2) + ".0Z";

      HttpContext.Current.Response.Write("<ResourceCollection PubDate=\"" + datetime + "\" SchemaVersion=\"2.1\" xmlns=\"http://schemas.microsoft.com/ts/2007/05/tswf\">" + "\r\n");
      HttpContext.Current.Response.Write("<Publisher LastUpdated=\"" + datetime + "\" Name=\"" + serverName + "\" ID=\"" + serverName + "\" Description=\"\">" + "\r\n");
      string resourcesFolder = "resources";
      string multiuserResourcesFolder = "multiuser-resources";
      
      HttpContext.Current.Response.Write("<Resources>" + "\r\n");

      ProcessResources(resourcesFolder, "", serverName);
      ProcessMultiuserResources(multiuserResourcesFolder, serverName);

      HttpContext.Current.Response.Write("</Resources>" + "\r\n");
      HttpContext.Current.Response.Write("<SubFolders>" + "\r\n");

      ProcessSubFolders(resourcesFolder, "");
      ProcessExtraSubFolders();

      HttpContext.Current.Response.Write("</SubFolders>" + "\r\n");
      HttpContext.Current.Response.Write("<TerminalServers>" + "\r\n");
      HttpContext.Current.Response.Write("<TerminalServer ID=\"" + serverName + "\" Name=\"" + serverName + "\" LastUpdated=\"" + datetime + "\" />" + "\r\n");
      HttpContext.Current.Response.Write("</TerminalServers>" + "\r\n");
      HttpContext.Current.Response.Write("</Publisher>" + "\r\n");
      HttpContext.Current.Response.Write("</ResourceCollection>" + "\r\n");
      HttpContext.Current.Response.End();
  }
%>
