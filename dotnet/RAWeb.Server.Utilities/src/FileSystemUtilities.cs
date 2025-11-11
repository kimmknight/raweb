using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using RAWeb.Server.Management;

namespace RAWeb.Server.Utilities;

public class FileAccessInfo {
    public static bool CanAccessPath(string path, UserInformation userInfo, out int httpStatus) {
        httpStatus = 200;

        // if the user information is null, deny access
        // (servers with anonymous authentication will set ISUR as the user info)
        if (userInfo == null) {
            httpStatus = 401;
            return false;
        }

        // if the path contains forward slashes, convert them to backslashes
        if (path.Contains('/')) {
            path = path.Replace('/', '\\');
        }

        // always allow access to default.ico
        if (path.Equals(Constants.AppRoot + "default.ico", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        // always allow any png files from lib/assets
        if (path.StartsWith(Constants.AssetsFolderPath, StringComparison.OrdinalIgnoreCase) && path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        // If the path includes multiuser-resources, we need to check permissions based
        // on folder name for the user/group. Otherwise, we check with the security descriptor.
        var method = "secdesc";
        if (path.Contains("multiuser-resources\\user")) {
            // extract the username from the path
            var startIndex = path.IndexOf("multiuser-resources\\user") + "multiuser-resources\\user".Length;
            var username = path.Substring(startIndex).TrimStart('\\').Split('\\')[0];

            method = "username:" + username;
        }
        if (path.Contains("multiuser-resources\\group")) {
            // extract the group name from the path
            var startIndex = path.IndexOf("multiuser-resources\\group") + "multiuser-resources\\group".Length;
            var groupName = path.Substring(startIndex).TrimStart('\\').Split('\\')[0];

            method = "group:" + groupName;
        }

        // If the path includes managed-resources, we need to check the embedded security descriptor
        // in the managed resource metadata.
        if (path.Contains("managed-resources")) {
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
                var allowedSids = managedResource.SecurityDescriptor.GetAllowedSids();
                var userSid = new SecurityIdentifier(userInfo.Sid);
                var groupSids = userInfo.Groups.Select(g => new SecurityIdentifier(g.Sid)).ToList();
                if (allowedSids.Any(sid => sid.Equals(userSid)) || groupSids.Any(gsid => allowedSids.Any(sid => sid.Equals(gsid)))) {
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
            // if the current user is anonymous, allow access
            if (userInfo.Sid == "S-1-4-447-1") {
                return true;
            }

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
