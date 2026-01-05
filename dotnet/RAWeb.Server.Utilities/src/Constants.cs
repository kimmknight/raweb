using System;
using System.IO;

namespace RAWeb.Server.Utilities;

public sealed class Constants {
  private const string AppDataFolderName = "App_Data";

  /// <summary>
  /// The root folder of the application.
  /// </summary>
  public static string AppRoot = AppContext.BaseDirectory;

  /// <summary>
  /// The full path to the App_Data folder.
  /// </summary>
  public static string AppDataFolderPath {
    get {
      var appDataFolderPath = Path.Combine(AppRoot, AppDataFolderName);
      return Path.GetFullPath(appDataFolderPath);
    }
  }

  /// <summary>
  /// The full path to the assets folder.
  /// </summary>
  public static string AssetsFolderPath {
    get {
      var assetsFolderPath = Path.Combine(AppRoot, "lib", "assets");
      return Path.GetFullPath(assetsFolderPath);
    }
  }

  public static string ManagedResourcesFolderPath {
    get {
      var managedResourcesFolderPath = Path.Combine(AppDataFolderPath, "managed-resources");
      return Path.GetFullPath(managedResourcesFolderPath);
    }
  }

  /// <summary>
  /// The fuil path to dockerd.exe, if present.
  /// </summary>
  public static string? DockerDaemonPath {
    get {
      var dockerDaemonFolderPath = Path.Combine(AppRoot, "bin", "dockerd.exe");
      if (!File.Exists(dockerDaemonFolderPath)) {
        return null;
      }
      return Path.GetFullPath(dockerDaemonFolderPath);
    }
  }

  /// <summary>
  /// The full path to docker.exe, if present.
  /// </summary>
  public static string? DockerCliPath {
    get {
      var dockerCliFolderPath = Path.Combine(AppRoot, "bin", "docker.exe");
      if (!File.Exists(dockerCliFolderPath)) {
        return null;
      }
      return Path.GetFullPath(dockerCliFolderPath);
    }
  }

  /// <summary>
  /// Gets the Terminal Server full address (IP:port) for RDP connections.
  /// </summary>
  public static string TerminalServerFullAddress {
    get {
      var fulladdress = PoliciesManager.RawPolicies["RegistryApps.FullAddressOverride"];
      if (fulladdress is null || string.IsNullOrEmpty(fulladdress)) {
        // get the machine's IP address
#if NET462
        var ipAddress = System.Web.HttpContext.Current.Request.ServerVariables["LOCAL_ADDR"];
        if (ipAddress == "::1") {
          ipAddress = $"{Environment.MachineName}.local";
        }
#else
        var ipAddress = $"{Environment.MachineName}.local";
#endif

        // get the rdp port  from HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp
        var rdpPort = "";
        using (var rdpKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp")) {
          if (rdpKey != null) {
            var portValue = rdpKey.GetValue("PortNumber");
            if (portValue != null) {
              rdpPort = ((int)portValue).ToString();
            }
          }
        }

        // construct the full address
        fulladdress = ipAddress + ":" + rdpPort;
      }

      return fulladdress;
    }
  }


  public const string DefaultAuthCookieName = ".ASPXAUTH";
}
