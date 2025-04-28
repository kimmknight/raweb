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

    public System.Guid GetResourceGUID(string rdpFilePath)
    {
        // read the entire contents of the file into a string
        string fileContents = System.IO.File.ReadAllText(rdpFilePath);

        // generate a guid from the file contents
        var byt = Encoding.UTF8.GetBytes(fileContents);
        var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(byt);
        var guid = new Guid(hash);

        return guid;
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
    private new Dictionary<string, DateTime> terminalServerTimestamps = new Dictionary<string, DateTime>();
    private double schemaVersion = 1.0;
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

    private void ProcessResources(string directoryPath, string relativePath, string folderPrefix = null)
    {
        // check if directorypath is a relative path or physical path

        if (System.IO.Directory.Exists(directoryPath) == false)
        {
            string fullRelativePath = Root() + directoryPath;
            directoryPath = HttpContext.Current.Server.MapPath(fullRelativePath);
        }

        string[] subDirectories = System.IO.Directory.GetDirectories(directoryPath);
        foreach (string subDirectory in subDirectories)
        {
            string folderName = relativePath + "/" + System.IO.Path.GetFileName(subDirectory);
            ProcessResources(subDirectory, folderPrefix + folderName);
        }

        // keep track of previous resource GUIDs to avoid duplicates
        string[] previousResourceGUIDs = new string[] {};
        
        string[] allfiles = System.IO.Directory.GetFiles(directoryPath, "*.rdp");
        foreach (string eachfile in allfiles)
        {
            if (!(GetRDPvalue(eachfile, "full address:s:") == ""))
            {
                // get the basefilename and remove the last 4 characters (.rdp)
                string basefilename = System.IO.Path.GetFileName(eachfile).Substring(0, System.IO.Path.GetFileName(eachfile).Length - 4);

                // extract full relative path from the directoryPath (including the resources or multiuser-resources folder)
                string relativePathFull = directoryPath.Replace(HttpContext.Current.Server.MapPath(Root()), "").TrimStart('\\').TrimEnd('\\').Replace("\\", "/") + "/";

                // prepare the info for the resource
                string appprogram = GetRDPvalue(eachfile, "remoteapplicationprogram:s:");
                string apptitle = GetRDPvalue(eachfile, "remoteapplicationname:s:");
                string apprdpfile = basefilename + ".rdp";
                string appresourceid = GetResourceGUID(eachfile).ToString();
                string appalias = relativePathFull + apprdpfile;
                string appfileextcsv = GetRDPvalue(eachfile, "remoteapplicationfileextensions:s:");
                string appfulladdress = GetRDPvalue(eachfile, "full address:s:");
                string rdptype = "RemoteApp";

                // ensure that the resource ID is unique: skip if it already exists
                if (Array.IndexOf(previousResourceGUIDs, appresourceid) >= 0)
                {
                    continue;
                }

                string subFolderName = relativePath;

                if (folderPrefix != null)
                {
                    subFolderName = folderPrefix;
                }

                if (appprogram == "")
                {
                    rdptype = "Desktop";
                    apptitle = basefilename;
                }
                else
                {
                    rdptype = "RemoteApp";
                }

                // get the paths to all files that start with the same basename as the rdp file
                // (e.g., get: *.rdp, *.ico, *.png, *.xlsx.ico, *.xls.png, etc.)
                string[] allResourceFiles = System.IO.Directory.GetFiles(directoryPath, basefilename + ".*");

                // calculate the timestamp for the resource, which is the latest of the rdp file and icon files
                DateTime resourceDateTime = System.IO.File.GetLastWriteTimeUtc(eachfile);
                foreach (string resourceFile in allResourceFiles)
                {
                    DateTime fileDateTime = System.IO.File.GetLastWriteTimeUtc(resourceFile);
                    if (fileDateTime > resourceDateTime)
                    {
                        resourceDateTime = fileDateTime;
                    }
                }
                string resourceTimestamp = resourceDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

                // construct the resource element
                resourcesBuffer.Append("<Resource ID=\"" + appresourceid + "\" Alias=\"" + appalias + "\" Title=\"" + apptitle + "\" LastUpdated=\"" + resourceTimestamp + "\" Type=\"" + rdptype + "\">" + "\r\n");
                resourcesBuffer.Append("<Icons>" + "\r\n");
                resourcesBuffer.Append("<IconRaw FileType=\"Ico\" FileURL=\"" + Root() + "get-image.aspx?image=" + relativePathFull + Regex.Replace(basefilename, "^/+", "") + "&amp;format=ico\" />" + "\r\n");
                resourcesBuffer.Append("<Icon32 Dimensions=\"32x32\" FileType=\"Png\" FileURL=\"" + Root() + "get-image.aspx?image=" + relativePathFull + Regex.Replace(basefilename, "^/+", "") + "&amp;format=png32\" />" + "\r\n");
                resourcesBuffer.Append("</Icons>" + "\r\n");
                if (appfileextcsv != "")
                {
                    resourcesBuffer.Append("<FileExtensions>" + "\r\n");
                    string[] fileExtensions = appfileextcsv.Split(',');
                    foreach(string fileExt in fileExtensions)
                    {
                        if (schemaVersion >= 2.0)
                        {
                            resourcesBuffer.Append("<FileExtension Name=\"" + fileExt + "\" PrimaryHandler=\"True\">" + "\r\n");
                        }
                        else
                        {
                            resourcesBuffer.Append("<FileExtension Name=\"" + fileExt + "\" >" + "\r\n");
                        }

                        if (schemaVersion >= 2.0)
                        {
                            // check if the icon exists, and if so, add it to the resource
                            string iconPath = System.IO.Path.Combine(directoryPath, basefilename + fileExt + ".ico");
                            string iconExt = ".ico";
                            string pngIconPath = System.IO.Path.Combine(directoryPath, basefilename + fileExt + ".png");
                            if (System.IO.File.Exists(pngIconPath))
                            {
                                iconPath = pngIconPath;
                                iconExt = ".png";
                            }
                            string relativeIconPath = relativePathFull + basefilename + fileExt + iconExt;
                            bool iconExists = System.IO.File.Exists(iconPath);
                            if (iconExists)
                            {
                                resourcesBuffer.Append("<FileAssociationIcons>" + "\r\n");
                                resourcesBuffer.Append("<IconRaw FileType=\"Ico\" FileURL=\"" + Root() + "get-image.aspx?image=" + relativeIconPath + "&amp;format=ico\" />" + "\r\n");
                                resourcesBuffer.Append("</FileAssociationIcons>" + "\r\n");
                            }
                        }

                        resourcesBuffer.Append("</FileExtension>" + "\r\n");
                    }
                    resourcesBuffer.Append("</FileExtensions>" + "\r\n");
                }
                else
                {
                    resourcesBuffer.Append("<FileExtensions />" + "\r\n");
                }
                if (schemaVersion >= 2.0)
                {
                    resourcesBuffer.Append("<Folders>" + "\r\n");
                    resourcesBuffer.Append("<Folder Name=\"" + (subFolderName==""?"/":subFolderName) + "\" />" + "\r\n");
                    resourcesBuffer.Append("</Folders>" + "\r\n");
                }
                resourcesBuffer.Append("<HostingTerminalServers>" + "\r\n");
                resourcesBuffer.Append("<HostingTerminalServer>" + "\r\n");
                resourcesBuffer.Append("<ResourceFile FileExtension=\".rdp\" URL=\"" + Root() + appalias + "\" />" + "\r\n");
                resourcesBuffer.Append("<TerminalServerRef Ref=\"" + appfulladdress + "\" />" + "\r\n");
                resourcesBuffer.Append("</HostingTerminalServer>" + "\r\n");
                resourcesBuffer.Append("</HostingTerminalServers>" + "\r\n");
                resourcesBuffer.Append("</Resource>" + "\r\n");

                // add the resource ID to the list of previous resource GUIDs to avoid duplicates
                Array.Resize(ref previousResourceGUIDs, previousResourceGUIDs.Length + 1);
                previousResourceGUIDs[previousResourceGUIDs.Length - 1] = appresourceid;

                // add the timestamp to the terminal server timestamps if it is the latest one
                if (!terminalServerTimestamps.ContainsKey(appfulladdress) || resourceDateTime > terminalServerTimestamps[appfulladdress])
                {
                    terminalServerTimestamps[appfulladdress] = resourceDateTime;
                }
            }
        }
    }

    private void ProcessMultiuserResources(string directoryPath)
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
            ProcessResources(UserFolder, "", "/" + authUser);
        }

        // Process resources in basePath\\group\\ [group name]

        foreach (string group in authUserGroups)
        {
            string GroupFolder = directoryPath + "\\group\\" + group + "\\";
            if (System.IO.Directory.Exists(GroupFolder))
            {
                ProcessResources(GroupFolder, "", "/" + group);
            }
        }

    }

    private class AliasResolver
    {
        private readonly Dictionary<string, string> _aliasMap;

        public AliasResolver()
        {
            string aliasConfig = System.Configuration.ConfigurationManager.AppSettings["TerminalServerAliases"] ?? "";
            _aliasMap = ParseConfigString(aliasConfig);
        }

        private Dictionary<string, string> ParseConfigString(string configString)
        {
            // split the aliases into a map that allows us to find the alias for a given input
            // the input is the key, and the alias is the value
            var aliasMap = new Dictionary<string, string>();
            // format: "INPUT=Alias;INPUT2=Alias with spaces; INPUT3=Alias with spaces ,and commas"
            string[] aliases = configString.Split(';');
            foreach (string alias in aliases)
            {
                string[] aliasPair = alias.Split('=');
                if (aliasPair.Length == 2)
                {
                    string input = aliasPair[0].Trim();
                    string aliasValue = aliasPair[1].Trim();
                    if (!aliasMap.ContainsKey(input))
                    {
                        aliasMap.Add(input, aliasValue);
                    }
                }
            }
            return aliasMap;
        }

        public string Resolve(string name)
        {
            // if the name is in the alias map, return the alias value
            if (_aliasMap.ContainsKey(name))
            {
                return _aliasMap[name];
            }

            // if the name is not in the alias map, return the name as is
            return name;
        }
    }
