using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// Issues the self-signed certificate bound to the HTTPS binding.
/// </summary>
public static class CertificateManager {
  public const string StoreName = "My";
  public const string FriendlyName = "RAWeb Self-Signed Certificate";

  private const int KeySizeInBits = 2048;
  private const int ValidityInYears = 1;
  private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";

  /// <summary>
  /// Creates the certificate in LocalMachine\My and returns its thumbprint.
  /// </summary>
  public static byte[] CreateSelfSigned(InstallLog log) {
    var dnsNames = BuildSubjectNames();
    log.Detail($"Subject alternative names: {string.Join(", ", dnsNames)}");

    using var key = new RSACng(KeySizeInBits);

    var request = new CertificateRequest(
      $"CN={Environment.MachineName}",
      key,
      HashAlgorithmName.SHA256,
      RSASignaturePadding.Pkcs1
    );

    var subjectAlternativeNames = new SubjectAlternativeNameBuilder();
    foreach (var name in dnsNames) {
      subjectAlternativeNames.AddDnsName(name);
    }
    // also add 127.0.0.1 as a real IP subject alternative name, which some
    // browsers might prefer or require over the DNS name "127.0.0.1"
    subjectAlternativeNames.AddIpAddress(IPAddress.Loopback);

    request.CertificateExtensions.Add(subjectAlternativeNames.Build());
    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(
      certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true)
    );
    request.CertificateExtensions.Add(new X509KeyUsageExtension(
      X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true)
    );
    request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
      [new Oid(ServerAuthenticationOid, "Server Authentication")], critical: false)
    );

    var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
    using var generated = request.CreateSelfSigned(notBefore, notBefore.AddYears(ValidityInYears));

    return Persist(generated);
  }

  /// <summary>
  /// Moves the certificate into the machine store.
  /// </summary>
  private static byte[] Persist(X509Certificate2 certificate) {
    var password = GeneratePassword();

    try {
      var pfx = certificate.Export(X509ContentType.Pfx, password);

      var persisted = new X509Certificate2(
        pfx,
        password,
        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable) {
        FriendlyName = FriendlyName,
      };

      using var store = new X509Store(StoreName, StoreLocation.LocalMachine);
      store.Open(OpenFlags.ReadWrite);
      store.Add(persisted);

      return persisted.GetCertHash();
    }
    catch (CryptographicException exception) {
      throw new InstallFailedException($"Could not create the self-signed certificate: {exception.Message}", exception);
    }
  }

  private static IReadOnlyList<string> BuildSubjectNames() {
    var computerName = Environment.MachineName;
    var dnsDomain = Environment.GetEnvironmentVariable("USERDNSDOMAIN");
    if (string.IsNullOrWhiteSpace(dnsDomain)) {
      dnsDomain = "local";
    }

    string[] names = [
      computerName,
      computerName.ToLowerInvariant(),
      $"{computerName}.{dnsDomain}",
      $"{computerName}.{dnsDomain}".ToLowerInvariant(),
      "localhost",
      "127.0.0.1",
    ];

    // if for some reason the computer name is already a FQDN, this removes the duplicate entry
    return [.. names.Distinct(StringComparer.Ordinal)];
  }

  /// <summary>
  /// Generates a random password for the PFX file. The password is not used for anything else,
  /// but is required to export the certificate to a PFX file.
  /// </summary>
  private static string GeneratePassword() {
    var bytes = new byte[32];
    using (var random = RandomNumberGenerator.Create()) {
      random.GetBytes(bytes);
    }
    return Convert.ToBase64String(bytes);
  }
}
