using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Xml.Linq;
using Microsoft.Win32;

namespace RAWeb.Server.Management;

/// <summary>
/// Represents information about an installed application on the system.
/// </summary>
[DataContract]
public class InstalledApp(string path, string displayName, string displayFolder, string iconPath, int iconIndex = 0, string commandLineArguments = "") {
  [DataMember] public string Path { get; set; } = path;
  [DataMember] public string DisplayName { get; set; } = displayName;
  [DataMember] public string DisplayFolder { get; set; } = displayFolder;
  [DataMember] public string IconPath { get; set; } = iconPath;
  [DataMember] public int IconIndex { get; set; } = iconIndex;
  [DataMember] public string CommandLineArguments { get; set; } = commandLineArguments ?? "";

  /// <summary>
  /// Translates a shortcut file (.lnk) into an InstalledApp object.
  /// </summary>
  /// <param name="shortcutFilePath"></param>
  /// <param name="programsPath"></param>
  /// <returns></returns>
  public static InstalledApp? FromShortcut(string shortcutFilePath, string programsPath) {
    var shortcutName = System.IO.Path.GetFileNameWithoutExtension(shortcutFilePath);
    var displayFolder = System.IO.Path.GetDirectoryName(shortcutFilePath)?.Substring(programsPath.Length).TrimStart('\\');

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

    var installedApp = new InstalledApp(
      path: targetPath,
      displayName: shortcutName,
      displayFolder: displayFolder ?? "",
      iconPath: iconPath,
      iconIndex: iconIndex,
      commandLineArguments: targetArguments
    );
    return installedApp;
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
    foreach (var shortcutFilePath in shortcutFiles) {
      try {
        var installedApp = InstalledApp.FromShortcut(shortcutFilePath, folderPath);
        if (installedApp is not null) {
          foundApps.Add(installedApp);
        }
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
    return FromShortcutsInFolder(programsPath);
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
    Bundle
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
      var type = manifestXml.Root?.Name.LocalName == "Bundle" ? PackageOrBundleType.Bundle : PackageOrBundleType.Package;
      return (manifestXml, type, new ManifestFolder(Path.GetDirectoryName(manifestFilePath)!));
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
            using (var resourceReader = new PriReader(appPriPath)) {
              var resourceValue = resourceReader.ReadResource(displayName);
              if (!string.IsNullOrWhiteSpace(resourceValue)) {
                displayName = resourceValue;
                break;
              }
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
        var relativeIconPath = visualElements?.Attribute("Square44x44Logo")?.Value;

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

        // TODO: load the file type associations from Extensions element
        // TODO: (Application > Extensions > uap:Extension with Category="windows.fileTypeAssociation" > uap:FileTypeAssociation)
        // TODO: For each uap:FileTypeAssociation:
        // TODO: - uap:Logo -> the icon (without the resolution qualifier)
        // TODO: - uap:SupportedFileTypes -> the list of file extensions in this group of associations (all share the same icon)

        var installedApp = new InstalledApp(
          path: @"C:\Windows\explorer.exe",
          displayName: displayName,
          displayFolder: "",
          iconPath: iconPath ?? "",
          iconIndex: 0,

          commandLineArguments: applicationLaunchUri
        );
        installedApps.Add(installedApp);
      }
    }

    return installedApps;
  }
}
