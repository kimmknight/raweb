using AuthUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Web;

namespace RegistryUtilities
{
    public class Reader
    {

        public static Microsoft.Win32.RegistryKey OpenRemoteAppRegistryKey(string keyName)
        {
            // open the registry key for the specified application
            var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications\\" + keyName);
            if (regKey == null)
            {
                throw new ArgumentException("'keyName' must be a valid application name in HKEY_LOCAL_MACHINE\\SOFTWARE\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications.");
            }
            return regKey;
        }

        // checks if the application in the registry can be accessed by the current user
        // based on the SecurityDescriptor value in the registry key
        public static bool CanAccessRemoteApp(Microsoft.Win32.RegistryKey registryKey, AuthUtilities.UserInformation userInfo, out int httpStatus)
        {
            httpStatus = 200;

            try
            {
                // if the user information is null, deny access
                // (servers with anonymous authentication will set ISUR as the user info)
                if (userInfo == null)
                {
                    httpStatus = 401;
                    return false;
                }

                var securityDescriptorString = registryKey.GetValue("SecurityDescriptor") as string;
                if (string.IsNullOrEmpty(securityDescriptorString))
                {
                    // if there is no SecurityDescriptor, assume access is allowed
                    return true;
                }

                // if the current user is IUSR, allow access
                if (userInfo.Sid == "S-1-5-17")
                {
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
                if (accessDenied)
                {
                    httpStatus = 403;
                    return false;
                }

                // check if any access allowed rule matches the user or group SIDs
                var accessAllowed = knownAccessRules
                    .Where(ace => ace.AceType == AceType.AccessAllowed)
                    .Any(ace => allSids.Any(sid => sid.Equals(ace.SecurityIdentifier)));

                if (!accessAllowed)
                {
                    httpStatus = 403;
                }
                return accessAllowed;

            }
            catch (UnauthorizedAccessException)
            {
                httpStatus = 403;
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception("", ex);
                return false;
            }
        }

        public static bool CanAccessRemoteApp(Microsoft.Win32.RegistryKey registryKey, AuthUtilities.UserInformation userInfo)
        {
            int httpStatus = 0;
            return CanAccessRemoteApp(registryKey, userInfo, out httpStatus);
        }

        public static bool CanAccessRemoteApp(string keyName, AuthUtilities.UserInformation userInfo)
        {
            int httpStatus = 0;
            return CanAccessRemoteApp(keyName, userInfo, out httpStatus);
        }

        public static bool CanAccessRemoteApp(string keyName, AuthUtilities.UserInformation userInfo, out int httpStatus)
        {
            using (var regKey = OpenRemoteAppRegistryKey(keyName))
            {
                return CanAccessRemoteApp(regKey, userInfo, out httpStatus);
            }
        }

        public static string ConstructRdpFileFromRegistry(string keyName)
        {
            using (var regKey = OpenRemoteAppRegistryKey(keyName))
            {

                // get the application details from the registry key
                string appName = regKey.GetValue("Name") as string;
                string appPath = regKey.GetValue("Path") as string;

                // if the RDPFileContents key exists, serve the contents of that key
                object rdpFileContents = regKey.GetValue("RDPFileContents");
                if (rdpFileContents != null)
                {
                    return rdpFileContents as string;
                }

                string fulladdress = System.Configuration.ConfigurationManager.AppSettings["RegistryApps.FullAddressOverride"];
                if (string.IsNullOrEmpty(fulladdress))
                {
                    // get the machine's IP address
                    string ipAddress = HttpContext.Current.Request.ServerVariables["LOCAL_ADDR"];

                    // get the rdp port  from HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp
                    string rdpPort = "";
                    using (var rdpKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp"))
                    {
                        if (rdpKey != null)
                        {
                            object portValue = rdpKey.GetValue("PortNumber");
                            if (portValue != null)
                            {
                                rdpPort = ((int)portValue).ToString();
                            }
                        }
                    }

                    // construct the full address
                    fulladdress = ipAddress + ":" + rdpPort;
                }

                string names = "";
                foreach (string skn in regKey.GetSubKeyNames())
                {
                    names += skn + ", ";
                }

                // calculate the file extensions supported by the application
                string appFileExtCSV = "";
                using (var fileTypesKey = regKey.OpenSubKey("Filetypes"))
                {
                    if (fileTypesKey == null)
                    {
                    }
                    if (fileTypesKey != null)
                    {
                        string[] fileTypeNames = fileTypesKey.GetValueNames();
                        if (fileTypeNames.Length > 0)
                        {
                            appFileExtCSV = "." + string.Join(",.", fileTypeNames);
                        }
                    }
                }


                // create the RDP file
                StringBuilder rdpBuilder = new StringBuilder();
                rdpBuilder.AppendLine("full address:s:" + fulladdress);
                rdpBuilder.AppendLine("remoteapplicationname:s:" + appName);
                rdpBuilder.AppendLine("remoteapplicationprogram:s:||" + keyName);
                rdpBuilder.AppendLine("remoteapplicationmode:i:1");
                rdpBuilder.AppendLine("remoteapplicationfileextensions:s:" + appFileExtCSV);
                rdpBuilder.AppendLine("disableremoteappcapscheck:i:1");

                string additionalProperties = System.Configuration.ConfigurationManager.AppSettings["RegistryApps.AdditionalProperties"] ?? "";

                // replace ; (but not \;) with \n
                additionalProperties = additionalProperties.Replace(";", Environment.NewLine).Replace("\\" + Environment.NewLine, ";");

                // append each additional property to the RDP file
                foreach (string line in additionalProperties.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!line.StartsWith("remoteapplication")) // disallow changing the remoteapplication properties -- this should be done in the registry
                    {
                        rdpBuilder.AppendLine(line);
                    }
                }

                string rdpContent = rdpBuilder.ToString();

                // serve as an RDP file
                return rdpContent;
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern uint ExtractIconEx(string lpszFile, int nIconIndex, [Out] IntPtr[] phiconLarge, [Out] IntPtr[] phiconSmall, [In] uint nIcons);

        [DllImport("user32.dll")]
        public static extern int DestroyIcon(IntPtr hIcon);

        public static MemoryStream ReadImageFromRegistry(string appName, string maybeFileExtName, UserInformation userInfo)
        {
            string iconSourcePath = "";
            int iconIndex = 0;

            int permissionHttpStatus = 200;
            bool hasPermission = CanAccessRemoteApp(appName, userInfo, out permissionHttpStatus);
            if (!hasPermission)
            {
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Response.StatusCode = permissionHttpStatus; // Forbidden
                    HttpContext.Current.Response.End();
                    return new MemoryStream(); // return an empty stream
                }
                throw new UnauthorizedAccessException("You do not have permission to access the application: " + appName);
            }

            // get the icon path from the registry
            if (!string.IsNullOrEmpty(maybeFileExtName))
            {
                // handle the case where a file extension is specified
                using (var regKey = Reader.OpenRemoteAppRegistryKey(appName + "\\Filetypes"))
                {
                    string data = regKey.GetValue(maybeFileExtName) as string;
                    if (string.IsNullOrEmpty(data))
                    {
                        throw new Exception("File extension icon for " + maybeFileExtName + "not found in registry for application: " + appName);
                    }

                    iconSourcePath = data.Split(',')[0].Trim();
                    iconIndex = int.Parse(data.Split(',')[1].Trim() ?? "0");
                }
            }
            else
            {
                // use the application's default icon path
                using (var regKey = Reader.OpenRemoteAppRegistryKey(appName))
                {
                    // get the icon path from the registry key
                    iconSourcePath = regKey.GetValue("IconPath") as string;
                    if (string.IsNullOrEmpty(iconSourcePath))
                    {
                        throw new Exception("Icon path not found in registry for application: " + appName);
                    }

                    // get the icon index from the registry key
                    iconIndex = (int)(regKey.GetValue("IconIndex") ?? 0);
                }
            }

            // attempt to extract the icon
            try
            {
                // extract the icon handle
                IntPtr[] phiconLarge = new IntPtr[1];
                ExtractIconEx(iconSourcePath, iconIndex, phiconLarge, null, 1);

                // convert the icon handle to an Icon object and save it to a MemoryStream
                Icon iconLarge = Icon.FromHandle(phiconLarge[0]);
                var imageStream = new MemoryStream();
                iconLarge.ToBitmap().Save(imageStream, ImageFormat.Png);
                imageStream.Position = 0;

                // dispose the icon and handle
                DestroyIcon(phiconLarge[0]);
                iconLarge.Dispose();

                return imageStream;
            }
            catch (Exception ex)
            {
                throw new Exception("Error extracting icon: " + ex.Message);
            }
        }
    }
}