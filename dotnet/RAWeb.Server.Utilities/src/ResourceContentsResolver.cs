using System;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Win32;
using RAWeb.Server.Management;

namespace RAWeb.Server.Utilities;

public sealed class ResourceContentsResolver {
  public abstract record ResourceResult(HttpStatusCode PermissionHttpStatus);
  public record FailedResourceResult(HttpStatusCode PermissionHttpStatus, string? ErrorMessage = null) : ResourceResult(PermissionHttpStatus);
  public record ResolvedResourceResult(HttpStatusCode PermissionHttpStatus, string RdpFileContents, string FileName) : ResourceResult(PermissionHttpStatus);

  /// <summary>
  /// Resolves a resource path to an RDP file content and filename.
  /// <br/>
  /// If the resource cannot be resolved, returns a FailedResourceResult with the appropriate HTTP status code and optional error message.
  /// <br/><br/>
  /// File paths are validated to ensure that they do not escape the allowed directories.<br/>
  /// File paths should be provided as relative paths within the App_Data folder.<br/>
  /// For RDP files, the path must be within App_Data/resources or App_Data/multiuser-resources.<br/>
  /// Within multiuser-resources, the path must include the user or group folder (e.g. App_Data/multiuser-resources/user/&lt;username&gt;/...).<br/>
  /// For managed .resource files, the path must be within App_Data/managed-resources.
  /// <br/><br/>
  /// When 'from' is 'registry' or 'registryDesktop', the 'path' parameter must be the name of the registry key, not a file path.
  /// </summary>
  /// <param name="userInfo"></param>
  /// <param name="path"></param>
  /// <param name="from"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentException"></exception>
  /// <exception cref="Exception"></exception>
  public static ResourceResult ResolveResource(UserInformation userInfo, string path, ResourceOrigin from) {
    // if the path starts with App_Data/, remove that part
    if (path.StartsWith("App_Data/", StringComparison.OrdinalIgnoreCase)) {
      path = path.Substring("App_Data/".Length);
    }

    // Determines if the candidate path is inside the specified root folder.
    // For example, IsInFolder("C:\App_Data\resources", "C:\App_Data\resources\file.rdp") returns true,
    // while IsInFolder("C:\App_Data\resources", "C:\App_Data\resources_evil\file.rdp") returns false.
    static bool IsInFolder(string root, string candidate) {
      var rootFull = Path
        .GetFullPath(root)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        + Path.DirectorySeparatorChar;  // ensure trailing slash/backslash
      return candidate.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase);
    }

