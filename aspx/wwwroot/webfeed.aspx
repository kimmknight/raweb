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

    private string uncompressSidMap(string compressedSids) {
            if(compressedSids.IndexOf(";")<0)
                return compressedSids;
            string [] mappings = compressedSids.Substring(0,compressedSids.IndexOf(";")).Split(',');
            string sids = compressedSids.Substring(compressedSids.IndexOf(";")+1);
            foreach (string map in mappings) {
                    string sid = map.Substring(0,map.IndexOf("="));
                    string mapped = map.Substring(map.IndexOf("=")+1);
                    sids = sids.Replace(mapped,"S-1-5-21-" + sid + "-");
            }
            return sids;
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
            string [] sids = uncompressSidMap(authTicket.UserData).Split(',');
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

    private StringBuilder resourcesBuffer = new StringBuilder();
    private bool isSchemaVersion2 = false;
    //private StringBuilder extraSubFoldersBuffer = new StringBuilder();

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
            // HttpContext.Current.Response.Write("<Folder Name=\"" + folderName + "\" />" + "\r\n");
            ProcessSubFolders(subDirectory, folderName);
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
                resourcesBuffer.Append("<Resource ID=\"" + appresourceid + "\" Alias=\"" + appalias + "\" Title=\"" + apptitle + "\" LastUpdated=\"" + filedatetime + "\" Type=\"" + rdptype + "\">" + "\r\n");
                resourcesBuffer.Append("<Icons>" + "\r\n");
                resourcesBuffer.Append("<IconRaw FileType=\"Ico\" FileURL=\"" + Root() + "get-image.aspx?image=" + relativePathFull + Regex.Replace(basefilename, "^/+", "") + "&amp;format=ico\" />" + "\r\n");
                resourcesBuffer.Append("<Icon32 Dimensions=\"32x32\" FileType=\"Png\" FileURL=\"" + Root() + "get-image.aspx?image=" + relativePathFull + Regex.Replace(basefilename, "^/+", "") + "&amp;format=png32\" />" + "\r\n");
                resourcesBuffer.Append("</Icons>" + "\r\n");
                if (appftastring != "")
                {
                    resourcesBuffer.Append("<FileExtensions>" + "\r\n");
                    string[] appftaarray = appftastring.Split(',');
                    foreach(string filetype in appftaarray)
                    {
                        string docicon = basefilename + filetype + ".ico";
                        resourcesBuffer.Append("<FileExtension Name=\"" + filetype + "\" PrimaryHandler=\"True\">" + "\r\n");
                        resourcesBuffer.Append("<FileAssociationIcons>" + "\r\n");
                        resourcesBuffer.Append("<IconRaw FileType=\"Ico\" FileURL=\"" + Root() + "get-image.aspx?image=" + relativePathFull + Regex.Replace(docicon, "^/+", "") + "&amp;format=ico\" />" + "\r\n");
                        resourcesBuffer.Append("</FileAssociationIcons>" + "\r\n");
                        resourcesBuffer.Append("</FileExtension>" + "\r\n");
                    }
                    resourcesBuffer.Append("</FileExtensions>" + "\r\n");
                }
                else
                {
                    resourcesBuffer.Append("<FileExtensions />" + "\r\n");
                }
                if(isSchemaVersion2) {
                    resourcesBuffer.Append("<Folders>" + "\r\n");
                    resourcesBuffer.Append("<Folder Name=\"" + (subFolderName==""?"/":subFolderName) + "\" />" + "\r\n");
                    resourcesBuffer.Append("</Folders>" + "\r\n");
                }
                resourcesBuffer.Append("<HostingTerminalServers>" + "\r\n");
                resourcesBuffer.Append("<HostingTerminalServer>" + "\r\n");
                resourcesBuffer.Append("<ResourceFile FileExtension=\".rdp\" URL=\"" + Root() + relativePathFull + apprdpfile + "\" />" + "\r\n");
                resourcesBuffer.Append("<TerminalServerRef Ref=\"" + serverName + "\" />" + "\r\n");
                resourcesBuffer.Append("</HostingTerminalServer>" + "\r\n");
                resourcesBuffer.Append("</HostingTerminalServers>" + "\r\n");
                resourcesBuffer.Append("</Resource>" + "\r\n");
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
        }

        // Process resources in basePath\\group\\ [group name]

        foreach (string group in authUserGroups)
        {
            string GroupFolder = directoryPath + "\\group\\" + group + "\\";
            if (System.IO.Directory.Exists(GroupFolder))
            {
                ProcessResources(GroupFolder, "", serverName, "/" + group);
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
      if(HttpContext.Current.Request.Headers.GetValues("accept").FirstOrDefault().ToLower().Contains("radc_schema_version=2.0"))
        isSchemaVersion2=true;

      HttpContext.Current.Response.ContentType = (isSchemaVersion2?"application/x-msts-radc+xml; charset=utf-8":"text/xml; charset=utf-8");
      string serverName = System.Net.Dns.GetHostName();
      string datetime = DateTime.Now.Year.ToString() + "-" + (DateTime.Now.Month + 100).ToString().Substring(1, 2) + "-" + (DateTime.Now.Day + 100).ToString().Substring(1, 2) + "T" + (DateTime.Now.Hour + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Minute + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Second + 100).ToString().Substring(1, 2) + ".0Z";

      HttpContext.Current.Response.Write("<ResourceCollection PubDate=\"" + datetime + "\" SchemaVersion=\"" + (isSchemaVersion2? "2.1": "1.1" ) + "\" xmlns=\"http://schemas.microsoft.com/ts/2007/05/tswf\">" + "\r\n");
      HttpContext.Current.Response.Write("<Publisher LastUpdated=\"" + datetime + "\" Name=\"" + serverName + "\" ID=\"" + serverName + "\" Description=\"\">" + "\r\n");
      string resourcesFolder = "resources";
      string multiuserResourcesFolder = "multiuser-resources";

      ProcessResources(resourcesFolder, "", serverName);
      ProcessMultiuserResources(multiuserResourcesFolder, serverName);
      ProcessSubFolders(resourcesFolder, "");

      HttpContext.Current.Response.Write("<Resources>" + "\r\n");
        HttpContext.Current.Response.Write(resourcesBuffer.ToString());
      HttpContext.Current.Response.Write("</Resources>" + "\r\n");
      
      HttpContext.Current.Response.Write("<TerminalServers>" + "\r\n");
      HttpContext.Current.Response.Write("<TerminalServer ID=\"" + serverName + "\" Name=\"" + serverName + "\" LastUpdated=\"" + datetime + "\" />" + "\r\n");
      HttpContext.Current.Response.Write("</TerminalServers>" + "\r\n");
      HttpContext.Current.Response.Write("</Publisher>" + "\r\n");
      HttpContext.Current.Response.Write("</ResourceCollection>" + "\r\n");
      HttpContext.Current.Response.End();
  }
%>
