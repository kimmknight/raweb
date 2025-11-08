using System;
using System.IO;

namespace RAWeb.Server.Utilities;

public static class AppId {
  static readonly string s_appIdText = "The name of this file is a unique identifier for this RAWeb Server installation.\n\nRenaming, deleting, or editing this file may cause data loss.\n";

  /// <summary>
  /// Initializes the application ID by ensuring a .appid file exists in App_Data.
  /// If none exists, a new one is created with a new GUID as its filename.
  /// If more than one .appid file exists, an exception is thrown.
  /// </summary>
  /// <returns></returns>
  /// <exception cref="Exception"></exception>
  public static void Initialize() {
    var appDataPath = Constants.AppDataFolderPath;
    var existing = Directory.GetFiles(appDataPath, "*.appid");

    // ensure only one .appid file exists
    if (existing.Length > 1) {
      throw new Exception("There should only be one .appid file in App_Data.");
    }

    // if it exists, ensure its contents are correct
    if (existing.Length == 1) {
      var contents = File.ReadAllText(existing[0]);
      if (contents != s_appIdText) {
        // replace with correct contents
        File.WriteAllText(existing[0], s_appIdText);
      }
    }

    // create a new .appid file if none exists
    if (existing.Length == 0) {
      var newId = Guid.NewGuid();
      var appIdPath = Path.Combine(appDataPath, newId + ".appid");
      File.WriteAllText(appIdPath, s_appIdText);
      Console.WriteLine("Created new App ID: " + newId);
    }
  }

  /// <summary>
  /// Gets the application ID by reading the .appid file name in App_Data.
  /// </summary>
  /// <returns></returns>
  /// <exception cref="Exception"></exception>
  public static Guid ToGuid() {
    var appDataPath = Constants.AppDataFolderPath;
    var existing = Directory.GetFiles(appDataPath, "*.appid");

    if (existing.Length == 0) {
      throw new Exception("No .appid file found in App_Data. Ensure AppId.Initialize() has been called.");
    }

    var fileName = Path.GetFileNameWithoutExtension(existing[0]);
    return Guid.Parse(fileName);
  }

  /// <summary>
  /// Gets the application ID as a collection name for use as the centralized publishing collection name.
  /// <br />
  /// See HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\CentralPublishedResources.
  /// </summary>
  /// <returns></returns>
  public static string ToCollectionName() {
    return "RAWEB-" + ToGuid().ToString();
  }
}
