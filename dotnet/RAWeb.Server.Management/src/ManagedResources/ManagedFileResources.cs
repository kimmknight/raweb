using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RAWeb.Server.Management;

[DataContract]
public class ManagedFileResource : ManagedResource {
  public string RootedFilePath { get; init; }
  public string? PendingManagedIconLightBase64 { get; internal set; }
  public string? PendingManagedIconDarkBase64 { get; internal set; }

  public ManagedFileResource(
    string rootedFilePath,
    string? name,
    string rdpFileString,
    string? iconPath,
    int? iconIndex,
    bool? includeInWorkspace,
    RawSecurityDescriptor? securityDescriptor = null
  ) : base(
    source: ManagedResourceSource.File,
    identifier: ParseIdentifierFromFilePath(rootedFilePath),
    name: name ?? GetRdpFileStringProperty(rdpFileString, "remoteapplicationname:s:") ?? ParseIdentifierFromFilePath(rootedFilePath),
    iconPath: iconPath
  ) {
    RootedFilePath = TransformFilePath(rootedFilePath);
    IconIndex = iconIndex ?? 0;
    IncludeInWorkspace = includeInWorkspace ?? false;
    RdpFileString = rdpFileString;
    SecurityDescriptor = securityDescriptor;

    var maybeApplicationProgram = GetRdpFileStringProperty(rdpFileString, "remoteapplicationprogram:s:");
    var maybeCommandLine = GetRdpFileStringProperty(rdpFileString, "remoteapplicationcmdline:s:");
    var maybeFileExtensions = GetRdpFileStringProperty(rdpFileString, "remoteapplicationfileextensions:s:");
    if (maybeApplicationProgram is not null) {
      RemoteAppProperties = new RemoteAppProperties(
        applicationPath: maybeApplicationProgram,
        commandLineOption: RemoteAppProperties.CommandLineMode.Optional,
        commandLine: maybeCommandLine,
        fileTypeAssociations: maybeFileExtensions is not null
          ? new RemoteAppProperties.FileTypeAssociationCollection(
              maybeFileExtensions
                .Split([';'], StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => new RemoteAppProperties.FileTypeAssociation(ext, $"./icons/{ext}.png"))
            )
          : null
      );
    }
  }

  /// <summary>
  /// Transforms the file path to ensure it is rooted and has the correct extension.
  /// </summary>
  /// <param name="filePath"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentException"></exception>
  private static string TransformFilePath(string filePath) {
    // ensure the file path is rooted
    if (!Path.IsPathRooted(filePath)) {
      throw new ArgumentException("The file path must be rooted.", nameof(filePath));
    }

    // if the file name extension is not .resource, replace it with .resource
    if (!Path.GetExtension(filePath).Equals(".resource", StringComparison.InvariantCultureIgnoreCase)) {
      filePath = Path.ChangeExtension(filePath, ".resource");
    }

    return filePath;
  }

  /// <summary>
  /// Parses the identifier from the file path.
  /// </summary>
  /// <param name="filePath"></param>
  /// <returns></returns>
  private static string ParseIdentifierFromFilePath(string filePath) {
    return Path.GetFileName(TransformFilePath(filePath));
  }

