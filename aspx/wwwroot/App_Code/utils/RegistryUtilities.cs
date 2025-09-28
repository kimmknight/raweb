using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Web;

namespace RAWebServer.Utilities {
    public class RegistryReader {
        public static Microsoft.Win32.RegistryKey OpenRemoteAppRegistryKey(string keyName) {
            // open the registry key for the specified application
            var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications\\" + keyName);
            if (regKey == null) {
                throw new ArgumentException("'keyName' must be a valid application name in HKEY_LOCAL_MACHINE\\SOFTWARE\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications.");
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
                var knownAccessRules = securityDescriptor.DiscretionaryAcl
                    .OfType<CommonAce>()
                    .Where(ace => ace.AceType == AceType.AccessAllowed || ace.AceType == AceType.AccessDenied)
                    .ToList();

                // give priority to denial rules - if any deny rule matches, access is denied
                var accessDenied = knownAccessRules
                    .Where(ace => ace.AceType == AceType.AccessDenied)
                    .Any(ace => allSids.Any(sid => sid.Equals(ace.SecurityIdentifier)));
                if (accessDenied) {
                    httpStatus = 403;
                    return false;
                }

                // check if any access allowed rule matches the user or group SIDs
                var accessAllowed = knownAccessRules
                    .Where(ace => ace.AceType == AceType.AccessAllowed)
                    .Any(ace => allSids.Any(sid => sid.Equals(ace.SecurityIdentifier)));

                if (!accessAllowed) {
                    httpStatus = 403;
                }
                return accessAllowed;

            }
            catch (UnauthorizedAccessException) {
                httpStatus = 403;
                return false;
            }
            catch (Exception ex) {
                throw new Exception("", ex);
            }
        }

        public static bool CanAccessRemoteApp(Microsoft.Win32.RegistryKey registryKey, UserInformation userInfo) {
            int httpStatus;
            return CanAccessRemoteApp(registryKey, userInfo, out httpStatus);
        }

        public static bool CanAccessRemoteApp(string keyName, UserInformation userInfo) {
            int httpStatus;
            return CanAccessRemoteApp(keyName, userInfo, out httpStatus);
        }

        public static bool CanAccessRemoteApp(string keyName, UserInformation userInfo, out int httpStatus) {
            using (var regKey = OpenRemoteAppRegistryKey(keyName)) {
                return CanAccessRemoteApp(regKey, userInfo, out httpStatus);
            }
        }

