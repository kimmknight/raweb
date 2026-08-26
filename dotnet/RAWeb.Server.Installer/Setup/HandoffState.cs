using System.IO;
using Newtonsoft.Json;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// Everything the user chose, written to a temp file so the elevated instance can pick up exactly
/// where the unelevated one left off.
/// </summary>
public sealed class HandoffState {
  [JsonProperty("payloadRoot")] public string PayloadRoot { get; set; } = "";
  [JsonProperty("scratchDirectory")] public string? ScratchDirectory { get; set; }
  [JsonProperty("localSourcePath")] public string? LocalSourcePath { get; set; }
  [JsonProperty("displayVersion")] public string DisplayVersion { get; set; } = "";

  [JsonProperty("assetName")] public string? AssetName { get; set; }
  [JsonProperty("assetUrl")] public string? AssetUrl { get; set; }
  [JsonProperty("assetSize")] public long AssetSize { get; set; }

  [JsonProperty("webSite")] public string WebSite { get; set; } = "";
  [JsonProperty("virtualPath")] public string VirtualPath { get; set; } = "";
  [JsonProperty("installDirectory")] public string InstallDirectory { get; set; } = "";
  [JsonProperty("options")] public Dictionary<string, string> Options { get; set; } = [];

  [JsonProperty("express")] public bool Express { get; set; }
  [JsonProperty("overwrite")] public bool Overwrite { get; set; }
  [JsonProperty("autoClose")] public AutoCloseMode AutoClose { get; set; } = AutoCloseMode.Never;
  [JsonProperty("noWelcome")] public bool NoWelcome { get; set; }

  public ReleaseAsset? ToAsset() => AssetUrl is { Length: > 0 } ? new ReleaseAsset(AssetName ?? "", AssetUrl, AssetSize) : null;

  public string Save() {
    var directory = Path.Combine(Path.GetTempPath(), "RAWebInstaller");
    Directory.CreateDirectory(directory);

    var path = Path.Combine(directory, $"handoff-{Guid.NewGuid():n}.json");
    File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
    return path;
  }

  public static HandoffState Load(string path) =>
    JsonConvert.DeserializeObject<HandoffState>(File.ReadAllText(path))
    ?? throw new InstallFailedException($"The handoff file '{path}' could not be read.");
}
