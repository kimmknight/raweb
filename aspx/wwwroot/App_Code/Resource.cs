using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;

namespace RAWebServer {
  public enum ResourceType {
    Desktop,
    RemoteApp
  }

  public class Resource {
    public ResourceType Type { get; set; } // Desktop or RemoteApp
    public string Origin { get; set; } // rdp or registry
    public string FullAddress { get; set; }
    public string AppProgram { get; set; }
    public string Title { get; set; }
    public string Alias { get; set; }
    public string AppFileExtCSV { get; set; }
    public DateTime LastUpdated { get; set; }
    public string VirtualFolder { get; set; }
    public string Source { get; set; } // path the RDP file or registry entry
    public Guid Guid { get; set; }

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
    public string[] FileExtensions {
      get {
        if (string.IsNullOrEmpty(AppFileExtCSV)) {
          return new string[] { };
        }
        return AppFileExtCSV.Split(',');
      }
    }
    public string Id {
      get {
        return Guid.ToString();
      }
    }

    private readonly string _applicationDataPath = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
    /// <summary>
    /// The relative path to the RDP file in the App_Data folder or the registry key path.
    /// </summary>
    public string RelativePath {
      get {
        if (Origin == "rdp") {
          return Source.Replace(_applicationDataPath, "").TrimStart('\\').TrimEnd('\\').Replace("\\", "/");
        }
        return Source;
      }
    }

    public Resource(string title, string fullAddress, string appProgram, string alias, string appFileExtCSV, DateTime lastUpdated, string virtualFolder, string origin, string source) {
      VirtualFolder = virtualFolder;

      // full address is required because it is the connection address
      if (string.IsNullOrEmpty(fullAddress)) {
        throw new ArgumentException("Full address cannot be null or empty.");
      }
      FullAddress = fullAddress;

      // we need to know if this is from the registry or and rdp file because
      // the icon and rdp file path logic is different
      if (string.IsNullOrEmpty(origin)) {
        throw new ArgumentException("Origin cannot be null or empty. Use 'rdp' for RDP files or 'registry' for registry entries.");
      }
      if (origin != "rdp" && origin != "registry") {
        throw new ArgumentException("Origin must be either 'rdp' or 'registry'.");
      }
      Origin = origin;

      // source must be a valid path to an RDP file or registry entry
      if (string.IsNullOrEmpty(source)) {
        throw new ArgumentException("Source cannot be null or empty. It should be the path to the RDP file or registry entry.");
      }
      if (origin == "rdp" && !System.IO.File.Exists(source)) {
        throw new ArgumentException("Source must be a valid path to an RDP file. " +
            "Ensure the file exists at the specified path: " + source);
      }
      if (origin == "registry") {
        using (var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications\\" + source)) {
          if (regKey == null) {
            throw new ArgumentException("Source must be a valid application name in HKEY_LOCAL_MACHINE\\SOFTWARE\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications.");
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

    public Resource SetGuid(Guid guid) {
      Guid = guid;
      return this;
    }

    public Resource SetGuid(string guidString) {
      if (string.IsNullOrEmpty(guidString)) {
        throw new ArgumentException("GUID cannot be null or empty.");
      }
      try {
        Guid = new Guid(guidString);
        return this;
      }
      catch (FormatException) {
        throw new ArgumentException("GUID is not in a valid format: " + guidString);
      }
    }

    public Resource CalculateGuid(double schemaVersion, bool mergeTerminalServers) {
      CalculateGuid(Source, schemaVersion, mergeTerminalServers);
      return this;
    }

    public Resource CalculateGuid(string rdpFilePathOrContents, double schemaVersion, bool mergeTerminalServers) {
      // create a unique resource ID based on the RDP file contents
      var linesToOmit = mergeTerminalServers && IsApp ? new string[] { "full address:s:", "raweb source type:i:", "signature:s:", "signscope:s:", "raweb external flag:i:" } : null;
      Guid = GetResourceGUID(rdpFilePathOrContents, schemaVersion >= 2.0 ? "" : VirtualFolder, linesToOmit);
      return this;
    }

    public static Guid GetResourceGUID(string rdpFilePath, string suffix = "", string[] linesToOmit = null) {
      // read the entire contents of the file into a string
      // or if the file does not exist, treat the path as the contents
      string fileContents;
      try {
        fileContents = System.IO.File.ReadAllText(rdpFilePath);
      }
      catch {
        fileContents = rdpFilePath;
      }

      // alphabetically sort the lines in the file contents
      var lines = fileContents.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
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
  }
}