        public static string ConstructRdpFileFromRegistry(string keyName) {
            using (var regKey = OpenRemoteAppRegistryKey(keyName)) {

                // get the application details from the registry key
                var appName = regKey.GetValue("Name") as string;
                var appPath = regKey.GetValue("Path") as string;

                // if the RDPFileContents key exists, serve the contents of that key
                var rdpFileContents = regKey.GetValue("RDPFileContents");
                if (rdpFileContents != null) {
                    return rdpFileContents as string;
                }

                var fulladdress = System.Configuration.ConfigurationManager.AppSettings["RegistryApps.FullAddressOverride"];
                if (string.IsNullOrEmpty(fulladdress)) {
                    // get the machine's IP address
                    var ipAddress = HttpContext.Current.Request.ServerVariables["LOCAL_ADDR"];

                    // get the rdp port  from HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp
                    var rdpPort = "";
                    using (var rdpKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp")) {
                        if (rdpKey != null) {
                            var portValue = rdpKey.GetValue("PortNumber");
                            if (portValue != null) {
                                rdpPort = ((int)portValue).ToString();
                            }
                        }
                    }

                    // construct the full address
                    fulladdress = ipAddress + ":" + rdpPort;
                }

                var names = "";
                foreach (var skn in regKey.GetSubKeyNames()) {
                    names += skn + ", ";
                }

                // calculate the file extensions supported by the application
                var appFileExtCSV = "";
                using (var fileTypesKey = regKey.OpenSubKey("Filetypes")) {
                    if (fileTypesKey == null) {
                    }
                    if (fileTypesKey != null) {
                        var fileTypeNames = fileTypesKey.GetValueNames();
                        if (fileTypeNames.Length > 0) {
                            appFileExtCSV = "." + string.Join(",.", fileTypeNames);
                        }
                    }
                }


                // create the RDP file
                var rdpBuilder = new StringBuilder();
                rdpBuilder.AppendLine("full address:s:" + fulladdress);
                rdpBuilder.AppendLine("remoteapplicationname:s:" + appName);
                rdpBuilder.AppendLine("remoteapplicationprogram:s:||" + keyName);
                rdpBuilder.AppendLine("remoteapplicationmode:i:1");
                rdpBuilder.AppendLine("remoteapplicationfileextensions:s:" + appFileExtCSV);
                rdpBuilder.AppendLine("disableremoteappcapscheck:i:1");
                rdpBuilder.AppendLine("workspace id:s:" + new AliasResolver().Resolve(Environment.MachineName));

                var additionalProperties = System.Configuration.ConfigurationManager.AppSettings["RegistryApps.AdditionalProperties"] ?? "";

                // replace ; (but not \;) with \n
                additionalProperties = additionalProperties.Replace(";", Environment.NewLine).Replace("\\" + Environment.NewLine, ";");

                // append each additional property to the RDP file
                foreach (var line in additionalProperties.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)) {
                    if (!line.StartsWith("remoteapplication")) // disallow changing the remoteapplication properties -- this should be done in the registry
                    {
                        rdpBuilder.AppendLine(line);
                    }
                }

                var rdpContent = rdpBuilder.ToString();

                // serve as an RDP file
                return rdpContent;
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern uint ExtractIconEx(string lpszFile, int nIconIndex, [Out] IntPtr[] phiconLarge, [Out] IntPtr[] phiconSmall, [In] uint nIcons);

        [DllImport("user32.dll")]
        public static extern int DestroyIcon(IntPtr hIcon);

        public static MemoryStream ReadImageFromRegistry(string appName, string maybeFileExtName, UserInformation userInfo) {
            var iconSourcePath = "";
            var iconIndex = 0;

            int permissionHttpStatus;
            var hasPermission = CanAccessRemoteApp(appName, userInfo, out permissionHttpStatus);
            if (!hasPermission) {
                if (HttpContext.Current != null) {
                    HttpContext.Current.Response.StatusCode = permissionHttpStatus; // Forbidden
                    HttpContext.Current.Response.End();
                    return new MemoryStream(); // return an empty stream
                }
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

                    iconSourcePath = data.Split(',')[0].Trim();
                    iconIndex = int.Parse(data.Split(',')[1].Trim() ?? "0");
                }
            }
            else {
                // use the application's default icon path
                using (var regKey = OpenRemoteAppRegistryKey(appName)) {
                    // get the icon path from the registry key
                    iconSourcePath = regKey.GetValue("IconPath") as string;
                    if (string.IsNullOrEmpty(iconSourcePath)) {
                        throw new Exception("Icon path not found in registry for application: " + appName);
                    }

                    // get the icon index from the registry key
                    iconIndex = (int)(regKey.GetValue("IconIndex") ?? 0);
                }
            }

            // attempt to extract the icon
            try {
                // extract the icon handle
                var phiconLarge = new IntPtr[1];
                ExtractIconEx(iconSourcePath, iconIndex, phiconLarge, null, 1);

                // convert the icon handle to an Icon object and save it to a MemoryStream
                var iconLarge = Icon.FromHandle(phiconLarge[0]);
                var imageStream = new MemoryStream();
                iconLarge.ToBitmap().Save(imageStream, ImageFormat.Png);
                imageStream.Position = 0;

                // dispose the icon and handle
                DestroyIcon(phiconLarge[0]);
                iconLarge.Dispose();

                return imageStream;
            }
            catch (Exception ex) {
                throw new Exception("Error extracting icon: " + ex.Message);
            }
        }
    }
}
