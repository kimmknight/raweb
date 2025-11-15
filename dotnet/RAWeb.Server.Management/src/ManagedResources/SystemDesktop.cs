using System;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Text;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RAWeb.Server.Management;

[DataContract]
public sealed class SystemDesktop : ManagedResource {
  [DataMember] string? CollectionName { get; init; }
  public string collectionDesktopsRegistryPath {
    get {
      return $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\CentralPublishedResources\PublishedFarms\{CollectionName}\RemoteDesktops";
    }
  }

  public SystemDesktop(
    string identifier,
    string collectionName,
    string? desktopName = null,
    bool? includeInWorkspace = false,
    RawSecurityDescriptor? securityDescriptor = null,
    string? rdpFileString = null
  ) : base(ManagedResourceSource.CentralPublishedResourcesDesktop, collectionName, desktopName ?? Environment.MachineName, null) {
    CollectionName = collectionName;
    IncludeInWorkspace = includeInWorkspace ?? false;
    SecurityDescriptor = securityDescriptor;
    RdpFileString = rdpFileString;

    EnsureRegistryPathExists();

    // if desktop name was not provided, attempt to read it from the registry
    if (desktopName is null) {
      // read the name from the registry if it exists
      using (var desktopKey = Registry.LocalMachine.OpenSubKey($@"{collectionDesktopsRegistryPath}\{Identifier}")) {
        if (desktopKey is not null) {
          var name = (string?)desktopKey.GetValue("Name", null);
          if (name is not null && !string.IsNullOrWhiteSpace(name)) {
            Name = name;
          }
        }
      }
    }

    IconPath = FindSystemWallpaper();
  }

  public static SystemDesktop? FromJSON(JObject jsonObject, JsonSerializer serializer) {
    // extract the registry key
    var key = jsonObject["identifier"]?.Value<string>();
    if (key is null) return null;

    // extract the collection name
    var collectionName = jsonObject["collectionName"]?.Value<string>();

    // attempt to extract the name, falling back to the identifier if not present
    var name = jsonObject["name"]?.Value<string>() ?? key;

    // extract includeInWorkspace flag
    var includeInWorkspace = jsonObject["includeInWorkspace"]?.Value<bool>() ?? false;

    // extract security descriptor
    var securityDescription = jsonObject["securityDescription"] is JObject securityDescriptionJson
      ? securityDescriptionJson.ToObject<SecurityDescriptionDTO>(serializer)
      : null;
    var securityDescriptor = securityDescription?.ToRawSecurityDescriptor();

    // extract the RDP file string if it was provided
    var rdpFileString = jsonObject["rdpFileString"]?.Value<string>();

    return new SystemDesktop(key, collectionName ?? "", name, includeInWorkspace, securityDescriptor, rdpFileString);
  }

  public static SystemDesktop? FromRegistry(string collectionName, string identifier) {
    var collectionDesktopsRegistryPath = $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\CentralPublishedResources\PublishedFarms\{collectionName}\RemoteDesktops";

    // open the registry key if it exists
    using var regKey = Registry.LocalMachine.OpenSubKey($@"{collectionDesktopsRegistryPath}\{identifier}");
    if (regKey is null) {
      return null;
    }

    // read the properties from the registry
    var name = (string?)regKey.GetValue("Name", identifier) ?? identifier;
    var includeInWorkspace = ((int?)regKey.GetValue("ShowInPortal", 0) ?? 0) != 0;
    var rdpFileContents = (string?)regKey.GetValue("RDPFileContents", null);

    // read the security descriptor if it exists
    RawSecurityDescriptor? securityDescriptor = null;
    var sddl = (string?)regKey.GetValue("SecurityDescriptor", null);
    if (!string.IsNullOrWhiteSpace(sddl)) {
      securityDescriptor = new RawSecurityDescriptor(sddl);
    }

    return new SystemDesktop(identifier, collectionName, name, includeInWorkspace, securityDescriptor, rdpFileContents);
  }

