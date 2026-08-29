using System.IO;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// A local folder or .zip already on disk or a GitHub release tag.
/// </summary>
public sealed record SourceSelection(string? LocalPath, string? ReleaseTag, string? ReleaseOwner) {
  public string Repository => ReleaseOwner is { Length: > 0 } owner
    ? $"{owner}/{ReleaseSource.RepositoryName}"
    : ReleaseSource.DefaultRepository;

  public static SourceSelection Parse(string source) {
    if (Directory.Exists(source) || File.Exists(source)) {
      return new SourceSelection(source, null, null);
    }

    var separatorIndex = source.IndexOf("::", StringComparison.Ordinal);
    if (separatorIndex > 0) {
      var owner = source[..separatorIndex];
      var tag = source[(separatorIndex + 2)..];

      if (tag.Length == 0) {
        throw new InstallFailedException($"'--source {source}' is missing a release tag after '::'.");
      }

      if (!ReleaseSource.IsTrustedOwner(owner)) {
        throw new InstallFailedException($"'{owner}' is not a trusted --source owner. Allowed: {string.Join(", ", ReleaseSource.TrustedOwners)}.");
      }

      return new SourceSelection(null, tag, owner);
    }

    if (source.Length == 0) {
      throw new InstallFailedException("--source cannot be empty.");
    }

    return new SourceSelection(null, source, null);
  }
}
