using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace RAWeb.Server.Utilities.Tests;

[NotInParallel]
public class RdpFileSignerTests {
  [Test]
  public async Task ContainsSignature_NoSignatureLine_ReturnsFalse() {
    var result = RdpFileSigner.ContainsSignature("full address:s:myserver\r\n");
    await Assert.That(result).IsFalse();
  }

  [Test]
  public async Task ContainsSignature_HasSignatureLine_ReturnsTrue() {
    var result = RdpFileSigner.ContainsSignature("full address:s:myserver\r\nsignature:s:AQAB\r\n");
    await Assert.That(result).IsTrue();
  }

  [Test]
  public async Task RemoveSignatureProperties_RemovesSignatureAndSignScope() {
    var result = RdpFileSigner.RemoveSignatureProperties("full address:s:myserver\r\nsignature:s:AQAB\r\nsignscope:s:Full Address\r\n");

    await Assert.That(result.Contains("full address:s:myserver")).IsTrue();
    await Assert.That(result.Contains("signature:s:")).IsFalse();
    await Assert.That(result.Contains("signscope:s:")).IsFalse();
  }

  [Test]
  public async Task TryGetSigningCertificate_UnknownThumbprint_ReturnsFalse() {
    var found = RdpFileSigner.TryGetSigningCertificate("0000000000000000000000000000000000000000", out var certificate);

    await Assert.That(found).IsFalse();
    await Assert.That(certificate).IsNull();
  }

  [Test]
  public async Task TryGetSigningCertificate_NullOrEmptyThumbprint_ReturnsFalse() {
    var foundForNull = RdpFileSigner.TryGetSigningCertificate(null, out _);
    var foundForEmpty = RdpFileSigner.TryGetSigningCertificate("", out _);

    await Assert.That(foundForNull).IsFalse();
    await Assert.That(foundForEmpty).IsFalse();
  }

