using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using RAWebServer.Utilities;

public partial class GetWorkspace : System.Web.UI.Page {
    protected void Page_Load(object sender, EventArgs e) {
        if (getAuthenticatedUserInfo() == null) {
            Response.Redirect("auth/loginfeed.aspx");
        }
        else {
            if (HttpContext.Current.Request.Headers.GetValues("accept").FirstOrDefault().ToLower().Contains("radc_schema_version=2.0")) {
                _schemaVersion = 2.0;
            }
            else if (HttpContext.Current.Request.Headers.GetValues("accept").FirstOrDefault().ToLower().Contains("radc_schema_version=2.1")) {
                _schemaVersion = 2.1;
            }

            // process resources
            var resourcesFolder = "App_Data/resources";
            var multiuserResourcesFolder = "App_Data/multiuser-resources";
            if (System.Configuration.ConfigurationManager.AppSettings["RegistryApps.Enabled"] == "true") {
                ProcessRegistryResources();
            }
            ProcessResources(resourcesFolder);
            ProcessMultiuserResources(multiuserResourcesFolder);

            HttpContext.Current.Response.ContentType = (_schemaVersion >= 2.0 ? "application/x-msts-radc+xml; charset=utf-8" : "text/xml; charset=utf-8");
            var serverName = _searchParams["terminalServer"] ?? System.Net.Dns.GetHostName();
            var serverFQDN = HttpContext.Current.Request.Url.Host;
            var datetime = DateTime.Now.Year.ToString() + "-" + (DateTime.Now.Month + 100).ToString().Substring(1, 2) + "-" + (DateTime.Now.Day + 100).ToString().Substring(1, 2) + "T" + (DateTime.Now.Hour + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Minute + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Second + 100).ToString().Substring(1, 2) + ".0Z";

            // calculate publisher details
            var resolver = new AliasResolver();
            var publisherName = resolver.Resolve(serverName);
            var publisherDateTime = DateTime.MinValue;
            foreach (var terminalServer in _terminalServerTimestamps.Keys) {
                var serverTimestamp = _terminalServerTimestamps[terminalServer];
                if (serverTimestamp > publisherDateTime) {
                    publisherDateTime = serverTimestamp;
                }
            }
            var publisherTimestamp = publisherDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

            HttpContext.Current.Response.Write("<ResourceCollection PubDate=\"" + datetime + "\" SchemaVersion=\"" + _schemaVersion.ToString() + "\" " + (_schemaVersion >= 2.0 ? "SupportsReconnect=\"false\" " : "") + "xmlns=\"http://schemas.microsoft.com/ts/2007/05/tswf\">" + "\r\n");
            HttpContext.Current.Response.Write("<Publisher LastUpdated=\"" + publisherTimestamp + "\" Name=\"" + publisherName + "\" ID=\"" + serverFQDN + "\" Description=\"\">" + "\r\n");

            HttpContext.Current.Response.Write("<Resources>" + "\r\n");
            var resourcesXML = _resourcesBuffer.ToString();
            resourcesXML = Regex.Replace(resourcesXML, @"<FolderInjectionPoint.*?/>", "");
            resourcesXML = Regex.Replace(resourcesXML, @"<TerminalServerInjectionPoint.*?/>", "");
            HttpContext.Current.Response.Write(resourcesXML);
            HttpContext.Current.Response.Write("</Resources>" + "\r\n");

            HttpContext.Current.Response.Write("<TerminalServers>" + "\r\n");
            foreach (var terminalServer in _terminalServerTimestamps.Keys) {
                var terminalServerName = terminalServer;
                var terminalServerTimestamp = _terminalServerTimestamps[terminalServer].ToString("yyyy-MM-ddTHH:mm:ssZ");
                HttpContext.Current.Response.Write("<TerminalServer ID=\"" + terminalServerName + "\" LastUpdated=\"" + terminalServerTimestamp + "\" />" + "\r\n");
            }
            HttpContext.Current.Response.Write("</TerminalServers>" + "\r\n");
            HttpContext.Current.Response.Write("</Publisher>" + "\r\n");
            HttpContext.Current.Response.Write("</ResourceCollection>" + "\r\n");
            HttpContext.Current.Response.End();
        }
    }

