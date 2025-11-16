using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

namespace RAWeb.Server.Management;

public sealed class UserHiveReader : IDisposable {
  private string? TemporaryDatFilePath { get; init; }
  private string HiveMountName { get; init; }
  private MountLocation HiveMountLocation { get; init; }

  /// <summary>
  /// Initializes a new instance of the <see cref="UserHiveReader"/> class
  /// for the specified user SID.
  /// <br /><br />
  /// This class makes a copy of the user's NTUSER.DAT file to a temporary location
  /// and loads it as a registry hive under HKEY_LOCAL_MACHINE if the hive is not
  /// already loaded. Be sure to call <see cref="Dispose"/> to unload the hive
  /// and delete the temporary file when done.
  /// </summary>
  /// <param name="sid"></param>
  /// <exception cref="FileNotFoundException"></exception>
  /// <exception cref="Exception"></exception>
  public UserHiveReader(SecurityIdentifier sid) {
    var userProfiles = new SystemUserProfiles();
    var profile = userProfiles.FirstOrDefault(p => p.Sid.Equals(sid));
    if (profile == null) {
      throw new FileNotFoundException("User profile not found for SID " + sid.Value);
    }

    // ensure we can see NTUSER.DAT
    var userDatPath = Path.Combine(profile.ProfilePath, "NTUSER.DAT");
    if (!File.Exists(userDatPath)) {
      throw new FileNotFoundException("NTUSER.DAT not found for SID " + sid.Value);
    }

    // check if the hive is already loaded
    var existingHives = Registry.Users.GetSubKeyNames();
    if (existingHives.Contains(profile.Sid.Value)) {
      // use the existing loaded hive
      HiveMountName = profile.Sid.Value;
      HiveMountLocation = MountLocation.Users;
      TemporaryDatFilePath = null;
      return;
    }

    // since we need to load the hive, we need to grant ourselves the necessary privileges
    ElevatedPrivileges.Require();
    BackupAndRestorePrivilegesHelper.EnableBackupAndRestorePrivileges();

    // make a copy of the user's NTUSER.DAT file to a temporary location inside the user's folder
    var randomString = Guid.NewGuid().ToString("N");
    HiveMountName = "RAWeb_Temp_Hive_" + randomString;
    TemporaryDatFilePath = Path.Combine(profile.ProfilePath, HiveMountName + ".dat");
    File.Copy(userDatPath, TemporaryDatFilePath);

    // load the hive
    var loadResult = RegLoadKey(HKEY_LOCAL_MACHINE, HiveMountName, TemporaryDatFilePath);
    if (loadResult != 0) {
      Dispose();
      throw new Exception("Failed to load user hive for SID " + sid.Value + ". Error code: " + loadResult);
    }
    HiveMountLocation = MountLocation.LocalMachine;
  }

  enum MountLocation {
    Users,
    LocalMachine
  }

  /// <summary>
  /// Opens a subkey in the loaded user hive.
  /// </summary>
  /// <param name="subKeyPath"></param>
  /// <param name="writable"></param>
  /// <returns></returns>
  public RegistryKey? OpenSubKey(string subKeyPath) {
    var fullPath = HiveMountName + "\\" + subKeyPath;
    if (HiveMountLocation == MountLocation.Users) {
      return Registry.Users.OpenSubKey(fullPath, writable: false);
    }
    else {
      return Registry.LocalMachine.OpenSubKey(fullPath, writable: false);
    }
  }

  public void Dispose() {
    // unload the hive if we loaded it
    if (HiveMountLocation == MountLocation.LocalMachine) {
      try {
        RegUnLoadKey(HKEY_LOCAL_MACHINE, HiveMountName);
      }
      catch { }
    }

    // remove the copy of the user's NTUSER.DAT from the file system
    if (TemporaryDatFilePath is not null) {
      try {
        if (File.Exists(TemporaryDatFilePath)) {
          File.Delete(TemporaryDatFilePath);
        }
      }
      catch { }
    }
  }

  [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
  private static extern int RegLoadKey(UIntPtr hKey, string lpSubKey, string lpFile);

  [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
  private static extern int RegUnLoadKey(UIntPtr hKey, string lpSubKey);

#pragma warning disable IDE1006 // Naming Styles
  private static readonly UIntPtr HKEY_LOCAL_MACHINE = new(0x80000002u);
#pragma warning restore IDE1006 // Naming Styles
}

public static class RegistryKeyExtensions {
  public static string? GetFullPath(this RegistryKey key) {
    var field = typeof(RegistryKey).GetField("keyName", BindingFlags.NonPublic | BindingFlags.Instance);
    return field?.GetValue(key) as string;
  }
}
