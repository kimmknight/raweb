using System;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Security.Principal;
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
    string identifier, // unused because the system desktop always uses the collection name as identifier
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

  /// <summary>
  /// Finds the user's desktop wallpaper based on the specified theme and user SID.
  /// <br /><br />
  /// If the user has a solid color background instead of a wallpaper image, a
  /// 128x128 JPEG image of the solid color will be returned in a memory stream.
  /// <br /><br />
  /// If the user SID is null or if any error occurs, the system wallpaper will be returned.
  /// <br /><br />
  /// The image stream can be up to 800x800 pixels in JPEG format. That could be a maximum
  /// of 1.83 MiB or 1,920,000 bytes in size.
  /// </summary>
  /// <param name="theme"></param>
  /// <param name="userSid"></param>
  /// <param name="rootedTempPath"></param>
  /// <returns></returns>
  public MemoryStream GetWallpaperStream(ManagedFileResource.ImageTheme theme, SecurityIdentifier? userSid) {
    ElevatedPrivileges.Require();

    MemoryStream ResizedDefaultIcon() {
      var systemWallpaperPath = FindSystemWallpaper(theme);
      var stream = new MemoryStream(File.ReadAllBytes(systemWallpaperPath));
      return Resize(stream);
    }

    /// <summary>
    /// Resizes the image in the provided stream to no larger than 800 pixels in width or height.
    /// </summary>
    MemoryStream Resize(MemoryStream stream) {
      using var image = System.Drawing.Image.FromStream(stream);
      var maxDimension = Math.Max(image.Width, image.Height);
      if (maxDimension > 800) {
        var scale = 800.0 / maxDimension;
        var newWidth = (int)(image.Width * scale);
        var newHeight = (int)(image.Height * scale);
        using var resizedImage = new System.Drawing.Bitmap(image, newWidth, newHeight);
        var resizedStream = new MemoryStream();
        resizedImage.Save(resizedStream, System.Drawing.Imaging.ImageFormat.Jpeg);
        resizedStream.Position = 0;
        return resizedStream;
      }
      stream.Position = 0;
      return stream;
    }

    if (userSid is null) {
      // no user SID provided: return the system wallpaper
      return ResizedDefaultIcon();
    }

    try {
      // open the user's hive
      using var hiveReader = new UserHiveReader(userSid);

      // read the desktop wallpaper path
      using var desktopKey = hiveReader.OpenSubKey(@"Control Panel\Desktop");
      var wallpaperPath = desktopKey?.GetValue("WallPaper") as string;
      if (wallpaperPath is not null && File.Exists(wallpaperPath)) {
        return Resize(new MemoryStream(File.ReadAllBytes(wallpaperPath)));
      }

      // if there is no valid wallpaper, that means the user is using a solid color background
      using var colorsKey = hiveReader.OpenSubKey(@"Control Panel\Colors");
      var backgroundColor = colorsKey?.GetValue("Background") as string;
      if (backgroundColor is not null) {
        // create a solid color bitmap
        var rgbParts = backgroundColor.Split(' ').Select(part => byte.Parse(part)).ToArray();
        if (rgbParts.Length == 3) {
          var size = 128;
          using var bitmap = new System.Drawing.Bitmap(size, size);
          using var graphics = System.Drawing.Graphics.FromImage(bitmap);
          var color = System.Drawing.Color.FromArgb(rgbParts[0], rgbParts[1], rgbParts[2]);
          graphics.Clear(color);

          var memoryStream = new MemoryStream();
          bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
          memoryStream.Position = 0;
          return memoryStream;
        }
      }

      // fall back to the default system wallpaper
      return ResizedDefaultIcon();
    }
    catch {
      // if any error occurs, fall back to the default system wallpaper
      return ResizedDefaultIcon();
    }
  }
}