    public string Root() {
        var DocPath = HttpContext.Current.Request.ServerVariables["PATH_INFO"];
        var aPath = ("/" + DocPath).Split('/');
        var Root = DocPath.Substring(0, (DocPath.Length - aPath[aPath.GetUpperBound(0)].Length));
        return Root;
    }

    public UserInformation getAuthenticatedUserInfo() {
        var authCookieHandler = new AuthCookieHandler();
        return authCookieHandler.GetUserInformationSafe(Request);
    }

    private StringBuilder _resourcesBuffer = new StringBuilder();
    private readonly Dictionary<string, DateTime> _terminalServerTimestamps = new Dictionary<string, DateTime>();
    private double _schemaVersion = 1.0;

    private readonly NameValueCollection _searchParams = HttpUtility.ParseQueryString(HttpContext.Current.Request.Url.Query);

    // keep track of previous resource GUIDs to avoid duplicates
    string[] _previousResourceGUIDs = new string[] { };

    private class Resource {
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
        public Guid Guid { get; set; }

        public bool IsApp {
            get {
                return Type == "RemoteApp";
            }
        }
        public bool IsDesktop {
            get {
                return Type == "Desktop";
            }
        }
        public string[] FileExtensions {
            get {
                if (string.IsNullOrEmpty(AppFileExtCSV)) {
                    return new string[] { };
                }
                return AppFileExtCSV.Split(',');
            }
        }
        public string Id {
            get {
                return this.Guid.ToString();
            }
        }

        // the relative path to the RDP file
        private string ApplicationRootPath { get; set; }
        public string RelativePath {
            get {
                if (this.Origin == "rdp") {
                    return this.Source.Replace(HttpContext.Current.Server.MapPath(this.ApplicationRootPath), "").TrimStart('\\').TrimEnd('\\').Replace("\\", "/");
                }
                return this.Source;
            }
        }

        public Resource(string title, string fullAddress, string appProgram, string alias, string appFileExtCSV, DateTime lastUpdated, string virtualFolder, string origin, string source, string applicationRootPath) {
            this.ApplicationRootPath = applicationRootPath;
            this.VirtualFolder = virtualFolder;

            // full address is required because it is the connection address
            if (string.IsNullOrEmpty(fullAddress)) {
                throw new ArgumentException("Full address cannot be null or empty.");
            }
            this.FullAddress = fullAddress;

            // we need to know if this is from the registry or and rdp file because
            // the icon and rdp file path logic is different
            if (string.IsNullOrEmpty(origin)) {
                throw new ArgumentException("Origin cannot be null or empty. Use 'rdp' for RDP files or 'registry' for registry entries.");
            }
            if (origin != "rdp" && origin != "registry") {
                throw new ArgumentException("Origin must be either 'rdp' or 'registry'.");
            }
            this.Origin = origin;

            // source must be a valid path to an RDP file or registry entry
            if (string.IsNullOrEmpty(source)) {
                throw new ArgumentException("Source cannot be null or empty. It should be the path to the RDP file or registry entry.");
            }
            if (origin == "rdp" && !System.IO.File.Exists(source)) {
                throw new ArgumentException("Source must be a valid path to an RDP file. " +
                    "Ensure the file exists at the specified path: " + source);
            }
            if (origin == "registry") {
                using (var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications\\" + source)) {
                    if (regKey == null) {
                        throw new ArgumentException("Source must be a valid application name in HKEY_LOCAL_MACHINE\\SOFTWARE\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications.");
                    }
                }
            }
            this.Source = source;

            // title should not be null or empty
            if (string.IsNullOrEmpty(title)) {
                throw new ArgumentException("Title cannot be null or empty.");
            }
            this.Title = title;
            this.Alias = alias;

            // if lastUpdated is empty, set it to the current time
            if (lastUpdated == DateTime.MinValue) {
                lastUpdated = DateTime.UtcNow;
            }
            this.LastUpdated = lastUpdated;

            // is app program is not provided, we assume it is a desktop
            if (string.IsNullOrEmpty(appProgram)) {
                this.Type = "Desktop";
                return;
            }

            // process the remaining remote application properties
            this.Type = "RemoteApp";
            this.AppProgram = appProgram;
            this.AppFileExtCSV = appFileExtCSV;
        }

