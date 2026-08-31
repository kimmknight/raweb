using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace RAWeb.Server.Installer.Stub;

/// <summary>
/// This exe carries no installation logic of its own. It exists only so a GitHub release, or a
/// preview build from install.raweb.app (see preview-backend.yaml), can offer a "just install this build" download.
///<br/><br/>
/// When run, it extracts the embedded RAWeb.Server.Installer.exe to a temporary directory and
/// launches it with "--source" and "--release-label") pointing at whatever this
/// stub was built to install, skipping the installer's version picker.
/// </summary>
internal static class Program {
  private const string EmbeddedInstallerResourceName = "RAWeb.Server.Installer.exe";
  private const string EmbeddedDistributablesResourceName = "distributables.zip";

  [STAThread]
  private static int Main(string[] args) {
    var assembly = Assembly.GetExecutingAssembly();
    var hasEmbeddedDistributables = assembly.GetManifestResourceNames().Contains(EmbeddedDistributablesResourceName);

    var tag = assembly
      .GetCustomAttributes<AssemblyMetadataAttribute>()
      .FirstOrDefault(attribute => attribute.Key == "ReleaseTag")
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

      var source = hasEmbeddedDistributables ? ExtractDistributables(scratchDirectory) : tag!;

      var arguments = new List<string> { "--source", source };
      if (hasEmbeddedDistributables && tag is { Length: > 0 }) {
        arguments.Add("--release-label");
        arguments.Add(tag);
      }
      arguments.AddRange(args);

      Process.Start(new ProcessStartInfo(installerPath, QuoteArguments(arguments)) {
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

  private static void ExtractEmbeddedResource(string resourceName, string destinationPath) {
    using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
      ?? throw new InvalidOperationException($"The '{resourceName}' payload is missing from this stub.");
    using var destination = File.Create(destinationPath);
    resourceStream.CopyTo(destination);
  }

  private static void ShowError(string message) => MessageBox.Show(message, "RAWeb Installer", MessageBoxButtons.OK, MessageBoxIcon.Error);

  private static string QuoteArguments(IEnumerable<string> args) =>
    string.Join(" ", args.Select(arg => arg.Contains(' ') ? $"\"{arg}\"" : arg));
}