  /// <summary>
  /// Creates a FileSystemResource from a JSON object representation.
  /// <br /><br />
  /// Use this method for easier deserialization from JSON.
  /// </summary>
  /// <param name="jsonObject"></param>
  /// <param name="rootedManagedResourcesPath"></param>
  /// <returns></returns>
  public static ManagedFileResource? FromJSON(JObject jsonObject, string rootedManagedResourcesPath, JsonSerializer serializer) {
    // extract the identifier
    var identifier = jsonObject["identifier"]?.Value<string>();
    if (identifier is null) return null;

    // build the rooted file path and the resolved identifier
    var rootedFilePath = TransformFilePath(Path.Combine(rootedManagedResourcesPath, identifier));
    identifier = ParseIdentifierFromFilePath(rootedFilePath);

    // attempt to extract the name, falling back to the identifier if not present
    var name = jsonObject["name"]?.Value<string>() ?? identifier;

    // extract the RDP file string
    var rdpFileString = jsonObject["rdpFileString"]?.Value<string>();
    if (rdpFileString is null) return null;

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

    // create the resource
    var resource = new ManagedFileResource(
      rootedFilePath: rootedFilePath,
      name: name,
      rdpFileString: rdpFileString,
      iconPath: iconPath,
      iconIndex: iconIndex,
      includeInWorkspace: includeInWorkspace,
      securityDescriptor: securityDescriptor
    );

    // extract additional remoteapp properties and add to FileSystemResource
    var remoteAppProperties = jsonObject["remoteAppProperties"] is JObject remoteAppPropertiesJson
      ? remoteAppPropertiesJson.ToObject<RemoteAppProperties>(serializer)
      : null;
    if (remoteAppProperties is not null) {
      resource.RemoteAppProperties = remoteAppProperties;
    }

    // refresh the rdpFileString to include remoteAppProperties
    resource.RdpFileString = resource.ToRdpFileStringBuilder().ToString();

    // if there are icons present, set them
    var managedIconLightBase64 = jsonObject["managedIconLightBase64"]?.Value<string>();
    if (managedIconLightBase64 is not null) {
      resource.PendingManagedIconLightBase64 = managedIconLightBase64;
    }
    var managedIconDarkBase64 = jsonObject["managedIconDarkBase64"]?.Value<string>();
    if (managedIconDarkBase64 is not null) {
      resource.PendingManagedIconDarkBase64 = managedIconDarkBase64;
    }

    return resource;
  }

  /// <summary>
  /// Properties for the metadata file (info.xml) within the resource file.
  /// </summary>
  [DataContract]
  internal class MetadataDTO {
    /// <summary>
    /// Schema version for the metadata file.
    /// <br />
    /// Metadata files without a version will be considered version 1.
    /// </summary>
    [DataMember] public int __Version { get; set; } = 1;
    [DataMember] public string? Name { get; set; }
    [DataMember] public bool? IncludeInWorkspace { get; set; }
    [DataMember] public string? IconPath { get; set; }
    [DataMember] public int IconIndex { get; set; } = 0;
    [DataMember] public string? SecurityDescriptorSddl { get; set; }
  }

  /// <summary>
  /// Reads a FileSystemResource from a resource file (.resource) at the specified path.
  /// </summary>
  /// <param name="resourceFilePath"></param>
  /// <returns></returns>
  /// <exception cref="InvalidDataException"></exception>
  public static ManagedFileResource FromResourceFile(string resourceFilePath) {
    if (!File.Exists(resourceFilePath)) {
      throw new FileNotFoundException("The specified resource file was not found.", resourceFilePath);
    }
    if (Path.GetExtension(resourceFilePath).ToLowerInvariant() != ".resource") {
      throw new InvalidDataException("The specified file is not a .resource file.");
    }

    using var archive = ZipFile.OpenRead(resourceFilePath);

    // check for the presence of the resource.rdp file
    var rdpFileEntry = archive.GetEntry("resource.rdp");
    if (rdpFileEntry is null) {
      throw new InvalidDataException("The resource file does not contain a resource.rdp entry.");
    }

    // read the contents of the resource.rdp file
    using var rdpStream = rdpFileEntry.Open();
    var rdpFileString = new StreamReader(rdpStream).ReadToEnd();
    if (string.IsNullOrEmpty(rdpFileString)) {
      throw new InvalidDataException("The resource.rdp entry is empty.");
    }

    // check for the presence of the info.json file
    var infoFileEntry = archive.GetEntry("info.json");
    if (infoFileEntry is null) {
      throw new InvalidDataException("The resource file does not contain an info.json entry.");
    }

    // read the contents of the info.json file
    using var infoStream = infoFileEntry.Open();
    using var reader = new StreamReader(infoStream);
    var json = reader.ReadToEnd();
    MetadataDTO metadata;
    try {
      var deserialized = JsonConvert.DeserializeObject<MetadataDTO>(json);
      if (deserialized is null || deserialized.__Version != 1) {
        throw new InvalidDataException("The info.json entry could not be deserialized.");
      }
      metadata = deserialized;
    }
    catch (JsonException) {
      throw new InvalidDataException("The info.json entry could not be deserialized.");
    }

    var app = new ManagedFileResource(
      rootedFilePath: Path.IsPathRooted(resourceFilePath) ? resourceFilePath : Path.GetFullPath(resourceFilePath),
      name: metadata.Name ?? ParseIdentifierFromFilePath(resourceFilePath),
      rdpFileString: rdpFileString,
      iconPath: metadata.IconPath,
      iconIndex: metadata.IconIndex,
      includeInWorkspace: metadata.IncludeInWorkspace,
      securityDescriptor: !string.IsNullOrEmpty(metadata.SecurityDescriptorSddl)
        ? new RawSecurityDescriptor(metadata.SecurityDescriptorSddl!)
        : null
    );
    return app;
  }

