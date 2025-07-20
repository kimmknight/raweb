using AuthUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;

namespace FileSystemUtilities
{
    public class Reader
    {
        public static bool CanAccessPath(string path, UserInformation userInfo, out int httpStatus)
        {
            httpStatus = 200;

            // if the user information is null, deny access
            // (servers with anonymous authentication will set ISUR as the user info)
            if (userInfo == null)
            {
                httpStatus = 401;
                return false;
            }

            // if the path contains forward slashes, convert them to backslashes
            if (path.Contains('/'))
            {
                path = path.Replace('/', '\\');
            }

            // If the path includes multiuser-resources, we need to check permissions based
            // on folder name for the user/group. Otherwise, we check with the security descriptor.
            string method = "secdesc";
            if (path.Contains("multiuser-resources\\user"))
            {
                // extract the username from the path
                int startIndex = path.IndexOf("multiuser-resources\\user") + "multiuser-resources\\user".Length;
                string username = path.Substring(startIndex).TrimStart('\\').Split('\\')[0];

                method = "username:" + username;
            }
            if (path.Contains("multiuser-resources\\group"))
            {
                // extract the group name from the path
                int startIndex = path.IndexOf("multiuser-resources\\group") + "multiuser-resources\\group".Length;
                string groupName = path.Substring(startIndex).TrimStart('\\').Split('\\')[0];

                method = "group:" + groupName;
            }

            // check whether the authenticated user is the user in the path
            if (method.StartsWith("username:"))
            {
                string pathUsername = method.Substring("username:".Length);
                if (pathUsername != userInfo.Username)
                {
                    httpStatus = 403;
                    return false;
                }

                // confirm that the user does not have read permissions denied
                // (but allow non-explicit allow permissions because the folder name counts as an implicit allow)
                try
                {
                    AccessInfo access = GetAccessInfo(path, userInfo);
                    if (access.Denied)
                    {
                        httpStatus = 403;
                        return false;
                    }
                    return true;
                }
                catch (FileNotFoundException)
                {
                    // if the path is invalid, deny access
                    httpStatus = 404;
                    return false;
                }
            }

            // check whether the authenticated user is a member of the group in the path
            if (method.StartsWith("group:"))
            {
                string pathGroupNameOrSid = method.Substring("group:".Length);
                bool isGroupMember = userInfo.Groups.Any(g => g.Name.Equals(pathGroupNameOrSid, StringComparison.OrdinalIgnoreCase) || g.Sid.Equals(pathGroupNameOrSid, StringComparison.OrdinalIgnoreCase));
                if (!isGroupMember)
                {
                    httpStatus = 403;
                    return false;
                }

                // confirm that the user does not have read permissions denied
                // (but allow non-explicit allow permissions because the folder name counts as an implicit allow)
                try
                {
                    AccessInfo access = GetAccessInfo(path, userInfo);
                    if (access.Denied)
                    {
                        httpStatus = 403;
                        return false;
                    }
                    return true;
                }
                catch (FileNotFoundException)
                {
                    // if the path is invalid, deny access
                    httpStatus = 404;
                    return false;
                }
            }

            // check the security descriptor for the path
            if (method == "secdesc")
            {
                // if the current user is IUSR, allow access
                if (userInfo.Sid == "S-1-5-17")
                {
                    return true;
                }

                try
                {
                    AccessInfo access = GetAccessInfo(path, userInfo);

                    // give priority to denial rules - if any deny rule matches, access is denied
                    if (access.Denied)
                    {
                        httpStatus = 403;
                        return false;
                    }

                    if (!access.Allowed)
                    {
                        httpStatus = 403;
                    }
                    return access.Allowed;
                }
                catch (FileNotFoundException)
                {
                    // if the path is invalid, deny access
                    httpStatus = 404;
                    return false;
                }
            }

            return false;
        }

        public static bool CanAccessPath(string path, UserInformation userInfo)
        {
            int httpStatus;
            return CanAccessPath(path, userInfo, out httpStatus);
        }

        private static List<FileSystemAccessRule> GetAccessRules(string path, UserInformation userInfo)
        {
            // get the security info for the path
            FileSystemSecurity security = null;
            if (File.Exists(path))
            {
                security = File.GetAccessControl(path);
            }
            else if (Directory.Exists(path))
            {
                security = Directory.GetAccessControl(path);
            }

            if (security == null)
            {
                throw new FileNotFoundException("The specified path does not exist.", path);
            }

            // get the rules from the security descriptor's discretionary access control list (DACL)
            var accessRules = security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)).Cast<AccessRule>().OfType<FileSystemAccessRule>().ToList();

            return accessRules;
        }

        private static AccessInfo GetAccessInfo(List<FileSystemAccessRule> accessRules, UserInformation userInfo)
        {
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

        private static AccessInfo GetAccessInfo(string path, UserInformation userInfo)
        {
            // get the access rules from the security descriptor's discretionary access control list (DACL)
            var accessRules = GetAccessRules(path, userInfo);

            return GetAccessInfo(accessRules, userInfo);
        }

        private class AccessInfo
        {
            public bool Allowed { get; set; }
            public bool Denied { get; set; }

            public AccessInfo(bool allowed, bool denied)
            {
                this.Allowed = allowed;
                this.Denied = denied;
            }
        }
    }
}