  /// <summary>
  /// Ensures that the registry path for desktops exists.
  /// </summary>
  public void EnsureRegistryPathExists() {
    var collectionApplicationsPathExists = string.IsNullOrEmpty(CollectionName);
    var applicationsPathExists = false;

    // first, check if the paths exists
    using (var key = Registry.LocalMachine.CreateSubKey(collectionDesktopsRegistryPath, writable: false)) {
      if (key != null) {
        applicationsPathExists = true;
      }
    }
    if (!collectionApplicationsPathExists) {
      using (var key = Registry.LocalMachine.CreateSubKey(collectionDesktopsRegistryPath, writable: false)) {
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
    using (Registry.LocalMachine.CreateSubKey(collectionDesktopsRegistryPath, writable: true)) {
    }
    using (Registry.LocalMachine.CreateSubKey(collectionDesktopsRegistryPath, writable: true)) {
    }
  }

  /// <summary>
  /// Writes the desktop information to the registry.
  /// <br /><br />
  /// Use this method to create or update the registry entry for this desktop.
  /// </summary>
  /// <exception cref="InvalidOperationException"></exception>
  public void WriteToRegistry() {
    ElevatedPrivileges.Require();
    EnsureRegistryPathExists();

    using (var appsKey = Registry.LocalMachine.OpenSubKey(collectionDesktopsRegistryPath, true)) {
      if (appsKey is null) {
        throw new InvalidOperationException("Failed to open collection desktop registry key for writing.");
      }

      using (var appKey = appsKey.CreateSubKey(Identifier)) {
        appKey.SetValue("Name", Name);
        appKey.SetValue("ShowInPortal", IncludeInWorkspace ? 1 : 0);
        appKey.SetValue("RDPFileContents", RdpFileString ?? ToRdpFileStringBuilder(null).ToString());

        if (SecurityDescriptor != null) {
          appKey.SetValue("SecurityDescriptor", SecurityDescriptor.GetSddlForm(AccessControlSections.All));
        }
        else {
          appKey.DeleteValue("SecurityDescriptor", false);
        }
      }
    }
  }

  /// <summary>
  /// Deletes the desktop from the registry.
  /// </summary>
  public void DeleteFromRegistry() {
    ElevatedPrivileges.Require();
    EnsureRegistryPathExists();

    using (var appsKey = Registry.LocalMachine.OpenSubKey(collectionDesktopsRegistryPath, true)) {
      if (appsKey is null) {
        throw new InvalidOperationException("Failed to open collection desktop registry key for writing.");
      }

      appsKey.DeleteSubKeyTree(Identifier, false);
    }
  }

  /// <summary>
  /// Gets the timestamp for when the resource was last modified in the registry (in UTC).
  /// </summary>
  /// <returns></returns>
  /// <exception cref="Exception"></exception>
  public override DateTime GetLastWriteTimeUtc() {
    // ensure the registry path exists
    EnsureRegistryPathExists();

    // open the registry key if it exists
    var keyName = collectionDesktopsRegistryPath + "\\" + Identifier;
    using var regKey = Registry.LocalMachine.OpenSubKey(keyName);
    if (regKey is null) {
      throw new Exception("The specified registry key does not exist: " + keyName);
    }

    // get the last write time for the registry key
    var result = SystemRemoteApps.SystemRemoteApp.RegQueryInfoKey(
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

    return DateTime.FromFileTime(fileTime);
  }

  public override StringBuilder ToRdpFileStringBuilder(string? fullAddress = null) {
    // if full address is missing, attempt to build it from the local computer name and domain
    if (string.IsNullOrWhiteSpace(fullAddress)) {
      string domain;
      try {
        domain = Domain.GetComputerDomain().Name;
      }
      catch {
        domain = IPGlobalProperties.GetIPGlobalProperties().DomainName ?? "local";
        if (string.IsNullOrEmpty(domain)) {
          domain = "local";
        }
      }

      fullAddress = $"{Environment.MachineName}.{domain}";
    }

    // search the registry for RDPFileContents - use it as a base if found
    // (only supported in centralized publishing collections)
    var builder = new StringBuilder();
    if (CollectionName is not null && !string.IsNullOrEmpty(CollectionName)) {
      using (var desktopKey = Registry.LocalMachine.OpenSubKey($@"{collectionDesktopsRegistryPath}\{Identifier}")) {
        if (desktopKey is not null) {
          var rdpFileContents = (string?)desktopKey.GetValue("RDPFileContents", null);
          if (!string.IsNullOrWhiteSpace(rdpFileContents)) {
            var text = rdpFileContents?
              .Replace("\\r\\n", "\r\n")
              .Replace("\\n", "\r\n") // normalize to Windows newlines
              .TrimEnd() ?? "";
            builder.AppendLine(text);
          }
        }
      }
    }

    // build the RDP file contents
    builder.AppendLine("full address:s:" + fullAddress);
    builder.AppendLine("remoteapplicationmode:i:0");

    // if there are duplicate lines, keep only the last occurrence of each setting
    var rdpLines = builder.ToString()
       .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries) // split into lines
       .GroupBy(line => line.Split([':'], 2)[0]) // group by property name
       .Select(group => group.Last()) // take the last occurrence of each property
       .OrderBy(line => line); // sort remaining lines alphabetically

    return rdpLines.Aggregate(new StringBuilder(), (sb, line) => sb.AppendLine(line));
  }

  /// <summary>
  /// Finds the system wallpaper path based on the specified theme.
  /// </summary>
  /// <param name="theme"></param>
  /// <returns></returns>
  public string FindSystemWallpaper(ManagedFileResource.ImageTheme theme = ManagedFileResource.ImageTheme.Light) {
    // if the theme is dark, check if there is a dark mode default wallpaper available
    if (theme == ManagedFileResource.ImageTheme.Dark) {
      var path = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\Web\Wallpaper\Windows\img19.jpg");
      if (File.Exists(path)) {
        return path;
      }
    }

    // otherwise, use the light mode default wallpaper
    return Environment.ExpandEnvironmentVariables(@"%SystemRoot%\Web\Wallpaper\Windows\img0.jpg");
  }
}