  /// <summary>
  /// Writes the resource file to the filesystem at the specified RootedFilePath.
  /// </summary>
  public void WriteToFile() {
    try {
      // create the directory if it does not exist
      var directory = Path.GetDirectoryName(RootedFilePath);
      if (directory is not null && !Directory.Exists(directory)) {
        Directory.CreateDirectory(directory);
      }

      using var archive = ZipFile.Open(RootedFilePath, ZipArchiveMode.Update);
      var existingEntries = archive.Entries.ToList();
      var existingRdpEntry = existingEntries.FirstOrDefault(e => e.Name == "resource.rdp");
      var existingInfoEntry = existingEntries.FirstOrDefault(e => e.Name == "info.json");

      // create the resource.rdp entry
      var rdpFileEntry = existingRdpEntry ?? archive.CreateEntry("resource.rdp");
      using (var rdpStream = rdpFileEntry.Open())
      using (var rdpWriter = new StreamWriter(rdpStream)) {
        rdpStream.SetLength(0); // clear existing content
        rdpWriter.Write(RdpFileString);
      }

      // create the info.json entry
      var infoFileEntry = existingInfoEntry ?? archive.CreateEntry("info.json");
      using var infoStream = infoFileEntry.Open();
      using var infoWriter = new StreamWriter(infoStream);
      var metadata = new MetadataDTO {
        __Version = 1,
        Name = Name,
        IncludeInWorkspace = IncludeInWorkspace,
        IconPath = string.IsNullOrWhiteSpace(IconPath) ? null : IconPath,
        IconIndex = IconIndex,
        SecurityDescriptorSddl = SecurityDescriptor?.GetSddlForm(AccessControlSections.All)
      };
      var settings = new JsonSerializerSettings {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented
      };
      var infoJson = JsonConvert.SerializeObject(metadata, settings);
      infoStream.SetLength(0); // clear existing content
      infoWriter.Write(infoJson);
    }
    catch (IOException ex) {
      if (ex.Message.Contains("being used by another process")) {
        throw new IOException("The resource file is currently in use by another process.", ex);
      }
    }

    // also update the alias via the desktop.ini file to match the resource display name
    // (note that there is sometimes a delay before Explorer picks up changes to desktop.ini files)
    try {
      var directory = Path.GetDirectoryName(RootedFilePath);
      if (directory is null || !Directory.Exists(directory)) return;

      // create the starter ini file if it does not exist
      var iniPath = Path.Combine(directory, "desktop.ini");
      if (!File.Exists(iniPath)) {
        var iniContent = "[.ShellClassInfo]\r\n\r\n[LocalizedFileNames]\r\n";
        File.WriteAllText(iniPath, iniContent);
      }

      // clear system attributes to allow editing
      File.SetAttributes(iniPath, File.GetAttributes(iniPath) & ~FileAttributes.Hidden & ~FileAttributes.System);

      // mark the folder as a system folder to enable desktop.ini processing
      var folderAttributes = File.GetAttributes(directory);
      folderAttributes |= FileAttributes.System;
      File.SetAttributes(directory, folderAttributes);

      // update the desktop.ini file with the new display name alias
      var iniLines = File.ReadAllLines(iniPath).ToList().FindAll(line => !string.IsNullOrWhiteSpace(line));
      var aliasLine = $"{Path.GetFileName(RootedFilePath)}={Name}";
      if (iniLines.Contains(aliasLine)) {
        // alias already exists; no need to update
      }
      else {
        // remove any existing alias for this resource
        iniLines.RemoveAll(line => line.StartsWith($"{Path.GetFileName(RootedFilePath)}="));
        // add the new alias line
        iniLines.Add(aliasLine);
        // write the updated ini content
        File.WriteAllText(iniPath, string.Join("\r\n", iniLines));
        Console.WriteLine($"Updated desktop.ini with alias for resource: {Name}");
      }

      // reapply system attributes
      File.SetAttributes(iniPath, File.GetAttributes(iniPath) | FileAttributes.Hidden | FileAttributes.System);
    }
    catch {
      throw;
    }

    // write any pending icons
    var isLightPending = PendingManagedIconLightBase64 is not null;
    var isDarkPending = PendingManagedIconDarkBase64 is not null;
    if (isLightPending) {
      Stream? lightIconStream = PendingManagedIconLightBase64 == "" ? null : new MemoryStream(Convert.FromBase64String(PendingManagedIconLightBase64!));
      WriteImage(lightIconStream, "resource.png", ImageTheme.Light, skipInfoUpdate: true);
      PendingManagedIconLightBase64 = null;
    }
    if (isDarkPending) {
      Stream? darkIconStream = PendingManagedIconDarkBase64 == "" ? null : new MemoryStream(Convert.FromBase64String(PendingManagedIconDarkBase64!));
      WriteImage(darkIconStream, "resource.png", ImageTheme.Dark, skipInfoUpdate: true);
      PendingManagedIconDarkBase64 = null;
    }

    // finally, rewrite the resource file to update the info.json entry if icons were updated
    if (isLightPending || isDarkPending) {
      WriteToFile();
    }
  }

