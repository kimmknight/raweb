using System.IO;
using Newtonsoft.Json.Linq;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// Downloads and installs the ASP.NET Core Hosting Bundle, which supplies the ASP.NET Core Module (ANCM).
/// </summary>
public static class HostingBundle {
  public static void Install(SetupManifest manifest, InstallLog log, CancellationToken cancellationToken) {
    log.Indeterminate("Locating the ASP.NET Core Hosting Bundle");

    var (downloadUrl, version) = ResolveDownload(manifest.Requirements.HostingBundleReleaseMetadataUrl);

    var installerPath = Path.Combine(Path.GetTempPath(), $"dotnet-hosting-{version}.exe");
    log.Detail($"Downloading {downloadUrl}...");

    Download(downloadUrl, installerPath, cancellationToken);

    try {
      log.Detail("Installing ASP.NET Core Module...");

      var exitCode = ProcessRunner.RunAndWaitVisible(
        installerPath,
        // raweb.exe is self-contained, so the shared runtime and x86 payloads are not needed.
        "/install /quiet /norestart OPT_NO_RUNTIME=1 OPT_NO_SHAREDFX=1 OPT_NO_X86=1"
      );

      // 1641 and 3010 both mean "installed, IIS restart pending".
      if (exitCode is not (0 or 1641 or 3010)) {
        throw new InstallFailedException($"Hosting Bundle installer failed with exit code {exitCode}.");
      }
    }
    finally {
      TryDelete(installerPath);
    }

    log.Detail("Restarting IIS to load the module...");
    ProcessRunner.Run(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "iisreset.exe"), "/noforce");

    log.Detail("ASP.NET Core Module installed successfully.");
  }

  private static (string Url, string Version) ResolveDownload(string releaseMetadataUrl) {
    string json;
    try {
      json = HttpHelper.GetString(releaseMetadataUrl);
    }
    catch (Exception exception) {
      throw new InstallFailedException(
        "Could not fetch the latest ASP.NET Core Module version from the .NET release metadata. "
        + "Install the .NET 10 Hosting Bundle manually and run the installer again. "
        + $"({exception.Message})");
    }

    var metadata = JObject.Parse(json);
    var latestRelease = (string?)metadata["latest-release"] ?? "latest";

    var url = metadata["releases"]?
      .FirstOrDefault()?["aspnetcore-runtime"]?["files"]?
      .FirstOrDefault(file => (string?)file["name"] == "dotnet-hosting-win.exe")?["url"];

    if (url is null) {
      throw new InstallFailedException("The .NET release metadata did not contain a dotnet-hosting-win.exe download.");
    }

    return ((string)url!, latestRelease);
  }

  private static void Download(string url, string destinationPath, CancellationToken cancellationToken) {
    using var response = HttpHelper.OpenRead(url);
    using var destination = File.Create(destinationPath);

    var buffer = new byte[81920];
    int read;
    while ((read = response.Read(buffer, 0, buffer.Length)) > 0) {
      cancellationToken.ThrowIfCancellationRequested();
      destination.Write(buffer, 0, read);
    }
  }

  private static void TryDelete(string path) {
    try {
      File.Delete(path);
    }
    catch (IOException) {
    }
    catch (UnauthorizedAccessException) {
    }
  }
}
