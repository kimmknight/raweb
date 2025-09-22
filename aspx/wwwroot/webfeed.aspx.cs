using AliasUtilities;
using FileSystemUtilities;
using RegistryUtilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

public partial class GetWorkspace : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (getAuthenticatedUserInfo() == null)
        {
            Response.Redirect("auth/loginfeed.aspx");
        }
        else
        {
            if (HttpContext.Current.Request.Headers.GetValues("accept").FirstOrDefault().ToLower().Contains("radc_schema_version=2.0"))
            {
                schemaVersion = 2.0;
            }
            else if (HttpContext.Current.Request.Headers.GetValues("accept").FirstOrDefault().ToLower().Contains("radc_schema_version=2.1"))
            {
                schemaVersion = 2.1;
            }

            // process resources
            string resourcesFolder = "App_Data/resources";
            string multiuserResourcesFolder = "App_Data/multiuser-resources";
            if (System.Configuration.ConfigurationManager.AppSettings["RegistryApps.Enabled"] == "true")
            {
                ProcessRegistryResources();
            }
            ProcessResources(resourcesFolder);
            ProcessMultiuserResources(multiuserResourcesFolder);

            HttpContext.Current.Response.ContentType = (schemaVersion >= 2.0 ? "application/x-msts-radc+xml; charset=utf-8" : "text/xml; charset=utf-8");
            string serverName = searchParams["terminalServer"] ?? System.Net.Dns.GetHostName();
            string serverFQDN = HttpContext.Current.Request.Url.Host;
            string datetime = DateTime.Now.Year.ToString() + "-" + (DateTime.Now.Month + 100).ToString().Substring(1, 2) + "-" + (DateTime.Now.Day + 100).ToString().Substring(1, 2) + "T" + (DateTime.Now.Hour + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Minute + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Second + 100).ToString().Substring(1, 2) + ".0Z";

            // calculate publisher details
            AliasUtilities.AliasResolver resolver = new AliasUtilities.AliasResolver();
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

            HttpContext.Current.Response.Write("<ResourceCollection PubDate=\"" + datetime + "\" SchemaVersion=\"" + schemaVersion.ToString() + "\" " + (schemaVersion >= 2.0 ? "SupportsReconnect=\"false\" " : "") + "xmlns=\"http://schemas.microsoft.com/ts/2007/05/tswf\">" + "\r\n");
            HttpContext.Current.Response.Write("<Publisher LastUpdated=\"" + publisherTimestamp + "\" Name=\"" + publisherName + "\" ID=\"" + serverFQDN + "\" Description=\"\">" + "\r\n");

            HttpContext.Current.Response.Write("<Resources>" + "\r\n");
            string resourcesXML = resourcesBuffer.ToString();
            resourcesXML = Regex.Replace(resourcesXML, @"<FolderInjectionPoint.*?/>", "");
            resourcesXML = Regex.Replace(resourcesXML, @"<TerminalServerInjectionPoint.*?/>", "");
            HttpContext.Current.Response.Write(resourcesXML);
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
    }

    public string GetRDPvalue(string eachfile, string valuename, string fallbackValue = "")
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
        GetRDPvalue = theName.Replace("|", "").Trim();

        if (string.IsNullOrEmpty(GetRDPvalue))
        {
            return fallbackValue;
        }
        else
        {
            return GetRDPvalue;
        }
    }

    public string Root()
    {
        string DocPath = HttpContext.Current.Request.ServerVariables["PATH_INFO"];
        string[] aPath = ("/" + DocPath).Split('/');
        string Root = DocPath.Substring(0, (DocPath.Length - aPath[aPath.GetUpperBound(0)].Length));
        return Root;
    }

    public AuthUtilities.UserInformation getAuthenticatedUserInfo()
    {
        AuthUtilities.AuthCookieHandler authCookieHandler = new AuthUtilities.AuthCookieHandler();
        return authCookieHandler.GetUserInformationSafe(Request);
    }

    private StringBuilder resourcesBuffer = new StringBuilder();
    private new Dictionary<string, DateTime> terminalServerTimestamps = new Dictionary<string, DateTime>();
    private double schemaVersion = 1.0;

    private NameValueCollection searchParams = System.Web.HttpUtility.ParseQueryString(HttpContext.Current.Request.Url.Query);

    private string GetIconElements(string relativeIconPath, string mode = "none", string defaultRelativeIconPath = "default.ico", bool skipMissing = false)
    {
        string defaultIconPath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath(Root()), defaultRelativeIconPath);

        string iconPath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath(Root()), relativeIconPath + ".png");

        int iconWidth = 0;
        int iconHeight = 0;

        // if mode is registry, we need to get the icon dimensions from the registry
        if (relativeIconPath.StartsWith("registry!"))
        {
            string appKeyName = relativeIconPath.Split('!').LastOrDefault();
            string maybeFileExtName = relativeIconPath.Split('!')[1];
            if (maybeFileExtName == appKeyName)
            {
                maybeFileExtName = "";
            }

            try
            {
                System.IO.Stream fileStream = RegistryUtilities.Reader.ReadImageFromRegistry(appKeyName, maybeFileExtName, getAuthenticatedUserInfo());
                if (fileStream == null)
                {
                    if (skipMissing)
                    {
                        return "";
                    }

                    // if the file stream is null, use the default icon
                    relativeIconPath = defaultRelativeIconPath;
                    fileStream = new System.IO.FileStream(defaultIconPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
                }

                using (var image = System.Drawing.Image.FromStream(fileStream, false, false))
                {
                    iconWidth = image.Width;
                    iconHeight = image.Height;
                }
            }
            catch (Exception ex)
            {
                if (skipMissing)
                {
                    return "";
                }

                // cause the icon to be the default icon
                iconWidth = 0;
                iconHeight = 1;
            }
        }
        else
        {
            // get the icon path, preferring the png icon first, then the ico icon, and finally the default icon
            if (!System.IO.File.Exists(iconPath))
            {
                iconPath = System.IO.Path.Combine(HttpContext.Current.Server.MapPath(Root()), relativeIconPath + ".ico");
            }
            if (!System.IO.File.Exists(iconPath))
            {
                if (skipMissing)
                {
                    return "";
                }
                iconPath = defaultIconPath;
                relativeIconPath = defaultRelativeIconPath;
            }

            // confirm that the current user has permission to access the icon file
            bool hasPermission = FileSystemUtilities.Reader.CanAccessPath(iconPath, getAuthenticatedUserInfo());
            if (!hasPermission)
            {
                if (skipMissing)
                {
                    return "";
                }

                // if the user does not have permission to access the icon file, use the default icon
                iconPath = defaultIconPath;
                relativeIconPath = defaultRelativeIconPath;
            }

            // get the icon dimensions
            using (var fileStream = new System.IO.FileStream(iconPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            {
                using (var image = System.Drawing.Image.FromStream(fileStream, false, false))
                {
                    iconWidth = image.Width;
                    iconHeight = image.Height;
                }
            }
        }


        // if the icon is not a square, use the default icon
        // or treat it as wallpaper if the mode is set to "wallpaper"
        string frame = "";
        if (iconWidth != iconHeight)
        {
            // if the icon is not a square, use the default icon instead
            if (mode == "none")
            {
                iconPath = defaultIconPath;
                relativeIconPath = defaultRelativeIconPath;
                using (var fileStream = new System.IO.FileStream(iconPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {
                    using (var image = System.Drawing.Image.FromStream(fileStream, false, false))
                    {
                        iconWidth = image.Width;
                        iconHeight = image.Height;
                    }
                }
            }

            // or, if the mode is set to "wallpaper", we will allow non-square icons
            if (mode == "wallpaper")
            {
                frame = "&amp;frame=pc";
            }
        }

        // remove App_Data/ from the relative icon path if it exists
        if (relativeIconPath.StartsWith("App_Data/", StringComparison.OrdinalIgnoreCase) || relativeIconPath.StartsWith("App_Data\\", StringComparison.OrdinalIgnoreCase))
        {
            relativeIconPath = relativeIconPath.Substring("App_Data/".Length);
        }

        // if the path is the default wallpaper, replace it with defaultwallpaper
        if (relativeIconPath == "lib/assets/wallpaper.png")
        {
            relativeIconPath = "defaultwallpaper";
        }

        // build the icons elements
        string iconElements = "<IconRaw FileType=\"Ico\" FileURL=\"" + Root() + "api/resources/image/" + relativeIconPath + "?format=ico" + frame + "\" />" + "\r\n";
        if (iconWidth >= 16)
        {
            iconElements += "<Icon16 Dimensions=\"16x16\" FileType=\"Png\" FileURL=\"" + Root() + "api/resources/image/" + relativeIconPath + "?format=png16" + frame + "\" />" + "\r\n";
        }
        if (iconWidth >= 32)
        {
            iconElements += "<Icon32 Dimensions=\"32x32\" FileType=\"Png\" FileURL=\"" + Root() + "api/resources/image/" + relativeIconPath + "?format=png32" + frame + "\" />" + "\r\n";
        }
        if (iconWidth >= 48)
        {
            iconElements += "<Icon48 Dimensions=\"48x48\" FileType=\"Png\" FileURL=\"" + Root() + "api/resources/image/" + relativeIconPath + "?format=png48" + frame + "\" />" + "\r\n";
        }
        if (iconWidth >= 64)
        {
            iconElements += "<Icon64 Dimensions=\"64x64\" FileType=\"Png\" FileURL=\"" + Root() + "api/resources/image/" + relativeIconPath + "?format=png64" + frame + "\" />" + "\r\n";
        }
        if (iconWidth >= 100)
        {
            iconElements += "<Icon100 Dimensions=\"100x100\" FileType=\"Png\" FileURL=\"" + Root() + "api/resources/image/" + relativeIconPath + "?format=png100" + frame + "\" />" + "\r\n";
        }
        if (iconWidth >= 256)
        {
            iconElements += "<Icon256 Dimensions=\"256x256\" FileType=\"Png\" FileURL=\"" + Root() + "api/resources/image/" + relativeIconPath + "?format=png256" + frame + "\" />" + "\r\n";
        }

        return iconElements;
    }

    // keep track of previous resource GUIDs to avoid duplicates
    string[] previousResourceGUIDs = new string[] { };

    private class Resource
    {
        public string Type { get; set; } // Desktop or RemoteApp
        public string Origin { get; set; } // rdp or registry
        public string FullAddress { get; set; }
        public string AppProgram { get; set; }
        public string Title { get; set; }
        public string Alias { get; set; }
        public string AppFileExtCSV { get; set; }
        public DateTime LastUpdated { get; set; }
        public string VirtualFolder { get; set; }
        public string Source { get; set; } // path the RDP file or registry entry
        public System.Guid Guid { get; set; }

        public bool IsApp
        {
            get
            {
                return Type == "RemoteApp";
            }
        }
        public bool IsDesktop
        {
            get
            {
                return Type == "Desktop";
            }
        }
        public string[] FileExtensions
        {
            get
            {
                if (string.IsNullOrEmpty(AppFileExtCSV))
                {
                    return new string[] { };
                }
                return AppFileExtCSV.Split(',');
            }
        }
        public string Id
        {
            get
            {
                return this.Guid.ToString();
            }
        }

        // the relative path to the RDP file
        private string ApplicationRootPath { get; set; }
        public string RelativePath
        {
            get
            {
                if (this.Origin == "rdp")
                {
                    return this.Source.Replace(HttpContext.Current.Server.MapPath(this.ApplicationRootPath), "").TrimStart('\\').TrimEnd('\\').Replace("\\", "/");
                }
                return this.Source;
            }
        }

        public Resource(string title, string fullAddress, string appProgram, string alias, string appFileExtCSV, DateTime lastUpdated, string virtualFolder, string origin, string source, string applicationRootPath)
        {
            this.ApplicationRootPath = applicationRootPath;
            this.VirtualFolder = virtualFolder;

            // full address is required because it is the connection address
            if (string.IsNullOrEmpty(fullAddress))
            {
                throw new ArgumentException("Full address cannot be null or empty.");
            }
            this.FullAddress = fullAddress;

            // we need to know if this is from the registry or and rdp file because
            // the icon and rdp file path logic is different
            if (string.IsNullOrEmpty(origin))
            {
                throw new ArgumentException("Origin cannot be null or empty. Use 'rdp' for RDP files or 'registry' for registry entries.");
            }
            if (origin != "rdp" && origin != "registry")
            {
                throw new ArgumentException("Origin must be either 'rdp' or 'registry'.");
            }
            this.Origin = origin;

            // source must be a valid path to an RDP file or registry entry
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentException("Source cannot be null or empty. It should be the path to the RDP file or registry entry.");
            }
            if (origin == "rdp" && !System.IO.File.Exists(source))
            {
                throw new ArgumentException("Source must be a valid path to an RDP file. " +
                    "Ensure the file exists at the specified path: " + source);
            }
            if (origin == "registry")
            {
                using (var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications\\" + source))
                {
                    if (regKey == null)
                    {
                        throw new ArgumentException("Source must be a valid application name in HKEY_LOCAL_MACHINE\\SOFTWARE\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications.");
                    }
                }
            }
            this.Source = source;

            // title should not be null or empty
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentException("Title cannot be null or empty.");
            }
            this.Title = title;
            this.Alias = alias;

            // if lastUpdated is empty, set it to the current time
            if (lastUpdated == DateTime.MinValue)
            {
                lastUpdated = DateTime.UtcNow;
            }
            this.LastUpdated = lastUpdated;

            // is app program is not provided, we assume it is a desktop
            if (string.IsNullOrEmpty(appProgram))
            {
                this.Type = "Desktop";
                return;
            }

            // process the remaining remote application properties
            this.Type = "RemoteApp";
            this.AppProgram = appProgram;
            this.AppFileExtCSV = appFileExtCSV;
        }

        public Resource SetGuid(System.Guid guid)
        {
            this.Guid = guid;
            return this;
        }

        public Resource SetGuid(string guidString)
        {
            if (string.IsNullOrEmpty(guidString))
            {
                throw new ArgumentException("GUID cannot be null or empty.");
            }
            try
            {
                this.Guid = new System.Guid(guidString);
                return this;
            }
            catch (FormatException)
            {
                throw new ArgumentException("GUID is not in a valid format: " + guidString);
            }
        }

        public Resource CalculateGuid(double schemaVersion, bool mergeTerminalServers)
        {
            CalculateGuid(this.Source, schemaVersion, mergeTerminalServers);
            return this;
        }

        public Resource CalculateGuid(string rdpFilePathOrContents, double schemaVersion, bool mergeTerminalServers)
        {
            // create a unique resource ID based on the RDP file contents
            string[] linesToOmit = mergeTerminalServers && this.IsApp ? new string[] { "full address:s:" } : null;
            this.Guid = GetResourceGUID(rdpFilePathOrContents, schemaVersion >= 2.0 ? "" : this.VirtualFolder, linesToOmit);
            return this;
        }

        public static System.Guid GetResourceGUID(string rdpFilePath, string suffix = "", string[] linesToOmit = null)
        {
            // read the entire contents of the file into a string
            // or if the file does not exist, treat the path as the contents
            string fileContents = "";
            try
            {
                fileContents = System.IO.File.ReadAllText(rdpFilePath);
            }
            catch
            {
                fileContents = rdpFilePath;
            }

            // alphabetically sort the lines in the file contents
            var lines = fileContents.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            Array.Sort(lines);
            fileContents = string.Join("\r\n", lines);

            // omit the full address from the hash calculation
            if (linesToOmit != null)
            {
                foreach (var lineToOmit in linesToOmit)
                {
                    fileContents = Regex.Replace(fileContents, @"(?m)^" + Regex.Escape(lineToOmit) + ".*$", "", RegexOptions.Multiline);
                }
            }

            // if there is a suffix, append it to the file contents
            if (!string.IsNullOrEmpty(suffix))
            {
                fileContents += suffix;
            }

            // generate a guid from the file contents
            var byt = Encoding.UTF8.GetBytes(fileContents);
            var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(byt);
            var guid = new Guid(hash);

            return guid;
        }
    }

    private void ProcessResource(Resource resource)
    {
        // if the terminal server is specified in the search parameters,
        // skip resources that do not match the terminal server
        if (!string.IsNullOrEmpty(searchParams["terminalServer"]) && resource.FullAddress != searchParams["terminalServer"])
        {
            return;
        }

        string resourceTimestamp = resource.LastUpdated.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // add the timestamp to the terminal server timestamps if it is the latest one
        if (!terminalServerTimestamps.ContainsKey(resource.FullAddress) || resource.LastUpdated > terminalServerTimestamps[resource.FullAddress])
        {
            terminalServerTimestamps[resource.FullAddress] = resource.LastUpdated;
        }

        // elements to use to create an injection point element for the folder element
        // that we can use to inject additional folders later
        string injectionPointElement = "<FolderInjectionPoint guid=\"" + resource.Id + "\"/>";
        string folderNameElement = "<Folder Name=\"" + (resource.VirtualFolder == "" ? "/" : resource.VirtualFolder) + "\" />" + "\r\n";

        //
        string tsInjectionPointElement = "<TerminalServerInjectionPoint guid=\"" + resource.Id + "\"/>";
        string tsElement = "<TerminalServerRef Ref=\"" + resource.FullAddress + "\" />" + "\r\n";
        string tsElements = "<HostingTerminalServer>" + "\r\n" +
            "<ResourceFile FileExtension=\".rdp\" URL=\"" + Root() + "get-rdp.aspx?from=" + resource.Origin + "&amp;path=" + resource.RelativePath + "\" />" + "\r\n" +
            tsElement +
            "</HostingTerminalServer>" + "\r\n";

        // ensure that the resource ID is unique: skip if it already exists
        if (Array.IndexOf(previousResourceGUIDs, resource.Id) >= 0)
        {
            string existingResources = resourcesBuffer.ToString();

            if (schemaVersion >= 2.0)
            {
                // ensure that the folder is not already in the list of folders for this resource
                int injectionPointIndex = existingResources.IndexOf(injectionPointElement);
                string frontTruncatedResources = existingResources.Substring(injectionPointIndex);
                int firstFoldersElemEndIndex = frontTruncatedResources.IndexOf("</Folders>");
                string currentFoldersElements = frontTruncatedResources.Substring(0, firstFoldersElemEndIndex);
                bool folderAlreadyExists = currentFoldersElements.Contains(folderNameElement.Trim());

                if (!folderAlreadyExists)
                {
                    // insert this folder element in front of the injection point element
                    resourcesBuffer = resourcesBuffer.Replace(injectionPointElement, injectionPointElement + folderNameElement);
                }
            }

            if (searchParams["mergeTerminalServers"] == "1")
            {
                // ensure that the terminal server is not already in the list of terminal servers for this resource
                int tsInjectionPointIndex = existingResources.IndexOf(tsInjectionPointElement);
                string tsFrontTruncatedResources = existingResources.Substring(tsInjectionPointIndex);
                int firstTerminalServerElemEndIndex = tsFrontTruncatedResources.IndexOf("</HostingTerminalServers>");
                string currentTerminalServerElements = tsFrontTruncatedResources.Substring(0, firstTerminalServerElemEndIndex);
                bool terminalServerAlreadyExists = currentTerminalServerElements.Contains(tsElement.Trim());

                if (!terminalServerAlreadyExists)
                {
                    // insert this terminal server element in front of the injection point element
                    resourcesBuffer = resourcesBuffer.Replace(tsInjectionPointElement, tsInjectionPointElement + tsElements);
                }
            }

            return;
        }

        // construct the resource element
        resourcesBuffer.Append("<Resource ID=\"" + resource.Id + "\" Alias=\"" + resource.Alias + "\" Title=\"" + resource.Title + "\" LastUpdated=\"" + resourceTimestamp + "\" Type=\"" + resource.Type + "\"" + (schemaVersion >= 2.1 ? " ShowByDefault=\"True\"" : "") + ">" + "\r\n");
        resourcesBuffer.Append("<Icons>" + "\r\n");
        resourcesBuffer.Append(GetIconElements((resource.Origin == "registry" ? "registry!" : "") + resource.RelativePath.Replace(".rdp", ""), resource.IsDesktop ? "wallpaper" : "none", resource.IsDesktop ? "lib/assets/wallpaper.png" : "default.ico"));
        resourcesBuffer.Append("</Icons>" + "\r\n");
        if (resource.FileExtensions.Length > 0)
        {
            resourcesBuffer.Append("<FileExtensions>" + "\r\n");
            foreach (string fileExt in resource.FileExtensions)
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
                    // if the icon exists, add it to the resource
                    string maybeIconElements = GetIconElements(relativeIconPath: (resource.Origin == "registry" ? ("registry!" + fileExt.Replace(".", "") + ":") : "") + resource.RelativePath.Replace(".rdp", resource.Origin == "registry" ? "" : fileExt), skipMissing: true);
                    if (!string.IsNullOrEmpty(maybeIconElements))
                    {
                        resourcesBuffer.Append("<FileAssociationIcons>" + "\r\n");
                        resourcesBuffer.Append(maybeIconElements);
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
            resourcesBuffer.Append(injectionPointElement);
            resourcesBuffer.Append(folderNameElement);
            resourcesBuffer.Append("</Folders>" + "\r\n");
        }
        resourcesBuffer.Append("<HostingTerminalServers>" + "\r\n");
        resourcesBuffer.Append(tsInjectionPointElement);
        resourcesBuffer.Append(tsElements);
        resourcesBuffer.Append("</HostingTerminalServers>" + "\r\n");
        resourcesBuffer.Append("</Resource>" + "\r\n");

        // add the resource ID to the list of previous resource GUIDs to avoid duplicates
        Array.Resize(ref previousResourceGUIDs, previousResourceGUIDs.Length + 1);
        previousResourceGUIDs[previousResourceGUIDs.Length - 1] = resource.Id;
    }

    public AliasResolver resolver = new AliasResolver();

    private void ProcessRegistryResources()
    {
        string publisherName = resolver.Resolve(System.Net.Dns.GetHostName());

        // get the registry entries for the remote applications
        using (var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications"))
        {
            if (regKey == null)
            {
                return; // no remote applications found
            }

            foreach (string appName in regKey.GetSubKeyNames())
            {
                using (var appKey = regKey.OpenSubKey(appName))
                {
                    if (appKey == null)
                    {
                        continue; // skip if the application key is not found
                    }

                    bool showInTSWA = appKey.GetValue("ShowInTSWA") as int? == 1;
                    if (!showInTSWA)
                    {
                        continue; // skip if the application is not allowed to be shown in the webfeed
                    }

                    string appProgram = appKey.GetValue("Path") as string;
                    if (string.IsNullOrEmpty(appProgram))
                    {
                        continue; // skip if the application path ismissing
                    }

                    bool hasPermission = RegistryUtilities.Reader.CanAccessRemoteApp(appKey, getAuthenticatedUserInfo());
                    if (!hasPermission)
                    {
                        continue; // skip if the user does not have permission to access the application
                    }

                    string appFileExtCSV = "";
                    using (var fileTypesKey = appKey.OpenSubKey("Filetypes"))
                    {
                        if (fileTypesKey != null)
                        {
                            string[] fileTypeNames = fileTypesKey.GetValueNames();
                            if (fileTypeNames.Length > 0)
                            {
                                appFileExtCSV = "." + string.Join(",.", fileTypeNames);
                            }
                        }
                    }

                    // get the display name of the application from the registry (if available),
                    // but fall back to the key name if not available
                    string displayName = (appKey.GetValue("Name") as string) ?? appName;

                    // get the generated rdp file
                    string rdpFileContents = RegistryUtilities.Reader.ConstructRdpFileFromRegistry(appName);

                    // create a resource from the registry entry
                    Resource resource = new Resource(
                        title: displayName,
                        fullAddress: publisherName,
                        appProgram: appProgram,
                        alias: "registry/" + appName,
                        appFileExtCSV: appFileExtCSV,
                        lastUpdated: DateTime.UtcNow,
                        virtualFolder: "",
                        origin: "registry",
                        source: appName,
                        applicationRootPath: Root()
                    ).CalculateGuid(rdpFileContents, schemaVersion, searchParams["mergeTerminalServers"] == "1");

                    ProcessResource(resource);
                }
            }
        }
    }

    /// <summary>
    /// Processes the resources in the specified directory and its subdirectors and adds them to the resources buffer.
    /// <br/>
    /// This method will recursively search for RDP files in the directory and its subdirectories.
    /// </summary>
    /// <param name="directoryPath">The directory to use when searching for RDP files.</param>
    /// <param name="virtualFolder">Provide a the resource's virtual folder in the webfeed. Virtual folders should start with a forward slash (/) and NOT end with a forward slash (/). If not provided, the root virtual folder will be used.</param>
    private void ProcessResources(string directoryPath, string virtualFolder = "")
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
            string subVirtualFolder = virtualFolder + "/" + System.IO.Path.GetFileName(subDirectory);
            ProcessResources(subDirectory, subVirtualFolder);
        }

        string[] allfiles = System.IO.Directory.GetFiles(directoryPath, "*.rdp");
        foreach (string eachfile in allfiles)
        {
            if (!(GetRDPvalue(eachfile, "full address:s:") == ""))
            {
                bool hasPermission = FileSystemUtilities.Reader.CanAccessPath(eachfile, getAuthenticatedUserInfo());
                if (!hasPermission)
                {
                    continue; // skip if the user does not have permission to access the rdp file
                }

                // get the basefilename and remove the last 4 characters (.rdp)
                string basefilename = System.IO.Path.GetFileName(eachfile).Substring(0, System.IO.Path.GetFileName(eachfile).Length - 4);

                // extract full relative path from the directoryPath (including the resources or multiuser-resources folder)
                string relativePathFull = directoryPath.Replace(HttpContext.Current.Server.MapPath(Root()), "").TrimStart('\\').TrimEnd('\\').Replace("\\", "/") + "/";

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

                // prepare the info for the resource
                var resource = new Resource(
                    title: GetRDPvalue(eachfile, "remoteapplicationname:s:", basefilename), // set the app title to the base filename if the remote application name is empty
                    fullAddress: GetRDPvalue(eachfile, "full address:s:"),
                    appProgram: GetRDPvalue(eachfile, "remoteapplicationprogram:s:"),
                    alias: relativePathFull + basefilename + ".rdp",
                    appFileExtCSV: GetRDPvalue(eachfile, "remoteapplicationfileextensions:s:"),
                    lastUpdated: resourceDateTime,
                    virtualFolder: virtualFolder,
                    origin: "rdp",
                    source: directoryPath + "\\" + System.IO.Path.GetFileName(System.IO.Path.GetFileName(eachfile)),
                    applicationRootPath: Root()
                ).CalculateGuid(schemaVersion, searchParams["mergeTerminalServers"] == "1");

                // process the resource
                ProcessResource(resource);
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

        AuthUtilities.UserInformation userInfo = getAuthenticatedUserInfo();

        bool showGroupAndUserNames = System.Configuration.ConfigurationManager.AppSettings["Workspace.ShowMultiuserResourcesUserAndGroupNames"] != "false";

        // Process resources in basePath\\user\\ [username]

        string UserFolder = directoryPath + "\\user\\" + userInfo.Username + "\\";
        if (System.IO.Directory.Exists(UserFolder))
        {
            string virtualFolder = showGroupAndUserNames ? "/" + userInfo.FullName : "";
            ProcessResources(UserFolder, virtualFolder);
        }

        // Process resources in basePath\\group\\ [group name]
        // and basePath\\group\\ [group SID]

        foreach (AuthUtilities.GroupInformation group in userInfo.Groups)
        {
            string virtualFolder = showGroupAndUserNames ? "/" + group.Name : "";

            string GroupNameFolder = directoryPath + "\\group\\" + group.Name + "\\";
            if (System.IO.Directory.Exists(GroupNameFolder))
            {
                ProcessResources(GroupNameFolder, virtualFolder);
            }

            string GroupSidFolder = directoryPath + "\\group\\" + group.Sid + "\\";
            if (System.IO.Directory.Exists(GroupSidFolder))
            {
                ProcessResources(GroupSidFolder, virtualFolder);
            }
        }
    }
}
