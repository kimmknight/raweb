using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

// if launched with --install-cert, just add the embedded cert to the Trusted People store and exit.
if (args is ["--install-cert"]) {
  try {
    AddCertToTrustedPeople();
    return 0;
  }
  catch (Exception ex) {
    Console.Error.WriteLine($"Certificate installation failed: {ex.Message}");
    return 1;
  }
}


var cert = LoadEmbeddedCert();

// if the certificate is not already trusted, add it to the
// Trusted People store on the local machine
if (!IsCertTrusted(cert)) {
  if (IsAdmin()) {
    AddCertToTrustedPeople();
  }
  else {
    // Relaunch self with elevation to install the cert, then continue here.
    var elevated = Process.Start(new ProcessStartInfo {
      FileName = Environment.ProcessPath!,
      Arguments = "--install-cert",
      Verb = "runas",
      UseShellExecute = true,
    }) ?? throw new Exception("Failed to start elevated process.");

    elevated.WaitForExit();

    if (elevated.ExitCode != 0)
      throw new Exception($"Certificate installation failed (exit {elevated.ExitCode}).");
  }
}

// Extract the embedded MSIX to a temp file and open it in App Installer.
// UseShellExecute on a .msix should open it in App Installer.
var msixPath = ExtractToTemp("app.msix", "RAWeb.DesktopApp.msix");
Process.Start(new ProcessStartInfo(msixPath) { UseShellExecute = true });
return 0;

static X509Certificate2 LoadEmbeddedCert() {
  using var stream = GetResource("app.cer");
  using var ms = new MemoryStream();
  stream.CopyTo(ms);
  return X509CertificateLoader.LoadCertificate(ms.ToArray());
}

static bool IsCertTrusted(X509Certificate2 cert) {
  using var store = new X509Store(StoreName.TrustedPeople, StoreLocation.LocalMachine);
  try {
    store.Open(OpenFlags.ReadOnly);
    var found = store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, validOnly: false);
    return found.Count > 0;
  }
  catch {
    return false;
  }
}

static void AddCertToTrustedPeople() {
  var cert = LoadEmbeddedCert();
  using var store = new X509Store(StoreName.TrustedPeople, StoreLocation.LocalMachine);
  store.Open(OpenFlags.ReadWrite);
  store.Add(cert);
}

static string ExtractToTemp(string resourceName, string fileName) {
  var dest = Path.Combine(Path.GetTempPath(), fileName);
  using var src = GetResource(resourceName);
  using var dst = new FileStream(dest, FileMode.Create, FileAccess.Write);
  src.CopyTo(dst);
  return dest;
}

static Stream GetResource(string name) {
  var assembly = Assembly.GetExecutingAssembly();
  var stream = assembly.GetManifestResourceStream(name);
  if (stream == null) {
    throw new Exception($"Resource '{name}' not found.");
  }
  return stream;
}

static bool IsAdmin() {
  using var identity = WindowsIdentity.GetCurrent();
  return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
}