  /// <summary>
  /// Writes an image stream to the specified icon path within the resource file.
  /// <br /><br />
  /// To remove an existing icon, provide a null ImageStream.
  /// </summary>
  /// <param name="imageStream"></param>
  /// <param name="iconPathInResource"></param>
  /// <exception cref="FileNotFoundException"></exception>
  public void WriteImage(Stream? imageStream, string iconPathInResource, ImageTheme theme = ImageTheme.Light, bool skipInfoUpdate = false) {
    if (!File.Exists(RootedFilePath)) {
      throw new FileNotFoundException("The specified resource file was not found.", RootedFilePath);
    }

    // strip out -dark suffix; consumers should specify theme via the 'theme' parameter instead of path
    var fileNameNoExt = Path.GetFileNameWithoutExtension(iconPathInResource);
    if (fileNameNoExt.EndsWith("-dark", StringComparison.InvariantCultureIgnoreCase)) {
      var extension = Path.GetExtension(iconPathInResource);
      fileNameNoExt = fileNameNoExt.Substring(0, fileNameNoExt.Length - 5);
      iconPathInResource = $"{fileNameNoExt}{extension}";
    }

    var finalIconPathInResource = iconPathInResource;
    if (theme == ImageTheme.Dark) {

      // for dark theme, append -dark before the file extension
      var extension = Path.GetExtension(iconPathInResource);
      var filenameWithoutExtension = iconPathInResource.Substring(0, iconPathInResource.Length - extension.Length);
      finalIconPathInResource = $"{filenameWithoutExtension}-dark{extension}";

    }

    using (var archive = ZipFile.Open(RootedFilePath, ZipArchiveMode.Update)) {
      // remove any existing entry for the icon path
      var existingIconEntry = archive.GetEntry(finalIconPathInResource);
      existingIconEntry?.Delete();

      // create a new entry for the icon
      if (imageStream is not null) {
        var iconEntry = archive.CreateEntry(finalIconPathInResource);
        using var iconStream = iconEntry.Open();
        imageStream.CopyTo(iconStream);
      }
    }

    // update the IconPath property
    IconPath = iconPathInResource;
    IconIndex = 0;

    // rewrite the resource file to update the info.json entry
    if (!skipInfoUpdate) {
      WriteToFile();
    }
  }

