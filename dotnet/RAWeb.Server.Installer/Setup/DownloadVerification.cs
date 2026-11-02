using System.IO;
using System.Security.Cryptography;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// Checks downloaded files before the installer runs or extracts them. The installer runs with
/// administrator rights, so a modified download would also run with administrator rights.
/// </summary>
public static class DownloadVerification {
  /// <summary>
  /// Throws if the file's hash does not match the published hash.
  /// </summary>
  /// <param name="path">The downloaded file.</param>
  /// <param name="algorithm">The algorithm the published hash uses. It is disposed by this method.</param>
  /// <param name="expectedHash">The published hash as hexadecimal characters (case-insensitive).</param>
  /// <param name="displayName">How the file should be described in the error message.</param>
  public static void VerifyHash(string path, HashAlgorithm algorithm, string expectedHash, string displayName) {
    string actualHash;
    using (algorithm)
    using (var stream = File.OpenRead(path)) {
      actualHash = BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "");
    }

    if (!string.Equals(actualHash, expectedHash.Trim(), StringComparison.OrdinalIgnoreCase)) {
      throw new InstallFailedException(
        $"The downloaded {displayName} does not match its published hash. "
        + "The download may have been corrupted or modified. Try again later.");
    }
  }

  /// <summary>
  /// Throws unless the file has a valid Authenticode signature from Microsoft Corporation.
  /// This is used for files for which Microsoft does not publish a hash, such as dotnet-install.ps1.
  /// </summary>
  public static void VerifyMicrosoftSignature(string path, string displayName) {
    var result = ProcessRunner.RunPowerShell(
      $"$signature = Get-AuthenticodeSignature -LiteralPath '{path.Replace("'", "''")}'; "
      + "$signature.Status.ToString(); $signature.SignerCertificate.Subject"
    );

    var lines = result.OutputLines.Select(line => line.Trim()).ToArray();
    var isValid = result.Succeeded && lines.Length >= 2 && lines[0] == "Valid";
    var isFromMicrosoft = isValid && lines[1]
      .Split(',')
      .Select(part => part.Trim())
      .Contains("O=Microsoft Corporation", StringComparer.OrdinalIgnoreCase);

    if (!isFromMicrosoft) {
      throw new InstallFailedException(
        $"The downloaded {displayName} does not have a valid signature from Microsoft. "
        + "The download may have been corrupted or modified. Try again later.");
    }
  }
}
