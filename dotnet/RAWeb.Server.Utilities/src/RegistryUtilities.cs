using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using RAWeb.Server.Management;

namespace RAWeb.Server.Utilities;


public class RegistryReader {
    public static Microsoft.Win32.RegistryKey OpenRemoteAppRegistryKey(string keyName) {
        var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
        var centralizedPublishingCollectionName = AppId.ToCollectionName();
        var registryPath = supportsCentralizedPublishing ?
            "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\CentralPublishedResources\\PublishedFarms\\" + centralizedPublishingCollectionName + "\\Applications" :
            "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications";

        // open the registry key for the specified application
        var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath + "\\" + keyName);
        if (regKey == null) {
            throw new ArgumentException("'keyName' must be a valid application name in " + registryPath + ".");
        }
        return regKey;
    }

    // checks if the application in the registry can be accessed by the current user
    // based on the SecurityDescriptor value in the registry key
    public static bool CanAccessRemoteApp(Microsoft.Win32.RegistryKey registryKey, UserInformation userInfo, out int httpStatus) {
        httpStatus = 200;

        try {
            // if the user information is null, deny access
            // (servers with anonymous authentication will set ISUR as the user info)
            if (userInfo == null) {
                httpStatus = 401;
                return false;
            }

            // if the current user is anonymous, allow access
            if (userInfo.IsAnonymousUser) {
                return true;
            }

            // if the current user is not a member of the Remote Desktop Users group or the Administrators, deny access
            if (!userInfo.IsRemoteDesktopUser && !userInfo.IsLocalAdministrator) {
                httpStatus = 403;
                return false;
            }

            var securityDescriptorString = registryKey.GetValue("SecurityDescriptor") as string;
            if (string.IsNullOrEmpty(securityDescriptorString)) {
                // if there is no SecurityDescriptor, assume access is allowed
                return true;
            }

            // get the security identifiers for the user and groups
            var userSid = new SecurityIdentifier(userInfo.Sid);
            var groupSids = userInfo.Groups.Select(g => new SecurityIdentifier(g.Sid)).ToList();
            var allSids = new List<SecurityIdentifier> { userSid };
            allSids.AddRange(groupSids);

            // parse the security descriptor from the SSDL string
            var securityDescriptor = new RawSecurityDescriptor(securityDescriptorString);

            // check if the user or any of their groups have read access
            var accessAllowed = securityDescriptor
                .GetAllowedSids(FileSystemRights.ReadData)
                .Any(aceSid => allSids.Any(sid => sid.Equals(aceSid)));

            if (!accessAllowed) {
                httpStatus = 403;
            }
            Console.WriteLine("\nkey name: " + string.Join(", ", securityDescriptor
                .GetAllowedSids(FileSystemRights.ReadData)));
            return accessAllowed;

        }
        catch (UnauthorizedAccessException) {
            httpStatus = 403;
            return false;
        }
        catch (Exception) {
            throw;
        }
    }

    public static bool CanAccessRemoteApp(Microsoft.Win32.RegistryKey registryKey, UserInformation userInfo) {
        return CanAccessRemoteApp(registryKey, userInfo, out _);
    }

    public static bool CanAccessRemoteApp(string keyName, UserInformation userInfo) {
        return CanAccessRemoteApp(keyName, userInfo, out _);
    }

    public static bool CanAccessRemoteApp(string keyName, UserInformation userInfo, out int httpStatus) {
        using (var regKey = OpenRemoteAppRegistryKey(keyName)) {
            return CanAccessRemoteApp(regKey, userInfo, out httpStatus);
        }
    }

    public static string ConstructRdpFileFromRegistry(string keyName) {
        var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
        var centralizedPublishingCollectionName = AppId.ToCollectionName();
        var remoteApps = new SystemRemoteApps(supportsCentralizedPublishing ? centralizedPublishingCollectionName : null);

        // generate the RDP file contents
        var registeredApp = remoteApps.GetRegistedApp(keyName);
        if (registeredApp is null) {
            throw new NullReferenceException("The specified RemoteApp '" + keyName + "' was not found in the registry.");
        }
        var rdpBuilder = registeredApp.ToRdpFileStringBuilder(Constants.TerminalServerFullAddress);

        var additionalProperties = PoliciesManager.RawPolicies["RegistryApps.AdditionalProperties"] ?? "";

        // replace ; (but not \;) with \n
        additionalProperties = additionalProperties.Replace(";", Environment.NewLine).Replace("\\" + Environment.NewLine, ";");

        // append each additional property to the RDP file
        foreach (var line in additionalProperties.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)) {
            if (!line.StartsWith("remoteapplication")) // disallow changing the remoteapplication properties -- this should be done in the registry
            {
                rdpBuilder.AppendLine(line);
            }
        }

        rdpBuilder.AppendLine("raweb source type:i:2"); // indicate that this RDP file was generated by RAWeb from the registry

        var rdpFileContent = rdpBuilder.ToString();
        return rdpFileContent;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegQueryInfoKey(
        IntPtr hKey,
        IntPtr lpClass,
        IntPtr lpcchClass,
        IntPtr lpReserved,
        out int lpcSubKeys,
        out int lpcbMaxSubKeyLen,
        out int lpcbMaxClassLen,
        out int lpcValues,
        out int lpcbMaxValueNameLen,
        out int lpcbMaxValueLen,
        out int lpcbSecurityDescriptor,
        out long lpftLastWriteTime // FILETIME; convert with `DateTime.FromFileTime(lpftLastWriteTime)`
    );

    /// <summary>
    /// Gets the last modified time of a remote app registry key.
    /// <br /><br />
    /// See https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-regqueryinfokeya
    /// </summary>
    /// <param name="keyName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static DateTime GetRemoteAppLastModifiedTime(string keyName) {
        using (var regKey = OpenRemoteAppRegistryKey(keyName)) {
            int _;

            var result = RegQueryInfoKey(
                regKey.Handle.DangerousGetHandle(),
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out var fileTime
            );

            if (result != 0) {
                throw new Exception("Failed to query registry key info. Error code: " + result);
            }

            return DateTime.FromFileTime(fileTime);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern uint ExtractIconEx(string lpszFile, int nIconIndex, [Out] IntPtr[] phiconLarge, [Out] IntPtr[] phiconSmall, [In] uint nIcons);

    [DllImport("user32.dll")]
    public static extern int DestroyIcon(IntPtr hIcon);

    /// <summary>
    /// Reads the icon for the specified RemoteApp from the registry and returns it as a MemoryStream.
    /// </summary>
    /// <param name="appName"></param>
    /// <param name="maybeFileExtName"></param>
    /// <param name="userInfo"></param>
    /// <returns></returns>
    /// <exception cref="UnauthorizedAccessException"></exception>
    /// <exception cref="Exception"></exception>
    public static MemoryStream? ReadImageFromRegistry(string appName, string maybeFileExtName, UserInformation userInfo) {
        var iconSourcePath = "";
        var iconIndex = 0;

        var hasPermission = CanAccessRemoteApp(appName, userInfo, out var permissionHttpStatus);
        if (!hasPermission) {
#if NET462
            if (System.Web.HttpContext.Current != null) {
                System.Web.HttpContext.Current.Response.StatusCode = permissionHttpStatus; // Forbidden
                System.Web.HttpContext.Current.Response.End();
                return new MemoryStream(); // return an empty stream
            }
#endif
            throw new UnauthorizedAccessException("You do not have permission to access the application: " + appName);
        }

        // get the icon path from the registry
        if (!string.IsNullOrEmpty(maybeFileExtName)) {
            // handle the case where a file extension is specified
            using (var regKey = OpenRemoteAppRegistryKey(appName + "\\Filetypes")) {
                var data = regKey.GetValue(maybeFileExtName) as string;
                if (string.IsNullOrEmpty(data)) {
                    throw new Exception("File extension icon for " + maybeFileExtName + " not found in registry for application: " + appName);
                }

                iconSourcePath = data?.Split(',')[0].Trim() ?? "";
                iconIndex = int.Parse(data?.Split(',')[1].Trim() ?? "0");
            }
        }
        else {
            // use the application's default icon path
            using (var regKey = OpenRemoteAppRegistryKey(appName)) {
                // get the icon path from the registry key
                iconSourcePath = regKey.GetValue("IconPath") as string;

                // use the application path for the icon if not explorer.exe (every packaged app uses explorer.exe)
                if (string.IsNullOrEmpty(iconSourcePath) && !Path.GetFileName(iconSourcePath ?? "").Equals("explorer.exe")) {
                    var path = regKey.GetValue("Path") as string;
                    iconSourcePath = path;
                }

                if (string.IsNullOrEmpty(iconSourcePath)) {
                    throw new Exception("Icon path not found in registry for application: " + appName);
                }

                // get the icon index from the registry key
                iconIndex = (int)(regKey.GetValue("IconIndex") ?? 0);
            }
        }

        // attempt to extract the icon
        try {
            return ImageUtilities.ImagePathToStream(iconSourcePath ?? "", iconIndex);
        }
        catch (FileNotFoundException) {
            return null;
        }
        catch (ImageUtilities.UnsupportedImageFormatException) {
            return null;
        }
        catch (Exception ex) {
            throw new Exception("Error extracting icon: " + ex.Message);
        }
    }
}
