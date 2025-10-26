using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;

namespace RAWebServer.Utilities {
    /// <summary>
    /// A builder class for constructing a MS-TSWP-compliant workspace
    /// XML string that represents a collection of Remote Desktop resources.
    /// </summary>
    public class WorkspaceBuilder {
        public enum SchemaVersion {
            v1 = 1,
            v2 = 2,
            v2_1 = 3
        }

        private readonly AliasResolver _resolver = new AliasResolver();

        private readonly UserInformation _authenticatedUserInfo = null;
        private readonly bool _mergeTerminalServers = false;
        private readonly string _terminalServerFilter = null;

        private readonly string _iisBase = VirtualPathUtility.ToAbsolute("~/");
        private double _schemaVersion = 1.0;

        private StringBuilder _resourcesBuffer = new StringBuilder();
        private readonly Dictionary<string, DateTime> _terminalServerTimestamps = new Dictionary<string, DateTime>();

        // keep track of previous resource GUIDs to avoid duplicates
        string[] _previousResourceGUIDs = new string[] { };

        readonly string _fullyQualifiedDomainName = "";

        /// <summary>
        /// Initializes a new instance of the <c>WorkspaceBuilder</c> class.
        /// </summary>
        /// <param name="version">The version of the MS-TWSP.</param>
        /// <param name="authenticatedUserInfo"></param>
        /// <param name="fullyQualifiedDomainName">The fully qualified domain name, used for the ID in the workspace XML. See HttpContext.Current.Request.Url.Host.</param>
        /// <param name="mergeTerminalServers">Whether identical resources across multiple terminal servers are provided as a single resource with mnultiple terminal servers. When this option is false, each resource is listed separately even though the resources are the same.</param>
        /// <param name="terminalServerFilter">Filter the resources to the specified terminal server.</param>
        /// <exception cref="ArgumentException"></exception>
        public WorkspaceBuilder(SchemaVersion version, UserInformation authenticatedUserInfo, string fullyQualifiedDomainName, bool mergeTerminalServers = false, string terminalServerFilter = null) {
            if (version == SchemaVersion.v1) {
                _schemaVersion = 1.0;
            }
            else if (version == SchemaVersion.v2) {
                _schemaVersion = 2.0;
            }
            else if (version == SchemaVersion.v2_1) {
                _schemaVersion = 2.1;
            }
            else {
                throw new ArgumentException("Unsupported workspace version: " + version.ToString());
            }

            _authenticatedUserInfo = authenticatedUserInfo;
            _fullyQualifiedDomainName = fullyQualifiedDomainName;
            _mergeTerminalServers = mergeTerminalServers;
            _terminalServerFilter = string.IsNullOrEmpty(terminalServerFilter) ? null : terminalServerFilter;
        }

        /// <summary>
        /// Processes the resources and generates the workspace XML as a string.
        /// </summary>
        /// <param name="resourcesFolder">The folder to use when searching for RDP files. This can be a relative path (e.g., "App_Data/resources") or an absolute path (e.g., "C:\inetpub\wwwroot\App_Data\resources").</param>
        /// <param name="multiuserResourcesFolder">The folder to use when searching for multiuser RDP files. This can be a relative path (e.g., "App_Data/multiuser-resources") or an absolute path (e.g., "C:\inetpub\wwwroot\App_Data\multiuser-resources").</param>
        /// <returns></returns>
        public string GetWorkspaceXmlString(string resourcesFolder = "App_Data/resources", string multiuserResourcesFolder = "App_Data/multiuser-resources") {
            var serverName = _terminalServerFilter ?? Environment.MachineName;
            var datetime = DateTime.Now.Year.ToString() + "-" + (DateTime.Now.Month + 100).ToString().Substring(1, 2) + "-" + (DateTime.Now.Day + 100).ToString().Substring(1, 2) + "T" + (DateTime.Now.Hour + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Minute + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Second + 100).ToString().Substring(1, 2) + ".0Z";

            // process resources
            ProcessRegistryResources();
            ProcessResources(resourcesFolder);
            ProcessMultiuserResources(multiuserResourcesFolder);

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

            // construct the final XML string
            var workspaceXml = new StringBuilder();
            workspaceXml.Append("<ResourceCollection PubDate=\"" + datetime + "\" SchemaVersion=\"" + _schemaVersion.ToString() + "\" " + (_schemaVersion >= 2.0 ? "SupportsReconnect=\"false\" " : "") + "xmlns=\"http://schemas.microsoft.com/ts/2007/05/tswf\">" + "\r\n");
            workspaceXml.Append("<Publisher LastUpdated=\"" + publisherTimestamp + "\" Name=\"" + publisherName + "\" ID=\"" + _fullyQualifiedDomainName + "\" Description=\"\">" + "\r\n");

            workspaceXml.Append("<Resources>" + "\r\n");
            var resourcesXML = _resourcesBuffer.ToString();
            resourcesXML = Regex.Replace(resourcesXML, @"<FolderInjectionPoint.*?/>", "");
            resourcesXML = Regex.Replace(resourcesXML, @"<TerminalServerInjectionPoint.*?/>", "");
            workspaceXml.Append(resourcesXML);
            workspaceXml.Append("</Resources>" + "\r\n");

            workspaceXml.Append("<TerminalServers>" + "\r\n");
            foreach (var terminalServer in _terminalServerTimestamps.Keys) {
                var terminalServerName = terminalServer;
                var terminalServerTimestamp = _terminalServerTimestamps[terminalServer].ToString("yyyy-MM-ddTHH:mm:ssZ");
                workspaceXml.Append("<TerminalServer ID=\"" + terminalServerName + "\" LastUpdated=\"" + terminalServerTimestamp + "\" />" + "\r\n");
            }
            workspaceXml.Append("</TerminalServers>" + "\r\n");

            workspaceXml.Append("</Publisher>" + "\r\n");
            workspaceXml.Append("</ResourceCollection>" + "\r\n");

            return workspaceXml.ToString();
        }

