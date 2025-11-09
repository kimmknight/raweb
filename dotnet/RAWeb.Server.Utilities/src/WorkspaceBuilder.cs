using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RAWeb.Server.Management;

namespace RAWeb.Server.Utilities;

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

    private readonly AliasResolver _resolver = new();

    private readonly UserInformation? _authenticatedUserInfo = null;
    private readonly bool _mergeTerminalServers = false;
    private readonly string? _terminalServerFilter = null;

    private readonly double _schemaVersion = 1.0;
    private readonly string _iisBase;

    private StringBuilder _resourcesBuffer = new();
    private readonly Dictionary<string, DateTime> _terminalServerTimestamps = new Dictionary<string, DateTime>();

    // keep track of previous resource GUIDs to avoid duplicates
    string[] _previousResourceGUIDs = [];

    readonly string _fullyQualifiedDomainName = "";

    /// <summary>
    /// Initializes a new instance of the <c>WorkspaceBuilder</c> class.
    /// </summary>
    /// <param name="version">The version of the MS-TWSP.</param>
    /// <param name="authenticatedUserInfo"></param>
    /// <param name="fullyQualifiedDomainName">The fully qualified domain name, used for the ID in the workspace XML. See HttpContext.Current.Request.Url.Host.</param>
    /// <param name="mergeTerminalServers">Whether identical resources across multiple terminal servers are provided as a single resource with mnultiple terminal servers. When this option is false, each resource is listed separately even though the resources are the same.</param>
    /// <param name="terminalServerFilter">Filter the resources to the specified terminal server.</param>
    /// <param name="iisBase">The IIS base path, e.g., VirtualPathUtility.ToAbsolute("~/")</param>
    /// <exception cref="ArgumentException"></exception>
    public WorkspaceBuilder(SchemaVersion version, UserInformation authenticatedUserInfo, string fullyQualifiedDomainName, bool mergeTerminalServers = false, string? terminalServerFilter = null, string iisBase = "/") {
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
        _iisBase = iisBase;
    }

    /// <summary>
    /// Processes the resources and generates the workspace XML as a string.
    /// </summary>
    /// <param name="resourcesFolder">The folder to use when searching for RDP files. This can be a relative path (e.g., "resources") or an absolute path (e.g., "C:\inetpub\wwwroot\App_Data\resources").</param>
    /// <param name="multiuserResourcesFolder">The folder to use when searching for multiuser RDP files. This can be a relative path (e.g., "multiuser-resources") or an absolute path (e.g., "C:\inetpub\wwwroot\App_Data\multiuser-resources").</param>
    /// <returns></returns>
    public string GetWorkspaceXmlString(string resourcesFolder = "resources", string multiuserResourcesFolder = "multiuser-resources", string managedResourcesFolder = "managed-resources") {
        var serverName = _terminalServerFilter ?? Environment.MachineName;
        var datetime = DateTime.Now.Year.ToString() + "-" + (DateTime.Now.Month + 100).ToString().Substring(1, 2) + "-" + (DateTime.Now.Day + 100).ToString().Substring(1, 2) + "T" + (DateTime.Now.Hour + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Minute + 100).ToString().Substring(1, 2) + ":" + (DateTime.Now.Second + 100).ToString().Substring(1, 2) + ".0Z";

        // process resources
        ProcessRegistryResources();
        ProcessResources(resourcesFolder);
        ProcessMultiuserResources(multiuserResourcesFolder);
        ProcessManagedResources(managedResourcesFolder);

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
        var tsInjectionPointElement = "<TerminalServerInjectionPoint guid=\"" + resource.Id + "\"/>";
        var tsElement = "<TerminalServerRef Ref=\"" + resource.FullAddress + "\" />" + "\r\n";
        var tsElements = "<HostingTerminalServer>" + "\r\n" +
            "<ResourceFile FileExtension=\".rdp\" URL=\"" + _iisBase + "api/resources/" + apiResourcePath + (resource.Origin == ResourceOrigin.Registry ? "?from=registry" : resource.Origin == ResourceOrigin.ManagedResource ? "?from=mr" : "") + "\" />" + "\r\n" +
            tsElement +
            "</HostingTerminalServer>" + "\r\n";

        // if a  resource with the same ID already exists, use special logic to make it appear in multiple
        // folders and/or terminal server listings instead of adding a duplicate resource
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
        _resourcesBuffer.Append(ConstructIconElements(_authenticatedUserInfo, (resource.Origin == ResourceOrigin.Registry ? "registry!" : "") + resource.RelativePath.Replace(".rdp", ""), resource.IsDesktop ? IconElementsMode.Wallpaper : IconElementsMode.Icon, resource.IsDesktop ? "../lib/assets/wallpaper.png" : "../lib/assets/default.ico"));
        _resourcesBuffer.Append("</Icons>" + "\r\n");
        if (resource.FileExtensions is not null && resource.FileExtensions.Length > 0) {
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
                    var maybeIconElements = ConstructIconElements(_authenticatedUserInfo, (resource.Origin == ResourceOrigin.Registry ? ("registry!" + fileExt.Replace(".", "") + ":") : "") + resource.RelativePath.Replace(".rdp", resource.Origin == ResourceOrigin.Registry ? "" : fileExt), resource.IsDesktop ? IconElementsMode.Wallpaper : IconElementsMode.Icon, resource.IsDesktop ? "../lib/assets/wallpaper.png" : "../lib/assets/default.ico", skipMissing: true);
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
        var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
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

                    if (appKey.GetValue("Path") is not string appProgram) {
                        continue; // skip if the application path is missing
                    }

                    var hasPermission = _authenticatedUserInfo is not null && RegistryReader.CanAccessRemoteApp(appKey, _authenticatedUserInfo);
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

                    // get the last time that the registry key was modified
                    DateTime lastUpdated;
                    try {
                        lastUpdated = RegistryReader.GetRemoteAppLastModifiedTime(appName);
                    }
                    catch {
                        lastUpdated = DateTime.MinValue;
                    }

                    var publisherName = _resolver.Resolve(Environment.MachineName);

                    // create a resource from the registry entry
                    var resource = new Resource(
                        title: displayName,
                        fullAddress: publisherName,
                        appProgram: appProgram,
                        alias: "registry/" + appName,
                        appFileExtCSV: appFileExtCSV,
                        lastUpdated: lastUpdated,
                        virtualFolder: "",
                        origin: ResourceOrigin.Registry,
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
        var root = Constants.AppDataFolderPath;
        var isRooted = Path.IsPathRooted(directoryPath);
        directoryPath = isRooted ? directoryPath : Path.Combine(root, directoryPath);

        if (!Directory.Exists(directoryPath)) {
            return; // skip if the directory does not exist
        }

        var subDirectories = Directory.GetDirectories(directoryPath);
        foreach (var subDirectory in subDirectories) {
            var subVirtualFolder = virtualFolder + "/" + Path.GetFileName(subDirectory);
            ProcessResources(subDirectory, subVirtualFolder);
        }

        var directoryRdpFilePaths = Directory.GetFiles(directoryPath, "*.rdp");
        foreach (var rdpFilePath in directoryRdpFilePaths) {
            try {
                var hasPermission = _authenticatedUserInfo is not null && FileAccessInfo.CanAccessPath(rdpFilePath, _authenticatedUserInfo);
                if (!hasPermission) {
                    continue; // skip if the user does not have permission to access the rdp file
                }

                // prepare the info for the resource
                var resource = Resource
                    .FromRdpFile(rdpFilePath, virtualFolder)
                    .CalculateGuid(_schemaVersion, _mergeTerminalServers);

                // process the resource
                ProcessResource(resource);
            }
            catch (Resource.FullAddressMissingException) {
                continue; // skip if the RDP file does not have a full address
            }

        }
    }

    private void ProcessMultiuserResources(string directoryPath) {
        // convert directoryPath to a physical path if it is a relative path
        var root = Constants.AppDataFolderPath;
        var isRooted = Path.IsPathRooted(directoryPath);
        directoryPath = isRooted ? directoryPath : Path.Combine(root, directoryPath);

        var showGroupAndUserNames = PoliciesManager.RawPolicies["Workspace.ShowMultiuserResourcesUserAndGroupNames"] != "false";

        if (_authenticatedUserInfo is null) {
            return; // skip if the user is not authenticated
        }

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

    private void ProcessManagedResources(string directoryPath) {
        // convert directoryPath to a physical path if it is a relative path
        var root = Constants.AppDataFolderPath;
        var isRooted = Path.IsPathRooted(directoryPath);
        directoryPath = isRooted ? directoryPath : Path.Combine(root, directoryPath);

        if (_authenticatedUserInfo is null) {
            return; // skip if the user is not authenticated
        }

        // process all managed resources in the directory
        var managedResources = ManagedFileResources.FromDirectory(directoryPath);
        foreach (var managedResource in managedResources) {
            var hasPermission = managedResource.SecurityDescriptor == null ||
                managedResource.SecurityDescriptor.GetAllowedSids().Any(sid => _authenticatedUserInfo.Sid == sid.ToString() || _authenticatedUserInfo.Groups.Any(g => g.Sid == sid.ToString()));
            if (!hasPermission) {
                continue; // skip if the user does not have permission to access the resource
            }

            if (managedResource.RdpFileString == null || string.IsNullOrEmpty(managedResource.RdpFileString)) {
                continue; // skip if the RDP file string is missing
            }

            var relativeFilePath = managedResource.RootedFilePath.Replace(root + Path.DirectorySeparatorChar, "").Replace("\\", "/");

            // ensure that there is a full address in the RDP file
            var fullAddress = Resource.Utilities.GetRdpStringProperty(managedResource.RdpFileString, "full address:s:");
            if (string.IsNullOrEmpty(fullAddress)) {
                throw new Resource.FullAddressMissingException();
            }

            // calculate the timestamp for the resource
            var resourceDateTime = File.GetLastWriteTimeUtc(managedResource.RootedFilePath);

            // build the resource
            var resource = new Resource(
                title: Resource.Utilities.GetRdpStringProperty(managedResource.RdpFileString, "remoteapplicationname:s:", managedResource.Name),
                fullAddress: Resource.Utilities.GetRdpStringProperty(managedResource.RdpFileString, "full address:s:"),
                appProgram: managedResource.RemoteAppProperties?.ApplicationPath ?? Resource.Utilities.GetRdpStringProperty(managedResource.RdpFileString, "remoteapplicationprogram:s:").Replace("|", ""),
                alias: relativeFilePath,
                appFileExtCSV: Resource.Utilities.GetRdpStringProperty(managedResource.RdpFileString, "remoteapplicationfileextensions:s:"),
                lastUpdated: resourceDateTime,
                virtualFolder: "",
                origin: ResourceOrigin.ManagedResource,
                source: managedResource.RootedFilePath
            ).CalculateGuid(managedResource.RdpFileString, _schemaVersion, _mergeTerminalServers);

            // process the resource
            ProcessResource(resource);
        }
    }

    public enum IconElementsMode {
        Icon,
        Wallpaper
    }

    /// <summary>
    /// Constructs XML elements for icons of various sizes based on the provided icon path and mode.
    /// It checks for the existence and accessibility of the icon file, and falls back to a
    /// default icon if necessary.
    /// </summary>
    /// <param name="authenticatedUserInfo"></param>
    /// <param name="relativeExtenesionlessIconPath">The path to the icon file. THe path should not include the file extension for the icon. The path should be relative to the App_Data folder.</param>
    /// <param name="mode"></param>
    /// <param name="relativeDefaultIconPath">The path to the default icon, relative to the App_Data folder. Unlke <c>relativeExtenesionlessIconPath</c>, this value should include the icon extension.</param>
    /// <param name="skipMissing">When <c>true</c>, the returned value of this method will be an empty string. Otherwise, the default icon will be used to generate the XML string instead.</param>
    /// <returns></returns>
    public string ConstructIconElements(
      UserInformation? authenticatedUserInfo,
      string relativeExtenesionlessIconPath,
      IconElementsMode mode,
      string relativeDefaultIconPath = "../lib/assets/default.ico",
      bool skipMissing = false
    ) {
        if (authenticatedUserInfo is null) {
            return "";
        }

        var appDataRoot = Constants.AppDataFolderPath;
        var defaultIconPath = Path.Combine(appDataRoot, relativeDefaultIconPath);

        var iconPath = Path.Combine(appDataRoot, string.Format("{0}", relativeExtenesionlessIconPath));

        // create placeholders for tracking the icon dimensions
        var iconWidth = 0;
        var iconHeight = 0;

        // if the icon is from the registry, we get the dimensions from there
        if (relativeExtenesionlessIconPath.StartsWith("registry!")) {
            var appKeyName = relativeExtenesionlessIconPath.Split('!').LastOrDefault();
            var maybeFileExtName = relativeExtenesionlessIconPath.Split('!')[1];
            if (maybeFileExtName == appKeyName) {
                maybeFileExtName = "";
            }

            if (appKeyName is null) {
                if (skipMissing) {
                    return "";
                }

                // if the app key name is null, use the default icon
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                iconPath = defaultIconPath;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
                relativeExtenesionlessIconPath = relativeDefaultIconPath;
            }
            else {
                try {
                    Stream? fileStream = RegistryReader.ReadImageFromRegistry(appKeyName, maybeFileExtName, authenticatedUserInfo);
                    if (fileStream == null) {
                        if (skipMissing) {
                            return "";
                        }

                        // if the file stream is null, use the default icon
                        iconPath = defaultIconPath;
                        relativeExtenesionlessIconPath = relativeDefaultIconPath;
                        fileStream = new FileStream(iconPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }

                    using (var image = System.Drawing.Image.FromStream(fileStream, false, false)) {
                        iconWidth = image.Width;
                        iconHeight = image.Height;
                    }
                }
                catch (Exception) {
                    if (skipMissing) {
                        return "";
                    }

                    // non-square dimensions will cause the default icon to be used
                    iconWidth = 0;
                    iconHeight = 1;
                }
            }
        }

        // otherwise, se get the icon dimensions from the file
        else {
            // get the icon path, preferring the png icon first, then the ico icon, and finally the default icon
            if (File.Exists(iconPath + ".png")) {
                iconPath += ".png";
            }
            else if (File.Exists(iconPath + ".ico")) {
                iconPath += ".ico";
            }
            else {
                if (skipMissing) {
                    return "";
                }

                iconPath = defaultIconPath;
                relativeExtenesionlessIconPath = relativeDefaultIconPath;
            }

            // confirm that the current user has permission to access the icon file
            var hasPermission = FileAccessInfo.CanAccessPath(iconPath, authenticatedUserInfo);
            if (!hasPermission) {
                if (skipMissing) {
                    return "";
                }

                // if the user does not have permission to access the icon file, use the default icon
                iconPath = defaultIconPath;
                relativeExtenesionlessIconPath = relativeDefaultIconPath;
            }

            // get the icon dimensions
            using (var fileStream = new FileStream(iconPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                using (var image = System.Drawing.Image.FromStream(fileStream, false, false)) {
                    iconWidth = image.Width;
                    iconHeight = image.Height;
                }
            }
        }

        // if the icon is not a square, use the default icon
        // or treat it as wallpaper if the mode is set to "wallpaper"
        var frame = "";
        if (iconWidth != iconHeight) {
            // if the icon is not a square, use the default icon instead
            if (mode == IconElementsMode.Icon) {
                iconPath = defaultIconPath;
                relativeExtenesionlessIconPath = relativeDefaultIconPath;
                using (var fileStream = new FileStream(iconPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (var image = System.Drawing.Image.FromStream(fileStream, false, false)) {
                        iconWidth = image.Width;
                        iconHeight = image.Height;
                    }
                }
            }

            // or, if the mode is set to "wallpaper", we will allow non-square icons
            if (mode == IconElementsMode.Wallpaper) {
                frame = "&amp;frame=pc";
            }
        }

        // if the path is the default wallpaper, replace it with defaultwallpaper
        if (relativeExtenesionlessIconPath == "../lib/assets/wallpaper.png") {
            relativeExtenesionlessIconPath = "defaultwallpaper";
        }

        // if the path is the default icon, replace it with defaulicon
        if (relativeExtenesionlessIconPath == "../lib/assets/default.ico") {
            relativeExtenesionlessIconPath = "defaulticon";
        }

        // build the icons elements
        var iconElements = "<IconRaw FileType=\"Ico\" FileURL=\"" + _iisBase + "api/resources/image/" + relativeExtenesionlessIconPath + "?format=ico" + frame + "\" />" + "\r\n";
        if (iconWidth >= 16) {
            iconElements += "<Icon16 Dimensions=\"16x16\" FileType=\"Png\" FileURL=\"" + _iisBase + "api/resources/image/" + relativeExtenesionlessIconPath + "?format=png16" + frame + "\" />" + "\r\n";
        }
        if (iconWidth >= 32) {
            iconElements += "<Icon32 Dimensions=\"32x32\" FileType=\"Png\" FileURL=\"" + _iisBase + "api/resources/image/" + relativeExtenesionlessIconPath + "?format=png32" + frame + "\" />" + "\r\n";
        }
        if (iconWidth >= 48) {
            iconElements += "<Icon48 Dimensions=\"48x48\" FileType=\"Png\" FileURL=\"" + _iisBase + "api/resources/image/" + relativeExtenesionlessIconPath + "?format=png48" + frame + "\" />" + "\r\n";
        }
        if (iconWidth >= 64) {
            iconElements += "<Icon64 Dimensions=\"64x64\" FileType=\"Png\" FileURL=\"" + _iisBase + "api/resources/image/" + relativeExtenesionlessIconPath + "?format=png64" + frame + "\" />" + "\r\n";
        }
        if (iconWidth >= 100) {
            iconElements += "<Icon100 Dimensions=\"100x100\" FileType=\"Png\" FileURL=\"" + _iisBase + "api/resources/image/" + relativeExtenesionlessIconPath + "?format=png100" + frame + "\" />" + "\r\n";
        }
        if (iconWidth >= 256) {
            iconElements += "<Icon256 Dimensions=\"256x256\" FileType=\"Png\" FileURL=\"" + _iisBase + "api/resources/image/" + relativeExtenesionlessIconPath + "?format=png256" + frame + "\" />" + "\r\n";
        }

        return iconElements;
    }
}
