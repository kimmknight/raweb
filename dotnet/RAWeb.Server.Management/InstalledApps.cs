using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Xml.Linq;
using Microsoft.Win32;
using static RAWeb.Server.Management.SystemRemoteApps;

namespace RAWeb.Server.Management;

/// <summary>
/// Represents information about an installed application on the system.
/// </summary>
[DataContract]
public class InstalledApp(string path, string displayName, string displayFolder, string iconPath, int iconIndex = 0, string commandLineArguments = "", FileTypeAssociations? fileTypeAssociations = null) {
  [DataMember] public string Path { get; set; } = path;
  [DataMember] public string DisplayName { get; set; } = displayName;
  [DataMember] public string DisplayFolder { get; set; } = displayFolder;
  [DataMember] public string IconPath { get; set; } = iconPath;
  [DataMember] public int IconIndex { get; set; } = iconIndex;
  [DataMember] public string CommandLineArguments { get; set; } = commandLineArguments ?? "";
  [DataMember] public FileTypeAssociations fileTypeAssociations { get; set; } = fileTypeAssociations ?? [];

  /// <summary>
  /// Translates a shortcut file (.lnk) into an InstalledApp object.
  /// </summary>
  /// <param name="shortcutFilePath"></param>
  /// <param name="programsPath"></param>
  /// <returns></returns>
  public static InstalledApp? FromShortcut(string shortcutFilePath, string programsPath) {

    // extract the shortcut name, which may be different than the .lnk file name
    string? shortcutName = null;
    try {
      shortcutName = GetFileOrFolderDisplayName(shortcutFilePath);
    }
    catch { }
    shortcutName ??= System.IO.Path.GetFileNameWithoutExtension(shortcutFilePath);

    // also check if the folder also has a custom display name
    var folderPath = System.IO.Path.GetDirectoryName(shortcutFilePath);
    var folderParts = folderPath?.Substring(programsPath.Length).TrimStart('\\').Split('\\') ?? [];
    var folderName = folderParts.LastOrDefault() ?? "";
    if (folderPath is not null && !string.IsNullOrWhiteSpace(folderPath) && folderPath != programsPath) {
      try {
        var resolvedFolderName = GetFileOrFolderDisplayName(folderPath);
        if (!string.IsNullOrWhiteSpace(resolvedFolderName)) {
          folderName = resolvedFolderName;
        }
      }
      catch { }
    }
    var displayFolder = !string.IsNullOrEmpty(folderName) ? folderParts.Length > 1 ? string.Join("\\", [.. folderParts.Take(folderParts.Length - 1), folderName]) : folderName : "";

    // read shortcut target and icon using COM interop
    var shellType = Type.GetTypeFromProgID("WScript.Shell");
    if (shellType is null) {
      throw new InvalidOperationException("Failed to get WScript.Shell COM type.");
    }
    dynamic? shell = Activator.CreateInstance(shellType);
    dynamic? shortcut = shell?.CreateShortcut(shortcutFilePath);
    string? targetPath;
    string? targetArguments;
    string? iconLocation;
    if (shell is not null && shortcut is not null) {
      targetPath = shortcut?.TargetPath;
      targetArguments = shortcut?.Arguments;
      iconLocation = shortcut?.IconLocation;
      Marshal.FinalReleaseComObject(shell);
      Marshal.FinalReleaseComObject(shortcut);
    }
    else {
      throw new InvalidOperationException("Failed to create WScript.Shell COM object or shortcut object.");
    }

    if (string.IsNullOrWhiteSpace(targetPath) || targetPath is null || string.IsNullOrWhiteSpace(iconLocation) || iconLocation is null || targetArguments is null) {
      return null;
    }

    var iconPath = Environment.ExpandEnvironmentVariables(iconLocation.Split(',')[0]);
    var iconIndex = iconLocation.Contains(",") ? int.Parse(iconLocation.Split(',')[1]) : 0;
    if (iconIndex < 0) {
      iconIndex = 0;
    }

    // seearch for file type associations
    var fileTypeAssociations = FindFileTypeAssociations(targetPath);

    var installedApp = new InstalledApp(
      path: targetPath,
      displayName: shortcutName,
      displayFolder: displayFolder ?? "",
      iconPath: !string.IsNullOrWhiteSpace(iconPath) ? iconPath : targetPath,
      iconIndex: iconIndex,
      commandLineArguments: targetArguments,
      fileTypeAssociations: fileTypeAssociations
    );
    return installedApp;
  }

