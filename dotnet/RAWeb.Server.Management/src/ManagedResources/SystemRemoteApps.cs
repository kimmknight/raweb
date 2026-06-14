using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Win32;
using static RAWeb.Server.Management.RemoteAppProperties;

namespace RAWeb.Server.Management;

public class SystemRemoteApps(string? collectionName = null) {
  string applicationsRegistryPath => @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\TSAppAllowList\Applications";
  string collectionApplicationsRegistryPath => !string.IsNullOrEmpty(collectionName)
      ? $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\CentralPublishedResources\PublishedFarms\{collectionName}\Applications"
      : applicationsRegistryPath;
  string? collectionName { get; set; } = collectionName;

  /// <summary>
  /// Represents a RemoteApp program as stored in the system registry.
  /// </summary>
  public class SystemRemoteApp : ManagedResource {
    /// <summary>
    /// The collection name for this RemoteApp. If null, the RemoteApp is stored
    /// in the standard TSAppAllowList registry path. If non-null, the RemoteApp is
    /// stored in the collection-specific CentralPublishedResources registry path.
    /// </summary>
    public string? CollectionName { get; private set; }
    [JsonIgnore] public SystemRemoteApps sra { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemRemoteApp"/> class.
    /// </summary>
    public SystemRemoteApp(
        string key,
        string? collectionName,
        string name,
        string path,
        string iconPath,
        int? iconIndex,
        string? commandLine,
        CommandLineMode? commandLineOption,
        bool? includeInWorkspace,
        string[]? virtualFolders,
        FileTypeAssociationCollection? fileTypeAssociations,
        RawSecurityDescriptor? securityDescriptor = null,
        SecurityDescriptionDTO? securityDescription = null
    ) : base(
        source: collectionName is null ? ManagedResourceSource.TSAppAllowList : ManagedResourceSource.CentralPublishedResourcesApp,
        identifier: key,
        name: name,
        iconPath: iconPath,
        virtualFolders: virtualFolders ?? ["/"]
      ) {
      CollectionName = collectionName;

      // use the application path for the icon if not explorer.exe (every packaged app uses explorer.exe)
      if (string.IsNullOrEmpty(iconPath) && !Path.GetFileName(iconPath).Equals("explorer.exe")) {
        IconPath = path;
      }
      IconIndex = iconIndex ?? 0;

      RemoteAppProperties = new RemoteAppProperties(path, commandLineOption ?? CommandLineMode.Optional, commandLine, fileTypeAssociations);
      IncludeInWorkspace = includeInWorkspace ?? false;

      // set the security descriptor property only once, preferring the RawSecurityDescriptor input first
      if (securityDescriptor is not null) {
        SecurityDescriptor = securityDescriptor;
      }
      else if (securityDescription is not null) {
        SecurityDescription = securityDescription;
      }

      sra = new SystemRemoteApps(collectionName);

      if (ElevatedPrivileges.Check()) {
        RestorePackagedAppIconPath();
      }
    }

    /// <summary>
    /// Called after deserialization to re-initialize non-serialized properties.
    public static SystemRemoteApp? FromJSON(JsonObject jsonObject, JsonSerializerOptions? options = null) {
      var opts = options ?? ManagementJsonContext.Default.Options;

      // extract the registry key
      var key = (string?)jsonObject["identifier"];
      if (key is null) return null;

      // extract the collection name
      var collectionName = (string?)jsonObject["collectionName"];

      // attempt to extract the name, falling back to the identifier if not present
      var name = (string?)jsonObject["name"] ?? key;

      // extract icon information
      var iconPath = (string?)jsonObject["iconPath"];
      var iconIndex = jsonObject["iconIndex"]?.GetValue<int>();

      // extract includeInWorkspace flag
      var includeInWorkspace = jsonObject["includeInWorkspace"]?.GetValue<bool>() ?? false;

      // extract security descriptor
      var securityDescription = jsonObject["securityDescription"] is JsonObject securityDescriptionJson
        ? securityDescriptionJson.Deserialize(ManagementJsonContext.Default.SecurityDescriptionDTO)
        : null;
      var securityDescriptor = securityDescription?.ToRawSecurityDescriptor();

      // extract remoteapp properties
      var remoteAppProperties = jsonObject["remoteAppProperties"] is JsonObject remoteAppPropertiesJson
        ? remoteAppPropertiesJson.Deserialize(ManagementJsonContext.Default.RemoteAppProperties)
        : null;
      if (remoteAppProperties is null) {
        return null;
      }

      // extract virtual folders
      var virtualFolders = jsonObject["virtualFolders"]?.AsArray()
          .Select(x => (string?)x)
          .Where(x => x is not null)
          .Cast<string>()
          .ToArray()
        ?? ["/"];

      var resource = new SystemRemoteApp(
        key: key,
        collectionName: collectionName,
        name: name,
        path: remoteAppProperties.ApplicationPath,
        iconPath: iconPath ?? "",
        iconIndex: iconIndex,
        commandLine: remoteAppProperties.CommandLine,
        commandLineOption: remoteAppProperties.CommandLineOption,
        includeInWorkspace: includeInWorkspace,
        fileTypeAssociations: remoteAppProperties.FileTypeAssociations,
        securityDescriptor: securityDescriptor,
        virtualFolders: virtualFolders
      );

      if (ElevatedPrivileges.Check()) {
        resource.RestorePackagedAppIconPath();
      }

      // extract the RDP file string if it was provided
      var rdpFileString = (string?)jsonObject["rdpFileString"];
      resource.RdpFileString = rdpFileString;

      return resource;
    }

    /// <summary>
    /// Sets the collection name for this RemoteApp.
    /// <br /><br />
    /// This method also updates the source type (<see cref="ManagedResourceSource"/> enum) accordingly.
    /// </summary>
    /// <param name="collectionName"></param>
    public void SetCollectionName(string? collectionName) {
      CollectionName = collectionName;
      sra = new SystemRemoteApps(collectionName);

      if (collectionName is not null) {
        SetSource(ManagedResourceSource.CentralPublishedResourcesApp);
      }
      else {
        SetSource(ManagedResourceSource.TSAppAllowList);
      }
    }

    /// <summary>
    /// Validates and updates the icon path for packaged Windows apps.
    /// <br /><br />
    /// If the icon points to a packaged app in C:\Program Files\WindowsApps,
    /// this method checks if the path is still valid and updates it to the newest version if necessary.
    /// <br /><br />
    /// The folder name for poackaged apps includes a version number, so it may change when the app is updated.
    /// This method updates the icon path accordingly.
    /// </summary>
    /// <returns>True if the icon path was updated; false otherwise.</returns>
    public bool RestorePackagedAppIconPath() {
      ElevatedPrivileges.Require();

      // if the icon points to a packaged app in C:\Program Files\WindowsApps,
      // check if the path is still valid and update it to the newest version if necessary
      var isPackagedWindowsAppAndIcon = RemoteAppProperties?.CommandLine is not null
        && RemoteAppProperties.CommandLine.StartsWith("shell:AppsFolder", StringComparison.OrdinalIgnoreCase)
        && RemoteAppProperties.CommandLine.Contains('!')
        && IconPath is not null
        && IconPath.StartsWith(@"C:\Program Files\WindowsApps", StringComparison.OrdinalIgnoreCase);
      var isValidIconPath = File.Exists(Environment.ExpandEnvironmentVariables(IconPath ?? ""));
      if (isPackagedWindowsAppAndIcon && !isValidIconPath) {

        // extract the relative icon path inside the packaged app folder
        var iconRelativePath = IconPath!.Substring("C:\\Program Files\\WindowsApps".Length).TrimStart('\\');
        iconRelativePath = iconRelativePath.Substring(iconRelativePath.IndexOf('\\') + 1); // remove the package folder name

        // look for a matching package in the list of installed packages
        var matchingApp = InstalledApps.FromAppPackages().FirstOrDefault(app => app.CommandLineArguments == RemoteAppProperties?.CommandLine);

        if (matchingApp is not null && matchingApp.PackageDirectory is not null) {
          IconPath = matchingApp.PackageDirectory + "\\" + iconRelativePath;
          WriteToRegistry();
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Writes the RemoteApp definition to the system registry.
    /// <br /><br />
    /// Use this method to create or update a RemoteApp.
    /// <br /><br />
    /// If there is a collection name specified in the parent <c>SystemRemoteApps</c> instance,
    /// the RemoteApp will be written to the collection-specific registry path in addition
    /// to the main RemoteApp applications path. In this case, file type associations are only
    /// written to the collection-specific entry.
    /// </summary>
    public void WriteToRegistry() {
      ElevatedPrivileges.Require();

      // ensure the collectionApplicationsRegistryPath is correct 
      if (sra.collectionName != CollectionName) {
        sra = new SystemRemoteApps(CollectionName);
      }

      sra.EnsureRegistryPathExists();

      // set the main registry values
      using (var appsKey = Registry.LocalMachine.OpenSubKey(sra.applicationsRegistryPath, true)) {
        if (appsKey is null) {
          throw new InvalidOperationException("Failed to open RemoteApp applications registry key for writing.");
        }

        using (var appKey = appsKey.CreateSubKey(Identifier)) {
          appKey.SetValue("Name", Name);
          if (RemoteAppProperties is not null) {
            appKey.SetValue("Path", RemoteAppProperties.ApplicationPath);
            appKey.SetValue("VPath", RemoteAppProperties.ApplicationPath);
            if (RemoteAppProperties.CommandLine is not null) {
              appKey.SetValue("RequiredCommandLine", RemoteAppProperties.CommandLine);
            }
            appKey.SetValue("CommandLineSetting", (int)RemoteAppProperties.CommandLineOption);
          }
          if (IconPath is not null) {
            appKey.SetValue("IconPath", IconPath);
          }
          appKey.SetValue("IconIndex", IconIndex);

          // only set ShowInTSWA when we are not setting ShowInPortal
          if (sra.collectionName is null) {
            appKey.SetValue("ShowInTSWA", IncludeInWorkspace ? 1 : 0);
          }

          if (SecurityDescriptor != null) {
            appKey.SetValue("SecurityDescriptor", SecurityDescriptor.GetSddlForm(AccessControlSections.All));
          }
          else {
            appKey.DeleteValue("SecurityDescriptor", false);
          }

          // write file type associations
          if (RemoteAppProperties is not null) {
            if (RemoteAppProperties.FileTypeAssociations.Count == 0) {
              appKey.DeleteSubKeyTree("FileTypeAssociations", false);
            }
            else {
              using (var ftaKey = appKey.CreateSubKey("FileTypeAssociations")) {
                foreach (var fta in RemoteAppProperties.FileTypeAssociations) {
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

      // set the collection-specific registry values if a collection name is provided
      if (sra.collectionName is not null && !string.IsNullOrEmpty(sra.collectionName)) {
        using (var appsKey = Registry.LocalMachine.OpenSubKey(sra.collectionApplicationsRegistryPath, true)) {
          if (appsKey is null) {
            throw new InvalidOperationException("Failed to open collection RemoteApp applications registry key for writing.");
          }

          using (var appKey = appsKey.CreateSubKey(Identifier)) {
            appKey.SetValue("Name", Name);
            if (RemoteAppProperties is not null) {
              appKey.SetValue("Path", RemoteAppProperties.ApplicationPath);
              appKey.SetValue("VPath", RemoteAppProperties.ApplicationPath);
              if (RemoteAppProperties.CommandLine is not null) {
                appKey.SetValue("RequiredCommandLine", RemoteAppProperties.CommandLine);
              }
              appKey.SetValue("CommandLineSetting", (int)RemoteAppProperties.CommandLineOption);
            }
            if (IconPath is not null) {
              appKey.SetValue("IconPath", IconPath);
            }
            appKey.SetValue("IconIndex", IconIndex);
            appKey.SetValue("Folders", VirtualFolders);
            appKey.SetValue("ShowInPortal", IncludeInWorkspace ? 1 : 0);
            appKey.SetValue("RDPFileContents", RdpFileString ?? ToRdpFileStringBuilder(null).ToString());

            if (SecurityDescriptor != null) {
              appKey.SetValue("SecurityDescriptor", SecurityDescriptor.GetSddlForm(AccessControlSections.All));
            }
            else {
              appKey.DeleteValue("SecurityDescriptor", false);
            }

            // write file type associations
            if (RemoteAppProperties is not null) {
              if (RemoteAppProperties.FileTypeAssociations.Count == 0) {
                appKey.DeleteSubKeyTree("FileTypeAssociations", false);
              }
              else {
                using (var ftaKey = appKey.CreateSubKey("FileTypeAssociations")) {
                  foreach (var fta in RemoteAppProperties.FileTypeAssociations) {
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
      }
    }

    /// <summary>
    /// Deletes the RemoteApp definition from the system registry.
    /// </summary>
    public void DeleteFromRegistry() {
      ElevatedPrivileges.Require();
      sra.EnsureRegistryPathExists();

      // ensure the collectionApplicationsRegistryPath is correct 
      if (sra.collectionName != CollectionName) {
        sra = new SystemRemoteApps(CollectionName);
      }

      using (var appsKey = Registry.LocalMachine.OpenSubKey(sra.applicationsRegistryPath, true)) {
        if (appsKey is null) {
          throw new InvalidOperationException("Failed to open RemoteApp applications registry key for writing.");
        }

        appsKey.DeleteSubKeyTree(Identifier, false);
      }

      // delete from collection-specific registry path if a collection name is provided
      if (!string.IsNullOrEmpty(sra.collectionName)) {
        using (var appsKey = Registry.LocalMachine.OpenSubKey(sra.collectionApplicationsRegistryPath, true)) {
          if (appsKey is null) {
            throw new InvalidOperationException("Failed to open collection RemoteApp applications registry key for writing.");
          }

          appsKey.DeleteSubKeyTree(Identifier, false);
        }
      }
    }

    /// <summary>
    /// Gets the timestamp for when the resource was last modified in the registry (in UTC).
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public override DateTime GetLastWriteTimeUtc() {
      // ensure the collectionApplicationsRegistryPath is correct 
      if (sra.collectionName != CollectionName) {
        sra = new SystemRemoteApps(CollectionName);
      }

      // ensure the registry path exists
      sra.EnsureRegistryPathExists();

      // open the registry key if it exists
      var keyName = sra.collectionApplicationsRegistryPath + "\\" + Identifier;
      var regKey = Registry.LocalMachine.OpenSubKey(keyName);

      if (regKey is null && sra.collectionName != null) {

        // try the TSAppAllowList path if the collection-specific path doesn't exist
        var fallbackKeyName = sra.applicationsRegistryPath + "\\" + Identifier;
        var fallbackRegKey = Registry.LocalMachine.OpenSubKey(fallbackKeyName);
        if (fallbackRegKey is not null) {
          regKey?.Dispose();
          regKey = fallbackRegKey;
        }
      }

      if (regKey is null) {
        throw new Exception("The specified registry key does not exist: " + keyName);
      }

      using (regKey) {
        // get the last write time for the registry key
        var result = RegQueryInfoKey(
            regKey.Handle.DangerousGetHandle(),
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _,
            out var fileTime
        );
        if (result != 0) {
          throw new Exception("Failed to query registry key info. Error code: " + result);
        }

        return DateTime.FromFileTimeUtc(fileTime);
      }

    }


    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int RegQueryInfoKey(
        IntPtr hKey,
        IntPtr lpClass,
        IntPtr lpcchClass,
        IntPtr lpReserved,
        out int lpcSubKeys,
        out int lpcbMaxSubKeyLen,
        out int lpcbMaxClassLen,
        out int lpcValues,
        out int lpcbMaxValueNameLen,
        out int lpcbMaxValueLen,
        out int lpcbSecurityDescriptor,
        out long lpftLastWriteTime // FILETIME; convert with `DateTime.FromFileTime(lpftLastWriteTime)`
    );

    /// <summary>
    /// See <see cref="GetAllRegisteredApps"/>.
    /// </summary>
    /// <param name="appName"></param>
    /// <param name="collectionName"></param>
    /// <returns></returns>
    public static SystemRemoteApp? FromRegistryKey(string appName, string? collectionName = null) {
      return new SystemRemoteApps(collectionName).GetRegistedApp(appName);
    }

    public override StringBuilder ToRdpFileStringBuilder(string? fullAddress) {
      // if full address is missing, attempt to build it from the local computer name and domain
      if (string.IsNullOrWhiteSpace(fullAddress)) {
        var domain = IPGlobalProperties.GetIPGlobalProperties().DomainName ?? "local";
        if (string.IsNullOrEmpty(domain)) {
          domain = "local";
        }

        fullAddress = $"{Environment.MachineName}.{domain}";
      }

      // calculate the file extensions supported by the application
      var appFileExtCSV = RemoteAppProperties?.FileTypeAssociations
          .Select(fta => fta.Extension.ToLowerInvariant())
          .Aggregate("", (current, ext) => current + (current.Length == 0 ? ext : $",{ext}"));

      // search the registry for RDPFileContents - use it as a base if found
      // (only supported in centralized publishing collections)
      var rdpBuilder = new StringBuilder();
      if (CollectionName is not null && !string.IsNullOrEmpty(CollectionName)) {
        using (var appKey = Registry.LocalMachine.OpenSubKey($@"{sra.collectionApplicationsRegistryPath}\{Identifier}")) {
          if (appKey is not null) {
            var rdpFileContents = (string?)appKey.GetValue("RDPFileContents", null);
            if (!string.IsNullOrWhiteSpace(rdpFileContents)) {
              var text = rdpFileContents?
                .Replace("\\r\\n", "\r\n")
                .Replace("\\n", "\r\n") // normalize to Windows newlines
                .TrimEnd() ?? "";
              rdpBuilder.AppendLine(text);
            }
          }
        }
      }

      // build the RDP file contents
      rdpBuilder.AppendLine($"full address:s:{fullAddress}");
      rdpBuilder.AppendLine($"remoteapplicationname:s:{Name}");
      if (RemoteAppProperties is not null) {
        rdpBuilder.AppendLine($"remoteapplicationprogram:s:{RemoteAppProperties.ApplicationPath}");
      }
      else {
        rdpBuilder.AppendLine($"remoteapplicationprogram:s:||{Identifier}");
      }
      rdpBuilder.AppendLine("remoteapplicationmode:i:1");
      if (RemoteAppProperties is not null && RemoteAppProperties.CommandLineOption != CommandLineMode.Disabled) {
        rdpBuilder.AppendLine($"remoteapplicationcmdline:s:{RemoteAppProperties.CommandLine}");
      }
      rdpBuilder.AppendLine($"remoteapplicationfileextensions:s:{appFileExtCSV}");
      rdpBuilder.AppendLine("disableremoteappcapscheck:i:1");

      // if there are duplicate lines, keep only the last occurrence of each setting
      var rdpLines = rdpBuilder.ToString()
         .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries) // split into lines
         .GroupBy(line => line.Split([':'], 2)[0]) // group by property name
         .Select(group => group.Last()) // take the last occurrence of each property
         .OrderBy(line => line); // sort remaining lines alphabetically

      return rdpLines.Aggregate(new StringBuilder(), (sb, line) => sb.AppendLine(line));
    }

    public override (ZipArchive archive, ZipArchiveEntry rdpFileEntry, ZipArchiveEntry metadataEntry, string rdpFileString, ManagedFileResource.MetadataDTO metadata) ToResourceFile(bool skipIcons = false, System.Security.Principal.SecurityIdentifier? userSid = null) {
      var memoryStream = new MemoryStream();
      var archive = new ZipArchive(memoryStream, ZipArchiveMode.Update);

      var rdpFileString = ToRdpFileStringBuilder(null).ToString();
      if (rdpFileString is null) {
        throw new InvalidOperationException("The RDP file string cannot be null when generating a resource file.");
      }

      var entryTimestamp = GetResourceFileEntryTimestamp();

      // add the RDP file to the archive
      var rdpFileEntry = archive.CreateEntry("resource.rdp", CompressionLevel.NoCompression);
      rdpFileEntry.LastWriteTime = entryTimestamp;
      using (var entryStream = rdpFileEntry.Open())
      using (var writer = new StreamWriter(entryStream)) {
        writer.Write(rdpFileString);
      }

      // calculate the info.json value
      var metadata = new ManagedFileResource.MetadataDTO {
        __Version = 1,
        Name = Name,
        IncludeInWorkspace = IncludeInWorkspace,
        IconPath = IconPath,
        IconIndex = IconIndex,
        SecurityDescriptorSddl = SecurityDescriptor?.GetSddlForm(AccessControlSections.All),
        VirtualFolders = VirtualFolders is not null && VirtualFolders.Length > 0
          ? [.. VirtualFolders.Where(path => !string.IsNullOrWhiteSpace(path))]
          : null
      };

      // add the metadata to the archive
      var metadataEntry = archive.CreateEntry("info.json", CompressionLevel.NoCompression);
      metadataEntry.LastWriteTime = entryTimestamp;
      using (var entryStream = metadataEntry.Open())
      using (var writer = new StreamWriter(entryStream)) {
        var json = JsonSerializer.Serialize(metadata, MetadataJsonContext.Default.MetadataDTO);
        writer.Write(json);
      }

      // TODO: consider storing the icon in the .resource file

      return (archive, rdpFileEntry, metadataEntry, rdpFileString, metadata);
    }
  }

  /// <summary>
  /// A collection of RemoteApp programs from the registry.
  /// </summary>
  public class SystemRemoteAppCollection(IList<SystemRemoteApp>? apps = null) : Collection<SystemRemoteApp>(apps ?? []) {
  }

  /// <summary>
  /// Ensures that the registry paths for RemoteApps exists.
  /// </summary>
  public void EnsureRegistryPathExists() {
    var collectionApplicationsPathExists = string.IsNullOrEmpty(collectionName);
    var applicationsPathExists = false;

    // first, check if the paths exists
    using (var key = Registry.LocalMachine.CreateSubKey(applicationsRegistryPath, writable: false)) {
      if (key != null) {
        applicationsPathExists = true;
      }
    }
    if (!collectionApplicationsPathExists) {
      using (var key = Registry.LocalMachine.CreateSubKey(collectionApplicationsRegistryPath, writable: false)) {
        if (key != null) {
          collectionApplicationsPathExists = true;
        }
      }
    }

    // if both paths exist, we are done
    if (applicationsPathExists && collectionApplicationsPathExists) {
      return;
    }

    // if we reach here, at least one of the keys do not exist, so we need to create it
    ElevatedPrivileges.Require();
    using (Registry.LocalMachine.CreateSubKey(applicationsRegistryPath, writable: true)) {
    }
    using (Registry.LocalMachine.CreateSubKey(collectionApplicationsRegistryPath, writable: true)) {
    }
  }

  /// <summary>
  /// Gets a RemoteApp program by name from the system registry.
  /// </summary>
  /// <param name="appName"></param>
  /// <returns></returns>
  public SystemRemoteApp? GetRegistedApp(string appName) {
    EnsureRegistryPathExists();

    SystemRemoteApp? ReadFromRegistryPath(string registryPath) {
      using (var appsKey = Registry.LocalMachine.OpenSubKey(registryPath)) {
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

          // read folders
          var rawVirtualFolders = appKey.GetValue("Folders", null);
          string[] virtualFolders = rawVirtualFolders switch {
            string[] foldersArray => [.. foldersArray.Where(path => path is not null).Cast<string>()],
            string folderString => [folderString],
            _ => ["/"]
          };

          var name = Convert.ToString(appKey.GetValue("Name", appName));
          var path = Convert.ToString(appKey.GetValue("Path", ""));
          var vPath = Convert.ToString(appKey.GetValue("VPath", ""));
          var iconPath = Convert.ToString(appKey.GetValue("IconPath", ""));
          var rdpFileString = Convert.ToString(appKey.GetValue("RDPFileContents", null));
          if (name is null || path is null || vPath is null || iconPath is null) {
            return null;
          }

          var app = new SystemRemoteApp(
            key: appName,
            collectionName: collectionName,
            name: name,
            path: path,
            iconPath: iconPath,
            iconIndex: Convert.ToInt32(appKey.GetValue("IconIndex", 0)),
            commandLine: Convert.ToString(appKey.GetValue("RequiredCommandLine", "")),
            commandLineOption: (CommandLineMode)Convert.ToInt32(appKey.GetValue("CommandLineSetting", 1)),
            includeInWorkspace: Convert.ToInt32(appKey.GetValue(collectionName is not null ? "ShowInPortal" : "ShowInTSWA", 0)) != 0,
            fileTypeAssociations: fileTypeAssociations,
            securityDescriptor: securityDescriptor,
            virtualFolders: virtualFolders
          );
          app.RdpFileString = rdpFileString;

          return app;
        }
      }
    }

    // try to read from the collection-specific path first if a collection name is provided
    if (collectionName is not null) {
      var app = ReadFromRegistryPath(collectionApplicationsRegistryPath);
      if (app != null) {
        return app;
      }
    }
    return ReadFromRegistryPath(applicationsRegistryPath);
  }

  /// <summary>
  /// Gets all RemoteApp programs from the system registry.
  /// </summary>
  /// <returns></returns>
  public SystemRemoteAppCollection GetAllRegisteredApps(bool? restorePackagedAppIconPaths = false) {
    EnsureRegistryPathExists();

    var apps = new SystemRemoteAppCollection();
    using (var appsKey = Registry.LocalMachine.OpenSubKey(applicationsRegistryPath)) {
      if (appsKey is null) {
        return apps;
      }

      foreach (var appName in appsKey.GetSubKeyNames()) {
        var app = GetRegistedApp(appName);

        if (app != null) {
          if (restorePackagedAppIconPaths == true) {
            app.RestorePackagedAppIconPath();
          }
          apps.Add(app);
        }
      }
    }
    return [.. apps.OrderBy(app => app.Name)];
  }
}

internal class SetsRequiredMembersAttribute : Attribute {
}
