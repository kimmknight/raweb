using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RAWeb.Server.Utilities;

public sealed class LocalVersions {
  /// <summary>
  /// Gets the application version string from the AssemblyFileVersion attribute.
  /// <br /><br />
  /// This version is from <c>AssemblyInfo.cs</c>, which is generated from <c>FileVersion</c>
  /// from <c>*.csproj</c> when <c>GenerateAssemblyInfo</c> is enabled.
  /// <br />
  /// The release process for RAWeb sets the <c>FileVersion</c> property appropriately.
  /// </summary>
  /// <returns></returns>
  public static string? GetServerVersionString() {
    // get the AssemblyFileVersion from AssemblyInfo.cs
    string? fileVersion = null;
    var versionAttribute = Assembly.GetExecutingAssembly()
      .GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false)
      .OfType<AssemblyFileVersionAttribute>()
      .FirstOrDefault();

    if (versionAttribute != null) {
      fileVersion = versionAttribute.Version;
    }

    if (!string.IsNullOrEmpty(fileVersion)) {
      return fileVersion;
    }

    return "1.0.0.0";
  }

  /// <summary>
  /// Gets the frontend version string from the <c>build.timestamp</c> file.
  /// </summary>
  /// <returns></returns>
  public static string? GetWebClientVersionString() {

    try {
      var assembly = Assembly.GetEntryAssembly();

      var hasTimestampFile = assembly?.GetManifestResourceNames().Contains("static/lib/build.timestamp") ?? false;
      if (!hasTimestampFile) {
        return null;
      }

      var timestampFileStream = assembly?.GetManifestResourceStream("static/lib/build.timestamp");
      if (timestampFileStream == null) {
        return null;
      }

      var timestamp = new StreamReader(timestampFileStream).ReadToEnd().Trim();
      return timestamp;
    }
    catch (Exception) {
      return null;
    }
  }
}