  /// <summary>
  /// Deletes the resource file from the filesystem.
  /// </summary>
  public void Delete() {
    if (File.Exists(RootedFilePath)) {
      File.Delete(RootedFilePath);

      // also remove the alias from the desktop.ini file
      try {
        var directory = Path.GetDirectoryName(RootedFilePath);
        if (directory is null || !Directory.Exists(directory)) return;
        var iniPath = Path.Combine(directory, "desktop.ini");
        if (File.Exists(iniPath)) {
          // clear system attributes to allow editing
          File.SetAttributes(iniPath, File.GetAttributes(iniPath) & ~FileAttributes.Hidden & ~FileAttributes.System);

          var iniText = File.ReadAllText(iniPath);
          var lines = iniText.Split(["\r\n", "\n"], StringSplitOptions.None).ToList().FindAll(line => !string.IsNullOrWhiteSpace(line));
          var aliasLine = $"{Path.GetFileName(RootedFilePath)}=";
          var initialLineCount = lines.Count;
          lines.RemoveAll(line => line.StartsWith(aliasLine));
          if (lines.Count < initialLineCount) {
            // write the updated ini content
            File.WriteAllText(iniPath, string.Join("\r\n", lines));
          }

          // reapply system attributes
          File.SetAttributes(iniPath, File.GetAttributes(iniPath) | FileAttributes.Hidden | FileAttributes.System);
        }
      }
      catch { }
    }
  }

  /// <summary>
  /// Reads the default icon or wallpaper image for this resource.
  /// </summary>
  /// <param name="theme"></param>
  /// <param name="fileTypeAssociation">If provided, an icon for this extension will be used instead.</param>
  /// <exception cref="FileNotFoundException"></exception>
  /// <returns></returns>
  public MemoryStream ReadImageStream(out string iconPath, ImageTheme theme = ImageTheme.Light, string? fileTypeAssociation = null) {
    if (!File.Exists(RootedFilePath)) {
      throw new FileNotFoundException("The specified resource file was not found.", RootedFilePath);
    }

    // remove the preceding dot from the file type association, if present
    var parsedFileTypeAssociation = fileTypeAssociation is not null && fileTypeAssociation.StartsWith(".")
      ? fileTypeAssociation.Substring(1)
      : null;

    using var archive = ZipFile.Open(RootedFilePath, ZipArchiveMode.Read);

    // determine the icon path within the archive
    iconPath = parsedFileTypeAssociation is not null ? parsedFileTypeAssociation : IconPath ?? "./resource.png";

    // only .png images are supported for .resource icons
    if (!iconPath.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase)) {
      throw new FileNotFoundException("The specified icon path does not point to a supported image format.");
    }

    // if the path is rooted, throw an error since paths
    // are expected to be relative within the archive
    if (Path.IsPathRooted(iconPath)) {
      throw new FileNotFoundException($"The specified icon path ({iconPath}) must be relative within the resource file.");
    }

    // remove preceeding ./ from the icon path, if present
    if (iconPath.StartsWith("./")) {
      iconPath = iconPath.Substring(2);
    }

    if (theme == ImageTheme.Dark) {
      // if the theme is dark, build the dark icon path
      // replace .png with -dark.png
      var darkIconPath = iconPath.Substring(0, iconPath.Length - 4) + "-dark.png";

      // if the dark icon exists, use it instead of the light mode icon
      var darkIconEntry = archive.GetEntry(darkIconPath);
      if (darkIconEntry is not null) {
        iconPath = darkIconPath;
      }
    }

    // attempt to find the icon entry
    var imageEntry = archive.GetEntry(iconPath);
    if (imageEntry is null) {
      throw new FileNotFoundException($"The specified icon path ({iconPath}) was not found within the resource file.");
    }