  [DllImport("shfolder.dll", SetLastError = true)]
  private static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder,
        IntPtr hToken, uint dwFlags, System.Text.StringBuilder pszPath);

  [DllImport("shell32.dll", CharSet = CharSet.Auto)]
  private static extern uint SHGetFileInfo(string pszPath, uint dwFileAttributes,
      ref SHFILEINFO pszFileInfo, uint cbFileInfo, uint uFlags);

  private const uint SHGFI_DISPLAYNAME = 0x000000200; // Get display name

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
  private struct SHFILEINFO {
    public IntPtr hIcon;
    public int iIcon;
    public uint dwAttributes;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string szDisplayName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
    public string szTypeName;
  }

  /// <summary>
  /// Gets the display name of a file or folder, which may be different than the actual path/file name.
  /// </summary>
  /// <param name="shortcutPath"></param>
  /// <returns></returns>
  public static string? GetFileOrFolderDisplayName(string shortcutPath) {
    var shfi = new SHFILEINFO();
    var flags = SHGFI_DISPLAYNAME;

    // SHGetFileInfo returns 0 on failure
    if (SHGetFileInfo(shortcutPath, 0, ref shfi, (uint)Marshal.SizeOf(shfi), flags) != 0) {
      return shfi.szDisplayName;
    }
    return null;
  }

  private static Dictionary<string, List<string>> s_extensionsProgIdsCache = new(StringComparer.OrdinalIgnoreCase);
  private static Dictionary<string, string> s_extensionsProgIdTargetsCache = new(StringComparer.OrdinalIgnoreCase);
  private static DateTime s_extensionsProgIdsCacheLastUpdated = DateTime.MinValue;
  private static readonly object s_extensionsProgIdsCacheLock = new();
  private static double s_extensionsProgIdsCacheUpdateIntervalMinutes = 0.5;

  /// <summary>
  /// Searches the registry for file type associations for this application.
  /// <br /><br />
  /// To prevent excessive registry access, this method caches a mapping of 
  /// file extensions to ProgIDs and ProgIDs to target commands. See the
  /// value of <see cref="s_extensionsProgIdsCacheUpdateIntervalMinutes"/> to adjust
  /// how often the cache is refreshed.
  /// <br /><br />
  /// Only system-wide file type associations in HKEY_LOCAL_MACHINE are searched.
  /// User-specific associations in HKEY_USERS are not searched because most
  /// users' NTUSER.DAT hives are not loaded.
  /// <br /><br />
  /// Do not use this method with packaged apps (Appx/MSIX); those associations
  /// should instead be extracted from the package manifest.
  /// </summary>
  private static FileTypeAssociations FindFileTypeAssociations(string targetPath) {
    // scan HKEY_LOCAL_MACHINE\SOFTWARE\Classes for shell open commands that start with the target path
    // Note: this will not find per-user file associations in HKEY_CURRENT_USER, and we
    // do not scan those because we do not want to open every user's NTUSER.DAT hive.
    using (var classesKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes")) {
      if (classesKey is null) {
        return [];
      }

      // first, try to use the SupportedTypes key, which may contain a list of extensions
      var supportedTypesKey = classesKey.OpenSubKey($@"Applications\{System.IO.Path.GetFileName(targetPath)}\SupportedTypes");
      if (supportedTypesKey is not null) {
        var supportedTypes = new List<string>();
        foreach (var valueName in supportedTypesKey.GetValueNames()) {
          supportedTypes.Add(valueName);
        }
        return [.. supportedTypes
          .Select(ext => new FileTypeAssociation(
            extension: ext,
            iconPath: "",
            iconIndex: 0
          ))];
      }


      // otherwise, look through each extension to find those that point to this target path

      var fileTypeAssociations = new FileTypeAssociations();

      static bool IsCacheStale() {
        return (DateTime.Now - s_extensionsProgIdsCacheLastUpdated).TotalMinutes > s_extensionsProgIdsCacheUpdateIntervalMinutes;
      }
      // caclulate a dictionary containing a mapping of extensions to ProgIDs, but only if the cache is stale
      if (IsCacheStale()) {
        lock (s_extensionsProgIdsCacheLock) { // lock if cache is stale
          if (IsCacheStale()) { // confirm cache is still stale
            s_extensionsProgIdsCache.Clear();
            s_extensionsProgIdTargetsCache.Clear();

            foreach (var extension in classesKey.GetSubKeyNames().Where(n => n.StartsWith("."))) {
              using var extensionProgramIds = classesKey.OpenSubKey($@"{extension}\OpenWithProgIDs");
              if (extensionProgramIds is null) {
                continue;
              }

              // add the ProgIDs for this extension to the cache if not already present
              foreach (var progId in extensionProgramIds.GetValueNames()) {
                if (!s_extensionsProgIdsCache.TryGetValue(extension, out var value)) {
                  value = [];
                  s_extensionsProgIdsCache[extension] = value;
                }
                value.Add(progId);
              }
            }
            s_extensionsProgIdsCacheLastUpdated = DateTime.Now;
          }
        }
      }

      // for each extension in the cache, look for ProgIDs that point to the target path
      foreach (var entry in s_extensionsProgIdsCache) {
        var extension = entry.Key;
        var programIds = entry.Value;

        // evaluate each ProgID listed for this extension
        foreach (var progId in programIds) {

          // search the cache for the target command for this ProgID, or populate it if not present
          if (!s_extensionsProgIdTargetsCache.TryGetValue(progId, out var cachedTargetCommand)) {
            using var cmdKey = classesKey.OpenSubKey($@"{progId}\shell\open\command");
            var cmd = (string?)cmdKey?.GetValue("");
            if (cmd is null || string.IsNullOrWhiteSpace(cmd)) {
              continue;
            }
            s_extensionsProgIdTargetsCache[progId] = cmd;
            cachedTargetCommand = cmd;
          }

          var isMatch = cachedTargetCommand!.StartsWith($"\"{targetPath}\"", StringComparison.OrdinalIgnoreCase) ||
                        cachedTargetCommand.StartsWith(targetPath, StringComparison.OrdinalIgnoreCase);
          if (!isMatch) {
            continue;
          }

          // attempt to get the icon path from the ProgID
          using var iconKey = classesKey.OpenSubKey($@"{progId}\DefaultIcon");
          string? iconPath = null;
          int? iconIndex = null;
          if (iconKey is not null) {
            var iconPathAndIndex = (string?)iconKey.GetValue("");
            if (iconPathAndIndex is not null && !string.IsNullOrWhiteSpace(iconPathAndIndex)) {
              var parts = iconPathAndIndex.Split(',');
              iconPath = parts[0];
              iconIndex = parts.Length > 1 && int.TryParse(parts[1], out var idx) ? idx : 0;
            }
          }

          fileTypeAssociations.Add(new FileTypeAssociation(extension, iconPath ?? "", iconIndex ?? 0));
        }
      }

      return fileTypeAssociations;
    }
  }
}