</script>
<%
  string authUser = getAuthenticatedUser();
  if(authUser=="") {
      Response.Redirect("auth/loginfeed.aspx");
  }
  else {
      if (HttpContext.Current.Request.Headers.GetValues("accept").FirstOrDefault().ToLower().Contains("radc_schema_version=2.0"))
      {
        schemaVersion = 2.0;
      }
      else if (HttpContext.Current.Request.Headers.GetValues("accept").FirstOrDefault().ToLower().Contains("radc_schema_version=2.1"))
      {
        schemaVersion = 2.1;
      }

      // process resources
      string resourcesFolder = "resources";
      string multiuserResourcesFolder = "multiuser-resources";
      ProcessResources(resourcesFolder, "");
      ProcessMultiuserResources(multiuserResourcesFolder);
      ProcessSubFolders(resourcesFolder, "");

      HttpContext.Current.Response.ContentType = (schemaVersion >= 2.0 ? "application/x-msts-radc+xml; charset=utf-8" : "text/xml; charset=utf-8");
      string serverName = System.Net.Dns.GetHostName();
      string datetime = DateTime.Now.Year.ToString() + "-" + (DateTime.Now.Month + 100).ToString().Substring(1, 2) + "-" + (DateTime.Now.Day + 100).ToString().Substring(1, 2) + "T" + (DateTime.Now.Hour + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Minute + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Second + 100).ToString().Substring(1, 2) + ".0Z";

      // calculate publisher details
      AliasResolver resolver = new AliasResolver();
      string publisherName = resolver.Resolve(serverName);
      DateTime publisherDateTime = DateTime.MinValue;
      foreach (string terminalServer in terminalServerTimestamps.Keys)
      {
        DateTime serverTimestamp = terminalServerTimestamps[terminalServer];
        if (serverTimestamp > publisherDateTime)
        {
          publisherDateTime = serverTimestamp;
        }
      }
      string publisherTimestamp = publisherDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

      HttpContext.Current.Response.Write("<ResourceCollection PubDate=\"" + datetime + "\" SchemaVersion=\"" + schemaVersion.ToString() + "\" xmlns=\"http://schemas.microsoft.com/ts/2007/05/tswf\">" + "\r\n");
      HttpContext.Current.Response.Write("<Publisher LastUpdated=\"" + publisherTimestamp + "\" Name=\"" + publisherName + "\" ID=\"" + serverName + "\" Description=\"\">" + "\r\n");

      HttpContext.Current.Response.Write("<Resources>" + "\r\n");
        HttpContext.Current.Response.Write(resourcesBuffer.ToString());
      HttpContext.Current.Response.Write("</Resources>" + "\r\n");
      
      HttpContext.Current.Response.Write("<TerminalServers>" + "\r\n");
      foreach (string terminalServer in terminalServerTimestamps.Keys)
      {
        string terminalServerName = terminalServer;
        string terminalServerTimestamp = terminalServerTimestamps[terminalServer].ToString("yyyy-MM-ddTHH:mm:ssZ");
        HttpContext.Current.Response.Write("<TerminalServer ID=\"" + terminalServerName + "\" LastUpdated=\"" + terminalServerTimestamp + "\" />" + "\r\n");
      }
      HttpContext.Current.Response.Write("</TerminalServers>" + "\r\n");
      HttpContext.Current.Response.Write("</Publisher>" + "\r\n");
      HttpContext.Current.Response.Write("</ResourceCollection>" + "\r\n");
      HttpContext.Current.Response.End();
  }
%>
