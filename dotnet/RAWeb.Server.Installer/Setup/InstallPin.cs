using System.IO;
using Newtonsoft.Json;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// A launcher that already knows what to install drops this file next to the installer EXE to
/// skip VersionPage's picker. It can specify either a GitHub release tag to resolve and install
/// or the path to an already-downloaded and extracted payload to install.
/// </summary>
public static class InstallPin {
  public const string FileName = "raweb-install-pin.json";

  public static string? ReleaseTag { get; }
  public static string? SourcePath { get; }

  static InstallPin() {
    var pinFilePath = Path.Combine(AppContext.BaseDirectory, FileName);
    if (!File.Exists(pinFilePath)) {
      return;
    }

    try {
      var data = JsonConvert.DeserializeObject<Data>(File.ReadAllText(pinFilePath));
      ReleaseTag = data?.ReleaseTag is { Length: > 0 } tag ? tag : null;
      SourcePath = data?.SourcePath is { Length: > 0 } path ? path : null;
    }
    catch (Exception exception) when (exception is JsonException or IOException) {
    }
  }

  private sealed class Data {
    [JsonProperty("releaseTag")] public string? ReleaseTag { get; set; }
    [JsonProperty("sourcePath")] public string? SourcePath { get; set; }
  }
}