/// <summary>
/// A collection of installed applications on the system.
/// </summary>
[CollectionDataContract]
public class InstalledApps : System.Collections.ObjectModel.Collection<InstalledApp> {
  public InstalledApps() {
  }
  public InstalledApps(IList<InstalledApp> apps) {
    foreach (var app in apps) {
      Add(app);
    }
  }

  /// <summary>
  /// Enumerates all .lnk shortcut files in the specified folder and its subfolders
  /// and returns a collection of InstalledApp objects representing them.
  /// <br /><br />
  /// If an individual shortcut cannot be accessed, it will be skipped.
  /// <br /><br />
  /// This method is used by <see cref="FromStartMenu"/> and <see cref="FromStartMenu(SecurityIdentifier)"/>.
  /// </summary>
  /// <param name="folderPath"></param>
  /// <returns></returns>
  private static InstalledApps FromShortcutsInFolder(string folderPath) {
    var foundApps = new InstalledApps();

    var shortcutFiles = Directory.GetFiles(folderPath, "*.lnk", SearchOption.AllDirectories);
    var seen = new HashSet<string>();
    foreach (var shortcutFilePath in shortcutFiles) {
      try {
        var installedApp = InstalledApp.FromShortcut(shortcutFilePath, folderPath);

        if (installedApp is null) {
          continue;
        }

        var isDocumentation = installedApp.DisplayName.ToLower().Contains("documentation") ||
                            installedApp.DisplayName.ToLower().Contains("docs") ||
                            installedApp.DisplayName.ToLower().Contains("readme") ||
                            installedApp.DisplayName.ToLower().Contains("about") ||
                            installedApp.DisplayName.ToLower().Contains("release notes") ||
                            installedApp.DisplayName.ToLower().Contains("help");

        var isUninstaller = installedApp.DisplayName.ToLower().Contains("uninstall") ||
                            installedApp.DisplayName.ToLower().Contains("remove");

        // skip documentation and uninstaller apps
        if (isDocumentation || isUninstaller) {
          continue;
        }

        // skip duplicate apps based on their display name
        if (seen.Contains(installedApp.DisplayName)) {
          continue;
        }

        foundApps.Add(installedApp);
        seen.Add(installedApp.DisplayName);
      }
      catch (Exception ex) {
        if (ex is UnauthorizedAccessException || ex is FileNotFoundException) {
          // skip individual files that cannot be accessed
          continue;
        }
        throw;
      }
    }

    return foundApps;
  }

