using System.IO;
using System.IO.Compression;
using System.Net;
using Newtonsoft.Json.Linq;

namespace RAWeb.Server.Installer.Setup;

public sealed record ReleaseAsset(string Name, string DownloadUrl, long SizeInBytes);

public sealed record ReleaseInfo(
  string TagName,
  string Title,
  DateTimeOffset PublishedAt,
  bool IsPrerelease,
  bool IsUnreleased,
  IReadOnlyList<ReleaseAsset> Assets,
  string? PreviewOwner = null,
  string? PreviewBranch = null) {
  /// <summary>
  /// The full archive, preferred over the trimmed __no_docs and __no_web_connect variants.
  /// </summary>
  public ReleaseAsset? PrimaryArchive => Assets
    .Where(asset => asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
    .OrderBy(asset => asset.Name.Count(character => character == '_'))
    .FirstOrDefault();
}

/// <summary>
/// Where the payload to install comes from: a GitHub release, a local ZIP, or an unpacked directory.
/// </summary>
public static class ReleaseSource {
  public const string DefaultRepository = "kimmknight/raweb";
  public const string RepositoryName = "raweb";

  public static IReadOnlyList<ReleaseInfo> ListGitHubReleases(string repository = DefaultRepository, int limit = 30) {
    var json = HttpHelper.GetString($"https://api.github.com/repos/{repository}/releases?per_page={limit}");

    return JArray.Parse(json)
      .Where(release => (bool?)release["draft"] != true)
      .Select(ParseRelease)
      .ToArray();
  }

  public static ReleaseInfo GetReleaseByTag(string tag, string repository = DefaultRepository) {
    var json = HttpHelper.GetString($"https://api.github.com/repos/{repository}/releases/tags/{Uri.EscapeDataString(tag)}");
    return ParseRelease(JObject.Parse(json));
  }

  private static ReleaseInfo ParseRelease(JToken release) => new(
    TagName: (string?)release["tag_name"] ?? "",
    Title: (string?)release["name"] is { Length: > 0 } name ? name : (string?)release["tag_name"] ?? "",
    PublishedAt: (DateTimeOffset?)release["published_at"] ?? DateTimeOffset.MinValue,
    IsPrerelease: (bool?)release["prerelease"] ?? false,
    IsUnreleased: false,
    Assets: [.. (release["assets"] as JArray ?? [])
      .Select(asset => new ReleaseAsset(
        (string?)asset["name"] ?? "",
        (string?)asset["browser_download_url"] ?? "",
        (long?)asset["size"] ?? 0))]
  );

  /// <summary>
  /// Trusted fork owners whose branches are offered alongside branches in the upstream repository
  /// itself, These are also the only owners permitted for manual branch or release tag specification.
  /// </summary>
  public static readonly string[] TrustedOwners = ["kimmknight", "jackbuehner"];

  public static bool IsTrustedOwner(string owner) => TrustedOwners.Contains(owner, StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Lists branches that have no release yet, but can still be installed via the install.raweb.app preview installer.
  /// This includes any branch (in this repository or a trusted fork) backing an open pull
  /// request.
  /// <br/><br/>
  /// This method only returns the next branch and any branch in a PR from a trusted fork. To ensure that
  /// branchs that are not ready for review are not offered, those are supressed from the returned list.
  /// </summary>
  public static IReadOnlyList<ReleaseInfo> ListUnreleasedBranches(string repository = DefaultRepository) {
    var results = new List<ReleaseInfo>();
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    var repositoryName = repository.Split('/')[1];
    var allowedRepositories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { repository };
    foreach (var owner in TrustedOwners) {
      allowedRepositories.Add($"{owner}/{repositoryName}");
    }

    var pullsJson = HttpHelper.GetString($"https://api.github.com/repos/{repository}/pulls?state=open&per_page=100");

    foreach (var pullRequest in JArray.Parse(pullsJson)) {
      var head = pullRequest["head"];
      var branch = (string?)head?["ref"];
      var headRepository = (string?)head?["repo"]?["full_name"];

      if (branch is not { Length: > 0 } || headRepository is not { Length: > 0 } || !allowedRepositories.Contains(headRepository)) {
        continue;
      }

      if (!seen.Add($"{headRepository}#{branch}")) {
        continue;
      }

      var title = (string?)pullRequest["title"] is { Length: > 0 } prTitle ? prTitle : branch;
      var updatedAt = (DateTimeOffset?)pullRequest["updated_at"] ?? DateTimeOffset.MinValue;
      results.Add(BranchRelease(repository, headRepository, branch, title, updatedAt));
    }

    if (seen.Add($"{repository}#next") && BranchExists(repository, "next")) {
      results.Add(BranchRelease(repository, repository, "next", "next", DateTimeOffset.UtcNow));
    }

    return results;
  }

  private static ReleaseInfo BranchRelease(string upstreamRepository, string sourceRepository, string branch, string title, DateTimeOffset updatedAt) {
    var isFork = !string.Equals(sourceRepository, upstreamRepository, StringComparison.OrdinalIgnoreCase);
    var owner = sourceRepository.Split('/')[0];
    var tag = isFork ? $"{owner}:{branch}" : branch;

    // the preview installer Cloudflare Worker, not this installer, resolves the actual download
    return new ReleaseInfo(
      tag,
      title,
      updatedAt,
      IsPrerelease: false,
      IsUnreleased: true,
      Assets: [],
      PreviewOwner: owner,
      PreviewBranch: branch
    );
  }

  private static bool BranchExists(string repository, string branch) {
    try {
      HttpHelper.GetString($"https://api.github.com/repos/{repository}/branches/{Uri.EscapeDataString(branch)}");
      return true;
    }
    catch (WebException) {
      return false;
    }
  }

  /// <summary>
  /// For now, install.raweb.app responds with a PowerShell script. Eventually, it will respond
  /// with the actual preview ZIP archive, which this app can then extract and install like any other release.
  /// </summary>
  public enum PreviewResponseKind { Archive, Script }

  public sealed record PreviewResponse(PreviewResponseKind Kind, byte[] Content);

  /// <summary>
  /// Fetches whatever install.raweb.app currently returns for a branch, without guessing its shape:
  /// a zip archive is detected by magic header, falling back to Content-Type, and anything
  /// else is treated as the PowerShell installer script it has always returned historically.
  /// </summary>
  public static PreviewResponse FetchPreviewInstall(string owner, string branch, IProgress<double>? progress, CancellationToken cancellationToken) {
    var url = $"https://install.raweb.app/preview/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(branch)}?artifact=true&f=zip";

    var request = (HttpWebRequest)WebRequest.Create(url);
    request.UserAgent = HttpHelper.UserAgent;
    request.AllowAutoRedirect = true;

    using var response = request.GetResponse();
    var contentType = ((HttpWebResponse)response).ContentType ?? "";
    using var source = response.GetResponseStream();
    using var buffer = new MemoryStream();

    var total = response.ContentLength;
    var chunk = new byte[81920];
    int read;

    while ((read = source.Read(chunk, 0, chunk.Length)) > 0) {
      cancellationToken.ThrowIfCancellationRequested();
      buffer.Write(chunk, 0, read);

      if (total > 0) {
        progress?.Report(buffer.Length * 100d / total);
      }
    }

    var content = buffer.ToArray();

    var isZip = (content.Length >= 4 && content[0] == 'P' && content[1] == 'K' && content[2] == 3 && content[3] == 4)
      || contentType.IndexOf("zip", StringComparison.OrdinalIgnoreCase) >= 0
      || contentType.IndexOf("octet-stream", StringComparison.OrdinalIgnoreCase) >= 0;

    return new PreviewResponse(isZip ? PreviewResponseKind.Archive : PreviewResponseKind.Script, content);
  }

  public static void Download(ReleaseAsset asset, string destinationPath, IProgress<double>? progress, CancellationToken cancellationToken) {
    var request = (HttpWebRequest)WebRequest.Create(asset.DownloadUrl);
    request.UserAgent = HttpHelper.UserAgent;
    request.AllowAutoRedirect = true;

    using var response = request.GetResponse();
    using var source = response.GetResponseStream();
    using var destination = File.Create(destinationPath);

    var total = response.ContentLength > 0 ? response.ContentLength : asset.SizeInBytes;
    var buffer = new byte[81920];
    long copied = 0;
    int read;

    while ((read = source.Read(buffer, 0, buffer.Length)) > 0) {
      cancellationToken.ThrowIfCancellationRequested();
      destination.Write(buffer, 0, read);
      copied += read;

      if (total > 0) {
        progress?.Report(copied * 100d / total);
      }
    }
  }

  public static void Extract(string archivePath, string destinationDirectory, IProgress<double>? progress, CancellationToken cancellationToken) {
    Directory.CreateDirectory(destinationDirectory);

    using var archive = ZipFile.OpenRead(archivePath);
    var entries = archive.Entries;

    for (var i = 0; i < entries.Count; i++) {
      cancellationToken.ThrowIfCancellationRequested();

      var entry = entries[i];
      var destination = Path.GetFullPath(Path.Combine(destinationDirectory, entry.FullName));

      // Reject entries whose path escapes the destination directory (zip slip).
      if (!destination.StartsWith(Path.GetFullPath(destinationDirectory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) {
        throw new InstallFailedException($"The archive contains an entry with an unsafe path: '{entry.FullName}'.");
      }

      if (entry.Name.Length == 0) {
        Directory.CreateDirectory(destination);
      }
      else {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        entry.ExtractToFile(destination, overwrite: true);
      }

      progress?.Report((i + 1) * 100d / entries.Count);
    }
  }

  /// <summary>
  /// Locates the directory inside an extracted archive that actually holds the payload, tolerating
  /// archives that wrap everything in a single top-level folder.
  /// </summary>
  public static string ResolvePayloadRoot(string extractedDirectory) {
    var isPayloadRoot = File.Exists(Path.Combine(extractedDirectory, "setup.json")) || File.Exists(Path.Combine(extractedDirectory, "setup.ps1"));
    if (isPayloadRoot) {
      return extractedDirectory;
    }

    var subdirectories = Directory.GetDirectories(extractedDirectory);
    if (subdirectories.Length == 1) {
      return ResolvePayloadRoot(subdirectories[0]);
    }

    return extractedDirectory;
  }
}

/// <summary>
/// How a chosen release must be installed, which decides whether the wizard can continue natively.
/// </summary>
public enum InstallStrategy {
  /// <summary>
  /// The release carries setup.json; the installer performs the installation itself.
  /// </summary>
  Native,

  /// <summary>
  /// The release predates setup.json; setup.ps1 has to run in a PowerShell window.
  /// </summary>
  LegacyPowerShell,

  /// <summary>
  /// An unreleased branch, handed off to the install.raweb.app preview installer script rather than
  /// downloaded by this app: that service knows how to find a matching GitHub Actions build
  /// artifact, which this app has no credentials to look up itself.
  /// </summary>
  RemotePreview,

  /// <summary>
  /// Neither setup.json nor setup.ps1 was found.
  /// </summary>
  Unsupported,
}

public static class InstallStrategyExtensions {
  /// <summary>
  /// True for strategies where a PowerShell window does the actual installing and the native wizard
  /// pages (IIS site, install directory, options, progress) have nothing to do.
  /// </summary>
  public static bool RequiresExternalHandoff(this InstallStrategy strategy) =>
    strategy is InstallStrategy.LegacyPowerShell or InstallStrategy.RemotePreview;
}

public static class ReleaseInspector {
  public static InstallStrategy DetermineStrategy(string payloadRoot) {
    if (File.Exists(Path.Combine(payloadRoot, "setup.json"))) {
      return InstallStrategy.Native;
    }

    return File.Exists(Path.Combine(payloadRoot, "setup.ps1"))
      ? InstallStrategy.LegacyPowerShell
      : InstallStrategy.Unsupported;
  }

  /// <summary>
  /// Hands a setup.ps1 release off to Windows PowerShell 5 in a visible window.
  /// </summary>
  public static void LaunchLegacySetup(string payloadRoot) {
    var script = Path.Combine(payloadRoot, "setup.ps1");
    LaunchPowerShell($"-NoProfile -ExecutionPolicy Bypass -File \"{script}\"", payloadRoot);
  }

  /// <summary>
  /// Fetches install.raweb.app's PowerShell installer script for an unreleased branch and runs it
  /// immediately. It is fetched fresh right before launch rather than reusing a copy saved earlier
  /// in the wizard because the artifact download URL it embeds is only valid for a minute.
  /// </summary>
  public static void LaunchPreviewScript(string owner, string branch) {
    var response = ReleaseSource.FetchPreviewInstall(owner, branch, progress: null, CancellationToken.None);

    var scratchDirectory = Path.Combine(Path.GetTempPath(), "RAWebInstaller", Guid.NewGuid().ToString("n"));
    Directory.CreateDirectory(scratchDirectory);
    var scriptPath = Path.Combine(scratchDirectory, "preview-install.ps1");

    // A UTF-8 BOM is required. Without one, Windows PowerShell 5.1 mangles the script's box-drawing characters.
    var bom = new byte[] { 0xEF, 0xBB, 0xBF };
    File.WriteAllBytes(scriptPath, [.. bom, .. response.Content]);

    try {
      LaunchPowerShell($"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"", scratchDirectory);
    }
    finally {
      try {
        Directory.Delete(scratchDirectory, recursive: true);
      }
      catch (IOException) { }
    }
  }

  private static void LaunchPowerShell(string arguments, string? workingDirectory) {
    var powerShell = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.System),
      "WindowsPowerShell", "v1.0", "powershell.exe");

    var startInfo = new System.Diagnostics.ProcessStartInfo(powerShell, arguments) {
      UseShellExecute = true,
      WorkingDirectory = workingDirectory ?? "",
    };

    if (!SystemChecks.IsRunningAsAdministrator()) {
      startInfo.Verb = "runas";
    }

    using var process = System.Diagnostics.Process.Start(startInfo)
      ?? throw new InstallFailedException("Could not start Windows PowerShell.");

    process.WaitForExit();
  }
}
