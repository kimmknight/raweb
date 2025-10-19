using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceModel;
using Microsoft.Win32;

namespace RAWeb.Server.Management;

#if NET462
/// <summary>
/// WCF service contract for managing RemoteApp programs in the system registry.
/// <br /><br />
/// This contract is implemented by RAWeb.Server.Management.ServiceHost.SystemRemoteAppsService.
/// The service is itended to run with elevated/administrative privileges,
/// allowing it to read and write RemoteApp definitions in the system registry.
/// All other processes (such as RAWeb web server) should access RemoteApp management functionality
/// via this service to ensure that they have the necessary privileges. Therefore, all other processes
/// should use a WCF client proxy to call this service instead of directly accessing
/// the RAWeb.Server.Management.SystemRemoteApps class for elevated operations. Additionally, these
/// processes should NOT run with elevated privileges themselves to minimize security risks.
/// </summary>
[ServiceContract]
public interface ISystemRemoteAppsService {
  /// <summary>
  /// Service implementation of <c>SystemRemoteApps.SystemRemoteApp.WriteToRegistry</c>.
  /// </summary>
  [OperationContract]
  void WriteRemoteAppToRegistry(SystemRemoteApps.SystemRemoteApp app);

  /// <summary>
  /// Service implementation of <c>SystemRemoteApps.SystemRemoteApp.DeleteFromRegistry</c>.
  /// </summary>
  [OperationContract]
  void DeleteRemoteAppFromRegistry(SystemRemoteApps.SystemRemoteApp app);
}
#endif

public class SystemRemoteApps {
  static readonly string s_applicationsRegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\TSAppAllowList\Applications";

  /// <summary>
  /// Represents a RemoteApp program as stored in the system registry.
  /// </summary>
  [DataContract]
  public class SystemRemoteApp {
    [DataMember] public string Key { get; set; }
    [DataMember] public string Name { get; set; }
    [DataMember] public string Path { get; set; }
    [DataMember] public string VPath { get; set; }
    [DataMember] public string IconPath { get; set; }
    [DataMember] public int IconIndex { get; set; }
    [DataMember] public string CommandLine { get; set; }
    [DataMember] public CommandLineMode CommandLineOption { get; set; }
    [DataMember] public bool IncludeInWorkspace { get; set; }
    [DataMember] public FileTypeAssociationCollection FileTypeAssociations { get; set; }
    [IgnoreDataMember] public RawSecurityDescriptor? SecurityDescriptor { get; set; }

    /// <summary>
    /// Gets or sets the security descriptor in SDDL string format for serialization.
    /// </summary>
    [DataMember]
    public string? SecurityDescriptorSddl {
      private get {
        return SecurityDescriptor?.GetSddlForm(AccessControlSections.All);
      }
      set {
        // set the non-string version of the property
        SecurityDescriptor = !string.IsNullOrEmpty(value)
          ? new RawSecurityDescriptor(value)
          : null;
      }
    }

    public enum CommandLineMode {
      Disabled = 0,
      Optional = 1,
      Enforced = 2
    }

    public SystemRemoteApp(string key, string name, string path, string vPath, string iconPath, int? iconIndex, string? commandLine, CommandLineMode? commandLineOption, bool? includeInWorkspace, FileTypeAssociationCollection? fileTypeAssociations, RawSecurityDescriptor? securityDescriptor) {
      Key = key;
      Name = name;
      Path = path;
      VPath = vPath;
      IconPath = iconPath;
      IconIndex = iconIndex ?? 0;
      CommandLine = commandLine ?? "";
      CommandLineOption = commandLineOption ?? CommandLineMode.Optional;
      IncludeInWorkspace = includeInWorkspace ?? false;
      FileTypeAssociations = fileTypeAssociations ?? [];
      SecurityDescriptor = securityDescriptor;
    }

