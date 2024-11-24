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

    private void ProcessSubFolders(string directoryPath, string relativePath)
    {
        string[] subDirectories = System.IO.Directory.GetDirectories(directoryPath);
        foreach (string subDirectory in subDirectories)
        {
            string folderName = relativePath + "/" + System.IO.Path.GetFileName(subDirectory);
            HttpContext.Current.Response.Write("<Folder Name=\"" + folderName + "\" />" + "\r\n");
            ProcessSubFolders(subDirectory, folderName);
        }
    }

    private void ProcessResources(string directoryPath, string relativePath, string ServerName)
    {
        string[] subDirectories = System.IO.Directory.GetDirectories(directoryPath);
        foreach (string subDirectory in subDirectories)
        {
            string folderName = relativePath + "/" + System.IO.Path.GetFileName(subDirectory);
            ProcessResources(subDirectory, folderName, ServerName);
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
                
                string relativePathSlash = "";
                if (relativePath != "") {
                    relativePathSlash = relativePath.TrimStart('/').TrimEnd('/') + "/";
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
                HttpContext.Current.Response.Write("<IconRaw FileType=\"Ico\" FileURL=\"" + Root() + "get-image.aspx?image=" + relativePathSlash + Regex.Replace(basefilename, "^/+", "") + "&amp;format=ico\" />" + "\r\n");
                HttpContext.Current.Response.Write("<Icons>" + "\r\n");
                HttpContext.Current.Response.Write("<IconRaw FileType=\"Ico\" FileURL=\"" + Root() + "get-image.aspx?image=" + relativePathSlash + Regex.Replace(basefilename, "^/+", "") + "&amp;format=ico\" />" + "\r\n");
                HttpContext.Current.Response.Write("<Icon32 Dimensions=\"32x32\" FileType=\"Png\" FileURL=\"" + Root() + "get-image.aspx?image=" + relativePathSlash + Regex.Replace(basefilename, "^/+", "") + "&amp;format=png32\" />" + "\r\n");
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
                        HttpContext.Current.Response.Write("<IconRaw FileType=\"Ico\" FileURL=\"" + Root() + "get-image.aspx?image=" + relativePathSlash + Regex.Replace(docicon, "^/+", "") + "&amp;format=ico\" />" + "\r\n");
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
                HttpContext.Current.Response.Write("<Folder Name=\"" + relativePath + "\" />" + "\r\n");
                HttpContext.Current.Response.Write("</Folders>" + "\r\n");
                HttpContext.Current.Response.Write("<HostingTerminalServers>" + "\r\n");
                HttpContext.Current.Response.Write("<HostingTerminalServer>" + "\r\n");
                HttpContext.Current.Response.Write("<ResourceFile FileExtension=\".rdp\" URL=\"" + Root() + "resources/" + relativePathSlash + apprdpfile + "\" />" + "\r\n");
                HttpContext.Current.Response.Write("<TerminalServerRef Ref=\"" + ServerName + "\" />" + "\r\n");
                HttpContext.Current.Response.Write("</HostingTerminalServer>" + "\r\n");
                HttpContext.Current.Response.Write("</HostingTerminalServers>" + "\r\n");
                HttpContext.Current.Response.Write("</Resource>" + "\r\n");
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
      string ServerName = System.Net.Dns.GetHostName();
      string datetime = DateTime.Now.Year.ToString() + "-" + (DateTime.Now.Month + 100).ToString().Substring(1, 2) + "-" + (DateTime.Now.Day + 100).ToString().Substring(1, 2) + "T" + (DateTime.Now.Hour + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Minute + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Second + 100).ToString().Substring(1, 2) + ".0Z";

      HttpContext.Current.Response.Write("<ResourceCollection PubDate=\"" + datetime + "\" SchemaVersion=\"1.1\" xmlns=\"http://schemas.microsoft.com/ts/2007/05/tswf\">" + "\r\n");
      HttpContext.Current.Response.Write("<Publisher LastUpdated=\"" + datetime + "\" Name=\"" + ServerName + "\" ID=\"" + ServerName + "\" Description=\"\">" + "\r\n");
      HttpContext.Current.Response.Write("<SubFolders>" + "\r\n");

      string Whichfolder = HttpContext.Current.Server.MapPath("resources\\") + "/";
      ProcessSubFolders(Whichfolder, "");

      HttpContext.Current.Response.Write("</SubFolders>" + "\r\n");
      HttpContext.Current.Response.Write("<Resources>" + "\r\n");

      ProcessResources(Whichfolder, "", ServerName);

      HttpContext.Current.Response.Write("</Resources>" + "\r\n");
      HttpContext.Current.Response.Write("<TerminalServers>" + "\r\n");
      HttpContext.Current.Response.Write("<TerminalServer ID=\"" + ServerName + "\" Name=\"" + ServerName + "\" LastUpdated=\"" + datetime + "\" />" + "\r\n");
      HttpContext.Current.Response.Write("</TerminalServers>" + "\r\n");
      HttpContext.Current.Response.Write("</Publisher>" + "\r\n");
      HttpContext.Current.Response.Write("</ResourceCollection>" + "\r\n");
  }
%>