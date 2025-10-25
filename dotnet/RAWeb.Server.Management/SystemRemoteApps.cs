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

  /// <summary>
  /// Service implementation of <c>InstalledApps.FromStartMenu</c> and <c>InstalledApps.FromAppPackages</c>.
  /// </summary>
  [OperationContract]
  InstalledApps ListInstalledApps(string? userSid = null);
}
#endif

public class SystemRemoteApps {
  static readonly string s_applicationsRegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\TSAppAllowList\Applications";

  [DataContract]
  public class SecurityDescriptionDTO(List<string>? readAccessAllowedSids = null, List<string>? readAccessDeniedSids = null) {
    [DataMember] public List<string> ReadAccessAllowedSids { get; set; } = readAccessAllowedSids ?? [];
    [DataMember] public List<string> ReadAccessDeniedSids { get; set; } = readAccessDeniedSids ?? [];
  }

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
    [DataMember] public FileTypeAssociations FileTypeAssociations { get; set; }
    [IgnoreDataMember] public RawSecurityDescriptor? SecurityDescriptor { get; set; }

    // expose the security identifier values with allowed and denied read access per the security descriptor
    [DataMember]
    public SecurityDescriptionDTO? SecurityDescription {
      get {
        if (SecurityDescriptor is null) {
          return null;
        }

        var allowed = SecurityDescriptorExtensions
            .GetExplicitlyAllowedSids(SecurityDescriptor, FileSystemRights.ReadData)
            .Select(sid => sid.Value)
            .ToList();

        var denied = SecurityDescriptorExtensions
            .GetExplicitlyDeniedSids(SecurityDescriptor, FileSystemRights.ReadData)
            .Select(sid => sid.Value)
            .ToList();

        return new SecurityDescriptionDTO(allowed, denied);
      }
      set {
        if (value is null) {
          SecurityDescriptor = null;
          return;
        }

        // set the non-serializable security descriptor property
        SecurityDescriptor = SecurityTransformers.SidRightsToRawSecurityDescriptor(
          allowedSids: value.ReadAccessAllowedSids.ConvertAll(sid => new Tuple<string, FileSystemRights?>(sid, FileSystemRights.ReadData)),
          deniedSids: value.ReadAccessDeniedSids.ConvertAll(sid => new Tuple<string, FileSystemRights?>(sid, FileSystemRights.ReadData))
        );
      }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemRemoteApp"/> class.
    /// </summary>
    public SystemRemoteApp(
        string key,
        string name,
        string path,
        string vPath,
        string iconPath,
        int? iconIndex,
        string? commandLine,
        CommandLineMode? commandLineOption,
        bool? includeInWorkspace,
        FileTypeAssociations? fileTypeAssociations,
        RawSecurityDescriptor? securityDescriptor = null,
        SecurityDescriptionDTO? securityDescription = null
    ) {
      Key = key;
      Name = name;
      Path = path;
      VPath = vPath ?? path;

      IconPath = iconPath;
      IconIndex = iconIndex ?? 0;
      CommandLine = commandLine ?? "";
      CommandLineOption = commandLineOption ?? CommandLineMode.Optional;
      IncludeInWorkspace = includeInWorkspace ?? false;
      FileTypeAssociations = fileTypeAssociations ?? [];

      // set the security descriptor property only once, preferring the RawSecurityDescriptor input first
      if (securityDescriptor is not null) {
        SecurityDescriptor = securityDescriptor;
      }
      else if (securityDescription is not null) {
        SecurityDescription = securityDescription;
      }

    }

    public enum CommandLineMode {
      Disabled = 0,
      Optional = 1,
      Enforced = 2
    }

    /// <summary>
    /// Writes the RemoteApp definition to the system registry.
    /// <br /><br />
    /// Use this method to create or update a RemoteApp.
    /// </summary>
    public void WriteToRegistry() {
      ElevatedPrivileges.Require();
      EnsureRegistryPathExists();

      using (var appsKey = Registry.LocalMachine.OpenSubKey(s_applicationsRegistryPath, true)) {
        if (appsKey is null) {
          throw new InvalidOperationException("Failed to open RemoteApp applications registry key for writing.");
        }

        using (var appKey = appsKey.CreateSubKey(Key)) {
          appKey.SetValue("Name", Name);
          appKey.SetValue("Path", Path);
          appKey.SetValue("VPath", VPath);
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
      ElevatedPrivileges.Require();
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
  public class FileTypeAssociation(string extension, string iconPath, int iconIndex = 0) {
    [DataMember] public string Extension { get; set; } = extension;
    [DataMember] public string IconPath { get; set; } = iconPath;
    [DataMember] public int IconIndex { get; set; } = iconIndex;
  }

  /// <summary>
  /// A collection of file type associations for a RemoteApp.
  /// </summary>
  [CollectionDataContract]
  public class FileTypeAssociations : System.Collections.ObjectModel.Collection<FileTypeAssociation> {
    public FileTypeAssociations() {
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
    ElevatedPrivileges.Require();
    using (Registry.LocalMachine.CreateSubKey(s_applicationsRegistryPath, writable: true)) {
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
        var fileTypeAssociations = new FileTypeAssociations();
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
        var vPath = Convert.ToString(appKey.GetValue("VPath", ""));
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
}
