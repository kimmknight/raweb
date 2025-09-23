using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Web;

namespace VersionUtilities
{
    public class VersionHelpers
    {
        static public Version ToVersion(string versionString)
        {
            if (string.IsNullOrEmpty(versionString))
            {
                return new Version(1, 0, 0, 0);
            }

            string[] versionParts = versionString.Split('.');
            if (versionParts.Length >= 4)
            {
                int year = int.Parse(versionParts[0]);
                int month = int.Parse(versionParts[1]);
                int day = int.Parse(versionParts[2]);
                int revision = int.Parse(versionParts[3]);

                return new Version(year, month, day, revision);
            }

            return new Version(1, 0, 0, 0);
        }
    }
    public class LocalVersions
    {
        static public string GetApplicationVersionString()
        {
            // get the AssemblyFileVersion from AssemblyInfo.cs
            // (this version is set during the release process for RAWeb)
            string fileVersion = null;
            AssemblyFileVersionAttribute versionAttribute = Assembly.GetExecutingAssembly()
                                                                    .GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false)
                                                                    .OfType<AssemblyFileVersionAttribute>()
                                                                    .FirstOrDefault();

            if (versionAttribute != null)
            {
                fileVersion = versionAttribute.Version;
            }

            if (!string.IsNullOrEmpty(fileVersion))
            {
                return fileVersion;
            }

            return "1.0.0.0";
        }

        static public string GetFrontendVersionString()
        {
            string timestampFilePath = HttpContext.Current.Server.MapPath("~/lib/build.timestamp");
            if (File.Exists(timestampFilePath))
            {
                try
                {
                    string timestamp = File.ReadAllText(timestampFilePath).Trim();
                    return timestamp;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return null;
        }
    }
}
