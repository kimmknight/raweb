namespace RAWeb.Server.Installer.Setup;

public sealed record FeatureInstallResult(bool RestartRequired);

/// <summary>
/// Enables the IIS features RAWeb needs.DISM is used on client editions and
/// Install-WindowsFeature is used on Server. Both stream their output into the install log.
/// </summary>
public static class WindowsFeatures {
  public static FeatureInstallResult Enable(IReadOnlyList<string> features, bool isServer, InstallLog log) {
    if (features.Count == 0) {
      return new FeatureInstallResult(false);
    }

    return isServer ? EnableServerFeatures(features, log) : EnableClientFeatures(features, log);
  }

  private static FeatureInstallResult EnableServerFeatures(IReadOnlyList<string> features, InstallLog log) {
    var restartRequired = false;

    for (var i = 0; i < features.Count; i++) {
      var feature = features[i];
      log.Detail($"Installing {feature} ({i + 1}/{features.Count})...");

      var escaped = feature.Replace("'", "''");
      var result = ProcessRunner.RunPowerShell(
        "$ProgressPreference = 'SilentlyContinue'; " +
        $"Import-Module ServerManager; $r = Install-WindowsFeature -Name '{escaped}'; " +
        "if (-not $r.Success) { Write-Error \"failed\"; exit 1 }; Write-Output \"RestartNeeded=$($r.RestartNeeded)\"",
        log,
        echoOutput: true);

      if (!result.Succeeded) {
        throw new InstallFailedException($"Installing Windows feature '{feature}' failed.");
      }

      if (result.OutputLines.Any(line =>
          line.StartsWith("RestartNeeded=", StringComparison.OrdinalIgnoreCase)
          && !line.EndsWith("=No", StringComparison.OrdinalIgnoreCase)
          )
      ) {
        restartRequired = true;
      }
    }

    return new FeatureInstallResult(restartRequired);
  }

  private static FeatureInstallResult EnableClientFeatures(IReadOnlyList<string> features, InstallLog log) {
    var restartRequired = false;

    for (var i = 0; i < features.Count; i++) {
      var feature = features[i];
      log.Detail($"Installing {feature} ({i + 1}/{features.Count})...");

      var result = ProcessRunner.Run(
        SystemChecks.DismPath,
        $"/online /enable-feature /featurename:{feature} /norestart /quiet /english");

      // 3010 is DISM's "success, but a restart is pending".
      if (result.ExitCode == 3010) {
        restartRequired = true;
        continue;
      }

      if (!result.Succeeded) {
        throw new InstallFailedException($"Enabling Windows feature '{feature}' failed with exit code {result.ExitCode}.");
      }
    }

    return new FeatureInstallResult(restartRequired);
  }
}