    int permissionHttpStatus;
    bool hasPermission;
    // if it is an RDP file, serve it from the file system
    if (from == ResourceOrigin.Rdp) {
      var root = Constants.AppDataFolderPath;
      var filePath = Path.GetFullPath(Path.Combine(root, path));

      // black access to paths outside of the App_Data/resources and App_Data/multiuser-resources folders
      string[] allowedPathRoots = [
        Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, "resources")),
        Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, "multiuser-resources")),
      ];
      if (!allowedPathRoots.Any(allowedRoot => IsInFolder(allowedRoot, filePath))) {
        return new FailedResourceResult(HttpStatusCode.Forbidden, "Access to the specified path is not allowed.");
      }

      // for multiuser resources, ensure the path includes the user or group folder (e.g. App_Data/multiuser-resources/user/<username>/**)
      if (filePath.Contains("multiuser-resources")) {
        var segmentsAfterMultiuser = filePath
          .Split(["multiuser-resources"], StringSplitOptions.None)[1]
          .TrimStart(Path.DirectorySeparatorChar)
          .Split(Path.DirectorySeparatorChar);
        var firstSegmentIsApprovedType = segmentsAfterMultiuser.Length >= 1 &&
                                          (segmentsAfterMultiuser[0] == "user" || segmentsAfterMultiuser[0] == "group");
        var containsUserOrGroupName = segmentsAfterMultiuser.Length >= 2 &&
                                        !string.IsNullOrWhiteSpace(segmentsAfterMultiuser[1]);
        if (!firstSegmentIsApprovedType || !containsUserOrGroupName) {
          return new FailedResourceResult(HttpStatusCode.BadRequest, "For multiuser resources, the path must include the user or group folder after 'multiuser-resources'.");
        }
      }

      // for legacy purposes, the path passed in might not include the .rdp extensionq
      if (!filePath.EndsWith(".rdp", StringComparison.OrdinalIgnoreCase)) {
        filePath += ".rdp";
      }

      if (!File.Exists(filePath)) {
        return new FailedResourceResult(HttpStatusCode.NotFound);
      }

      // check that the user has permission to access the RDP file
      hasPermission = FileAccessInfo.CanAccessPath(filePath, userInfo, out permissionHttpStatus);
      if (!hasPermission) {
        return new FailedResourceResult((HttpStatusCode)permissionHttpStatus);
      }

      // resolve the RDP file
      return new ResolvedResourceResult(HttpStatusCode.OK, File.ReadAllText(filePath), Path.GetFileName(filePath));
    }

    // if it is a managed .resource file, serve it from the file system
    if (from == ResourceOrigin.ManagedResource) {
      var rootedPath = Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, path));

      // block access to paths outside of the App_Data/managed-resources folder
      var allowedRoot = Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, "managed-resources"));
      if (!IsInFolder(allowedRoot, rootedPath)) {
        return new FailedResourceResult(HttpStatusCode.Forbidden, "Access to the specified path is not allowed.");
      }

      var resource = ManagedFileResource.FromResourceFile(rootedPath);
      if (resource == null) {
        return new FailedResourceResult(HttpStatusCode.NotFound);
      }

      // check that the user has permission to access the managed resource file
      hasPermission = resource.SecurityDescriptor == null ||
                      resource.SecurityDescriptor.GetAllowedSids().Any(sid => userInfo.Sid == sid.ToString() || userInfo.Groups.Any(g => g.Sid == sid.ToString()));
      if (!hasPermission) {
        return new FailedResourceResult((HttpStatusCode)403);
      }

      // resolve the RDP file
      return new ResolvedResourceResult(HttpStatusCode.OK, resource.ToRdpFileStringBuilder().ToString(), Path.GetFileNameWithoutExtension(path) + ".rdp");
    }

    // if it is a registry desktop, construct the RDP file from the registry
    if (from == ResourceOrigin.RegistryDesktop) {
      // ensure the path is a valid registry key name
      if (path.Contains('\\') || path.Contains('/')) {
        return new FailedResourceResult(HttpStatusCode.BadRequest, "When 'from' is 'registryDesktop', 'path' must be the name of the registry key, not a file path.");
      }
      var desktopKeyName = path;

      var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
      var centralizedPublishingCollectionName = AppId.ToCollectionName();
      if (!supportsCentralizedPublishing) {
        throw new Exception("Centralized Publishing is not enabled on this server.");
      }

      var desktopResource = SystemDesktop.FromRegistry(centralizedPublishingCollectionName, desktopKeyName);
      if (desktopResource is null) {
        return new FailedResourceResult(HttpStatusCode.NotFound);
      }

      // check that the user has permission to access the desktop in the registry
      var registryKey = Registry.LocalMachine.OpenSubKey(desktopResource.collectionDesktopsRegistryPath + "\\" + desktopKeyName);
      if (registryKey is null) {
        return new FailedResourceResult(HttpStatusCode.NotFound);
      }
      hasPermission = RegistryReader.CanAccessRemoteApp(registryKey, userInfo, out permissionHttpStatus);
      if (!hasPermission) {
        return new FailedResourceResult((HttpStatusCode)permissionHttpStatus);
      }

      // construct an RDP file from the values in the registry and return it
      return new ResolvedResourceResult(HttpStatusCode.OK, RegistryReader.ConstructRdpFileFromRegistry(desktopKeyName, isDesktop: true), desktopKeyName + ".rdp");
    }

    // ensure the path is a valid registry key name
    if (path.Contains("\\") || path.Contains("/")) {
      return new FailedResourceResult(HttpStatusCode.BadRequest, "When 'from' is 'registry', 'path' must be the name of the registry key, not a file path.");
    }
    var appKeyName = path;

    // check that the user has permission to access the remoteapp in the registry
    hasPermission = RegistryReader.CanAccessRemoteApp(appKeyName, userInfo, out permissionHttpStatus);
    if (!hasPermission) {
      return new FailedResourceResult((HttpStatusCode)permissionHttpStatus);
    }

    // construct an RDP file from the values in the registry and return it
    return new ResolvedResourceResult(HttpStatusCode.OK, RegistryReader.ConstructRdpFileFromRegistry(appKeyName), appKeyName + ".rdp");
  }
}