        private void ProcessResource(Resource resource) {
            // skip resources that do not match the terminal server
            if (!string.IsNullOrEmpty(_terminalServerFilter) && resource.FullAddress != _terminalServerFilter) {
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
                "<ResourceFile FileExtension=\".rdp\" URL=\"" + _iisBase + "api/resources/" + apiResourcePath + (resource.Origin == "registry" ? "?from=registry" : "") + "\" />" + "\r\n" +
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

                if (_mergeTerminalServers) {
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

            // construct the resource element
            _resourcesBuffer.Append("<Resource ID=\"" + resource.Id + "\" Alias=\"" + resource.Alias + "\" Title=\"" + resource.Title + "\" LastUpdated=\"" + resourceTimestamp + "\" Type=\"" + resource.Type + "\"" + (_schemaVersion >= 2.1 ? " ShowByDefault=\"True\"" : "") + ">" + "\r\n");
            _resourcesBuffer.Append("<Icons>" + "\r\n");
            _resourcesBuffer.Append(ResourceUtilities.ConstructIconElements(_authenticatedUserInfo, (resource.Origin == "registry" ? "registry!" : "") + resource.RelativePath.Replace("App_Data/", "").Replace(".rdp", ""), resource.IsDesktop ? ResourceUtilities.IconElementsMode.Wallpaper : ResourceUtilities.IconElementsMode.Icon, resource.IsDesktop ? "../lib/assets/wallpaper.png" : "../lib/assets/default.ico"));
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
                        var maybeIconElements = ResourceUtilities.ConstructIconElements(_authenticatedUserInfo, (resource.Origin == "registry" ? ("registry!" + fileExt.Replace(".", "") + ":") : "") + resource.RelativePath.Replace("App_Data/", "").Replace(".rdp", resource.Origin == "registry" ? "" : fileExt), resource.IsDesktop ? ResourceUtilities.IconElementsMode.Wallpaper : ResourceUtilities.IconElementsMode.Icon, resource.IsDesktop ? "../lib/assets/wallpaper.png" : "../lib/assets/default.ico", skipMissing: true);
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

        private void ProcessRegistryResources() {
            var publisherName = _resolver.Resolve(Environment.MachineName);

            var supportsCentralizedPublishing = System.Configuration.ConfigurationManager.AppSettings["RegistryApps.Enabled"] != "true";
            var centralizedPublishingCollectionName = AppId.ToCollectionName();
            var registryPath = supportsCentralizedPublishing ?
                "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\CentralPublishedResources\\PublishedFarms\\" + centralizedPublishingCollectionName + "\\Applications" :
                "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications";

            // get the registry entries for the remote applications
            using (var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath)) {
                if (regKey == null) {
                    return; // no remote applications found
                }

                foreach (var appName in regKey.GetSubKeyNames()) {
                    using (var appKey = regKey.OpenSubKey(appName)) {
                        if (appKey == null) {
                            continue; // skip if the application key is not found
                        }

                        var showInTSWA = appKey.GetValue(supportsCentralizedPublishing ? "ShowInPortal" : "ShowInTSWA") as int? == 1;
                        if (!showInTSWA) {
                            continue; // skip if the application is not allowed to be shown in the webfeed
                        }

                        var appProgram = appKey.GetValue("Path") as string;
                        if (string.IsNullOrEmpty(appProgram)) {
                            continue; // skip if the application path is missing
                        }

                        var hasPermission = RegistryReader.CanAccessRemoteApp(appKey, _authenticatedUserInfo);
                        if (!hasPermission) {
                            Console.WriteLine("Skipping registry RemoteApp (no permission): " + appName);
                            continue; // skip if the user does not have permission to access the application
                        }

                        Console.WriteLine("\nProcessing registry RemoteApp: " + appName);
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

                        // get the last time that the registry key was modified
                        DateTime lastUpdated;
                        try {
                            lastUpdated = RegistryReader.GetRemoteAppLastModifiedTime(appName);
                        }
                        catch {
                            lastUpdated = DateTime.MinValue;
                        }

                        // create a resource from the registry entry
                        var resource = new Resource(
                            title: displayName,
                            fullAddress: publisherName,
                            appProgram: appProgram,
                            alias: "registry/" + appName,
                            appFileExtCSV: appFileExtCSV,
                            lastUpdated: lastUpdated,
                            virtualFolder: "",
                            origin: "registry",
                            source: appName
                        ).CalculateGuid(rdpFileContents, _schemaVersion, _mergeTerminalServers);

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
            // convert directoryPath to a physical path if it is a relative path
            if (Directory.Exists(directoryPath) == false) {
                var fullRelativePath = _iisBase + directoryPath;
                directoryPath = HostingEnvironment.MapPath(fullRelativePath);
            }

            var subDirectories = Directory.GetDirectories(directoryPath);
            foreach (var subDirectory in subDirectories) {
                var subVirtualFolder = virtualFolder + "/" + Path.GetFileName(subDirectory);
                ProcessResources(subDirectory, subVirtualFolder);
            }

            var directoryRdpFilePaths = Directory.GetFiles(directoryPath, "*.rdp");
            foreach (var rdpFilePath in directoryRdpFilePaths) {
                if (!(ResourceUtilities.GetRdpFileProperty(rdpFilePath, "full address:s:") == "")) {
                    var hasPermission = FileAccessInfo.CanAccessPath(rdpFilePath, _authenticatedUserInfo);
                    if (!hasPermission) {
                        continue; // skip if the user does not have permission to access the rdp file
                    }

                    // get the rdp file name and remove the last 4 characters (.rdp)
                    var baseRdpFileName = Path.GetFileNameWithoutExtension(rdpFilePath);

                    // extract full relative path from the directoryPath (including the resources or multiuser-resources folder)
                    var relativePathFull = directoryPath.Replace(HostingEnvironment.MapPath(_iisBase), "").TrimStart('\\').TrimEnd('\\').Replace("\\", "/") + "/";

                    // get the paths to all files that start with the same basename as the rdp file
                    // (e.g., get: *.rdp, *.ico, *.png, *.xlsx.ico, *.xls.png, etc.)
                    var allResourceFiles = Directory.GetFiles(directoryPath, baseRdpFileName + ".*");

                    // calculate the timestamp for the resource, which is the latest of the rdp file and icon files
                    var resourceDateTime = File.GetLastWriteTimeUtc(rdpFilePath);
                    foreach (var resourceFile in allResourceFiles) {
                        var fileDateTime = File.GetLastWriteTimeUtc(resourceFile);
                        if (fileDateTime > resourceDateTime) {
                            resourceDateTime = fileDateTime;
                        }
                    }

                    // prepare the info for the resource
                    var resource = new Resource(
                        title: ResourceUtilities.GetRdpFileProperty(rdpFilePath, "remoteapplicationname:s:", baseRdpFileName), // set the app title to the base filename if the remote application name is empty
                        fullAddress: ResourceUtilities.GetRdpFileProperty(rdpFilePath, "full address:s:"),
                        appProgram: ResourceUtilities.GetRdpFileProperty(rdpFilePath, "remoteapplicationprogram:s:").Replace("|", ""),
                        alias: relativePathFull + baseRdpFileName + ".rdp",
                        appFileExtCSV: ResourceUtilities.GetRdpFileProperty(rdpFilePath, "remoteapplicationfileextensions:s:"),
                        lastUpdated: resourceDateTime,
                        virtualFolder: virtualFolder,
                        origin: "rdp",
                        source: directoryPath + "\\" + Path.GetFileName(rdpFilePath)
                    ).CalculateGuid(_schemaVersion, _mergeTerminalServers);

                    // process the resource
                    ProcessResource(resource);
                }
            }
        }

        private void ProcessMultiuserResources(string directoryPath) {
            // convert directoryPath to a physical path if it is a relative path
            if (Directory.Exists(directoryPath) == false) {
                var fullRelativePath = _iisBase + directoryPath;
                directoryPath = HostingEnvironment.MapPath(fullRelativePath);
            }

            var showGroupAndUserNames = System.Configuration.ConfigurationManager.AppSettings["Workspace.ShowMultiuserResourcesUserAndGroupNames"] != "false";

            // process resources in basePath\\user\\ [username]
            var userFolder = directoryPath + "\\user\\" + _authenticatedUserInfo.Username + "\\";
            if (Directory.Exists(userFolder)) {
                var virtualFolder = showGroupAndUserNames ? "/" + _authenticatedUserInfo.FullName : "";
                ProcessResources(userFolder, virtualFolder);
            }

            // process resources in basePath\\group\\ [group name]
            // and basePath\\group\\ [group SID]
            foreach (var group in _authenticatedUserInfo.Groups) {
                var virtualFolder = showGroupAndUserNames ? "/" + group.Name : "";

                var GroupNameFolder = directoryPath + "\\group\\" + group.Name + "\\";
                if (Directory.Exists(GroupNameFolder)) {
                    ProcessResources(GroupNameFolder, virtualFolder);
                }

                var GroupSidFolder = directoryPath + "\\group\\" + group.Sid + "\\";
                if (Directory.Exists(GroupSidFolder)) {
                    ProcessResources(GroupSidFolder, virtualFolder);
                }
            }
        }
    }
}
