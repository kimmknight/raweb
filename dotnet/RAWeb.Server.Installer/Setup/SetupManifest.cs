using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Newtonsoft.Json;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// Object model for setup.json.
/// <br/><br/>
/// setup.schema.json is generated from it on every build (see SetupSchemaGenerator)
/// and written next to the built exe. It is not checked into source control.
/// </summary>
public sealed class SetupManifest {
  public const int SupportedSchemaVersion = 1;
  private const string VersionPattern = @"^[0-9]+(\.[0-9]+){0,3}$";

  /// <summary>
  /// Manifest format version. The installer refuses archives with a version it does not understand.
  /// </summary>
  [JsonProperty("schemaVersion", Required = Required.Always)]
  public int SchemaVersion { get; set; }

  /// <summary>
  /// Lowest RAWeb.Server.Installer version able to install this archive.
  /// </summary>
  [JsonProperty("minimumInstallerVersion")]
  [RegularExpression(VersionPattern)]
  public string? MinimumInstallerVersion { get; set; }

  [JsonProperty("product", Required = Required.Always)] public ProductInfo Product { get; set; } = new();
  [JsonProperty("layout", Required = Required.Always)] public LayoutInfo Layout { get; set; } = new();
  [JsonProperty("defaults", Required = Required.Always)] public DefaultsInfo Defaults { get; set; } = new();
  [JsonProperty("requirements", Required = Required.Always)] public RequirementsInfo Requirements { get; set; } = new();

  /// <summary>
  /// User-configurable installation options. The installer renders one control
  /// per entry and exposes each as a command-line switch of the same id.
  /// </summary>
  [JsonProperty("options")]
  public List<SetupOption> Options { get; set; } = [];

  public static SetupManifest Load(string path) {
    var manifest = JsonConvert.DeserializeObject<SetupManifest>(File.ReadAllText(path))
      ?? throw new SetupManifestException($"'{path}' is empty or not a JSON object.");

    if (manifest.SchemaVersion != SupportedSchemaVersion) {
      throw new SetupManifestException(
        $"setup.json declares schema version {manifest.SchemaVersion}, but this installer only understands version {SupportedSchemaVersion}. Use a newer installer.");
    }

    if (manifest.MinimumInstallerVersion is { Length: > 0 } minimum
      && Version.TryParse(minimum, out var required)
      && required > InstallerVersion.Current) {
      throw new SetupManifestException(
        $"This release requires installer version {required} or newer; this installer is {InstallerVersion.Current}.");
    }

    return manifest;
  }

  /// <summary>
  /// Returns the manifest for a release directory, or null when the release predates setup.json.
  /// </summary>
  public static SetupManifest? TryLoadFromReleaseDirectory(string releaseDirectory) {
    var path = Path.Combine(releaseDirectory, "setup.json");
    return File.Exists(path) ? Load(path) : null;
  }

  public sealed class ProductInfo {
    [JsonProperty("name", Required = Required.Always)] public string Name { get; set; } = "RAWeb";
    [JsonProperty("publisher", Required = Required.Always)] public string Publisher { get; set; } = "RAWeb";

    /// <summary>
    /// Optional. When omitted, the installer reads the file version of layout.mainExecutable.
    /// </summary>
    [JsonProperty("version")] public string? Version { get; set; }

    [JsonProperty("helpUrl")]
    [Url]
    public string? HelpUrl { get; set; }
  }

  /// <summary>
  /// Where the payload lives inside the archive.
  /// </summary>
  public sealed class LayoutInfo {
    /// <summary>
    /// Archive-relative directory holding the files to copy into the versioned install directory.
    /// "." means the archive root.
    /// </summary>
    [JsonProperty("sourceRoot", Required = Required.Always)]
    public string SourceRoot { get; set; } = ".";

    /// <summary>
    /// sourceRoot-relative path to the web server host executable (raweb.exe).
    /// </summary>
    [JsonProperty("mainExecutable", Required = Required.Always)]
    public string MainExecutable { get; set; } = "raweb.exe";

    /// <summary>
    /// sourceRoot-relative path to the management service executable (rawebmgmtsvc.exe).
    /// </summary>
    [JsonProperty("serviceExecutable", Required = Required.Always)]
    public string ServiceExecutable { get; set; } = "rawebmgmtsvc.exe";

    /// <summary>
    /// sourceRoot-relative directory holding per-installation state that survives upgrades.
    /// </summary>
    [JsonProperty("appDataDirectory")]
    [DefaultValue("App_Data")]
    public string AppDataDirectory { get; set; } = "App_Data";

    /// <summary>
    /// sourceRoot-relative path to the appSettings XML file.
    /// </summary>
    [JsonProperty("appSettingsFile")]
    [DefaultValue("App_Data/appSettings.config")]
    public string AppSettingsFile { get; set; } = "App_Data/appSettings.config";
  }

