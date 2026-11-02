using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using RAWeb.Server.Management;

namespace RAWeb.Server.Utilities;

public static class FileAccessInfo {
    public static bool CanAccessPath(string path, UserInformation? userInfo, out int httpStatus) {
        httpStatus = 200;

        // if the user information is null, deny access
        // (servers with anonymous authentication will set ISUR as the user info)
        if (userInfo == null) {
            httpStatus = 401;
            return false;
        }

        // always allow any png files from the assets folder
        // (this must be checked before normalizing the path because resource:// paths
        // are embedded resource names, not file system paths)
        if (path.StartsWith(Constants.AssetsFolderPath, StringComparison.OrdinalIgnoreCase) && path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        // use full path to avoid relative path traversal attacks
        try {
            path = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) {
            httpStatus = 400;
            return false;
        }

        // always allow access to default.ico
        if (path.Equals(Path.GetFullPath(Constants.AppRoot + "default.ico"), StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        // If the path is for multiuser-resources, we need to check permissions based
        // on folder name for the user/group. Otherwise, we check with the security descriptor.
        var multiuserUserRoot = Path.Combine(Constants.AppDataFolderPath, "multiuser-resources", "user");
        var multiuserGroupRoot = Path.Combine(Constants.AppDataFolderPath, "multiuser-resources", "group");
        var managedResourcesRoot = Path.Combine(Constants.AppDataFolderPath, "managed-resources");

        var method = "secdesc";
        if (TryGetFirstChildFolderName(multiuserUserRoot, path, out var username)) {
            method = "username:" + username;
        }
        else if (TryGetFirstChildFolderName(multiuserGroupRoot, path, out var groupName)) {
            method = "group:" + groupName;
        }
        else if (IsInFolder(multiuserUserRoot, path) || IsInFolder(multiuserGroupRoot, path)) {
            // paths directly inside the user or group folders do not belong to any user or group
            httpStatus = 403;
            return false;
        }

        // If the path is for managed-resources, we need to check the embedded security descriptor
        // in the managed resource metadata.
        else if (IsInFolder(managedResourcesRoot, path)) {
            method = "mgrsc";
        }

        // check whether the authenticated user is the user in the path
        if (method.StartsWith("username:")) {
            var pathUsername = method.Substring("username:".Length);
            if (pathUsername != userInfo.Username) {
                httpStatus = 403;
                return false;
            }

            // confirm that the user does not have read permissions denied
            // (but allow non-explicit allow permissions because the folder name counts as an implicit allow)
            try {
                var access = GetAccessInfo(path, userInfo);
                if (access.Denied) {
                    httpStatus = 403;
                    return false;
                }
                return true;
            }
            catch (FileNotFoundException) {
                // if the path is invalid, deny access
                httpStatus = 404;
                return false;
            }
        }

        // check whether the authenticated user is a member of the group in the path
        if (method.StartsWith("group:")) {
            var pathGroupNameOrSid = method.Substring("group:".Length);
            var isGroupMember = userInfo.Groups.Any(g => g.Name is not null && g.Name.Equals(pathGroupNameOrSid, StringComparison.OrdinalIgnoreCase) || g.Sid.Equals(pathGroupNameOrSid, StringComparison.OrdinalIgnoreCase));
            if (!isGroupMember) {
                httpStatus = 403;
                return false;
            }

            // confirm that the user does not have read permissions denied
            // (but allow non-explicit allow permissions because the folder name counts as an implicit allow)
            try {
                var access = GetAccessInfo(path, userInfo);
                if (access.Denied) {
                    httpStatus = 403;
                    return false;
                }
                return true;
            }
            catch (FileNotFoundException) {
                // if the path is invalid, deny access
                httpStatus = 404;
                return false;
            }
        }

        // check the security descriptor for the managed resource
        if (method == "mgrsc") {
            try {
                var managedResource = ManagedFileResource.FromResourceFile(path);

                // if there is no security descriptor, allow access
                if (managedResource.SecurityDescriptor == null) {
                    return true;
                }

                // otherwise, check if the user or their groups are in the allowed SIDs
                var userSid = new SecurityIdentifier(userInfo.Sid);
                var groupSids = userInfo.Groups.Select(g => new SecurityIdentifier(g.Sid)).ToArray();
                if (managedResource.SecurityDescriptor.CanRead(userSid, groupSids)) {
                    return true;
                }
                else {
                    httpStatus = 403;
                    return false;
                }
            }
            catch (FileNotFoundException) {
                httpStatus = 404;
                return false;
            }
            catch (InvalidDataException) { // the file was not a valid .resource file
                httpStatus = 404;
                return false;
            }
        }

        // check the security descriptor for the path
        if (method == "secdesc") {
            try {
                var access = GetAccessInfo(path, userInfo);

                // give priority to denial rules - if any deny rule matches, access is denied
                if (access.Denied) {
                    httpStatus = 403;
                    return false;
                }

                if (!access.Allowed) {
                    httpStatus = 403;
                }
                return access.Allowed;
            }
            catch (FileNotFoundException) {
                // if the path is invalid, deny access
                httpStatus = 404;
                return false;
            }
        }

        return false;
    }

    public static bool CanAccessPath(string path, UserInformation userInfo) {
        return CanAccessPath(path, userInfo, out _);
    }

    /// <summary>
    /// A version of <see cref="CanAccessPath(string, UserInformation, out int)"/>
    /// that can also handle paths that start with "resource://".
    /// Use this check whenever it is possible for a path to be a resource:// path.
    /// </summary>
    public static bool CanAccessServedImage(string? servedImagePath, UserInformation userInfo, out int httpStatus) {
        httpStatus = 200;
        if (servedImagePath is null || servedImagePath.StartsWith("resource://", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }
        return CanAccessPath(servedImagePath, userInfo, out httpStatus);
    }

    extension(Path) {
        /// <summary>
        /// Determines if the candidate full path is inside the specified root folder.
        /// <br /><br />
        /// For example, IsInFolder("C:\App_Data\managed-resources", "C:\App_Data\managed-resources\file.resource") returns true,
        /// while IsInFolder("C:\App_Data\managed-resources", "C:\App_Data\managed-resources_evil\file.resource") returns false.
        /// <br /><br />
        /// This method requires that both the root and candidate paths are rooted (absolute) paths.
        /// </summary>
        /// <exception cref="ArgumentException">When at least one path is not rooted.</exception>
        public static bool IsInFolder(string root, string candidate) {
            if (!Path.IsPathRooted(root) || !Path.IsPathRooted(candidate)) {
                throw new ArgumentException("Both root and candidate paths must be rooted.");
            }

            var rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Gets the name of the first folder inside the root folder that contains the candidate full path.
    /// <br /><br />
    /// For example, TryGetFirstSegmentInFolder("C:\App_Data\multiuser-resources\user", "C:\App_Data\multiuser-resources\user\jdoe\file.rdp")
    /// returns true with segment "jdoe".
    /// </summary>
    private static bool TryGetFirstChildFolderName(string root, string candidate, out string segment) {
        segment = "";
        if (!IsInFolder(root, candidate)) {
            return false;
        }

        var rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var segments = candidate.Substring(rootWithSeparator.Length).Split(Path.DirectorySeparatorChar);
        if (segments.Length < 2 || string.IsNullOrWhiteSpace(segments[0]) || string.IsNullOrWhiteSpace(segments[1])) {
            return false;
        }

        segment = segments[0];
        return true;
    }

    private static List<FileSystemAccessRule> GetAccessRules(string path, UserInformation userInfo) {
        // get the security info for the path
        FileSystemSecurity? security = null;
        if (File.Exists(path)) {
            security = new FileSecurity(path, AccessControlSections.Access);
        }
        else if (Directory.Exists(path)) {
            security = new DirectorySecurity(path, AccessControlSections.Access);
        }

        if (security == null) {
            throw new FileNotFoundException("The specified path does not exist.", path);
        }

        // get the rules from the security descriptor's discretionary access control list (DACL)
        var accessRules = security.GetAccessRules(true, true, typeof(NTAccount)).Cast<AccessRule>().OfType<FileSystemAccessRule>().ToList();

        return accessRules;
    }

    private static AccessInfo GetAccessInfo(List<FileSystemAccessRule> accessRules, UserInformation userInfo) {
        // get the security identifiers for the user and their groups
        var userSid = new SecurityIdentifier(userInfo.Sid);
        var groupSids = userInfo.Groups.Select(g => new SecurityIdentifier(g.Sid)).ToList();
        var allSids = new List<SecurityIdentifier> { userSid };
        allSids.AddRange(groupSids);

        // check for the presence of access denied and access allowed rules
        var readDenied = accessRules
            .Where(rule => rule.AccessControlType == AccessControlType.Deny)
            .Where(rule => (rule.FileSystemRights & FileSystemRights.Read) == FileSystemRights.Read)
            .Any(rule => allSids.Any(sid => sid.Equals(rule.IdentityReference.Translate(typeof(SecurityIdentifier)))));
        var readAllowed = accessRules
            .Where(rule => rule.AccessControlType == AccessControlType.Allow)
            .Where(rule => (rule.FileSystemRights & FileSystemRights.Read) == FileSystemRights.Read)
            .Any(rule => allSids.Any(sid => sid.Equals(rule.IdentityReference.Translate(typeof(SecurityIdentifier)))));

        return new AccessInfo(readAllowed, readDenied);
    }

    private static AccessInfo GetAccessInfo(string path, UserInformation userInfo) {
        // get the access rules from the security descriptor's discretionary access control list (DACL)
        var accessRules = GetAccessRules(path, userInfo);

        return GetAccessInfo(accessRules, userInfo);
    }

    private class AccessInfo(bool allowed, bool denied) {
        public bool Allowed { get; set; } = allowed;
        public bool Denied { get; set; } = denied;
    }
}

public class FileSystemInitializer {
    /// <summary>
    /// Checks if the App_Data folder exists, and if it does not, it
    /// creates the App_Data folder and populates it with the default contents
    /// stored in the raweb assembly's embedded resources.
    /// <br /><br />
    /// If the App_Data folder already exists, but the folder is missing any
    /// of the default contents, the missing contents will also be placed
    /// in the existing App_Data folder. Existing files will not be overwritten.
    /// </summary>
    /// <exception cref="Exception"></exception>
    public static void EnsureAppDataFolderContents() {
        var assembly = Constants.ServerResourceAssembly;

        if (assembly == null) {
            throw new Exception("ServerResourceAssembly is missing. Make sure that this program has access to raweb.exe or raweb.dll.");
        }

        // ensure that the default app data files are all present
        assembly.GetManifestResourceNames()
            .Where(x => x.StartsWith("defaultappdata/"))
            .Select(resourceName => {
                var fileName = resourceName.Substring("defaultappdata/".Length);
                var filePath = Path.Combine(Constants.AppDataFolderPath, fileName);
                return (resourceName, filePath);
            })
            .Where(info => !File.Exists(info.filePath))
            .ToList()
            .ForEach(info => {
                var (resourceName, filePath) = info;

                // create the directory if it does not exist
                var directory = Path.GetDirectoryName(filePath);
                if (directory is not null && !Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }

                // copy the embedded resource to the file system
                using var resourceStream = assembly.GetManifestResourceStream(resourceName);
                if (resourceStream is not null) {
                    using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                    resourceStream.CopyTo(fileStream);
                }
            });

        // ensure that the app id file is in the folder
        AppId.Initialize();
    }
}
