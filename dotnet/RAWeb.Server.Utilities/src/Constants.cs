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


  public const string DefaultAuthCookieName = ".ASPXAUTH";
}
