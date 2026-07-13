using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Storage;

namespace RAWeb.DesktopApp;

static class AppStorage {
  /// <summary>
  /// Gets the appropriate storage folder for the application, depending on whether
  /// the app is running in packaged mode or unpackaged mode. In packaged mode, this
  /// will be the LocalFolder provided by the Windows APIs. In unpackaged mode, this
  /// will be a "LocalState" folder created in the same directory as the app executable.
  /// </summary>
  public static StorageFolder StorageFolder {
    get {
      // ApplicationData.Current throws InvalidOperationException when the app
      // has no package identity, so we must check IsPackaged first and avoid
      // touching ApplicationData.Current entirely when unpackaged.
      if (IsPackaged) {
        return ApplicationData.Current.LocalFolder;
      }
      else {
        var path = Path.Combine(AppContext.BaseDirectory, "LocalState");
        Directory.CreateDirectory(path);
        return StorageFolder.GetFolderFromPathAsync(path).GetAwaiter().GetResult();
      }
    }
  }

  /// <summary>
  /// Whether the app is running in packaged mode or unpackaged mode.
  /// </summary>
  public static bool IsPackaged => CurrentPackageFullName != null;

  /// <summary>
  /// The full name of the current package if the app is running in packaged
  /// mode, or null if the app is running in unpackaged mode.
  /// <br /><br />
  /// If you only need to know whether the app is packaged or unpackaged, consider
  /// using the <see cref="IsPackaged"/> property instead.
  /// </summary>
  public static string? CurrentPackageFullName {
    get {
      // Step 1. Find the length of the package identity string.
      //         We use ref so that Windows will directly modify
      //         the value of length.
      var length = 0;
      var result = GetCurrentPackageFullName(ref length, null);

      if (result == ERROR_INSUFFICIENT_BUFFER) {
        // Step 2. Allocate a StringBuilder with the length
        //         required for the package name.
        var sb = new StringBuilder(length);
        result = GetCurrentPackageFullName(ref length, sb);

        if (result == ERROR_SUCCESS) {
          return sb.ToString();
        }
      }

      return null;
    }
  }

  private const long ERROR_SUCCESS = 0x0;
  private const long ERROR_INSUFFICIENT_BUFFER = 0x7A;

  [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
  private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);
}