    // read the image data into a MemoryStream and return it
    using var imageStream = imageEntry.Open();
    var memoryStream = new MemoryStream();
    imageStream.CopyTo(memoryStream);
    memoryStream.Position = 0;
    return memoryStream;
  }

  public enum ImageTheme {
    Light,
    Dark
  }

  public override StringBuilder ToRdpFileStringBuilder(string? fullAddressOverride = null) {
    var builder = new StringBuilder();
    builder.Append(RdpFileString);

    if (fullAddressOverride is not null) {
      // override the full address property
      builder.AppendLine($"full address:s:{fullAddressOverride}");
    }

    // ensure that the properties from the RemoteAppProperties object take precedence
    if (RemoteAppProperties is not null) {
      builder.AppendLine("remoteapplicationname:s:" + Name);
      builder.AppendLine("remoteapplicationprogram:s:" + RemoteAppProperties.ApplicationPath);
      builder.AppendLine("remoteapplicationmode:i:1");
      if (RemoteAppProperties.CommandLineOption != RemoteAppProperties.CommandLineMode.Disabled) {
        builder.AppendLine("remoteapplicationcmdline:s:" + RemoteAppProperties.CommandLine);
      }
      builder.AppendLine("disableremoteappcapscheck:i:1");

      // calculate the file extensions supported by the application
      var appFileExtCSV = RemoteAppProperties?.FileTypeAssociations
          .Select(fta => fta.Extension.ToLowerInvariant())
          .Aggregate("", (current, ext) => current + (current.Length == 0 ? ext : $",{ext}"));

      builder.AppendLine("remoteapplicationfileextensions:s:" + appFileExtCSV);
    }

    // if there are duplicate lines, keep only the last occurrence of each setting
    var rdpLines = builder.ToString()
        .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries) // split into lines
        .GroupBy(line => line.Split([':'], 2)[0]) // group by property name
        .Select(group => group.Last()) // take the last occurrence of each property
        .OrderBy(line => line); // sort remaining lines alphabetically

    return rdpLines.Aggregate(new StringBuilder(), (sb, line) => sb.AppendLine(line));
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
  private static string? GetRdpFileStringProperty(string rdpContents, string propertyName, string? fallbackValue = null) {
    // ensure the property name includes the type (:s:, :i:, etc.)
    if (!propertyName.Contains(":")) {
      throw new InvalidDataException("The property name must include the type (e.g., :s:, :i:, etc.).");
    }
    if (!propertyName.EndsWith(":")) {
      propertyName += ":";
    }

    using (var stringReader = new StringReader(rdpContents)) {
      string? line;
      var value = fallbackValue;
      while ((line = stringReader.ReadLine()) is not null) {
        if (line.StartsWith(propertyName)) {
          // split into 3 parts only (property name, type, value)]
          var parts = line.Split([':'], 3);
          if (parts.Length == 3) {
            // set the found value to the third part
            value = parts[2];
            // we do not break because we want the last instance of the property
          }
        }
      }

      value = value?.Trim();

      if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(fallbackValue)) {
        return fallbackValue;
      }
      return value;
    }
  }

  public static explicit operator ManagedFileResource(int v) => throw new NotImplementedException();
}

public sealed class ManagedFileResources : Collection<ManagedFileResource> {
  public ManagedFileResources() {
  }
  public ManagedFileResources(IList<ManagedFileResource> apps) : base(apps) {
  }
  public ManagedFileResources(IEnumerable<ManagedFileResource> apps) : base([.. apps]) {
  }

  /// <summary>
  /// Creates a FileSystemRemoteApps collection by scanning the specified directory
  /// for .resource files and loading each valid resource file as a FileSystemResource.
  /// </summary>
  /// <param name="directoryPath"></param>
  /// <returns></returns>
  public static ManagedFileResources FromDirectory(string directoryPath) {
    if (!Directory.Exists(directoryPath)) {
      return [];
    }

    // find all .resource files in the directory
    var resourceFiles = Directory.GetFiles(directoryPath, "*.resource", SearchOption.TopDirectoryOnly);
    var apps = new List<ManagedFileResource>();
    foreach (var resourceFile in resourceFiles) {
      try {
        var app = ManagedFileResource.FromResourceFile(resourceFile);
        apps.Add(app);
      }
      catch {
        // ignore invalid resource files
      }
    }

    return new ManagedFileResources(apps);
  }
}
