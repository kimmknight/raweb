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
  /// If the resource cannot be resolved, returns a FailedResourceResult with the appropriate HTTP status code and optional error message.
  /// </summary>
  /// <param name="userInfo"></param>
  /// <param name="path"></param>
  /// <param name="from"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentException"></exception>
  /// <exception cref="Exception"></exception>
  public static ResourceResult ResolveResource(UserInformation userInfo, string path, string from) {
    // ensure the parameters are valid formats
    if (from != "rdp" && from != "registry" && from != "mr" && from != "registryDesktop") {
      throw new ArgumentException("Parameter 'from' must be either 'rdp', 'mr', 'registry', or 'registryDesktop'.");
    }

    // if the path starts with App_Data/, remove that part
    if (path.StartsWith("App_Data/", StringComparison.OrdinalIgnoreCase)) {
      path = path.Substring("App_Data/".Length);
    }

    int permissionHttpStatus;
    bool hasPermission;
    // if it is an RDP file, serve it from the file system
    if (from == "rdp") {
      var root = Constants.AppDataFolderPath;
      var filePath = Path.Combine(root, string.Format("{0}", path));
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
    if (from == "mr") {
      var rootedPath = Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, path));
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
    if (from == "registryDesktop") {
      // ensure the path is a valid registry key name
      if (path.Contains("\\") || path.Contains("/")) {
        return new FailedResourceResult(HttpStatusCode.BadRequest, "When 'from' is 'registryDesktop', 'path' must be the name of the registry key, not a file path.");
      }

      var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
      var centralizedPublishingCollectionName = AppId.ToCollectionName();
      if (!supportsCentralizedPublishing) {
        throw new Exception("Centralized Publishing is not enabled on this server.");
      }

      var desktopResource = SystemDesktop.FromRegistry(centralizedPublishingCollectionName, path);
      if (desktopResource is null) {
        return new FailedResourceResult(HttpStatusCode.NotFound);
      }

      // check that the user has permission to access the remoteapp in the registry
      var registryKey = Registry.LocalMachine.OpenSubKey(desktopResource.collectionDesktopsRegistryPath + "\\" + path);
      if (registryKey is null) {
        return new FailedResourceResult(HttpStatusCode.NotFound);
      }
      hasPermission = RegistryReader.CanAccessRemoteApp(registryKey, userInfo, out permissionHttpStatus);
      if (!hasPermission) {
        return new FailedResourceResult((HttpStatusCode)permissionHttpStatus);
      }

      // construct an RDP file from the values in the registry and return it
      return new ResolvedResourceResult(HttpStatusCode.OK, RegistryReader.ConstructRdpFileFromRegistry(path, isDesktop: true), path + ".rdp");
    }

    // ensure the path is a valid registry key name
    if (path.Contains("\\") || path.Contains("/")) {
      return new FailedResourceResult(HttpStatusCode.BadRequest, "When 'from' is 'registry', 'path' must be the name of the registry key, not a file path.");
    }

    // check that the user has permission to access the remoteapp in the registry
    hasPermission = RegistryReader.CanAccessRemoteApp(path, userInfo, out permissionHttpStatus);
    if (!hasPermission) {
      return new FailedResourceResult((HttpStatusCode)permissionHttpStatus);
    }

    // construct an RDP file from the values in the registry and return it
    return new ResolvedResourceResult(HttpStatusCode.OK, RegistryReader.ConstructRdpFileFromRegistry(path), path + ".rdp");
  }

}
