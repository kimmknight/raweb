using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace RAWeb.Server.Utilities;

public enum ResourceType {
  Desktop,
  RemoteApp
}

public enum ResourceOrigin {
  Rdp,
  Registry
}

public class Resource {
  public ResourceType Type { get; private set; }
  public ResourceOrigin Origin { get; private set; }
  public string FullAddress { get; private set; }
  public string? AppProgram { get; private set; }
  public string Title { get; private set; }
  public string Alias { get; private set; }
  public string? AppFileExtCSV { get; private set; }
  public DateTime LastUpdated { get; private set; }
  public string VirtualFolder { get; private set; }
  public string Source { get; private set; } // path the RDP file or registry entry
  public Guid Guid { get; private set; }

  public bool IsApp {
    get {
      return Type == ResourceType.RemoteApp;
    }
  }
  public bool IsDesktop {
    get {
      return Type == ResourceType.Desktop;
    }
  }
  public string[]? FileExtensions {
    get {
      if (string.IsNullOrEmpty(AppFileExtCSV)) {
        return [];
      }
      return AppFileExtCSV?.Split(',');
    }
  }
  public string Id {
    get {
      return Guid.ToString();
    }
  }

  /// <summary>
  /// Creates a Resource object from an RDP file.
  /// <br /><br />
  /// The RDP file must contain a "full address" property; otherwise,
  /// a FullAddressMissingException is thrown.
  /// </summary>
  /// <param name="rdpFilePath"></param>
  /// <param name="virtualFolder"></param>
  /// <returns></returns>
  /// <exception cref="FullAddressMissingException"></exception>
  public static Resource FromRdpFile(string rdpFilePath, string virtualFolder = "") {
    var directoryPath = Path.GetDirectoryName(rdpFilePath);
    var relativeRdpFilePath = Path.GetFullPath(rdpFilePath).Replace(Constants.AppRoot, "").TrimStart('\\').TrimEnd('\\');

    // ensure that there is a full address in the RDP file
    var fullAddress = Utilities.GetRdpFileProperty(rdpFilePath, "full address:s:");
    if (string.IsNullOrEmpty(fullAddress)) {
      throw new FullAddressMissingException();
    }

    // get the rdp file name and remove the last 4 characters (.rdp)
    var baseRdpFileName = Path.GetFileNameWithoutExtension(relativeRdpFilePath);

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

    return new Resource(
      title: Utilities.GetRdpFileProperty(rdpFilePath, "remoteapplicationname:s:", baseRdpFileName), // set the app title to the base filename if the remote application name is empty
      fullAddress: Utilities.GetRdpFileProperty(rdpFilePath, "full address:s:"),
      appProgram: Utilities.GetRdpFileProperty(rdpFilePath, "remoteapplicationprogram:s:").Replace("|", ""),
      alias: relativeRdpFilePath,
      appFileExtCSV: Utilities.GetRdpFileProperty(rdpFilePath, "remoteapplicationfileextensions:s:"),
      lastUpdated: resourceDateTime,
      virtualFolder: virtualFolder,
      origin: ResourceOrigin.Rdp,
      source: rdpFilePath
    );
  }

  public sealed class FullAddressMissingException : Exception {
    public FullAddressMissingException() : base("The RDP file must contain a 'full address' property.") { }
  }

  /// <summary>
  /// The relative path to the RDP file in the App_Data folder or the registry key path.
  /// </summary>
  public string RelativePath {
    get {
      if (Origin == ResourceOrigin.Rdp) {
        return Source.Replace(Constants.AppDataFolderPath, "").TrimStart('\\').TrimEnd('\\').Replace("\\", "/");
      }
      return Source;
    }
  }