  /// <summary>
  /// Gets a collection of installed applications on the system based on the Start Menu entries.
  /// <br /><br />
  /// To get installed applications for a specific user, use
  /// <see cref="FromStartMenu(SecurityIdentifier)"/>.
  /// </summary>
  /// <returns></returns>
  public static InstalledApps FromStartMenu() {
    // get the applications from the common Start Menu
    var programsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + @"\Programs";
    var programs = FromShortcutsInFolder(programsPath);

    bool changed;
    do {
      changed = false;

      // group applications by their current DisplayFolder
      var foldersToProcess = programs
          .GroupBy(app => app.DisplayFolder)
          .Where(group => group.Count() == 1 && group.Key is not null && group.Key != "")
          .ToList();

      foreach (var group in foldersToProcess) {
        var appInFolder = group.First();
        var currentDisplayFolder = appInFolder.DisplayFolder;

        // ensure currentDisplayFolder is not null or empty
        if (currentDisplayFolder is null | string.IsNullOrWhiteSpace(currentDisplayFolder)) {
          continue;
        }

        // ensure there are no nested folders that contain other apps
        var nestedChildren = programs
            .Where(app => app.DisplayFolder is not null &&
                          app.DisplayFolder.StartsWith(currentDisplayFolder + "\\", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (nestedChildren.Count > 0) {
          continue; // skip folders that contain other apps in nested folders
        }

        // if a valid parent folder exists, promote the app
        var parentFolder = Path.GetDirectoryName(currentDisplayFolder);
        if (parentFolder is not null) {
          appInFolder.DisplayFolder = parentFolder;
          changed = true; // indicate that a change occurred, so another pass is needed
        }

      }
    } while (changed); // Repeat until no more single-item folders can be flattened

    return programs;
  }

  /// <summary>
  /// Gets a collection of installed applications for a specific user based on their
  /// Start Menu entries and installed AppX/MSIX packages.
  /// <br /><br />
  /// If a user profile cannot be found for the specified SID, an empty collection is returned.
  /// <br /><br />
  /// To get installed applications for the system, use
  /// <see cref="FromStartMenu()"/>.
  /// </summary>
  /// <param name="userSid"></param>
  /// <returns></returns>
  public static InstalledApps FromStartMenu(SecurityIdentifier userSid, SystemUserProfile? userProfile = null) {
    ElevatedPrivileges.Require();

    if (userProfile is null) {
      // verify that the user profile exists
      var userProfiles = new SystemUserProfiles();
      userProfile = userProfiles.FirstOrDefault(up => up.Sid.Equals(userSid));
    }
    if (userProfile is null) {
      return [];
    }

    // get the applications from the user's Start Menu
    var startMenuPath = Path.Combine(userProfile.ProfilePath, @"AppData\Roaming\Microsoft\Windows\Start Menu\Programs");
    return FromShortcutsInFolder(startMenuPath);
  }

  enum PackageOrBundleType {
    Package,
    Bundle,
    Split
  }

  /// <summary>
  /// Gets a collection of AppX/MSIX package manifests based on the Registry entries
  /// in AppxAllUserStore and the packages discovered in the WindowsApps and SystemApps folders.
  /// </summary>
  /// <returns></returns>
  private static IEnumerable<(XDocument, ManifestFolder, PublisherHash)> GetAppPackageManifests() {
    ElevatedPrivileges.Require();
    var seenPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var subKey in new string[] { "InboxApplications", "Applications" }) {
      if (string.IsNullOrWhiteSpace(subKey)) {
        continue;
      }

      // construct the full registry path
      var registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\" + subKey;
      Console.WriteLine($"Checking registry path: {registryPath}");

      // process each registry key and extract package manifests within
      using (var key = Registry.LocalMachine.OpenSubKey(registryPath)) {
        if (key is null) {
          continue;
        }

        // loop through each package entry
        foreach (var packageName in key.GetSubKeyNames()) {
          var parts = packageName.Split('_');
          if (parts.Length < 4) {
            continue;
          }

          var packageBaseName = parts.FirstOrDefault();
          if (string.IsNullOrWhiteSpace(packageBaseName)) {
            continue;
          }
          if (seenPackages.Contains(packageBaseName)) {
            continue;
          }

          var publisherHash = parts.LastOrDefault() ?? "";
          if (string.IsNullOrWhiteSpace(publisherHash)) {
            continue;
          }

          // attempt to load the package manifest xml from the Path value in the registry entry
          // (some packages may not have this value set)
          using (var packageKey = key.OpenSubKey(packageName)) {
            var manifestFilePath = (string?)packageKey?.GetValue("Path");
            if (manifestFilePath is not null && File.Exists(manifestFilePath)) {
              var result = LoadPackageOrBundleManifestXml(manifestFilePath);
              if (result is not null) {
                var (manifestXml, type, packageFolder) = result.Value;
                if (type != PackageOrBundleType.Package) {
                  continue;
                }

                yield return (manifestXml, packageFolder, new PublisherHash(publisherHash));
                seenPackages.Add(packageBaseName ?? "");
                continue;
              }
            }
          }
        }
      }

      // scan for system apps
      var systemAppsPath = @"C:\Windows\SystemApps";
      var systemAppsAppxManifests = Directory.GetFiles(systemAppsPath, "AppxManifest.xml", SearchOption.AllDirectories);
      foreach (var manifestFilePath in systemAppsAppxManifests) {
        var result = LoadPackageOrBundleManifestXml(manifestFilePath);
        if (result is not null) {
          var (manifestXml, type, folder) = result.Value;
          if (type != PackageOrBundleType.Package) {
            continue;
          }

          var packageBaseName = Path.GetFileName(folder).Split('_').FirstOrDefault();
          if (string.IsNullOrWhiteSpace(packageBaseName) || seenPackages.Contains(packageBaseName)) {
            continue;
          }

          var publisherHash = Path.GetFileName(folder).Split('_').LastOrDefault() ?? "";
          if (string.IsNullOrWhiteSpace(publisherHash)) {
            continue;
          }

          yield return (manifestXml, folder, new PublisherHash(publisherHash));
          seenPackages.Add(packageBaseName ?? "");
        }
      }

      // scan for all other apps in WindowsApps
      var windowsAppsPath = @"C:\Program Files\WindowsApps";
      var windowsAppsAppxManifests = Directory.GetFiles(windowsAppsPath, "AppxManifest.xml", SearchOption.AllDirectories);
      foreach (var manifestFilePath in windowsAppsAppxManifests) {
        var result = LoadPackageOrBundleManifestXml(manifestFilePath);
        if (result is not null) {
          var (manifestXml, type, folder) = result.Value;
          if (type != PackageOrBundleType.Package) {
            continue;
          }

          var packageBaseName = Path.GetFileName(folder).Split('_').FirstOrDefault();
          if (string.IsNullOrWhiteSpace(packageBaseName) || seenPackages.Contains(packageBaseName)) {
            continue;
          }

          var publisherHash = Path.GetFileName(folder).Split('_').LastOrDefault() ?? "";
          if (string.IsNullOrWhiteSpace(publisherHash)) {
            continue;
          }

          yield return (manifestXml, folder, new PublisherHash(publisherHash));
          seenPackages.Add(packageBaseName ?? "");
        }
      }
    }
  }

  public readonly record struct ManifestFolder(string Value) {
    public static implicit operator string(ManifestFolder folderPath) => folderPath.Value;
  }
  public readonly record struct PublisherHash(string Value) {
    public static implicit operator string(PublisherHash hash) => hash.Value;
  }

  /// <summary>
  /// Determines whether the specified XML document is an AppX/MSIX package or bundle manifest.
  /// <br /><br />
  /// An AppX package manifest has a root element of "Package".
  /// <br />
  /// An AppX bundle manifest has a root element of "Bundle".
  /// </summary>
  /// <param name="manifestXml"></param>
  /// <returns></returns>
  private static bool IsXmlPackageOrBundle(XDocument manifestXml) {
    var isBundle = manifestXml.Root?.Name.LocalName == "Bundle";
    var isPackage = manifestXml.Root?.Name.LocalName == "Package";
    return isBundle || isPackage;
  }

  private static (XDocument, PackageOrBundleType, ManifestFolder)? LoadPackageOrBundleManifestXml(string manifestFilePath) {
    XDocument? manifestXml = null;
    try {
      manifestXml = XDocument.Load(manifestFilePath);
    }
    catch { }
    if (manifestXml is not null && IsXmlPackageOrBundle(manifestXml)) {
      var manifestDirectory = Path.GetDirectoryName(manifestFilePath);
      if (manifestDirectory is null) {
        return null;
      }

      var type = PackageOrBundleType.Package;
      if (manifestXml.Root?.Name.LocalName == "Bundle") {
        // this is a bundle manifest that contains paths to multiple packages
        type = PackageOrBundleType.Bundle;
      }
      else if (manifestDirectory.Contains("_split")) {
        // this is a split package, usually used for partial assets
        type = PackageOrBundleType.Split;
      }

      // Console.WriteLine(manifestDirectory);

      return (manifestXml, type, new ManifestFolder(manifestDirectory));
    }
    return null;
  }

  /// <summary>
  /// Parses the scale qualifier from an icon file name.
  /// File names will always end with .scale-XX.extension or .scale-XX_morequalifiers.extension
  /// </summary>
  private static int? InterpretAssetScale(string fileName) {
    var scaleIndex = fileName.LastIndexOf(".scale-");
    if (scaleIndex >= 0) {
      var scalePart = fileName.Substring(scaleIndex + 7);
      var dotIndex = scalePart.IndexOf('.');
      if (dotIndex >= 0) {
        scalePart = scalePart.Substring(0, dotIndex);
      }
      if (int.TryParse(scalePart, out var parsedScale)) {
        return parsedScale;
      }
    }
    return null;
  }

  /// <summary>
  /// Gets a collection of installed app packages based on the Registry entries
  /// in AppxAllUserStore and the packages discovered in the WindowsApps and SystemApps folders.
  /// </summary>
  /// <param name="registryAppxAllUserStoreSubKeys"></param>
  /// <returns></returns>
  public static InstalledApps FromAppPackages() {
    var installedApps = new InstalledApps();

    foreach (var (manifestXml, packageDir, publisherHash) in GetAppPackageManifests()) {
      // ensure the Package element exists
      if (manifestXml.Root is null || manifestXml.Root.Name.LocalName != "Package") {
        continue;
      }

      // read the package name
      var identityElement = manifestXml.Root.Elements().FirstOrDefault(e => e.Name.LocalName == "Identity");
      var packageName = identityElement?.Attribute("Name")?.Value;
      if (string.IsNullOrWhiteSpace(packageName)) {
        continue;
      }
      var fullPackageName = packageName + "_" + publisherHash;

      // determine the mainfest version
      var packageXmlns = manifestXml.Root.GetDefaultNamespace().NamespaceName;
      int? manifestVersion = packageXmlns switch {
        "http://schemas.microsoft.com/appx/2010/manifest" => 1, // some SystemApps on Windows Server 2016
        "http://schemas.microsoft.com/appx/manifest/foundation/windows10" => 2, // Windows Server 2016 and newer
        _ => null
      };

      // read the included applications
      var applicationsElement = manifestXml.Root.Elements().FirstOrDefault(e => e.Name.LocalName == "Applications");
      var applicationElements = applicationsElement?.Elements().Where(e => e.Name.LocalName == "Application");
      foreach (var applicationElement in applicationElements ?? []) {
        var appId = applicationElement.Attribute("Id")?.Value;
        if (string.IsNullOrWhiteSpace(appId)) {
          continue;
        }

        // if the AppListEntry is set to none, skip this application
        var visualElements = applicationElement.Elements().FirstOrDefault(e => e.Name.LocalName == "VisualElements");
        var appListEntryAttribute = visualElements?.Attribute("AppListEntry");
        if (visualElements is null || (appListEntryAttribute is not null && appListEntryAttribute.Value.Equals("none", StringComparison.OrdinalIgnoreCase))) {
          continue;
        }

        var applicationLaunchUri = "shell:AppsFolder\\" + fullPackageName + "!" + appId;

        // get the display name from uap:VisualElements
        var displayName = visualElements?.Attribute("DisplayName")?.Value;

        // if the display name is a ms-resource reference, open the resources PRI files from the package to resolve it
        if (displayName is not null && displayName.StartsWith("ms-resource:")) {

          // look through ever .pri file in the package directory, starting with resources.pri
          var appPriPaths = Directory.GetFiles(packageDir, "*.pri", SearchOption.AllDirectories)
            .OrderBy(path => Path.GetFileName(path).Equals("resources.pri") ? 0 : 1);
          foreach (var appPriPath in appPriPaths) {
            try {
              using (var resourceReader = new PriReader(appPriPath)) {
                // see if there is a resource matching the display name
                var resourceValue = resourceReader.ReadResource(displayName);
                if (!string.IsNullOrWhiteSpace(resourceValue)) {
                  displayName = resourceValue;
                  break;
                }

                // if no match was found, also check if it exists without the package name prefix
                var unnamespacedResourceKey = displayName
                  .Replace($"ms-resource://{packageName}/", "ms-resource://")
                  .Replace($"ms-resource:{packageName}/", "ms-resource:");
                resourceValue = resourceReader.ReadResource(unnamespacedResourceKey);
                if (!string.IsNullOrWhiteSpace(resourceValue)) {
                  displayName = resourceValue;
                  break;
                }
              }
            }
            catch (Exception ex) {
              throw new Exception($"Failed to read PRI file at path: {appPriPath}", ex);
            }
          }

          // if we still don't have a resolved display name, fall back to the package name
          if (displayName is null) {
            if (applicationElements?.Count() == 1) {
              displayName = packageName;
            }
            else {
              displayName = $"{packageName}!{appId}";
            }
          }
        }
        if (displayName is null || string.IsNullOrWhiteSpace(displayName)) {
          displayName = fullPackageName + "!" + appId;
        }

        // get the relative icon path
        var relativeIconPath = manifestVersion == 1 ? visualElements?.Attribute("SmallLogo")?.Value : visualElements?.Attribute("Square44x44Logo")?.Value;

        // attempt to find the largest scale version of the icon in the package folder
        string? iconPath = null;
        if (!string.IsNullOrWhiteSpace(relativeIconPath)) {

          // find all matching icon files 
          var matches = Directory.GetFiles(packageDir, $"{Path.GetFileNameWithoutExtension(relativeIconPath)}*{Path.GetExtension(relativeIconPath)}", SearchOption.AllDirectories);

          // get the largest scale version of the icon
          var bestMatch = matches
            .Select(path => new {
              Path = path,
              Scale = InterpretAssetScale(path)
            })
            .OrderByDescending(x => x.Scale)
            .FirstOrDefault()?.Path;

          if (bestMatch is not null) {
            iconPath = bestMatch;
          }
        }

        // search for file type associations
        var extensionsElements = applicationElement.Descendants().Where(elem => elem.Name.LocalName == "Extension");
        var fileTypeAssociationsElements = extensionsElements
          .Where(extElem => extElem.Attribute("Category")?.Value == "windows.fileTypeAssociation")
          .Elements()
          .Where(childElem => childElem.Name.LocalName == "FileTypeAssociation");
        var fileTypeAssociations = new FileTypeAssociations();
        foreach (var element in fileTypeAssociationsElements) {
          // attempt to find the largest scale version of the icon in the package folder
          var relativeLogoPath = element.Elements().Where(elem => elem.Name.LocalName == "Logo").FirstOrDefault()?.Value;
          string? logoPath = null;
          if (!string.IsNullOrWhiteSpace(relativeLogoPath)) {

            // find all matching icon files 
            var matches = Directory.GetFiles(packageDir, $"{Path.GetFileNameWithoutExtension(relativeLogoPath)}*{Path.GetExtension(relativeLogoPath)}", SearchOption.AllDirectories);

            // get the largest scale version of the icon
            var bestMatch = matches
              .Select(path => new {
                Path = path,
                Scale = InterpretAssetScale(path)
              })
              .OrderByDescending(x => x.Scale)
              .FirstOrDefault()?.Path;

            if (bestMatch is not null) {
              logoPath = bestMatch;
            }
          }

          // list the supported file types (.ext1, .ext2, etc.)
          var supportedFileTypes = element
            .Descendants()
            .Where(elem => elem.Name.LocalName == "FileType" && elem.Parent?.Name.LocalName == "SupportedFileTypes")
            .Select(fileTypeElem => fileTypeElem.Value)
            .ToList();

          foreach (var fileType in supportedFileTypes) {
            fileTypeAssociations.Add(new FileTypeAssociation(
              extension: fileType,
              iconPath: logoPath ?? "",
              iconIndex: 0
            ));
          }
        }

        var installedApp = new InstalledApp(
          path: @"C:\Windows\explorer.exe",
          displayName: displayName,
          displayFolder: "",
          iconPath: iconPath ?? "",
          iconIndex: 0,
          commandLineArguments: applicationLaunchUri,
          fileTypeAssociations: fileTypeAssociations
        );
        installedApps.Add(installedApp);
      }
    }

    return installedApps;
  }
}
