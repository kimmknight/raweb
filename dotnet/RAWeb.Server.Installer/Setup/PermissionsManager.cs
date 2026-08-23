using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace RAWeb.Server.Installer.Setup;

public static class PermissionsManager {
  private const InheritanceFlags TreeInheritance = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;

  private static readonly SecurityIdentifier s_localSystem = new(WellKnownSidType.LocalSystemSid, null);
  private static readonly SecurityIdentifier s_administrators = new(WellKnownSidType.BuiltinAdministratorsSid, null);
  private static readonly SecurityIdentifier s_users = new(WellKnownSidType.BuiltinUsersSid, null);

  /// <summary>
  /// Applies proper file permissions to the installed files and directories.
  /// </summary>
  /// <param name="versionedDirectory"></param>
  /// <param name="appDataDirectory"></param>
  /// <param name="mainExecutablePath"></param>
  /// <param name="applicationPoolName"></param>
  public static void Apply(string versionedDirectory, string appDataDirectory, string mainExecutablePath, string applicationPoolName) {
    var appPoolIdentity = new NTAccount($"IIS AppPool\\{applicationPoolName}");

    // The install tree: inheritance off, full control for SYSTEM and Administrators, read for the poo
    // disable permissions inheritance on the RAWeb directory
    var treeSecurity = Directory.GetAccessControl(versionedDirectory);
    treeSecurity.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

    // allow full control for SYSTEM and Administrators
    treeSecurity.SetAccessRule(new FileSystemAccessRule(s_localSystem, FileSystemRights.FullControl, TreeInheritance, PropagationFlags.None, AccessControlType.Allow));
    treeSecurity.SetAccessRule(new FileSystemAccessRule(s_administrators, FileSystemRights.FullControl, TreeInheritance, PropagationFlags.None, AccessControlType.Allow));

    // grant read access to the RAWeb application pool identity
    treeSecurity.SetAccessRule(new FileSystemAccessRule(appPoolIdentity, FileSystemRights.Read, TreeInheritance, PropagationFlags.None, AccessControlType.Allow));
    Directory.SetAccessControl(versionedDirectory, treeSecurity);

    // additionally grant write access to the App_Data folder, which is
    // required for the policies web editor and resources manager
    GrantDirectory(appDataDirectory, appPoolIdentity, FileSystemRights.Write | FileSystemRights.Modify);

    // allow read access for the Users group for App_Data\resources since all users should have access to the resources by default
    GrantDirectory(Path.Combine(appDataDirectory, "resources"), s_users, FileSystemRights.Read, createIfMissing: true);

    // allow read access for the Users group for App_Data\inject\filestore since all users should have access to the filestore by default
    GrantDirectory(Path.Combine(appDataDirectory, "inject", "filestore"), s_users, FileSystemRights.Read, createIfMissing: true);

    // allow read and execute access to raweb.exe for the RAWeb application pool identity
    var executableSecurity = File.GetAccessControl(mainExecutablePath);
    executableSecurity.SetAccessRule(new FileSystemAccessRule(appPoolIdentity, FileSystemRights.ReadAndExecute, AccessControlType.Allow));
    File.SetAccessControl(mainExecutablePath, executableSecurity);

    // only allow the application pool identity and Administrators to read and modify the the DataProtection-Keys folder inside App_Data
    ApplyDataProtectionKeyPermissions(Path.Combine(appDataDirectory, "DataProtection-Keys"), appPoolIdentity);
  }

  /// <summary>
  /// Data protection keys decrypt every RAWeb auth cookie, so this directory drops inheritance and is
  /// readable only by the pool identity and Administrators.
  /// </summary>
  private static void ApplyDataProtectionKeyPermissions(string directory, NTAccount appPoolIdentity) {
    Directory.CreateDirectory(directory);

    var security = Directory.GetAccessControl(directory);
    security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
    security.SetAccessRule(new FileSystemAccessRule(appPoolIdentity, FileSystemRights.Modify, TreeInheritance, PropagationFlags.None, AccessControlType.Allow));
    security.SetAccessRule(new FileSystemAccessRule(s_administrators, FileSystemRights.FullControl, TreeInheritance, PropagationFlags.None, AccessControlType.Allow));
    Directory.SetAccessControl(directory, security);

    foreach (var subdirectory in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories)) {
      var subdirectorySecurity = Directory.GetAccessControl(subdirectory);
      subdirectorySecurity.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
      subdirectorySecurity.SetAccessRule(new FileSystemAccessRule(appPoolIdentity, FileSystemRights.Modify, TreeInheritance, PropagationFlags.None, AccessControlType.Allow));
      subdirectorySecurity.SetAccessRule(new FileSystemAccessRule(s_administrators, FileSystemRights.FullControl, TreeInheritance, PropagationFlags.None, AccessControlType.Allow));
      Directory.SetAccessControl(subdirectory, subdirectorySecurity);
    }

    foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories)) {
      var fileSecurity = File.GetAccessControl(file);
      fileSecurity.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
      fileSecurity.SetAccessRule(new FileSystemAccessRule(appPoolIdentity, FileSystemRights.Modify, AccessControlType.Allow));
      fileSecurity.SetAccessRule(new FileSystemAccessRule(s_administrators, FileSystemRights.FullControl, AccessControlType.Allow));
      File.SetAccessControl(file, fileSecurity);
    }
  }

  private static void GrantDirectory(string directory, IdentityReference identity, FileSystemRights rights, bool createIfMissing = false) {
    if (createIfMissing || !Directory.Exists(directory)) {
      Directory.CreateDirectory(directory);
    }

    var security = Directory.GetAccessControl(directory);
    security.SetAccessRule(new FileSystemAccessRule(identity, rights, TreeInheritance, PropagationFlags.None, AccessControlType.Allow));
    Directory.SetAccessControl(directory, security);
  }
}
