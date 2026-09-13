using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace RAWeb.Server.Installer.Setup;

public static class RdpSigningCertificateManager {
  public const string StoreName = "My";

  /// <summary>
  /// Grants the IIS application pool identity read access to a certificate's private key so RAWeb
  /// (running as that identity) can use it to sign RDP files.
  /// </summary>
  public static void GrantPrivateKeyAccess(string thumbprint, string appPoolName, InstallLog log) {
    using var store = new X509Store(StoreName, StoreLocation.LocalMachine);
    store.Open(OpenFlags.ReadOnly);
    var certificate = store.Certificates
      .Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false)
      .OfType<X509Certificate2>()
      .FirstOrDefault();

    if (certificate is null) {
      log.Warning($"Could not find the web site's certificate ('{thumbprint}') to grant RDP signing access.");
      return;
    }

    var keyFilePath = FindPrivateKeyFilePath(certificate);
    if (keyFilePath is null) {
      log.Warning(
        $"Could not locate the private key file for '{certificate.FriendlyName}'. " +
        $"If RDP file signing does not work, grant 'IIS AppPool\\{appPoolName}' read access to its private key manually via the Local Computer Certificates MMC snap-in (All Tasks » Manage Private Keys…)."
      );
      return;
    }

    try {
      var account = new NTAccount("IIS AppPool\\" + appPoolName);
      var sid = (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));

      var fileSecurity = File.GetAccessControl(keyFilePath);
      fileSecurity.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.Read, AccessControlType.Allow));
      File.SetAccessControl(keyFilePath, fileSecurity);
    }
    catch (Exception exception) when (exception is IdentityNotMappedException or UnauthorizedAccessException or IOException) {
      log.Warning($"Could not grant 'IIS AppPool\\{appPoolName}' access to the web site certificate's private key: {exception.Message}");
    }
  }

  private static string? FindPrivateKeyFilePath(X509Certificate2 certificate) {
    var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

    if (certificate.GetRSAPrivateKey() is RSACng cngKey) {
      var cngPath = Path.Combine(commonAppData, "Microsoft", "Crypto", "Keys", cngKey.Key.UniqueName);
      if (File.Exists(cngPath)) {
        return cngPath;
      }
    }

    if (certificate.GetRSAPrivateKey() is RSACryptoServiceProvider cspKey) {
      var cspPath = Path.Combine(commonAppData, "Microsoft", "Crypto", "RSA", "MachineKeys", cspKey.CspKeyContainerInfo.UniqueKeyContainerName);
      if (File.Exists(cspPath)) {
        return cspPath;
      }
    }

    return null;
  }
}
