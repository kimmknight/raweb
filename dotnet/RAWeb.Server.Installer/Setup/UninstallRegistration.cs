using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

namespace RAWeb.Server.Installer.Setup;

public sealed record UninstallRegistrationRequest(
  string InstallDirectory,
  string VersionedDirectory,
  string WebSite,
  string VirtualPath,
  string ApplicationPoolName,
  string ServiceName,
  string Version,
  string Publisher,
  string MainExecutablePath,
  int SitePort);

/// <summary>
/// Writes the uninstall script and the Add/Remove Programs entry.
/// </summary>
public static class UninstallRegistration {
  private const string UninstallRoot = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
  private const string TemplateResourceName = "RAWeb.Server.Installer.Resources.uninstall.template.ps1";

  public static string WriteUninstallScript(UninstallRegistrationRequest request) {
    var displayName = Naming.DisplayName(request.WebSite, request.VirtualPath);
    var registryKeyName = Naming.UninstallRegistryKeyName(request.WebSite, request.VirtualPath);

    var replacements = new Dictionary<string, string> {
      ["GENERATED"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
      ["DISPLAY_NAME"] = displayName,
      ["DISPLAY_NAME_ESCAPED"] = EscapeForDoubleQuotedPowerShell(displayName),
      ["INSTALL_DIR"] = request.InstallDirectory,
      ["INSTALL_DIR_ESCAPED"] = EscapeForDoubleQuotedPowerShell(request.InstallDirectory),
      ["WEB_SITE"] = request.WebSite,
      ["WEB_SITE_ESCAPED"] = EscapeForDoubleQuotedPowerShell(request.WebSite),
      ["VIRTUAL_PATH"] = request.VirtualPath,
      ["VIRTUAL_PATH_ESCAPED"] = EscapeForDoubleQuotedPowerShell(request.VirtualPath),
      ["APP_POOL"] = request.ApplicationPoolName,
      ["SERVICE_NAME"] = request.ServiceName,
      ["REG_KEY_NAME"] = registryKeyName,
      ["SITE_PORT"] = request.SitePort.ToString(),
    };

    var content = LoadTemplate();
    foreach (var replacement in replacements) {
      content = content.Replace("{{" + replacement.Key + "}}", replacement.Value);
    }

    var uninstallPath = Path.Combine(request.InstallDirectory, "uninstall.ps1");
    Directory.CreateDirectory(request.InstallDirectory);
    File.WriteAllText(uninstallPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

    return uninstallPath;
  }

  public static void WriteRegistryEntry(UninstallRegistrationRequest request, string uninstallScriptPath) {
    var displayName = Naming.DisplayName(request.WebSite, request.VirtualPath);
    var registryKeyName = Naming.UninstallRegistryKeyName(request.WebSite, request.VirtualPath);

    var appDataPrefix = Path.Combine(request.VersionedDirectory, "App_Data");
    var sizeInBytes = FileOperations.DirectorySize(
      request.VersionedDirectory,
      file => !file.StartsWith(appDataPrefix, StringComparison.OrdinalIgnoreCase)
    );

    using var key = Registry.LocalMachine.CreateSubKey($@"{UninstallRoot}\{registryKeyName}")
      ?? throw new InstallFailedException($"Could not create the uninstall registry key '{registryKeyName}'.");

    key.SetValue("DisplayName", displayName);
    key.SetValue("DisplayVersion", request.Version);
    key.SetValue("Publisher", request.Publisher);
    key.SetValue("DisplayIcon", request.MainExecutablePath + ",0");
    key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
    key.SetValue("InstallLocation", request.InstallDirectory);
    key.SetValue("UninstallString", $"powershell -ExecutionPolicy Bypass -File \"{uninstallScriptPath}\"");
    key.SetValue("NoModify", 1, RegistryValueKind.DWord);
    key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    key.SetValue("EstimatedSize", (int)Math.Round(sizeInBytes / 1024d), RegistryValueKind.DWord);
  }

  public static void RemoveRegistryEntry(string webSite, string virtualPath) {
    var registryKeyName = Naming.UninstallRegistryKeyName(webSite, virtualPath);
    Registry.LocalMachine.DeleteSubKeyTree($@"{UninstallRoot}\{registryKeyName}", throwOnMissingSubKey: false);
  }

  private static string LoadTemplate() {
    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TemplateResourceName)
      ?? throw new InstallFailedException($"The embedded resource '{TemplateResourceName}' is missing from the installer.");
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
  }

  private static string EscapeForDoubleQuotedPowerShell(string value) => value.Replace("`", "``").Replace("\"", "`\"").Replace("$", "`$");
}
