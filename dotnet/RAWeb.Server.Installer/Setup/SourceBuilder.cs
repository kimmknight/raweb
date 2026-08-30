using System.IO;
using Newtonsoft.Json.Linq;

namespace RAWeb.Server.Installer.Setup;

public static class SourceBuilder {
  private const string DotnetInstallScriptUrl = "https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1";

  /// <summary>
  /// True when the manifest's executables are missing from the payload, meaning it is source that
  /// still needs to be compiled before it can be installed.
  /// </summary>
  public static bool NeedsBuild(string payloadRoot, SetupManifest manifest) {
    var sourceRoot = Path.Combine(payloadRoot, manifest.Layout.SourceRoot);
    return !File.Exists(Path.Combine(sourceRoot, manifest.Layout.MainExecutable)) || !File.Exists(Path.Combine(sourceRoot, manifest.Layout.ServiceExecutable));
  }

  public static void Build(string payloadRoot, InstallLog log, CancellationToken cancellationToken) {
    var slnf = Path.Combine(payloadRoot, "RAWeb.Server.slnf");
    var globalJsonPath = Path.Combine(payloadRoot, "global.json");
    var frontendDirectory = Path.Combine(payloadRoot, "frontend");

    if (!File.Exists(slnf) || !File.Exists(globalJsonPath) || !Directory.Exists(frontendDirectory)) {
      throw new InstallFailedException(
        "This archive contains source code but is missing the files needed to build it (RAWeb.Server.slnf, global.json, or frontend/)."
      );
    }

    log.Indeterminate("Building RAWeb from source");
    log.Detail("This archive contains source code and needs to be built before it can be installed.");

    cancellationToken.ThrowIfCancellationRequested();
    var (dotnetPath, temporarySdkDirectory) = EnsureDotnetSdk(globalJsonPath, log, cancellationToken);

    try {
      cancellationToken.ThrowIfCancellationRequested();
      BuildFrontend(frontendDirectory, log, cancellationToken);

      cancellationToken.ThrowIfCancellationRequested();
      BuildBackend(payloadRoot, slnf, dotnetPath, log, cancellationToken);
    }
    finally {
      if (temporarySdkDirectory is { Length: > 0 }) {
        log.Detail("Removing the temporary .NET SDK download...");
        TryDeleteDirectory(temporarySdkDirectory);
      }
    }
  }

  /// <summary>
  /// Returns the path to a "dotnet" executable carrying the exact SDK version global.json requires.
  /// An already-present SDK (on PATH) is used as-is. Otherwise, the SDK is downloaded into a scratch
  /// directory for this build only.
  /// </summary>
  private static (string DotnetPath, string? TemporaryDirectory) EnsureDotnetSdk(string globalJsonPath, InstallLog log, CancellationToken cancellationToken) {
    var requiredVersion = (string?)JObject.Parse(File.ReadAllText(globalJsonPath))["sdk"]?["version"];
    if (requiredVersion is not { Length: > 0 }) {
      throw new InstallFailedException("global.json does not specify a .NET SDK version.");
    }

    log.Detail($"Checking for .NET SDK {requiredVersion}...");

    if (HasSdkVersion("dotnet", requiredVersion)) {
      return ("dotnet", null);
    }

    log.Detail($".NET SDK {requiredVersion} was not found.");
    log.Detail($"Downloading .NET SDK {requiredVersion} binaries...");

    var installScriptPath = Path.Combine(Path.GetTempPath(), "dotnet-install.ps1");
    using (var source = HttpHelper.OpenRead(DotnetInstallScriptUrl))
    using (var destination = File.Create(installScriptPath)) {
      source.CopyTo(destination);
    }

    var temporarySdkDirectory = Path.Combine(Path.GetTempPath(), "RAWebInstaller", "dotnet-sdk-" + Guid.NewGuid().ToString("n"));
    Directory.CreateDirectory(temporarySdkDirectory);

    // -NoPath: this SDK is only for the dotnet publish call below, never a system-wide default.
    var downloadResult = ProcessRunner.RunPowerShell(
      $"& '{installScriptPath}' -Version {requiredVersion} -InstallDir '{temporarySdkDirectory}' -NoPath",
      log,
      echoOutput: true
    );

    var temporaryDotnet = Path.Combine(temporarySdkDirectory, "dotnet.exe");
    if (!downloadResult.Succeeded || !File.Exists(temporaryDotnet)) {
      TryDeleteDirectory(temporarySdkDirectory);
      throw new InstallFailedException($"Downloading .NET SDK {requiredVersion} failed.");
    }

    return (temporaryDotnet, temporarySdkDirectory);
  }

  private static bool HasSdkVersion(string dotnetPath, string requiredVersion) {
    var result = ProcessRunner.Run(dotnetPath, "--list-sdks");
    return result.Succeeded && result.OutputLines.Any(line => line.StartsWith(requiredVersion + " ", StringComparison.Ordinal));
  }

  private static void TryDeleteDirectory(string path) {
    try {
      Directory.Delete(path, recursive: true);
    }
    catch (IOException) { }
    catch (UnauthorizedAccessException) { }
  }

  private static void BuildFrontend(string frontendDirectory, InstallLog log, CancellationToken cancellationToken) {
    log.Detail("Building the frontend...");

    var binDirectory = Path.Combine(frontendDirectory, "bin");
    var pnpm = Path.Combine(binDirectory, "pnpm.exe");
    if (!File.Exists(pnpm)) {
      throw new InstallFailedException($"'{pnpm}' was not found; cannot build the frontend.");
    }

    cancellationToken.ThrowIfCancellationRequested();
    var install = ProcessRunner.Run(pnpm, "install --loglevel=warn", log, echoOutput: true, frontendDirectory, binDirectory);
    if (!install.Succeeded) {
      throw new InstallFailedException($"'pnpm install' failed with exit code {install.ExitCode}.");
    }

    cancellationToken.ThrowIfCancellationRequested();
    var build = ProcessRunner.Run(pnpm, "build", log, echoOutput: true, frontendDirectory, binDirectory);
    if (!build.Succeeded) {
      throw new InstallFailedException($"'pnpm build' failed with exit code {build.ExitCode}.");
    }
  }

  private static void BuildBackend(string payloadRoot, string slnf, string dotnetPath, InstallLog log, CancellationToken cancellationToken) {
    log.Detail("Building the backend...");

    var fileVersion = DateTime.UtcNow.ToString("yyyy.MM.dd.HHmm");
    var arguments = $"publish \"{slnf}\" --configuration Release -p:FileVersion={fileVersion}-unstable";

    var result = ProcessRunner.Run(dotnetPath, arguments, log, echoOutput: true, payloadRoot);
    if (!result.Succeeded) {
      throw new InstallFailedException($"'dotnet publish' failed with exit code {result.ExitCode}.");
    }
  }
}
