using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace RAWeb.Server.Installer.Stub;

/// <summary>
/// This exe carries no installation logic of its own. It exists only so a GitHub release, or a
/// preview build from install.raweb.app/public.yaml, can offer a "just install this build" download.
///<br/><br/>
/// When run, it extracts the embedded RAWeb.Server.Installer.exe to a temporary directory, writes a
/// raweb-install-pin.json file next to it, and launches the installer. The installer reads
/// raweb-install-pin.json and skips the version picker, going straight to installing the pinned
/// build.
/// </summary>
internal static class Program {
  private const string EmbeddedInstallerResourceName = "RAWeb.Server.Installer.exe";
  private const string EmbeddedDistributablesResourceName = "distributables.zip";
  private const string PinFileName = "raweb-install-pin.json";

  [STAThread]
  private static int Main() {
    var assembly = Assembly.GetExecutingAssembly();
    var hasEmbeddedDistributables = assembly.GetManifestResourceNames().Contains(EmbeddedDistributablesResourceName);

    var tag = assembly
      .GetCustomAttributes<AssemblyMetadataAttribute>()
      .FirstOrDefault(attribute => attribute.Key == "PinnedReleaseTag")
      ?.Value;

    if (!hasEmbeddedDistributables && tag is not { Length: > 0 }) {
      ShowError("This copy of the installer was not built with a pinned release or build. Download the multi-version installer instead.");
      return 1;
    }

    try {
      var scratchDirectory = Path.Combine(Path.GetTempPath(), "RAWebInstaller", Guid.NewGuid().ToString("n"));
      Directory.CreateDirectory(scratchDirectory);

      var installerPath = Path.Combine(scratchDirectory, EmbeddedInstallerResourceName);
      ExtractEmbeddedResource(EmbeddedInstallerResourceName, installerPath);

      var pinJson = hasEmbeddedDistributables
        ? BuildPin(distributablesPath: ExtractDistributables(scratchDirectory))
        : BuildPin(releaseTag: tag!);
      File.WriteAllText(Path.Combine(scratchDirectory, PinFileName), pinJson);

      Process.Start(new ProcessStartInfo(installerPath) {
        UseShellExecute = true,
      });

      return 0;
    }
    catch (Exception exception) {
      ShowError($"Could not start the RAWeb installer.\n\n{exception.Message}");
      return 1;
    }
  }

  private static string ExtractDistributables(string scratchDirectory) {
    var distributablesPath = Path.Combine(scratchDirectory, EmbeddedDistributablesResourceName);
    ExtractEmbeddedResource(EmbeddedDistributablesResourceName, distributablesPath);
    return distributablesPath;
  }

  private static string BuildPin(string? releaseTag = null, string? distributablesPath = null) =>
    distributablesPath is { Length: > 0 }
      ? $$"""{"sourcePath": {{JsonStringLiteral(distributablesPath)}}}"""
      : $$"""{"releaseTag": {{JsonStringLiteral(releaseTag!)}}}""";

  private static void ExtractEmbeddedResource(string resourceName, string destinationPath) {
    using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
      ?? throw new InvalidOperationException($"The '{resourceName}' payload is missing from this stub.");
    using var destination = File.Create(destinationPath);
    resourceStream.CopyTo(destination);
  }

  private static void ShowError(string message) => MessageBox.Show(message, "RAWeb Installer", MessageBoxButtons.OK, MessageBoxIcon.Error);

  /// <summary>
  /// Minimal JSON string escaping so raweb-install-pin.json can be written without additional dependencies.
  /// </summary>
  private static string JsonStringLiteral(string value) {
    var builder = new StringBuilder(value.Length + 2).Append('"');

    foreach (var character in value) {
      switch (character) {
        case '"': builder.Append("\\\""); break;
        case '\\': builder.Append("\\\\"); break;
        case '\n': builder.Append("\\n"); break;
        case '\r': builder.Append("\\r"); break;
        case '\t': builder.Append("\\t"); break;
        default:
          if (character < 0x20) {
            builder.Append("\\u").Append(((int)character).ToString("x4"));
          }
          else {
            builder.Append(character);
          }
          break;
      }
    }

    return builder.Append('"').ToString();
  }
}
