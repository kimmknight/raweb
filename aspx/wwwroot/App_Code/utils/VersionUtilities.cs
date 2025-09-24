using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

namespace RAWebServer.Utilities {
    public class VersionHelpers {
        public static Version ToVersion(string versionString) {
            if (string.IsNullOrEmpty(versionString)) {
                return new Version(1, 0, 0, 0);
            }

            var versionParts = versionString.Split('.');
            if (versionParts.Length >= 4) {
                var year = int.Parse(versionParts[0]);
                var month = int.Parse(versionParts[1]);
                var day = int.Parse(versionParts[2]);
                var revision = int.Parse(versionParts[3]);

                return new Version(year, month, day, revision);
            }

            return new Version(1, 0, 0, 0);
        }
    }

    public class LocalVersions {
        public static string GetApplicationVersionString() {
            // get the AssemblyFileVersion from AssemblyInfo.cs
            // (this version is set during the release process for RAWeb)
            string fileVersion = null;
            var versionAttribute = Assembly.GetExecutingAssembly()
                                                                    .GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false)
                                                                    .OfType<AssemblyFileVersionAttribute>()
                                                                    .FirstOrDefault();

            if (versionAttribute != null) {
                fileVersion = versionAttribute.Version;
            }

            if (!string.IsNullOrEmpty(fileVersion)) {
                return fileVersion;
            }

            return "1.0.0.0";
        }

        public static string GetFrontendVersionString() {
            var timestampFilePath = HttpContext.Current.Server.MapPath("~/lib/build.timestamp");
            if (File.Exists(timestampFilePath)) {
                try {
                    var timestamp = File.ReadAllText(timestampFilePath).Trim();
                    return timestamp;
                }
                catch (Exception) {
                    return null;
                }
            }
            return null;
        }
    }
}
