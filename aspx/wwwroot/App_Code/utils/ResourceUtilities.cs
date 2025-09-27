using System;
using System.IO;
using System.Linq;
using System.Web;

namespace RAWebServer.Utilities {
    public class ResourceUtilities {
        /// <summary>
        /// Reads an RDP file and extracts the value of a specified property. If the property is not found,
        /// returns the provided fallback value (or an empty string if no fallback is provided).
        /// </summary>
        /// <param name="path"></param>
        /// <param name="propertyName"></param>
        /// <param name="fallbackValue"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException">When the path does not resolve to a file.</exception>
        /// <exception cref="InvalidDataException">When the property name is missing the type (e.g., :s:, :i:, etc.)</exception>
        public static string GetRdpFileProperty(string path, string propertyName, string fallbackValue = "") {
            // check if the path exists
            if (!File.Exists(path)) {
                throw new FileNotFoundException("The specified RDP file does not exist.", path);
            }

            // ensure the property name includes the type (:s:, :i:, etc.)
            if (!propertyName.Contains(":")) {
                throw new InvalidDataException("The property name must include the type (e.g., :s:, :i:, etc.).");
            }
            if (!propertyName.EndsWith(":")) {
                propertyName += ":";
            }

            var fileReader = new StreamReader(path);
            string line;
            var value = fallbackValue;
            while ((line = fileReader.ReadLine()) != null) {
                if (line.StartsWith(propertyName)) {
                    // split into 3 parts only (property name, type, value)]
                    var parts = line.Split(new char[] { ':' }, 3);
                    if (parts.Length == 3) {
                        // set the found value to the third part
                        value = parts[2];
                        // we do not break because we want the last instance of the property
                    }
                }
            }
            fileReader.Close();

            value = value.Trim();

            if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(fallbackValue)) {
                return fallbackValue;
            }
            return value;
        }

        public enum IconElementsMode {
            Icon,
            Wallpaper
        }

