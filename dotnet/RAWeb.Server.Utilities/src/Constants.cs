using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

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

      if (!Directory.Exists(appDataFolderPath)) {
        Directory.CreateDirectory(appDataFolderPath);
      }

      return Path.GetFullPath(appDataFolderPath);
    }
  }

  /// <summary>
  /// The full path to the assets folder.
  /// </summary>
  public static string AssetsFolderPath {
    get {
      return "resource://static/lib/assets";
    }
  }

  public static string ManagedResourcesFolderPath {
    get {
      var managedResourcesFolderPath = Path.Combine(AppDataFolderPath, "managed-resources");
      return Path.GetFullPath(managedResourcesFolderPath);
    }
  }

  /// <summary>
  /// Gets the Terminal Server full address (IP:port) for RDP connections.
  /// </summary>
  [Obsolete("Use the 'Constants.GetTerminalServerFullAddress' method instead")]
  public static string TerminalServerFullAddress {
    get {
      return GetTerminalServerFullAddress();
    }
  }

  public static string GetTerminalServerFullAddress(Microsoft.AspNetCore.Http.HttpContext? httpContext = null) {
    var fulladdress = PoliciesManager.RawPolicies["RegistryApps.FullAddressOverride"];
    if (fulladdress is not null && !string.IsNullOrEmpty(fulladdress)) {
      return fulladdress;
    }

    // get the machine's IP address
    var ipAddress = $"{Environment.MachineName}.local";
    if (httpContext is not null) {
      var foundIpAddress = httpContext.Connection.LocalIpAddress?.ToString();
      if (!string.IsNullOrEmpty(foundIpAddress) && foundIpAddress != "::1") {
        ipAddress = foundIpAddress;
      }
    }
    else {
      Console.WriteLine("Warning: HttpContext is null. Using machine name as IP address.");
    }

    // get the rdp port from HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp
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

    return fulladdress;
  }


  public const string DefaultAuthCookieName = ".ASPXAUTH";
  public const string SudoAuthCookieName = ".ASPXAUTH_SUDO";

  private static readonly Lock s_assemblyLookupLock = new();

  /// <summary>
  /// The assembly that contains the embedded static frontend resources (resource names beginning with "static/").
  /// This is almost always the "raweb" assembly, regardless of whether it is the entry assembly.
  /// If the raweb assembly is not loaded, this method will fall back to and loaded
  /// assembly with "static/" embedded resources.
  /// <br /><br />
  /// This value is cached after the first lookup.
  /// </summary>
  public static Assembly? ServerResourceAssembly {
    get {
      if (field != null) {
        return field;
      }

      // look up the assembly, but only allow one thread at a time
      lock (s_assemblyLookupLock) {
        // if another thread has already found the assembly while we
        // were waiting for the lock, return it
        if (field != null) {
          return field;
        }

        var entry = Assembly.GetEntryAssembly();
        if (entry?.GetName().Name == "raweb") {
          field = entry;
          return field;
        }

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
          if (asm.GetName().Name == "raweb") {
            field = asm;
            return field;
          }
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
          if (assembly.GetManifestResourceNames().Any(name => name.StartsWith("static/"))) {
            field = assembly;
            return field;
          }
        }

        field = null;
        return field;
      }
    }
  }
}