    /// <summary>
    /// Writes the RemoteApp definition to the system registry.
    /// <br /><br />
    /// Use this method to create or update a RemoteApp.
    /// </summary>
    public void WriteToRegistry() {
      RequireElevatedPrivileges();
      EnsureRegistryPathExists();

      using (var appsKey = Registry.LocalMachine.OpenSubKey(s_applicationsRegistryPath, true)) {
        if (appsKey is null) {
          throw new InvalidOperationException("Failed to open RemoteApp applications registry key for writing.");
        }

        using (var appKey = appsKey.CreateSubKey(Key)) {
          appKey.SetValue("Name", Name);
          appKey.SetValue("Path", Path);
          appKey.SetValue("VirtualPath", VPath);
          appKey.SetValue("IconPath", IconPath);
          appKey.SetValue("IconIndex", IconIndex);
          appKey.SetValue("RequiredCommandLine", CommandLine);
          appKey.SetValue("CommandLineSetting", (int)CommandLineOption);
          appKey.SetValue("ShowInTSWA", IncludeInWorkspace ? 1 : 0);

          if (SecurityDescriptor != null) {
            appKey.SetValue("SecurityDescriptor", SecurityDescriptor.GetSddlForm(AccessControlSections.All));
          }
          else {
            appKey.DeleteValue("SecurityDescriptor", false);
          }

          // write file type associations
          if (FileTypeAssociations.Count == 0) {
            appKey.DeleteSubKeyTree("FileTypeAssociations", false);
          }
          else {
            using (var ftaKey = appKey.CreateSubKey("FileTypeAssociations")) {
              foreach (var fta in FileTypeAssociations) {
                using (var extKey = ftaKey.CreateSubKey(fta.Extension)) {
                  extKey.SetValue("IconPath", fta.IconPath);
                  extKey.SetValue("IconIndex", fta.IconIndex);
                }
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Deletes the RemoteApp definition from the system registry.
    /// </summary>
    public void DeleteFromRegistry() {
      RequireElevatedPrivileges();
      EnsureRegistryPathExists();

      using (var appsKey = Registry.LocalMachine.OpenSubKey(s_applicationsRegistryPath, true)) {
        if (appsKey is null) {
          throw new InvalidOperationException("Failed to open RemoteApp applications registry key for writing.");
        }

        appsKey.DeleteSubKeyTree(Key, false);
      }
    }
  }

  /// <summary>
  /// A collection of RemoteApp programs from the registry.
  /// </summary>
  [CollectionDataContract]
  public class SystemRemoteAppCollection : System.Collections.ObjectModel.Collection<SystemRemoteApp> {
    public SystemRemoteAppCollection() {
    }
  }

  /// <summary>
  /// Represents a file type association for a RemoteApp.
  /// </summary>
  [DataContract]
  public class FileTypeAssociation {
    [DataMember] public string Extension { get; set; }
    [DataMember] public string IconPath { get; set; }
    [DataMember] public int IconIndex { get; set; }

    public FileTypeAssociation(string extension, string iconPath, int iconIndex = 0) {
      Extension = extension;
      IconPath = iconPath;
      IconIndex = iconIndex;
    }
  }

  /// <summary>
  /// A collection of file type associations for a RemoteApp.
  /// </summary>
  [CollectionDataContract]
  public class FileTypeAssociationCollection : System.Collections.ObjectModel.Collection<FileTypeAssociation> {
    public FileTypeAssociationCollection() {
    }
  }

  /// <summary>
  /// Ensures that the registry path for RemoteApps exists.
  /// </summary>
  private static void EnsureRegistryPathExists() {
    // first, check if the path exists
    using (var key = Registry.LocalMachine.CreateSubKey(s_applicationsRegistryPath, writable: false)) {
      if (key != null) {
        return;
      }
    }

    // if we reach here, the key does not exist, so we need to create it
    RequireElevatedPrivileges();
    using (Registry.LocalMachine.CreateSubKey(s_applicationsRegistryPath, writable: true)) {
    }
  }

  /// <summary>
  /// Ensures that the current process has elevated privileges.
  /// </summary>
  private static void RequireElevatedPrivileges() {
    var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    var isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
    if (!isElevated) {
      throw new UnauthorizedAccessException("The current process does not have elevated/administrative privileges.");
    }
  }

  /// <summary>
  /// Gets a RemoteApp program by name from the system registry.
  /// </summary>
  /// <param name="appName"></param>
  /// <returns></returns>
  public SystemRemoteApp? GetRegistedApp(string appName) {
    EnsureRegistryPathExists();

    using (var appsKey = Registry.LocalMachine.OpenSubKey(s_applicationsRegistryPath)) {
      if (appsKey is null) {
        return null;
      }

      using (var appKey = appsKey.OpenSubKey(appName)) {
        if (appKey == null) {
          return null;
        }

        // read the security descriptor
        var securityDescriptorString = (string?)appKey.GetValue("SecurityDescriptor", null);
        var securityDescriptor = string.IsNullOrEmpty(securityDescriptorString) ? null : new RawSecurityDescriptor(securityDescriptorString);

        // read file type associations, where are stored in a subkey
        var fileTypeAssociations = new FileTypeAssociationCollection();
        using (var fileTypeAssociationsKey = appKey.OpenSubKey("FileTypeAssociations")) {
          if (fileTypeAssociationsKey != null) {
            foreach (var ext in fileTypeAssociationsKey.GetSubKeyNames()) {
              using (var extKey = fileTypeAssociationsKey.OpenSubKey(ext)) {
                if (extKey is null) {
                  continue;
                }

                var fta = new FileTypeAssociation(
                  extension: ext,
                  iconPath: (string)extKey.GetValue("IconPath", ""),
                  iconIndex: (int)extKey.GetValue("IconIndex", 0)
                );
                fileTypeAssociations.Add(fta);
              }
            }
          }
        }

        var name = Convert.ToString(appKey.GetValue("Name", appName));
        var path = Convert.ToString(appKey.GetValue("Path", ""));
        var vPath = Convert.ToString(appKey.GetValue("VirtualPath", ""));
        var iconPath = Convert.ToString(appKey.GetValue("IconPath", ""));
        if (name is null || path is null || vPath is null || iconPath is null) {
          return null;
        }

        var app = new SystemRemoteApp(
          key: appName,
          name: name,
          path: path,
          vPath: vPath,
          iconPath: iconPath,
          iconIndex: Convert.ToInt32(appKey.GetValue("IconIndex", 0)),
          commandLine: Convert.ToString(appKey.GetValue("RequiredCommandLine", "")),
          commandLineOption: (SystemRemoteApp.CommandLineMode)Convert.ToInt32(appKey.GetValue("CommandLineSetting", 1)),
          includeInWorkspace: Convert.ToInt32(appKey.GetValue("ShowInTSWA", 0)) != 0,
          fileTypeAssociations: fileTypeAssociations,
          securityDescriptor: securityDescriptor
        );

        return app;
      }
    }
  }

  /// <summary>
  /// Gets all RemoteApp programs from the system registry.
  /// </summary>
  /// <returns></returns>
  public SystemRemoteAppCollection GetAllRegisteredApps() {
    EnsureRegistryPathExists();

    var apps = new SystemRemoteAppCollection();
    using (var appsKey = Registry.LocalMachine.OpenSubKey(s_applicationsRegistryPath)) {
      if (appsKey is null) {
        return apps;
      }

      foreach (var appName in appsKey.GetSubKeyNames()) {
        var app = GetRegistedApp(appName);
        if (app != null) {
          apps.Add(app);
        }
      }
    }
    return apps;
  }

  /// <summary>
  /// Represents information about an installed application on the system.
  /// </summary>
  public class InstalledApp {
    public string Path { get; set; }
    public string DisplayName { get; set; }
    public string DisplayFolder { get; set; }
    public string IconPath { get; set; }
    public int IconIndex { get; set; }
    public string CommandLineArguments { get; set; }

    public InstalledApp(string path, string displayName, string displayFolder, string iconPath, int iconIndex = 0, string commandLineArguments = "") {
      Path = path;
      DisplayName = displayName;
      DisplayFolder = displayFolder;
      IconPath = iconPath;
      IconIndex = iconIndex;
      CommandLineArguments = commandLineArguments ?? "";
    }
  }

  /// <summary>
  /// A collection of installed applications on the system.
  /// </summary>
  public class InstalledAppsCollection : System.Collections.ObjectModel.Collection<InstalledApp> {
    public InstalledAppsCollection() {
    }
    public InstalledAppsCollection(IList<InstalledApp> apps) {
      foreach (var app in apps) {
        this.Add(app);
      }
    }
  }

  /// <summary>
  /// Translates a shortcut file (.lnk) into an InstalledApp object.
  /// </summary>
  /// <param name="shortcutFilePath"></param>
  /// <param name="programsPath"></param>
  /// <returns></returns>
  private InstalledApp? TranslateShortcutToInstalledApp(string shortcutFilePath, string programsPath) {
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

  /// <summary>
  /// Gets a list of installed applications on the system based on the Start Menu entries.
  /// </summary>
  /// <returns></returns>
  public InstalledAppsCollection GetInstalledApps() {
    var installedApps = new InstalledAppsCollection();

    // enumerate all .lnk files in the Start Menu Programs folder and its subfolders
    var programsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + @"\Programs";
    var shortcutFiles = System.IO.Directory.GetFiles(programsPath, "*.lnk", System.IO.SearchOption.AllDirectories);
    foreach (var shortcutFilePath in shortcutFiles) {
      var installedApp = TranslateShortcutToInstalledApp(shortcutFilePath, programsPath);
      if (installedApp is not null) {
        installedApps.Add(installedApp);
      }
    }

    return installedApps;
  }

  /// <summary>
  /// Gets a list of installed applications for a specific user based on their
  /// Start Menu entries and installed AppX/MSIX packages.
  /// </summary>
  /// <param name="sid"></param>
  /// <returns></returns>
  public InstalledAppsCollection GetInstalledAppsForUser(SecurityIdentifier sid) {
    var userProfiles = GetAllUserProfiles();
    var userProfile = userProfiles.FirstOrDefault(up => up.Sid.Equals(sid));
    if (userProfile != null) {
      var startMenuApps = userProfile.GetStartMenuApps();
      var appPackages = userProfile.GetInstalledAppPackages();
      return new InstalledAppsCollection(startMenuApps.Concat(appPackages).ToList());
    }
    return new InstalledAppsCollection();
  }

  /// <summary>
  /// Represents information about a user profile on the system.
  /// </summary>
  public class RegistryUserProfile {
    public string UserName { get; set; }
    public string ProfilePath { get; set; }
    public string DisplayName { get; set; }
    public SecurityIdentifier Sid { get; set; }

    public RegistryUserProfile(string username, string profilePath, string displayName, SecurityIdentifier sid) {
      UserName = username;
      ProfilePath = profilePath;
      DisplayName = displayName;
      Sid = sid;
    }

    public override string ToString() {
      return DisplayName + " (" + UserName + ")";
    }

    /// <summary>
    /// Gets a list of installed applications for this user profile based on the Start Menu entries.
    /// </summary>
    /// <returns></returns>
    public InstalledAppsCollection GetStartMenuApps() {
      RequireElevatedPrivileges();

      var foundApps = new InstalledAppsCollection();

      // TODO: Run this in a separate service that has administrative privileges
      // TODO: to ensure that we can read the profile data even if the current process
      // TODO: does not have access to the user's profile folder.
      // TODO: (which is likely since RAWeb runs as an IIS App Pool user with the app pool identity)
      var startMenuPath = System.IO.Path.Combine(ProfilePath, @"AppData\Roaming\Microsoft\Windows\Start Menu\Programs");
      if (!System.IO.Directory.Exists(startMenuPath)) {
        return foundApps;
      }

      var shortcutFiles = System.IO.Directory.GetFiles(startMenuPath, "*.lnk", System.IO.SearchOption.AllDirectories);
      foreach (var shortcutFilePath in shortcutFiles) {
        try {
          var installedApp = new SystemRemoteApps().TranslateShortcutToInstalledApp(shortcutFilePath, startMenuPath);
          if (installedApp is not null) {
            foundApps.Add(installedApp);
          }
        }
        catch (Exception ex) {
          if (ex is UnauthorizedAccessException || ex is System.IO.FileNotFoundException) {
            // skip individual files that cannot be accessed
            continue;
          }
          throw;
        }
      }

      return foundApps;
    }

    /// <summary>
    /// Gets a list of installed app packages for this user profile
    /// based on the results of the Get-AppxPackage PowerShell cmdlet.
    /// <br /><br />
    /// This method finds AppX and MSIX packages installed for the user.
    /// </summary>
    /// <returns></returns>
    public InstalledAppsCollection GetInstalledAppPackages() {
      RequireElevatedPrivileges();

      var foundApps = new InstalledAppsCollection();

      // TODO: Create a service that will load SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages 
      // TODO: from the specified user's NTUSER.DAT hive, and read the installed packages from there.
      // TODO: It will need to load the hive if not already loaded, and unload it afterwards.
      // TODO: Addiitonally, it will need to validate each entry to ensure that there is a
      // TODO: real package at the path specified in the registry, and it will also need to
      // TODO: find the correct shortcut (starts with shell:AppsFolder) and icon for each package.
      // TODO: (Determine the icon name from Square44x44Logo in the AppxManifest.xml inside the package folder
      // TODO: and then find the largest scale version of that icon inside the package folder.)
      // TODO: Note that some packages may contain multiple apps inside them, so the shortcut
      // TODO: and icon logic will need to run only after enumerating all apps inside the package.
      // TODO: Additionally, the supported file type associations and their associated icon 
      // TODO: for each package will need to be read from the registry.
      // FYI: System.Management.Automation must be loaded from C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Management.Automation\v4.0_3.0.0.0__31bf3856ad364e35\System.Management.Automation.dll
      // <!-- load System.Management.Automation so we can execute powershell 5 commands -->
      // <Target Name="CopyPS" BeforeTargets="ResolveAssemblyReferences">
      //   <Copy
      //     SourceFiles="C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Management.Automation\v4.0_3.0.0.0__31bf3856ad364e35\System.Management.Automation.dll"
      //     DestinationFiles="$(BaseIntermediateOutputPath)\System.Management.Automation.dll" />
      // </Target>
      // <ItemGroup>
      //   <Reference Include="System.Management.Automation">
      //     <HintPath>$(BaseIntermediateOutputPath)\System.Management.Automation.dll</HintPath>
      //   </Reference>
      // </ItemGroup>
      // (and also via web.config)
      // using (var ps = PowerShell.Create()) {
      //   ps.AddScript("Get-AppxPackage -User " + Sid.Value + " | Select-Object Name, InstallLocation");
      //   var results = ps.Invoke();
      //   Console.WriteLine(results.GetType().FullName);

      //   Console.WriteLine("Found " + results.Count + " app packages for user " + DisplayName);
      //   Console.WriteLine("Found " + results.Count + " app packages for user " + DisplayName);
      //   Console.WriteLine("Found " + results.Count + " app packages for user " + DisplayName);
      //   Console.WriteLine("Found " + results.Count + " app packages for user " + DisplayName);
      //   Console.WriteLine("Get-AppxPackage -User " + Sid.Value + " | Select-Object Name, InstallLocation");

      //   // loop through results and extract app information
      //   foreach (dynamic result in results) {
      //     Console.WriteLine(result);
      //     try {
      //       if (result && result.Members) {
      //         string name = result.Members["Name"].Value;
      //         string path = result.Members["InstallLocation"].Value;

      //         var app = new InstalledApp(
      //           path: path,
      //           displayName: name,
      //           displayFolder: "",
      //           iconPath: ""
      //         );
      //         foundApps.Add(app);
      //       }
      //     }
      //     catch (Exception exception) {
      //       Console.WriteLine("Failed to process package entry.");
      //       Console.WriteLine(exception.ToString());
      //       throw;
      //       // ignore errors
      //     }
      //   }
      // }

      return foundApps;
    }
  }

  /// <summary>
  /// A collection of user profiles on the system.
  /// </summary>
  public class RegistryUserProfileCollection : System.Collections.ObjectModel.Collection<RegistryUserProfile> {
    public RegistryUserProfileCollection() {
    }
  }

  /// <summary>
  /// Gets a list of all user profiles on the system based on the registry.
  /// <br /><br />
  /// The profiles are read from the ProfileList registry key.
  /// If a profile folder does not exist on disk, the profile is skipped.
  /// This means that accounts that have never signed in to an interactive session
  /// (such as service accounts) will not be included in the list.
  /// </summary>
  /// <returns></returns>
  public RegistryUserProfileCollection GetAllUserProfiles() {
    var collection = new RegistryUserProfileCollection();

    var registryProfileListPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList";
    foreach (var sid in Registry.LocalMachine?.OpenSubKey(registryProfileListPath)?.GetSubKeyNames() ?? []) {
      using (var profileKey = Registry.LocalMachine?.OpenSubKey(registryProfileListPath + @"\" + sid)) {
        // get the path to the profile folder according to the registry
        var profilePath = (string?)profileKey?.GetValue("ProfileImagePath");

        // confirm that the profile path actualy exists
        // (service accounts, which don't have profile folders, may also be listed in the registry)
        var exists = profilePath is not null && System.IO.Directory.Exists(profilePath);
        if (!exists) {
          continue;
        }

        // resolve the sid into an account name
        var sidObj = new SecurityIdentifier(sid);
        string username;
        string domain;
        try {
          var ntAccount = (NTAccount)sidObj.Translate(typeof(NTAccount));
          var accountName = ntAccount.Value;
          var parts = accountName.Split('\\');
          if (parts.Length == 2) {
            domain = parts[0];
            username = parts[1];
          }
          else {
            domain = Environment.MachineName;
            username = accountName;
          }
        }
        catch {
          username = profilePath!.Split('\\').Last() ?? sid;
          domain = Environment.MachineName;
        }

        // resolve the sid into a display name
        var displayName = domain + "\\" + username;
        try {
          using (var context = new PrincipalContext(ContextType.Domain, domain)) {
            var principal = UserPrincipal.FindByIdentity(context, IdentityType.Sid, sid);

            if (principal != null && !string.IsNullOrEmpty(principal.DisplayName))
              displayName = principal.DisplayName;
          }
        }
        catch {
          if (domain == Environment.MachineName) {
            displayName = username;
          }
        }

        // create the user profile object
        var userProfile = new RegistryUserProfile(
          username: username,
          profilePath: profilePath!,
          displayName: displayName,
          sid: sidObj
        );
        collection.Add(userProfile);
      }
    }

    return collection;
  }
}
