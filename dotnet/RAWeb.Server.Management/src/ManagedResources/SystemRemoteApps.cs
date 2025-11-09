using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.ServiceModel;
using System.Text;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static RAWeb.Server.Management.RemoteAppProperties;

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
  /// Service implementation of <c>SystemRemoteApps.EnsureRegistryPathExists</c>.
  /// </summary>
  [OperationContract]
  void InitializeRegistryPaths(string? collectionName = null);

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

public class SystemRemoteApps(string? collectionName = null) {
  string applicationsRegistryPath => @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\TSAppAllowList\Applications";
  string collectionApplicationsRegistryPath => !string.IsNullOrEmpty(collectionName)
      ? $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\CentralPublishedResources\PublishedFarms\{collectionName}\Applications"
      : applicationsRegistryPath;
  string? collectionName { get; set; } = collectionName;

  /// <summary>
  /// Represents a RemoteApp program as stored in the system registry.
  /// </summary>
  [DataContract]
  public class SystemRemoteApp : ManagedResource {
    /// <summary>
    /// The collection name for this RemoteApp. If null, the RemoteApp is stored
    /// in the standard TSAppAllowList registry path. If non-null, the RemoteApp is
    /// stored in the collection-specific CentralPublishedResources registry path.
    /// </summary>
    [DataMember] public string? CollectionName { get; private set; }
    [IgnoreDataMember][JsonIgnore] public SystemRemoteApps sra { get; set; }

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
        FileTypeAssociationCollection? fileTypeAssociations,
        RawSecurityDescriptor? securityDescriptor = null,
        SecurityDescriptionDTO? securityDescription = null
    ) : base(collectionName is null ? ManagedResourceSource.TSAppAllowList : ManagedResourceSource.CentralPublishedResourcesApp, key, name, iconPath) {
      CollectionName = collectionName;

      // use the application path for the icon if not explorer.exe (every packaged app uses explorer.exe)
      if (string.IsNullOrEmpty(iconPath) && !System.IO.Path.GetFileName(iconPath).Equals("explorer.exe")) {
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
    }

    /// <summary>
    /// Called after deserialization to re-initialize non-serialized properties.
    /// This is necessary because the serialization-deserialization process
    /// bypasses the constructor, so properties scuh as <c>sra</c> need to be
    /// manually re-initialized here.
    /// </summary>
    /// <param name="ctx"></param>
    [OnDeserialized]
    private void OnDeserialized(StreamingContext ctx) {
      sra ??= new SystemRemoteApps(CollectionName);
    }

    public static SystemRemoteApp? FromJSON(JObject jsonObject, JsonSerializer serializer) {
      // extract the registry key
      var key = jsonObject["identifier"]?.Value<string>();
      if (key is null) return null;

      // extract the collection name
      var collectionName = jsonObject["collectionName"]?.Value<string>();

      // attempt to extract the name, falling back to the identifier if not present
      var name = jsonObject["name"]?.Value<string>() ?? key;

      // extract icon information
      var iconPath = jsonObject["iconPath"]?.Value<string>();
      var iconIndex = jsonObject["iconIndex"]?.Value<int>();

      // extract includeInWorkspace flag
      var includeInWorkspace = jsonObject["includeInWorkspace"]?.Value<bool>() ?? false;

      // extract security descriptor
      var securityDescription = jsonObject["securityDescription"] is JObject securityDescriptionJson
        ? securityDescriptionJson.ToObject<SecurityDescriptionDTO>(serializer)
        : null;
      var securityDescriptor = securityDescription?.ToRawSecurityDescriptor();

      // extract remoteapp properties
      var remoteAppProperties = jsonObject["remoteAppProperties"] is JObject remoteAppPropertiesJson
        ? remoteAppPropertiesJson.ToObject<RemoteAppProperties>(serializer)
        : null;
      if (remoteAppProperties is null) {
        return null;
      }

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
        securityDescriptor: securityDescriptor
      );

      // extract the RDP file string if it was provided
      var rdpFileString = jsonObject["rdpFileString"]?.Value<string>();
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
            appKey.SetValue("RequiredCommandLine", RemoteAppProperties.CommandLine);
            appKey.SetValue("CommandLineSetting", (int)RemoteAppProperties.CommandLineOption);
          }
          appKey.SetValue("IconPath", IconPath);
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
              appKey.SetValue("RequiredCommandLine", RemoteAppProperties.CommandLine);
              appKey.SetValue("CommandLineSetting", (int)RemoteAppProperties.CommandLineOption);
            }
            appKey.SetValue("IconPath", IconPath);
            appKey.SetValue("IconIndex", IconIndex);
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
        var computerName = Environment.MachineName;

        string domain;
        try {
          domain = Domain.GetComputerDomain().Name;
        }
        catch {
          domain = IPGlobalProperties.GetIPGlobalProperties().DomainName ?? "local";
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
      rdpBuilder.AppendLine("full address:s:" + fullAddress);
      rdpBuilder.AppendLine("remoteapplicationname:s:" + Name);
      if (RemoteAppProperties is not null) {
        rdpBuilder.AppendLine("remoteapplicationprogram:s:" + RemoteAppProperties.ApplicationPath);
      }
      else {
        rdpBuilder.AppendLine("remoteapplicationprogram:s:||" + Identifier);
      }
      rdpBuilder.AppendLine("remoteapplicationmode:i:1");
      if (RemoteAppProperties is not null && RemoteAppProperties.CommandLineOption != CommandLineMode.Disabled) {
        rdpBuilder.AppendLine("remoteapplicationcmdline:s:" + RemoteAppProperties.CommandLine);
      }
      rdpBuilder.AppendLine("remoteapplicationfileextensions:s:" + appFileExtCSV);
      rdpBuilder.AppendLine("disableremoteappcapscheck:i:1");

      // if there are duplicate lines, keep only the last occurrence of each setting
      var rdpLines = rdpBuilder.ToString()
         .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries) // split into lines
         .GroupBy(line => line.Split([':'], 2)[0]) // group by property name
         .Select(group => group.Last()) // take the last occurrence of each property
         .OrderBy(line => line); // sort remaining lines alphabetically

      return rdpLines.Aggregate(new StringBuilder(), (sb, line) => sb.AppendLine(line));
    }
  }

  /// <summary>
  /// A collection of RemoteApp programs from the registry.
  /// </summary>
  [CollectionDataContract]
  public class SystemRemoteAppCollection : Collection<SystemRemoteApp> {
    public SystemRemoteAppCollection() {
    }

    public SystemRemoteAppCollection(IList<SystemRemoteApp> apps) : base(apps) {
    }
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
            securityDescriptor: securityDescriptor
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
  public SystemRemoteAppCollection GetAllRegisteredApps() {
    EnsureRegistryPathExists();

    var apps = new SystemRemoteAppCollection();
    using (var appsKey = Registry.LocalMachine.OpenSubKey(applicationsRegistryPath)) {
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
    return [.. apps.OrderBy(app => app.Name)];
  }
}

internal class SetsRequiredMembersAttribute : Attribute {
}