        public Resource SetGuid(Guid guid) {
            this.Guid = guid;
            return this;
        }

        public Resource SetGuid(string guidString) {
            if (string.IsNullOrEmpty(guidString)) {
                throw new ArgumentException("GUID cannot be null or empty.");
            }
            try {
                this.Guid = new Guid(guidString);
                return this;
            }
            catch (FormatException) {
                throw new ArgumentException("GUID is not in a valid format: " + guidString);
            }
        }

        public Resource CalculateGuid(double schemaVersion, bool mergeTerminalServers) {
            CalculateGuid(this.Source, schemaVersion, mergeTerminalServers);
            return this;
        }

        public Resource CalculateGuid(string rdpFilePathOrContents, double schemaVersion, bool mergeTerminalServers) {
            // create a unique resource ID based on the RDP file contents
            var linesToOmit = mergeTerminalServers && this.IsApp ? new string[] { "full address:s:" } : null;
            this.Guid = GetResourceGUID(rdpFilePathOrContents, schemaVersion >= 2.0 ? "" : this.VirtualFolder, linesToOmit);
            return this;
        }

        public static Guid GetResourceGUID(string rdpFilePath, string suffix = "", string[] linesToOmit = null) {
            // read the entire contents of the file into a string
            // or if the file does not exist, treat the path as the contents
            string fileContents;
            try {
                fileContents = System.IO.File.ReadAllText(rdpFilePath);
            }
            catch {
                fileContents = rdpFilePath;
            }

            // alphabetically sort the lines in the file contents
            var lines = fileContents.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            Array.Sort(lines);
            fileContents = string.Join("\r\n", lines);

            // omit the full address from the hash calculation
            if (linesToOmit != null) {
                foreach (var lineToOmit in linesToOmit) {
                    fileContents = Regex.Replace(fileContents, @"(?m)^" + Regex.Escape(lineToOmit) + ".*$", "", RegexOptions.Multiline);
                }
            }

            // if there is a suffix, append it to the file contents
            if (!string.IsNullOrEmpty(suffix)) {
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

    private void ProcessResource(Resource resource) {
        // if the terminal server is specified in the search parameters,
        // skip resources that do not match the terminal server
        if (!string.IsNullOrEmpty(_searchParams["terminalServer"]) && resource.FullAddress != _searchParams["terminalServer"]) {
            return;
        }

        var resourceTimestamp = resource.LastUpdated.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // add the timestamp to the terminal server timestamps if it is the latest one
        if (!_terminalServerTimestamps.ContainsKey(resource.FullAddress) || resource.LastUpdated > _terminalServerTimestamps[resource.FullAddress]) {
            _terminalServerTimestamps[resource.FullAddress] = resource.LastUpdated;
        }

        // elements to use to create an injection point element for the folder element
        // that we can use to inject additional folders later
        var injectionPointElement = "<FolderInjectionPoint guid=\"" + resource.Id + "\"/>";
        var folderNameElement = "<Folder Name=\"" + (resource.VirtualFolder == "" ? "/" : resource.VirtualFolder) + "\" />" + "\r\n";

        //
        var apiResourcePath = resource.RelativePath;
        if (apiResourcePath.StartsWith("App_Data/", StringComparison.OrdinalIgnoreCase) || apiResourcePath.StartsWith("App_Data\\", StringComparison.OrdinalIgnoreCase)) {
            apiResourcePath = apiResourcePath.Substring("App_Data/".Length);
        }
        var tsInjectionPointElement = "<TerminalServerInjectionPoint guid=\"" + resource.Id + "\"/>";
        var tsElement = "<TerminalServerRef Ref=\"" + resource.FullAddress + "\" />" + "\r\n";
        var tsElements = "<HostingTerminalServer>" + "\r\n" +
            "<ResourceFile FileExtension=\".rdp\" URL=\"" + Root() + "api/resources/" + apiResourcePath + (resource.Origin == "registry" ? "?from=registry" : "") + "\" />" + "\r\n" +
            tsElement +
            "</HostingTerminalServer>" + "\r\n";

        // ensure that the resource ID is unique: skip if it already exists
        if (Array.IndexOf(_previousResourceGUIDs, resource.Id) >= 0) {
            var existingResources = _resourcesBuffer.ToString();

            if (_schemaVersion >= 2.0) {
                // ensure that the folder is not already in the list of folders for this resource
                var injectionPointIndex = existingResources.IndexOf(injectionPointElement);
                var frontTruncatedResources = existingResources.Substring(injectionPointIndex);
                var firstFoldersElemEndIndex = frontTruncatedResources.IndexOf("</Folders>");
                var currentFoldersElements = frontTruncatedResources.Substring(0, firstFoldersElemEndIndex);
                var folderAlreadyExists = currentFoldersElements.Contains(folderNameElement.Trim());

                if (!folderAlreadyExists) {
                    // insert this folder element in front of the injection point element
                    _resourcesBuffer = _resourcesBuffer.Replace(injectionPointElement, injectionPointElement + folderNameElement);
                }
            }

            if (_searchParams["mergeTerminalServers"] == "1") {
                // ensure that the terminal server is not already in the list of terminal servers for this resource
                var tsInjectionPointIndex = existingResources.IndexOf(tsInjectionPointElement);
                var tsFrontTruncatedResources = existingResources.Substring(tsInjectionPointIndex);
                var firstTerminalServerElemEndIndex = tsFrontTruncatedResources.IndexOf("</HostingTerminalServers>");
                var currentTerminalServerElements = tsFrontTruncatedResources.Substring(0, firstTerminalServerElemEndIndex);
                var terminalServerAlreadyExists = currentTerminalServerElements.Contains(tsElement.Trim());

                if (!terminalServerAlreadyExists) {
                    // insert this terminal server element in front of the injection point element
                    _resourcesBuffer = _resourcesBuffer.Replace(tsInjectionPointElement, tsInjectionPointElement + tsElements);
                }
            }

            return;
        }

        // throw new Exception("resources/" + resource.RelativePath.Replace(".rdp", ""));

        // construct the resource element
        _resourcesBuffer.Append("<Resource ID=\"" + resource.Id + "\" Alias=\"" + resource.Alias + "\" Title=\"" + resource.Title + "\" LastUpdated=\"" + resourceTimestamp + "\" Type=\"" + resource.Type + "\"" + (_schemaVersion >= 2.1 ? " ShowByDefault=\"True\"" : "") + ">" + "\r\n");
        _resourcesBuffer.Append("<Icons>" + "\r\n");
        _resourcesBuffer.Append(ResourceUtilities.ConstructIconElements(getAuthenticatedUserInfo(), (resource.Origin == "registry" ? "registry!" : "") + resource.RelativePath.Replace("App_Data/", "").Replace(".rdp", ""), resource.IsDesktop ? ResourceUtilities.IconElementsMode.Wallpaper : ResourceUtilities.IconElementsMode.Icon, resource.IsDesktop ? "../lib/assets/wallpaper.png" : "../lib/assets/default.ico"));
        _resourcesBuffer.Append("</Icons>" + "\r\n");
        if (resource.FileExtensions.Length > 0) {
            _resourcesBuffer.Append("<FileExtensions>" + "\r\n");
            foreach (var fileExt in resource.FileExtensions) {
                if (_schemaVersion >= 2.0) {
                    _resourcesBuffer.Append("<FileExtension Name=\"" + fileExt + "\" PrimaryHandler=\"True\">" + "\r\n");
                }
                else {
                    _resourcesBuffer.Append("<FileExtension Name=\"" + fileExt + "\" >" + "\r\n");
                }

                if (_schemaVersion >= 2.0) {
                    // if the icon exists, add it to the resource
                    var maybeIconElements = ResourceUtilities.ConstructIconElements(getAuthenticatedUserInfo(), (resource.Origin == "registry" ? ("registry!" + fileExt.Replace(".", "") + ":") : "") + resource.RelativePath.Replace("App_Data/", "").Replace(".rdp", resource.Origin == "registry" ? "" : fileExt), resource.IsDesktop ? ResourceUtilities.IconElementsMode.Wallpaper : ResourceUtilities.IconElementsMode.Icon, resource.IsDesktop ? "../lib/assets/wallpaper.png" : "../lib/assets/default.ico", skipMissing: true);
                    if (!string.IsNullOrEmpty(maybeIconElements)) {
                        _resourcesBuffer.Append("<FileAssociationIcons>" + "\r\n");
                        _resourcesBuffer.Append(maybeIconElements);
                        _resourcesBuffer.Append("</FileAssociationIcons>" + "\r\n");
                    }
                }

                _resourcesBuffer.Append("</FileExtension>" + "\r\n");
            }
            _resourcesBuffer.Append("</FileExtensions>" + "\r\n");
        }
        else {
            _resourcesBuffer.Append("<FileExtensions />" + "\r\n");
        }
        if (_schemaVersion >= 2.0) {
            _resourcesBuffer.Append("<Folders>" + "\r\n");
            _resourcesBuffer.Append(injectionPointElement);
            _resourcesBuffer.Append(folderNameElement);
            _resourcesBuffer.Append("</Folders>" + "\r\n");
        }
        _resourcesBuffer.Append("<HostingTerminalServers>" + "\r\n");
        _resourcesBuffer.Append(tsInjectionPointElement);
        _resourcesBuffer.Append(tsElements);
        _resourcesBuffer.Append("</HostingTerminalServers>" + "\r\n");
        _resourcesBuffer.Append("</Resource>" + "\r\n");

        // add the resource ID to the list of previous resource GUIDs to avoid duplicates
        Array.Resize(ref _previousResourceGUIDs, _previousResourceGUIDs.Length + 1);
        _previousResourceGUIDs[_previousResourceGUIDs.Length - 1] = resource.Id;
    }

    public AliasResolver resolver = new AliasResolver();

    private void ProcessRegistryResources() {
        var publisherName = resolver.Resolve(System.Net.Dns.GetHostName());

        // get the registry entries for the remote applications
        using (var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications")) {
            if (regKey == null) {
                return; // no remote applications found
            }

            foreach (var appName in regKey.GetSubKeyNames()) {
                using (var appKey = regKey.OpenSubKey(appName)) {
                    if (appKey == null) {
                        continue; // skip if the application key is not found
                    }

                    var showInTSWA = appKey.GetValue("ShowInTSWA") as int? == 1;
                    if (!showInTSWA) {
                        continue; // skip if the application is not allowed to be shown in the webfeed
                    }

                    var appProgram = appKey.GetValue("Path") as string;
                    if (string.IsNullOrEmpty(appProgram)) {
                        continue; // skip if the application path is missing
                    }

                    var hasPermission = RegistryReader.CanAccessRemoteApp(appKey, getAuthenticatedUserInfo());
                    if (!hasPermission) {
                        continue; // skip if the user does not have permission to access the application
                    }

                    var appFileExtCSV = "";
                    using (var fileTypesKey = appKey.OpenSubKey("Filetypes")) {
                        if (fileTypesKey != null) {
                            var fileTypeNames = fileTypesKey.GetValueNames();
                            if (fileTypeNames.Length > 0) {
                                appFileExtCSV = "." + string.Join(",.", fileTypeNames);
                            }
                        }
                    }

                    // get the display name of the application from the registry (if available),
                    // but fall back to the key name if not available
                    var displayName = (appKey.GetValue("Name") as string) ?? appName;

                    // get the generated rdp file
                    var rdpFileContents = RegistryReader.ConstructRdpFileFromRegistry(appName);

                    // create a resource from the registry entry
                    var resource = new Resource(
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
                    ).CalculateGuid(rdpFileContents, _schemaVersion, _searchParams["mergeTerminalServers"] == "1");

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
    private void ProcessResources(string directoryPath, string virtualFolder = "") {
        // check if directorypath is a relative path or physical path

        if (System.IO.Directory.Exists(directoryPath) == false) {
            var fullRelativePath = Root() + directoryPath;
            directoryPath = HttpContext.Current.Server.MapPath(fullRelativePath);
        }

        var subDirectories = System.IO.Directory.GetDirectories(directoryPath);
        foreach (var subDirectory in subDirectories) {
            var subVirtualFolder = virtualFolder + "/" + System.IO.Path.GetFileName(subDirectory);
            ProcessResources(subDirectory, subVirtualFolder);
        }

        var allfiles = System.IO.Directory.GetFiles(directoryPath, "*.rdp");
        foreach (var eachfile in allfiles) {
            if (!(ResourceUtilities.GetRdpFileProperty(eachfile, "full address:s:") == "")) {
                var hasPermission = FileAccessInfo.CanAccessPath(eachfile, getAuthenticatedUserInfo());
                if (!hasPermission) {
                    continue; // skip if the user does not have permission to access the rdp file
                }

                // get the basefilename and remove the last 4 characters (.rdp)
                var basefilename = System.IO.Path.GetFileName(eachfile).Substring(0, System.IO.Path.GetFileName(eachfile).Length - 4);

                // extract full relative path from the directoryPath (including the resources or multiuser-resources folder)
                var relativePathFull = directoryPath.Replace(HttpContext.Current.Server.MapPath(Root()), "").TrimStart('\\').TrimEnd('\\').Replace("\\", "/") + "/";

                // get the paths to all files that start with the same basename as the rdp file
                // (e.g., get: *.rdp, *.ico, *.png, *.xlsx.ico, *.xls.png, etc.)
                var allResourceFiles = System.IO.Directory.GetFiles(directoryPath, basefilename + ".*");

                // calculate the timestamp for the resource, which is the latest of the rdp file and icon files
                var resourceDateTime = System.IO.File.GetLastWriteTimeUtc(eachfile);
                foreach (var resourceFile in allResourceFiles) {
                    var fileDateTime = System.IO.File.GetLastWriteTimeUtc(resourceFile);
                    if (fileDateTime > resourceDateTime) {
                        resourceDateTime = fileDateTime;
                    }
                }

                // prepare the info for the resource
                var resource = new Resource(
                    title: ResourceUtilities.GetRdpFileProperty(eachfile, "remoteapplicationname:s:", basefilename), // set the app title to the base filename if the remote application name is empty
                    fullAddress: ResourceUtilities.GetRdpFileProperty(eachfile, "full address:s:"),
                    appProgram: ResourceUtilities.GetRdpFileProperty(eachfile, "remoteapplicationprogram:s:").Replace("|", ""),
                    alias: relativePathFull + basefilename + ".rdp",
                    appFileExtCSV: ResourceUtilities.GetRdpFileProperty(eachfile, "remoteapplicationfileextensions:s:"),
                    lastUpdated: resourceDateTime,
                    virtualFolder: virtualFolder,
                    origin: "rdp",
                    source: directoryPath + "\\" + System.IO.Path.GetFileName(System.IO.Path.GetFileName(eachfile)),
                    applicationRootPath: Root()
                ).CalculateGuid(_schemaVersion, _searchParams["mergeTerminalServers"] == "1");

                // process the resource
                ProcessResource(resource);
            }
        }
    }

    private void ProcessMultiuserResources(string directoryPath) {
        if (System.IO.Directory.Exists(directoryPath) == false) {
            var fullRelativePath = Root() + directoryPath;
            directoryPath = HttpContext.Current.Server.MapPath(fullRelativePath);
        }

        var userInfo = getAuthenticatedUserInfo();

        var showGroupAndUserNames = System.Configuration.ConfigurationManager.AppSettings["Workspace.ShowMultiuserResourcesUserAndGroupNames"] != "false";

        // Process resources in basePath\\user\\ [username]

        var UserFolder = directoryPath + "\\user\\" + userInfo.Username + "\\";
        if (System.IO.Directory.Exists(UserFolder)) {
            var virtualFolder = showGroupAndUserNames ? "/" + userInfo.FullName : "";
            ProcessResources(UserFolder, virtualFolder);
        }

        // Process resources in basePath\\group\\ [group name]
        // and basePath\\group\\ [group SID]

        foreach (var group in userInfo.Groups) {
            var virtualFolder = showGroupAndUserNames ? "/" + group.Name : "";

            var GroupNameFolder = directoryPath + "\\group\\" + group.Name + "\\";
            if (System.IO.Directory.Exists(GroupNameFolder)) {
                ProcessResources(GroupNameFolder, virtualFolder);
            }

            var GroupSidFolder = directoryPath + "\\group\\" + group.Sid + "\\";
            if (System.IO.Directory.Exists(GroupSidFolder)) {
                ProcessResources(GroupSidFolder, virtualFolder);
            }
        }
    }
}