  public sealed class DefaultsInfo {
    /// <summary>
    /// Base directory under which "&lt;site&gt;\&lt;virtual path&gt;" install directories are created.
    /// </summary>
    [JsonProperty("installDirBase", Required = Required.Always)]
    public string InstallDirBase { get; set; } = @"C:\Program Files\RAWeb";

    [JsonProperty("webSite", Required = Required.Always)] public string WebSite { get; set; } = "Default Web Site";
    [JsonProperty("virtualPath", Required = Required.Always)] public string VirtualPath { get; set; } = "RAWeb";
  }

  public sealed class RequirementsInfo {
    /// <summary>
    /// Minimum ASP.NET Core Hosting Bundle (ANCM) version.
    /// </summary>
    [JsonProperty("hostingBundleMinimumVersion", Required = Required.Always)]
    [RegularExpression(VersionPattern)]
    public string HostingBundleMinimumVersion { get; set; } = "10.0.0";

    /// <summary>
    /// dotnet release-metadata releases.json used to locate dotnet-hosting-win.exe.
    /// </summary>
    [JsonProperty("hostingBundleReleaseMetadataUrl")]
    [Url]
    public string HostingBundleReleaseMetadataUrl { get; set; } = "https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json";

    /// <summary>
    /// Install-WindowsFeature names required on Windows Server.
    /// </summary>
    [JsonProperty("serverFeatures", Required = Required.Always)]
    public List<string> ServerFeatures { get; set; } = [];

    /// <summary>
    /// DISM optional feature names required on Windows client editions.
    /// </summary>
    [JsonProperty("clientFeatures", Required = Required.Always)]
    public List<string> ClientFeatures { get; set; } = [];

    /// <summary>
    /// When true, a missing WSL2 blocks installation. When false, it only produces a warning.
    /// </summary>
    [JsonProperty("requiresWsl2")]
    [DefaultValue(false)]
    public bool RequiresWsl2 { get; set; }

    /// <summary>
    /// Virtual-path-relative URL polled after installation to confirm the app started.
    /// </summary>
    [JsonProperty("healthCheckPath")]
    [DefaultValue("api/app-init-details")]
    public string HealthCheckPath { get; set; } = "api/app-init-details";
  }

  public sealed class SetupOption {
    /// <summary>
    /// Stable identifier. Also the command-line switch name.
    /// </summary>
    [JsonProperty("id", Required = Required.Always)]
    [RegularExpression("^[A-Za-z][A-Za-z0-9]*$")]
    public string Id { get; set; } = "";

    [JsonProperty("type", Required = Required.Always)]
    [RegularExpression("^(bool|enum|string)$")]
    public string Type { get; set; } = "string";

    [JsonProperty("label", Required = Required.Always)] public string Label { get; set; } = "";
    [JsonProperty("description")] public string? Description { get; set; }

    /// <summary>
    /// Value used when the user does not choose one.
    /// </summary>
    [JsonProperty("default")]
    public object? Default { get; set; }

    /// <summary>
    /// When true, this option will be hidden behind the options page's "Advanced" expander.
    /// </summary>
    [JsonProperty("advanced")]
    [DefaultValue(false)]
    public bool Advanced { get; set; }

    /// <summary>
    /// When present, the resolved value is written to this key in layout.appSettingsFile. An
    /// existing value in a previous installation's appSettings takes precedence over the default
    /// and is carried forward on upgrade.
    /// </summary>
    [JsonProperty("appSetting")]
    public string? AppSetting { get; set; }

    /// <summary>
    /// Required when type is "enum".
    /// </summary>
    [JsonProperty("choices")]
    public List<SetupOptionChoice> Choices { get; set; } = [];

    [JsonIgnore]
    public bool IsBoolean => string.Equals(Type, "bool", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The declared default in the canonical string form used throughout the engine ("true"/"false" for booleans).
    /// </summary>
    [JsonIgnore]
    public string DefaultValue => Default switch {
      bool flag => flag ? "true" : "false",
      null => IsBoolean ? "false" : "",
      _ => Convert.ToString(Default, System.Globalization.CultureInfo.InvariantCulture) ?? "",
    };
  }

  public sealed class SetupOptionChoice {
    [JsonProperty("value", Required = Required.Always)] public string Value { get; set; } = "";
    [JsonProperty("label", Required = Required.Always)] public string Label { get; set; } = "";
    [JsonProperty("description")] public string? Description { get; set; }

    [JsonProperty("recommended")]
    [DefaultValue(false)]
    public bool Recommended { get; set; }
  }
}

public sealed class SetupManifestException(string message) : Exception(message);

public static class InstallerVersion {
  public static readonly Version Current =
    typeof(InstallerVersion).Assembly.GetName().Version ?? new Version(1, 0, 0, 0);
}