        /// <summary>
        /// Constructs XML elements for icons of various sizes based on the provided icon path and mode.
        /// It checks for the existence and accessibility of the icon file, and falls back to a
        /// default icon if necessary.
        /// </summary>
        /// <param name="authenticatedUserInfo"></param>
        /// <param name="relativeExtenesionlessIconPath">The path to the icon file. THe path should not include the file extension for the icon. The path should be relative to the App_Data folder.</param>
        /// <param name="mode"></param>
        /// <param name="relativeDefaultIconPath">The path to the default icon, relative to the App_Data folder. Unlke <c>relativeExtenesionlessIconPath</c>, this value should include the icon extension.</param>
        /// <param name="skipMissing">When <c>true</c>, the returned value of this method will be an empty string. Otherwise, the default icon will be used to generate the XML string instead.</param>
        /// <returns></returns>
        public static string ConstructIconElements(
          UserInformation authenticatedUserInfo,
          string relativeExtenesionlessIconPath,
          IconElementsMode mode,
          string relativeDefaultIconPath = "../lib/assets/default.ico",
          bool skipMissing = false
        ) {
            var appDataRoot = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
            var defaultIconPath = Path.Combine(appDataRoot, relativeDefaultIconPath);

            var iconPath = Path.Combine(appDataRoot, string.Format("{0}", relativeExtenesionlessIconPath));

            // create placeholders for tracking the icon dimensions
            var iconWidth = 0;
            var iconHeight = 0;

            // if the icon is from the registry, we get the dimensions from there
            if (relativeExtenesionlessIconPath.StartsWith("registry!")) {
                var appKeyName = relativeExtenesionlessIconPath.Split('!').LastOrDefault();
                var maybeFileExtName = relativeExtenesionlessIconPath.Split('!')[1];
                if (maybeFileExtName == appKeyName) {
                    maybeFileExtName = "";
                }

                try {
                    Stream fileStream = RegistryReader.ReadImageFromRegistry(appKeyName, maybeFileExtName, authenticatedUserInfo);
                    if (fileStream == null) {
                        if (skipMissing) {
                            return "";
                        }

                        // if the file stream is null, use the default icon
                        iconPath = defaultIconPath;
                        relativeExtenesionlessIconPath = relativeDefaultIconPath;
                        fileStream = new FileStream(iconPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }

                    using (var image = System.Drawing.Image.FromStream(fileStream, false, false)) {
                        iconWidth = image.Width;
                        iconHeight = image.Height;
                    }
                }
                catch (Exception) {
                    if (skipMissing) {
                        return "";
                    }

                    // non-square dimensions will cause the default icon to be used
                    iconWidth = 0;
                    iconHeight = 1;
                }
            }

            // otherwise, se get the icon dimensions from the file
            else {
                // get the icon path, preferring the png icon first, then the ico icon, and finally the default icon
                if (File.Exists(iconPath + ".png")) {
                    iconPath += ".png";
                }
                else if (File.Exists(iconPath + ".ico")) {
                    iconPath += ".ico";
                }
                else {
                    if (skipMissing) {
                        return "";
                    }

                    iconPath = defaultIconPath;
                    relativeExtenesionlessIconPath = relativeDefaultIconPath;
                }

                // confirm that the current user has permission to access the icon file
                var hasPermission = FileAccessInfo.CanAccessPath(iconPath, authenticatedUserInfo);
                if (!hasPermission) {
                    if (skipMissing) {
                        return "";
                    }

                    // if the user does not have permission to access the icon file, use the default icon
                    iconPath = defaultIconPath;
                    relativeExtenesionlessIconPath = relativeDefaultIconPath;
                }

                // get the icon dimensions
                using (var fileStream = new FileStream(iconPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (var image = System.Drawing.Image.FromStream(fileStream, false, false)) {
                        iconWidth = image.Width;
                        iconHeight = image.Height;
                    }
                }
            }

            // if the icon is not a square, use the default icon
            // or treat it as wallpaper if the mode is set to "wallpaper"
            var frame = "";
            if (iconWidth != iconHeight) {
                // if the icon is not a square, use the default icon instead
                if (mode == IconElementsMode.Icon) {
                    iconPath = defaultIconPath;
                    relativeExtenesionlessIconPath = relativeDefaultIconPath;
                    using (var fileStream = new FileStream(iconPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        using (var image = System.Drawing.Image.FromStream(fileStream, false, false)) {
                            iconWidth = image.Width;
                            iconHeight = image.Height;
                        }
                    }
                }

                // or, if the mode is set to "wallpaper", we will allow non-square icons
                if (mode == IconElementsMode.Wallpaper) {
                    frame = "&amp;frame=pc";
                }
            }

            // if the path is the default wallpaper, replace it with defaultwallpaper
            if (relativeExtenesionlessIconPath == "../lib/assets/wallpaper.png") {
                relativeExtenesionlessIconPath = "defaultwallpaper";
            }

            // if the path is the default icon, replace it with defaulicon
            if (relativeExtenesionlessIconPath == "../lib/assets/default.ico") {
                relativeExtenesionlessIconPath = "defaulticon";
            }

            // build the icons elements
            var iisBase = VirtualPathUtility.ToAbsolute("~/");
            var iconElements = "<IconRaw FileType=\"Ico\" FileURL=\"" + iisBase + "api/resources/image/" + relativeExtenesionlessIconPath + "?format=ico" + frame + "\" />" + "\r\n";
            if (iconWidth >= 16) {
                iconElements += "<Icon16 Dimensions=\"16x16\" FileType=\"Png\" FileURL=\"" + iisBase + "api/resources/image/" + relativeExtenesionlessIconPath + "?format=png16" + frame + "\" />" + "\r\n";
            }
            if (iconWidth >= 32) {
                iconElements += "<Icon32 Dimensions=\"32x32\" FileType=\"Png\" FileURL=\"" + iisBase + "api/resources/image/" + relativeExtenesionlessIconPath + "?format=png32" + frame + "\" />" + "\r\n";
            }
            if (iconWidth >= 48) {
                iconElements += "<Icon48 Dimensions=\"48x48\" FileType=\"Png\" FileURL=\"" + iisBase + "api/resources/image/" + relativeExtenesionlessIconPath + "?format=png48" + frame + "\" />" + "\r\n";
            }
            if (iconWidth >= 64) {
                iconElements += "<Icon64 Dimensions=\"64x64\" FileType=\"Png\" FileURL=\"" + iisBase + "api/resources/image/" + relativeExtenesionlessIconPath + "?format=png64" + frame + "\" />" + "\r\n";
            }
            if (iconWidth >= 100) {
                iconElements += "<Icon100 Dimensions=\"100x100\" FileType=\"Png\" FileURL=\"" + iisBase + "api/resources/image/" + relativeExtenesionlessIconPath + "?format=png100" + frame + "\" />" + "\r\n";
            }
            if (iconWidth >= 256) {
                iconElements += "<Icon256 Dimensions=\"256x256\" FileType=\"Png\" FileURL=\"" + iisBase + "api/resources/image/" + relativeExtenesionlessIconPath + "?format=png256" + frame + "\" />" + "\r\n";
            }

            return iconElements;
        }
    }
}