  /// <summary>
  /// Creates a new Resource object. If the origin is "rdp", the source must
  /// be a valid path to an RDP file. If the origin is "registry", the source
  /// must be a valid application name in the registry.
  /// </summary>
  /// <exception cref="ArgumentException"></exception>
  public Resource(string title, string fullAddress, string appProgram, string alias, string appFileExtCSV, DateTime lastUpdated, string virtualFolder, ResourceOrigin origin, string source) {
    VirtualFolder = virtualFolder;

    // full address is required because it is the connection address
    if (string.IsNullOrEmpty(fullAddress)) {
      throw new ArgumentException("Full address cannot be null or empty.");
    }
    FullAddress = fullAddress;

    // we need to know if this is from the registry or and rdp file because
    // the icon and rdp file path logic is different
    Origin = origin;

    // source must be a valid path to an RDP file or registry entry
    if (string.IsNullOrEmpty(source)) {
      throw new ArgumentException("Source cannot be null or empty. It should be the path to the RDP file or registry entry.");
    }
    if (origin == ResourceOrigin.Rdp && !File.Exists(source)) {
      throw new ArgumentException("Source must be a valid path to an RDP file. " +
          "Ensure the file exists at the specified path: " + source);
    }
    if (origin == ResourceOrigin.Registry) {
      using (var regKey = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\TSAppAllowList\Applications\{source}")) {
        if (regKey == null) {
          throw new ArgumentException(@"Source must be a valid application name in HKEY_LOCAL_MACHINE\SOFTWARE\Windows NT\CurrentVersion\Terminal Server\TSAppAllowList\Applications.");
        }
      }
    }
    Source = source;

    // title should not be null or empty
    if (string.IsNullOrEmpty(title)) {
      throw new ArgumentException("Title cannot be null or empty.");
    }
    Title = title;
    Alias = alias;

    // if lastUpdated is empty, set it to the current time
    if (lastUpdated == DateTime.MinValue) {
      lastUpdated = DateTime.UtcNow;
    }
    LastUpdated = lastUpdated;

    // is app program is not provided, we assume it is a desktop
    if (string.IsNullOrEmpty(appProgram)) {
      Type = ResourceType.Desktop;
      return;
    }

    // process the remaining remote application properties
    Type = ResourceType.RemoteApp;
    AppProgram = appProgram;
    AppFileExtCSV = appFileExtCSV;
  }

  /// <summary>
  /// Sets the GUID for the resource based on the contents of an RDP file.
  /// <br /><br />
  /// This overload uses the Source property as the RDP file path or contents.
  /// Therefore, the Origin property must be ResourceOrigin.Rdp.
  /// </summary>
  /// <param name="schemaVersion"></param>
  /// <param name="mergeTerminalServers"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public Resource CalculateGuid(double schemaVersion, bool mergeTerminalServers) {
    if (Origin != ResourceOrigin.Rdp) {
      throw new InvalidOperationException("Cannot calculate GUID because the resource origin is not 'ResourceOrigin.Rdp'.");
    }

