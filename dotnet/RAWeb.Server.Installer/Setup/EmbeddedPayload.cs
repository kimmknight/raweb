using System.IO;
using System.Reflection;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// Pinned builds of the installer (see the EmbeddedReleaseTag and EmbeddedDistributablesPath
/// properties in the csproj) carry a RAWeb server release to install: either a GitHub release tag
/// (downloaded at install time) or an embedded distributables zip. This turns that into the same
/// "--source" and "--release-label" arguments a user could have typed, so the rest of the
/// installer does not need to know the difference.
/// </summary>
public static class EmbeddedPayload {
  private const string DistributablesResourceName = "distributables.zip";
  private const string ReleaseTagMetadataKey = "EmbeddedReleaseTag";

  /// <summary>
  /// Returns <paramref name="args"/> unchanged when this is not a pinned build. Otherwise, returns the
  /// arguments with the pinned source (and label) prepended, replacing any "--source" that was passed.
  /// The returned arguments are what should be forwarded when relaunching elevated.
  /// </summary>
  public static string[] Apply(string[] args) {
    var assembly = Assembly.GetExecutingAssembly();
    var hasDistributables = assembly.GetManifestResourceNames().Contains(DistributablesResourceName);
    var tag = assembly
      .GetCustomAttributes<AssemblyMetadataAttribute>()
      .FirstOrDefault(attribute => attribute.Key == ReleaseTagMetadataKey)
      ?.Value;
    var hasTag = tag is { Length: > 0 };

    if (!hasDistributables && !hasTag) {
      return args;
    }

    List<string> pinned = [StartupOptions.SourceSwitch];
    if (hasDistributables) {
      pinned.Add(ExtractDistributables(assembly));

      // the tag is only a label when distributables are provided
      if (hasTag) {
        pinned.Add(StartupOptions.ReleaseLabelSwitch);
        pinned.Add(tag!);
      }
    }
    else {
      pinned.Add(tag!);
    }

    return [.. pinned, .. WithoutSource(args)];
  }

  /// <summary>
  /// Drops any "--source &lt;value&gt;" or "--source=&lt;value&gt;" so the embedded source cannot be
  /// overridden.
  /// </summary>
  private static IEnumerable<string> WithoutSource(string[] args) {
    for (var i = 0; i < args.Length; i++) {
      if (args[i].StartsWith(StartupOptions.SourceSwitch + "=", StringComparison.OrdinalIgnoreCase)) {
        continue;
      }

      if (args[i].Equals(StartupOptions.SourceSwitch, StringComparison.OrdinalIgnoreCase)) {
        if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal)) {
          i++;
        }
        continue;
      }

      yield return args[i];
    }
  }

  private static string ExtractDistributables(Assembly assembly) {
    var scratchDirectory = Path.Combine(Path.GetTempPath(), "RAWebInstaller", Guid.NewGuid().ToString("n"));
    Directory.CreateDirectory(scratchDirectory);

    var destinationPath = Path.Combine(scratchDirectory, DistributablesResourceName);
    using var resourceStream = assembly.GetManifestResourceStream(DistributablesResourceName)
      ?? throw new InstallFailedException($"The '{DistributablesResourceName}' payload is missing from this installer.");
    using var destination = File.Create(destinationPath);
    resourceStream.CopyTo(destination);

    return destinationPath;
  }
}
