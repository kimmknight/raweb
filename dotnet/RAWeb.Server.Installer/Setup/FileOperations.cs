using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace RAWeb.Server.Installer.Setup;

public static class FileOperations {
  private const AccessControlSections CopiedSections = AccessControlSections.Access;

  /// <summary>
  /// Recursively copies a directory tree. Data, attributes, timestamps, and the
  /// NTFS access rules are all carried across.
  /// </summary>
  /// <remarks>
  /// RAWeb decides which users may see which resources by reading the access rules on
  /// on each item under App_Data\resources, so a copy that dropped them would silently
  /// change resource visibility on every upgrade.
  /// </remarks>
  public static void CopyDirectory(string sourceDirectory, string destinationDirectory, CancellationToken cancellationToken) {
    sourceDirectory = Path.GetFullPath(sourceDirectory).TrimEnd(Path.DirectorySeparatorChar);
    destinationDirectory = Path.GetFullPath(destinationDirectory).TrimEnd(Path.DirectorySeparatorChar);

    Directory.CreateDirectory(destinationDirectory);
    CopyDirectoryMetadata(sourceDirectory, destinationDirectory);

    var sourceDirectories = Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories);

    foreach (var directory in sourceDirectories) {
      cancellationToken.ThrowIfCancellationRequested();
      var destination = Rebase(directory, sourceDirectory, destinationDirectory);
      Directory.CreateDirectory(destination);
      CopyDirectoryMetadata(directory, destination);
    }

    foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories)) {
      cancellationToken.ThrowIfCancellationRequested();
      CopyFile(file, Rebase(file, sourceDirectory, destinationDirectory));
    }

    // Directory timestamps have to be set last; writing the children updates them again.
    // This is similar to robocopy's /DCOPY:T.
    CopyDirectoryTimestamps(sourceDirectory, destinationDirectory);
    foreach (var directory in sourceDirectories) {
      CopyDirectoryTimestamps(directory, Rebase(directory, sourceDirectory, destinationDirectory));
    }
  }

  private static void CopyFile(string sourcePath, string destinationPath) {
    // We need to make sure that the destination is writable before we copy, so that
    // the copy succeeds and then we can apply the original attributes.
    if (File.Exists(destinationPath)) {
      File.SetAttributes(destinationPath, FileAttributes.Normal);
    }

    File.Copy(sourcePath, destinationPath, overwrite: true);

    // We store the original attributes so that we can reapply them after
    // we finish setting access rules and timestamps
    var attributes = File.GetAttributes(sourcePath);
    File.SetAttributes(destinationPath, FileAttributes.Normal);

    CopyAccessRules(
      () => File.GetAccessControl(sourcePath, CopiedSections),
      () => File.GetAccessControl(destinationPath, CopiedSections),
      security => File.SetAccessControl(destinationPath, security));

    File.SetCreationTimeUtc(destinationPath, File.GetCreationTimeUtc(sourcePath));
    File.SetLastWriteTimeUtc(destinationPath, File.GetLastWriteTimeUtc(sourcePath));
    File.SetLastAccessTimeUtc(destinationPath, File.GetLastAccessTimeUtc(sourcePath));

    File.SetAttributes(destinationPath, attributes);
  }

  private static void CopyDirectoryMetadata(string sourcePath, string destinationPath) {
    CopyAccessRules(
      () => Directory.GetAccessControl(sourcePath, CopiedSections),
      () => Directory.GetAccessControl(destinationPath, CopiedSections),
      security => Directory.SetAccessControl(destinationPath, security));

    new DirectoryInfo(destinationPath).Attributes = new DirectoryInfo(sourcePath).Attributes;
  }

  private static void CopyDirectoryTimestamps(string sourcePath, string destinationPath) {
    Directory.SetCreationTimeUtc(destinationPath, Directory.GetCreationTimeUtc(sourcePath));
    Directory.SetLastWriteTimeUtc(destinationPath, Directory.GetLastWriteTimeUtc(sourcePath));
    Directory.SetLastAccessTimeUtc(destinationPath, Directory.GetLastAccessTimeUtc(sourcePath));
  }

  /// <summary>
  /// Copies the explicit access rules and the inheritance-protection flag from source to destination.
  /// </summary>
  /// <remarks>
  /// Only explicit rules are carried over. Inherited rules are deliberately left behind so the copy
  /// re-inherits from its new parent instead of freezing the old parent's rules into place as explicit
  /// entries.
  /// </remarks>
  private static void CopyAccessRules<TSecurity>(
    Func<TSecurity> readSource,
    Func<TSecurity> readDestination,
    Action<TSecurity> writeDestination)
    where TSecurity : FileSystemSecurity {
    TSecurity source;
    TSecurity destination;

    try {
      source = readSource();
      destination = readDestination();
    }
    catch (Exception exception) when (exception is UnauthorizedAccessException or PrivilegeNotHeldException) {
      return;
    }

    var isProtected = source.AreAccessRulesProtected;
    destination.SetAccessRuleProtection(isProtected, preserveInheritance: !isProtected);

    var rules = source.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier));
    foreach (FileSystemAccessRule rule in rules) {
      destination.AddAccessRule(rule);
    }

    try {
      writeDestination(destination);
    }
    catch (Exception exception) when (exception is UnauthorizedAccessException or PrivilegeNotHeldException) {
    }
  }

  private static string Rebase(string path, string sourceRoot, string destinationRoot) =>
    destinationRoot + path.Substring(sourceRoot.Length);

  /// <summary>
  /// Sums the sizes of all files under <paramref name="directory"/>, optionally filtering by <paramref name="include"/>.
  /// </summary>
  /// <returns></returns>
  public static long DirectorySize(string directory, Func<string, bool>? include = null) {
    if (!Directory.Exists(directory)) {
      return 0;
    }

    return Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
      .Where(file => include is null || include(file))
      .Sum(file => new FileInfo(file).Length);
  }

  public static void DeleteDirectoryIfExists(string directory) {
    if (!Directory.Exists(directory)) {
      return;
    }

    try {
      Directory.Delete(directory, recursive: true);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) {
    }
  }

  /// <summary>
  /// Deletes <paramref name="startDirectory"/> and each empty ancestor, stopping at the first
  /// non-empty directory or the drive root so an upgrade never leaves empty folders behind.
  /// </summary>
  public static void RemoveEmptyAncestorDirectories(string startDirectory) {
    var current = startDirectory;

    while (!string.IsNullOrEmpty(current)) {
      if (!Directory.Exists(current)) {
        current = Path.GetDirectoryName(current);
        continue;
      }

      if (Directory.EnumerateFileSystemEntries(current).Any()) {
        return;
      }

      var parent = Path.GetDirectoryName(current);

      try {
        Directory.Delete(current);
      }
      catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) {
        return;
      }

      if (string.IsNullOrEmpty(parent) || parent!.Length <= 3) {
        return;
      }

      current = parent;
    }
  }

  public static bool IsUnderInetpub(string path) => path.TrimEnd('\\', '/').StartsWith(@"C:\inetpub", StringComparison.OrdinalIgnoreCase);
}