    CalculateGuid(Source, schemaVersion, mergeTerminalServers);
    return this;
  }

  /// <summary>
  /// Sets the GUID for the resource based on the contents of an RDP file.
  /// <br /><br />
  /// The RDP file contents are hashed using the MD5 algorithm to produce a unique identifier,
  /// and then a Guid is created from the hash. See <see cref="GetResourceGUID"/> for more details.
  /// </summary>
  /// <param name="rdpFilePathOrContents"></param>
  /// <param name="schemaVersion"></param>
  /// <param name="mergeTerminalServers"></param>
  /// <returns></returns>
  public Resource CalculateGuid(string rdpFilePathOrContents, double schemaVersion, bool mergeTerminalServers) {
    string[]? linesToOmit = mergeTerminalServers && IsApp ? ["full address:s:", "raweb source type:i:", "signature:s:", "signscope:s:", "raweb external flag:i:"] : null;
    Guid = GetResourceGUID(rdpFilePathOrContents, schemaVersion >= 2.0 ? "" : VirtualFolder, linesToOmit);
    return this;
  }

  /// <summary>
  /// Generates a GUID for a resource based on the contents of an RDP file.
  /// <br /><br />
  /// The RDP file contents are hashed using the MD5 algorithm to produce a unique identifier,
  /// and then a Guid is created from the hash.
  /// </summary>
  /// <param name="rdpFilePathOrContents">The path to an RDP file or a string representation of an RDP file's contents.</param>
  /// <param name="suffix">Text to append to the the RDP file contents before the hash is created.</param>
  /// <param name="linesToOmit">An array of substrings, where any line that contains a substring in the array is omitted from the hashing process.</param>
  /// <returns></returns>
  public static Guid GetResourceGUID(string rdpFilePathOrContents, string suffix = "", string[]? linesToOmit = null) {
    var isFilePath = File.Exists(rdpFilePathOrContents);
    if (!isFilePath && string.IsNullOrEmpty(rdpFilePathOrContents)) {
      throw new ArgumentException("RDP file path or contents cannot be null or empty.");
    }

    // read the entire contents of the file into a string
    // or if the file does not exist, treat the path as the contents
    string fileContents;
    if (isFilePath) {
      fileContents = File.ReadAllText(rdpFilePathOrContents);
    }
    else {
      fileContents = rdpFilePathOrContents;
    }

    // alphabetically sort the lines in the file contents
    var lines = fileContents.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
    Array.Sort(lines);
    fileContents = string.Join("\r\n", lines);

    // omit the full address from the hash calculation
    if (linesToOmit != null) {
      foreach (var lineToOmit in linesToOmit) {
        fileContents = Regex.Replace(fileContents, @"(?m)^\s*" + Regex.Escape(lineToOmit) + @".*[\r\n]*", "", RegexOptions.Multiline);
      }
    }

    // if there is a suffix, append it to the file contents
    if (!string.IsNullOrEmpty(suffix)) {
      fileContents += suffix;
    }

    // generate a guid from the file contents
    var byt = Encoding.UTF8.GetBytes(fileContents);
    var md5 = System.Security.Cryptography.MD5.Create();
    var hash = md5.ComputeHash(byt);
    var guid = new Guid(hash);

    return guid;
  }

  public sealed class Utilities {
    /// <summary>
    /// Reads an RDP file and extracts the value of a specified property. If the property is not found,
    /// returns the provided fallback value (or an empty string if no fallback is provided).
    /// </summary>
    /// <param name="path"></param>
    /// <param name="propertyName"></param>
    /// <param name="fallbackValue"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException">When the path does not resolve to a file.</exception>
    /// <exception cref="InvalidDataException">When the property name is missing the type (e.g., :s:, :i:, etc.)</exception>
    public static string GetRdpFileProperty(string path, string propertyName, string fallbackValue = "") {
      // check if the path exists
      if (!File.Exists(path)) {
        throw new FileNotFoundException("The specified RDP file does not exist.", path);
      }

      return GetRdpStringProperty(File.ReadAllText(path), propertyName, fallbackValue);
    }

    /// <summary>
    /// Reads RDP file contents and extracts the value of a specified property. If the property is not found,
    /// returns the provided fallback value (or an empty string if no fallback is provided).
    /// </summary>
    /// <param name="rdpContents"></param>
    /// <param name="propertyName"></param>
    /// <param name="fallbackValue"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public static string GetRdpStringProperty(string rdpContents, string propertyName, string fallbackValue = "") {
      // ensure the property name includes the type (:s:, :i:, etc.)
      if (!propertyName.Contains(":")) {
        throw new InvalidDataException("The property name must include the type (e.g., :s:, :i:, etc.).");
      }
      if (!propertyName.EndsWith(":")) {
        propertyName += ":";
      }

      using (var stringReader = new StringReader(rdpContents)) {
        string line;
        var value = fallbackValue;
        while ((line = stringReader.ReadLine()) != null) {
          if (line.StartsWith(propertyName)) {
            // split into 3 parts only (property name, type, value)]
            var parts = line.Split(new char[] { ':' }, 3);
            if (parts.Length == 3) {
              // set the found value to the third part
              value = parts[2];
              // we do not break because we want the last instance of the property
            }
          }
        }

        value = value.Trim();

        if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(fallbackValue)) {
          return fallbackValue;
        }
        return value;
      }
    }
  }
}