  private static X509Certificate2 CreateTestCodeSigningCertificate() {
    using var key = RSA.Create(2048);
    var request = new CertificateRequest("CN=RAWeb Test Signing Certificate", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));
    request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.3", "Code Signing")], critical: false));
    var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
    return request.CreateSelfSigned(notBefore, notBefore.AddYears(1));
  }

  private static byte[] InstallTestCertificate(X509Certificate2 certificate) {
    var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var pfx = certificate.Export(X509ContentType.Pfx, password);
    var persisted = X509CertificateLoader.LoadPkcs12(pfx, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

    using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
    store.Open(OpenFlags.ReadWrite);
    store.Add(persisted);
    return persisted.GetCertHash();
  }

  private static void RemoveTestCertificate(byte[] certificateHash) {
    using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
    store.Open(OpenFlags.ReadWrite);
    foreach (var certificate in store.Certificates.Find(X509FindType.FindByThumbprint, Convert.ToHexString(certificateHash), validOnly: false)) {
      store.Remove(certificate);
    }
  }

  [Test]
  public async Task TryGetSigningCertificate_InstalledUsableCertificate_ReturnsTrue() {
    await Assert.That(Management.ElevatedPrivileges.Check()).IsTrue().Because("test requires elevated privileges to add/remove a certificate in the LocalMachine\\My store");

    using var certificate = CreateTestCodeSigningCertificate();
    var hash = InstallTestCertificate(certificate);
    var thumbprint = Convert.ToHexString(hash);

    try {
      var found = RdpFileSigner.TryGetSigningCertificate(thumbprint, out var resolved);

      await Assert.That(found).IsTrue();
      await Assert.That(resolved).IsNotNull();
      await Assert.That(resolved!.HasPrivateKey).IsTrue();
    }
    finally {
      RemoveTestCertificate(hash);
    }
  }

  [Test]
  public async Task Sign_OnlySignsKnownSignableProperties() {
    await Assert.That(Management.ElevatedPrivileges.Check()).IsTrue().Because("test requires elevated privileges to add/remove a certificate in the LocalMachine\\My store");

    using var certificate = CreateTestCodeSigningCertificate();
    var hash = InstallTestCertificate(certificate);
    var thumbprint = Convert.ToHexString(hash);

    try {
      RdpFileSigner.TryGetSigningCertificate(thumbprint, out var resolved);
      var signed = RdpFileSigner.Sign(
        "full address:s:myserver\r\nremoteapplicationname:s:My App\r\nunrelatedcustomsetting:s:untouched\r\n",
        resolved!);

      await Assert.That(signed.Contains("full address:s:myserver")).IsTrue();
      await Assert.That(signed.Contains("unrelatedcustomsetting:s:untouched")).IsTrue();
      await Assert.That(signed.Contains("signature:s:")).IsTrue();
      // an unsigned "alternate full address" must never be allowed to override the signed "full address"
      await Assert.That(signed.Contains("alternate full address:s:myserver")).IsTrue();

      // signscope lists only the signable properties actually present in the file in a fixed order
      await Assert.That(signed.Contains("signscope:s:Full Address,Alternate Full Address,RemoteApplicationName\r\n")).IsTrue();
    }
    finally {
      RemoveTestCertificate(hash);
    }
  }

  [Test]
  public async Task Sign_RedirectWebAuthnPresent_IsDeclaredInSignScope() {
    await Assert.That(Management.ElevatedPrivileges.Check()).IsTrue().Because("test requires elevated privileges to add/remove a certificate in the LocalMachine\\My store");

    using var certificate = CreateTestCodeSigningCertificate();
    var hash = InstallTestCertificate(certificate);
    var thumbprint = Convert.ToHexString(hash);

    try {
      RdpFileSigner.TryGetSigningCertificate(thumbprint, out var resolved);
      var signed = RdpFileSigner.Sign("full address:s:myserver\r\nredirectwebauthn:i:1\r\n", resolved!);

      await Assert.That(signed.Contains("redirectwebauthn:i:1")).IsTrue();
      await Assert.That(signed.Contains("RedirectWebAuthn")).IsTrue();
    }
    finally {
      RemoveTestCertificate(hash);
    }
  }

  /// <summary>
  /// "enablerdsaadauth" was confirmed signable the same way "redirectwebauthn" was: signing a test
  /// file with Windows' own rdpsign.exe on a current build produces "signscope:s:...,
  /// EnableRdsAadAuth,RedirectWebAuthn" - it sits between EventLogUploadAddress and
  /// RedirectWebAuthn. Also not part of the 2015-era rdpsign.py reference this implementation
  /// started from.
  /// </summary>
  [Test]
  public async Task Sign_EnableRdsAadAuthPresent_IsDeclaredInSignScope() {
    await Assert.That(Management.ElevatedPrivileges.Check()).IsTrue().Because("test requires elevated privileges to add/remove a certificate in the LocalMachine\\My store");

    using var certificate = CreateTestCodeSigningCertificate();
    var hash = InstallTestCertificate(certificate);
    var thumbprint = Convert.ToHexString(hash);

    try {
      RdpFileSigner.TryGetSigningCertificate(thumbprint, out var resolved);
      var signed = RdpFileSigner.Sign("full address:s:myserver\r\nenablerdsaadauth:i:1\r\n", resolved!);

      await Assert.That(signed.Contains("enablerdsaadauth:i:1")).IsTrue();
      await Assert.That(signed.Contains("EnableRdsAadAuth")).IsTrue();
    }
    finally {
      RemoveTestCertificate(hash);
    }
  }

  [Test]
  public async Task Sign_DuplicatePropertyLines_OnlyLastOccurrenceIsUsedAndSignScopeHasNoDuplicates() {
    await Assert.That(Management.ElevatedPrivileges.Check()).IsTrue().Because("test requires elevated privileges to add/remove a certificate in the LocalMachine\\My store");

    using var certificate = CreateTestCodeSigningCertificate();
    var hash = InstallTestCertificate(certificate);
    var thumbprint = Convert.ToHexString(hash);

    try {
      RdpFileSigner.TryGetSigningCertificate(thumbprint, out var resolved);
      var signed = RdpFileSigner.Sign(
        "full address:s:myserver\r\nremoteapplicationmode:i:0\r\nremoteapplicationmode:i:1\r\n",
        resolved!);

      var signscopeLine = signed.Split(["\r\n"], StringSplitOptions.None).First(line => line.StartsWith("signscope:s:"));
      var names = signscopeLine["signscope:s:".Length..].Split(',');
      await Assert.That(names.Length).IsEqualTo(names.Distinct().Count());

      // the last occurrence's value should win
      await Assert.That(signed.Contains("remoteapplicationmode:i:1")).IsTrue();
      await Assert.That(signed.Contains("remoteapplicationmode:i:0")).IsFalse();
    }
    finally {
      RemoveTestCertificate(hash);
    }
  }

  [Test]
  public async Task Sign_PropertyAbsentFromFile_IsNotDeclaredInSignScope() {
    await Assert.That(Management.ElevatedPrivileges.Check()).IsTrue().Because("test requires elevated privileges to add/remove a certificate in the LocalMachine\\My store");

    using var certificate = CreateTestCodeSigningCertificate();
    var hash = InstallTestCertificate(certificate);
    var thumbprint = Convert.ToHexString(hash);

    try {
      RdpFileSigner.TryGetSigningCertificate(thumbprint, out var resolved);
      // "alternate shell" is signable but not present here, which means it should not be in the signscope
      var signed = RdpFileSigner.Sign("full address:s:myserver\r\n", resolved!);

      await Assert.That(signed.Contains("Alternate Shell")).IsFalse();
    }
    finally {
      RemoveTestCertificate(hash);
    }
  }

  /// <summary>
  /// rdpsign.exe uses a specific format for the signature
  /// </summary>
  [Test]
  public async Task Sign_ProducesSignatureOverTheRdpSignMessageFormat() {
    await Assert.That(Management.ElevatedPrivileges.Check()).IsTrue().Because("test requires elevated privileges to add/remove a certificate in the LocalMachine\\My store");

    using var certificate = CreateTestCodeSigningCertificate();
    var hash = InstallTestCertificate(certificate);
    var thumbprint = Convert.ToHexString(hash);

    try {
      RdpFileSigner.TryGetSigningCertificate(thumbprint, out var resolved);
      var signed = RdpFileSigner.Sign("full address:s:myserver\r\nremoteapplicationmode:i:1\r\n", resolved!);

      var signatureLine = signed
        .Split(["\r\n"], StringSplitOptions.None)
        .First(line => line.StartsWith("signature:s:"));
      var blob = Convert.FromBase64String(signatureLine["signature:s:".Length..].Replace(" ", ""));

      // 12 byte header: 0x00010001, 0x00000001, then the length of the CMS blob that follows
      await Assert.That(BitConverter.ToUInt32(blob, 0)).IsEqualTo(0x00010001u);
      await Assert.That(BitConverter.ToUInt32(blob, 4)).IsEqualTo(0x00000001u);
      await Assert.That(BitConverter.ToUInt32(blob, 8)).IsEqualTo((uint)(blob.Length - 12));

      // the base64 is emitted in 64 character chunks, each followed by two spaces
      await Assert.That(signatureLine.EndsWith("  ")).IsTrue();

      var expectedMessage = Encoding.Unicode.GetBytes(
        "full address:s:myserver\r\n" +
        "alternate full address:s:myserver\r\n" +
        "remoteapplicationmode:i:1\r\n" +
        "signscope:s:Full Address,Alternate Full Address,RemoteApplicationMode\r\n" +
        "\0");

      var cms = new System.Security.Cryptography.Pkcs.SignedCms(
        new System.Security.Cryptography.Pkcs.ContentInfo(expectedMessage), detached: true);
      cms.Decode(blob[12..]);
      cms.CheckSignature(verifySignatureOnly: true);

      var signer = cms.SignerInfos[0];
      await Assert.That(signer.DigestAlgorithm.Value).IsEqualTo("2.16.840.1.101.3.4.2.1"); // SHA-256
      await Assert.That(signer.SignedAttributes.Count).IsEqualTo(0);
      await Assert.That(cms.Certificates.Count).IsEqualTo(1);
    }
    finally {
      RemoveTestCertificate(hash);
    }
  }

  private static string RdpSignExePath => Path.Combine(Environment.SystemDirectory, "rdpsign.exe");

  /// <summary>
  /// rdpsign.exe only reads certificates from CurrentUser\My, so we must
  /// temporarily install a test certificate there in order to test our
  /// implementation against rdpsign.exe.
  /// </summary>
  private static X509Certificate2 CreateAndInstallCurrentUserCertificate() {
    using var generated = CreateTestCodeSigningCertificate();
    var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var pfx = generated.Export(X509ContentType.Pfx, password);
    var persisted = X509CertificateLoader.LoadPkcs12(pfx, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

    using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
    store.Open(OpenFlags.ReadWrite);
    store.Add(persisted);
    return persisted;
  }

  private static void RemoveCurrentUserCertificate(X509Certificate2 certificate) {
    using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
    store.Open(OpenFlags.ReadWrite);
    store.Remove(certificate);
  }

  /// <summary>
  /// Signs <paramref name="content"/> with Windows' own rdpsign.exe and returns the resulting file
  /// text. Note that rdpsign.exe outputs UTF-16LE wuth BOM, but RAWeb outputs UTF-8.
  /// </summary>
  private static string SignWithRdpSignExe(string content, X509Certificate2 certificate) {
    var tempFile = Path.Combine(Path.GetTempPath(), $"rdpsign-test-{Guid.NewGuid():N}.rdp");
    File.WriteAllText(tempFile, content, Encoding.UTF8);

    try {
      var startInfo = new ProcessStartInfo(RdpSignExePath) {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
      };
      startInfo.ArgumentList.Add("/sha256");
      startInfo.ArgumentList.Add(certificate.Thumbprint);
      startInfo.ArgumentList.Add(tempFile);

      using var process = Process.Start(startInfo)!;
      var stdout = process.StandardOutput.ReadToEnd();
      var stderr = process.StandardError.ReadToEnd();
      process.WaitForExit();

      if (process.ExitCode != 0) {
        throw new InvalidOperationException($"rdpsign.exe failed (exit code {process.ExitCode}): {stdout} {stderr}");
      }

      return File.ReadAllText(tempFile, Encoding.Unicode).TrimStart('\uFEFF');
    }
    finally {
      File.Delete(tempFile);
    }
  }

  private static string[] NormalizeLines(string content) =>
    content.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);

  /// <summary>
  /// Compares our implementation directly against rdpsign.exe.
  /// </summary>
  [Test]
  public async Task Sign_MatchesRdpSignExeOutput() {
    await Assert.That(File.Exists(RdpSignExePath)).IsTrue().Because("test requires rdpsign.exe to be present on this machine");

    var certificate = CreateAndInstallCurrentUserCertificate();
    try {
      const string content =
        "full address:s:myserver\r\n" +
        "remoteapplicationmode:i:1\r\n" +
        "remoteapplicationname:s:My App\r\n" +
        @"remoteapplicationprogram:s:C:\app.exe" + "\r\n" +
        "redirectclipboard:i:1\r\n" +
        "redirectwebauthn:i:1\r\n" +
        "unrelatedcustomsetting:s:untouched\r\n";

      var referenceOutput = SignWithRdpSignExe(content, certificate);
      var ourOutput = RdpFileSigner.Sign(content, certificate);

      var referenceLines = NormalizeLines(referenceOutput);
      var ourLines = NormalizeLines(ourOutput);

      const string expectedSignScope =
        "signscope:s:Full Address,Alternate Full Address,RemoteApplicationProgram,RemoteApplicationMode,RemoteApplicationName,RedirectClipboard,RedirectWebAuthn";

      var referenceSignScope = referenceLines.Single(line => line.StartsWith("signscope:s:"));
      var ourSignScope = ourLines.Single(line => line.StartsWith("signscope:s:"));
      await Assert.That(referenceSignScope).IsEqualTo(expectedSignScope);
      await Assert.That(ourSignScope).IsEqualTo(expectedSignScope);

      // every non-signature property line must match
      var referenceProperties = referenceLines
        .Where(line => !line.StartsWith("signscope:s:") && !line.StartsWith("signature:s:"))
        .OrderBy(line => line, StringComparer.Ordinal);
      var ourProperties = ourLines
        .Where(line => !line.StartsWith("signscope:s:") && !line.StartsWith("signature:s:"))
        .OrderBy(line => line, StringComparer.Ordinal);
      await Assert.That(string.Join('\n', ourProperties)).IsEqualTo(string.Join('\n', referenceProperties));

      var expectedMessage = Encoding.Unicode.GetBytes(
        "full address:s:myserver\r\n" +
        "alternate full address:s:myserver\r\n" +
        @"remoteapplicationprogram:s:C:\app.exe" + "\r\n" +
        "remoteapplicationmode:i:1\r\n" +
        "remoteapplicationname:s:My App\r\n" +
        "redirectclipboard:i:1\r\n" +
        "redirectwebauthn:i:1\r\n" +
        expectedSignScope + "\r\n" +
        "\0");

      var referenceSignatureLine = referenceLines.Single(line => line.StartsWith("signature:s:"));
      var referenceBlob = Convert.FromBase64String(referenceSignatureLine["signature:s:".Length..].Replace(" ", ""));
      var referenceCms = new System.Security.Cryptography.Pkcs.SignedCms(
        new System.Security.Cryptography.Pkcs.ContentInfo(expectedMessage),
        detached: true
      );
      referenceCms.Decode(referenceBlob[12..]);
      referenceCms.CheckSignature(verifySignatureOnly: true);

      var ourSignatureLine = ourLines.Single(line => line.StartsWith("signature:s:"));
      var ourBlob = Convert.FromBase64String(ourSignatureLine["signature:s:".Length..].Replace(" ", ""));
      var ourCms = new System.Security.Cryptography.Pkcs.SignedCms(
        new System.Security.Cryptography.Pkcs.ContentInfo(expectedMessage),
        detached: true
      );
      ourCms.Decode(ourBlob[12..]);
      ourCms.CheckSignature(verifySignatureOnly: true);
    }
    finally {
      RemoveCurrentUserCertificate(certificate);
    }
  }
}
