using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using RAWeb.Server.Management;

namespace RAWeb.Server.Utilities;

/// <summary>
/// A .NET implementation of the uncompunted rdpsign.exe.
/// <br /><br />
/// This class is inspired by 
/// <see href="https://github.com/nfedera/rdpsign/blob/master/rdpsign.py">rdpsign.py</see>
/// and verified by comparing its output to rdpsign.exe's output on the same input files.
/// <br /><br />
/// Only the fixed set of properties below can be signed.
/// It may silently grow on newer Windows builds (e.g. "redirectwebauthn" is a newer property).
/// A property outside this list is never part of the signature.
/// </summary>
public static class RdpFileSigner {

  public static readonly (string Prefix, string DisplayName)[] SignableProperties = RdpSignableProperties.All;

  private static readonly string[] s_lineSeparators = ["\r\n", "\n"];

  /// <summary>
  /// Writes to App_Data/logs/rdp-signing_&lt;date&gt;.log.
  /// </summary>
  private static readonly Logger s_diagnosticsLogger = new("rdp-signing");

  public static bool ContainsSignature(string content) => RdpSignableProperties.ContainsSignature(content);

  /// <summary>
  /// Removes any existing "signscope:s:" and "signature:s:" lines from the RDP file contents.
  /// </summary>
  public static string RemoveSignatureProperties(string content) {
    var builder = new StringBuilder();
    foreach (var line in SplitLinesRaw(content)) {
      if (line.StartsWith("signscope:s:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("signature:s:", StringComparison.OrdinalIgnoreCase)) {
        continue;
      }
      builder.AppendLine(line);
    }
    return builder.ToString().TrimEnd() + Environment.NewLine;
  }

  /// <summary>
  /// Looks up the signing certificate in the LocalMachine\My store by thumbprint, and confirms it
  /// is currently usable for signing (has an accessible private key and isn't expired). Never
  /// throws - any failure (missing certificate, no private key, expired, etc.) simply means
  /// signing is unavailable.
  /// </summary>
  public static bool TryGetSigningCertificate(string? thumbprint, out X509Certificate2? certificate) {
    certificate = null;
    if (string.IsNullOrWhiteSpace(thumbprint)) {
      s_diagnosticsLogger.WriteLogline("Not signing: RDP.SigningThumbprint is not set.");
      return false;
    }

    var normalizedThumbprint = new string([.. thumbprint.Where(Uri.IsHexDigit)]); // strip out colons, spaces, and invisible characters
    if (normalizedThumbprint.Length == 0) {
      s_diagnosticsLogger.WriteLogline($"Not signing: RDP.SigningThumbprint ('{thumbprint}') contains no hex digits.");
      return false;
    }

    try {
      // look inside LocalMachine\My
      using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
      store.Open(OpenFlags.ReadOnly);

      var now = DateTime.Now;
      var candidates = store.Certificates
        .Find(X509FindType.FindByThumbprint, normalizedThumbprint, validOnly: false)
        .OfType<X509Certificate2>()
        .ToArray();

      if (candidates.Length == 0) {
        var allThumbprints = string.Join(", ", store.Certificates.Cast<X509Certificate2>().Select(c => c.Thumbprint));
        s_diagnosticsLogger.WriteLogline($"Not signing: no certificate with thumbprint '{normalizedThumbprint}' was found in LocalMachine\\My. ");
        return false;
      }

      foreach (var candidate in candidates) {
        if (!candidate.HasPrivateKey) {
          s_diagnosticsLogger.WriteLogline($"Not signing: certificate '{normalizedThumbprint}' has no private key attached.");
          continue;
        }
        if (candidate.NotBefore > now || candidate.NotAfter <= now) {
          s_diagnosticsLogger.WriteLogline($"Not signing: certificate '{normalizedThumbprint}' is not currently valid (NotBefore={candidate.NotBefore:o}, NotAfter={candidate.NotAfter:o}, now={now:o}).");
          continue;
        }

        System.Security.Cryptography.RSA? rsa;
        try {
          rsa = candidate.GetRSAPrivateKey();
        }
        catch (Exception exception) {
          s_diagnosticsLogger.WriteLogline($"Not signing: could not access the private key for certificate '{normalizedThumbprint}'. {exception.GetType().Name}: {exception.Message}. The application pool identity may need private key access explicitly granted via the Certificates MMC snap-in.");
          continue;
        }

        if (rsa is null) {
          s_diagnosticsLogger.WriteLogline($"Not signing: certificate '{normalizedThumbprint}' does not expose an RSA private key.");
          continue;
        }

        certificate = candidate;
        return true;
      }

      return false;
    }
    catch (Exception exception) {
      s_diagnosticsLogger.WriteLogline($"Not signing: failed to look up certificate '{normalizedThumbprint}'. {exception.GetType().Name}: {exception.Message}");
      return false;
    }
  }

  /// <summary>
  /// Same as <see cref="Sign"/> except it never throws.
  /// </summary>
  public static string? TrySign(string content, X509Certificate2 certificate) {
    try {
      var signed = Sign(content, certificate);
      s_diagnosticsLogger.WriteLogline($"Signed with certificate '{certificate.Thumbprint}'.");
      return signed;
    }
    catch (Exception exception) {
      s_diagnosticsLogger.WriteLogline($"Not signing: signing with certificate '{certificate.Thumbprint}' failed - {exception.GetType().Name}: {exception.Message}. The application pool identity may need private key access granted via the Certificates MMC snap-in (Manage Private Keys...).");
      return null;
    }
  }

  /// <summary>
  /// Signs the RDP file contents, replacing any existing "signscope:s:" and "signature:s:" lines with
  /// a freshly computed signature based on the signable properties present in the file.
  /// </summary>
  public static string Sign(string content, X509Certificate2 certificate) {
    var lines = content
      .TrimEnd('\r', '\n')
      .Split(s_lineSeparators, StringSplitOptions.None)
      .Select(line => line.Trim())
      .ToList();

    var settings = new List<string>();

    foreach (var line in lines) {
      if (line.StartsWith("signature:s:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("signscope:s:", StringComparison.OrdinalIgnoreCase)) {
        continue;
      }
      settings.Add(line);
    }

    // remove duplicate properties (keep the last one) since it may interfere with generating a valid signature
    settings = settings
      .GroupBy(line => line.Split(':', 2)[0], StringComparer.OrdinalIgnoreCase)
      .Select(group => group.Last())
      .ToList();

    var fullAddress = settings
      .FirstOrDefault(setting => setting.StartsWith("full address:s:", StringComparison.OrdinalIgnoreCase))
      ?[15..];
    var alternateFullAddress = settings
      .FirstOrDefault(setting => setting.StartsWith("alternate full address:s:", StringComparison.OrdinalIgnoreCase))
      ?[25..];

    // prevent a full address signed by RAWeb from being bypassed via an
    // attacker-supplied "alternate full address"
    if (!string.IsNullOrEmpty(fullAddress) && string.IsNullOrEmpty(alternateFullAddress)) {
      settings.Add("alternate full address:s:" + fullAddress);
    }

    var signLines = new List<string>();
    var signNames = new List<string>();
    foreach (var (prefix, displayName) in SignableProperties) {
      foreach (var setting in settings) {
        if (setting.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
          signNames.Add(displayName);
          signLines.Add(setting);
        }
      }
    }

    var messageText = string.Join("\r\n", signLines) + "\r\n" + "signscope:s:" + string.Join(",", signNames) + "\r\n" + "\0";
    var messageBytes = Encoding.Unicode.GetBytes(messageText); // UTF-16LE

    var signatureValue = FormatSignatureValue(BuildSignatureBlob(messageBytes, certificate));

    var output = new StringBuilder();
    output.Append(string.Join("\r\n", settings)).Append("\r\n");
    output.Append("signscope:s:").Append(string.Join(",", signNames)).Append("\r\n");
    output.Append("signature:s:").Append(signatureValue).Append("\r\n");
    return output.ToString();
  }

  /// <summary>
  /// I'm not sure why we need spaces after every 64 base64 characters,
  /// but this is what rdpsign.exe does. -jackbuehner
  /// </summary>
  private static string FormatSignatureValue(byte[] blob) {
    const int chunkSize = 64;

    var base64 = Convert.ToBase64String(blob);
    var builder = new StringBuilder(base64.Length + (base64.Length / chunkSize + 1) * 2);
    for (var offset = 0; offset < base64.Length; offset += chunkSize) {
      builder.Append(base64, offset, Math.Min(chunkSize, base64.Length - offset)).Append("  ");
    }

    return builder.ToString();
  }

  /// <summary>
  /// Signs the given bytes with the given certificate using the SHA-256 digest algorithm
  /// and returns a signature blob in the format expected by RDP files.
  /// <br /><br />
  /// In particular, the header with 0x00010001/0x00000001/length is generated
  /// based on the input message bytes and prepending to the signature.
  /// </summary>
  private static byte[] BuildSignatureBlob(byte[] messageBytes, X509Certificate2 certificate) {
    var contentInfo = new ContentInfo(messageBytes);
    var cms = new SignedCms(contentInfo, detached: true);
    var signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate) {
      DigestAlgorithm = new System.Security.Cryptography.Oid("2.16.840.1.101.3.4.2.1"), // SHA-256
      IncludeOption = X509IncludeOption.EndCertOnly,
    };
    cms.ComputeSignature(signer, silent: true);
    var derBytes = cms.Encode();

    var blob = new byte[12 + derBytes.Length];
    BinaryPrimitives.WriteUInt32LittleEndian(blob.AsSpan(0, 4), 0x00010001);
    BinaryPrimitives.WriteUInt32LittleEndian(blob.AsSpan(4, 4), 0x00000001);
    BinaryPrimitives.WriteUInt32LittleEndian(blob.AsSpan(8, 4), (uint)derBytes.Length);
    derBytes.CopyTo(blob, 12);
    return blob;
  }

  private static string[] SplitLinesRaw(string content) => content.Split(s_lineSeparators, StringSplitOptions.None);
}
