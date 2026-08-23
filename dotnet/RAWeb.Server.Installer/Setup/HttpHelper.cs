using System.IO;
using System.Net;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// Minimal HTTP access for the release listing, archive download, and Hosting Bundle download.
/// </summary>
internal static class HttpHelper {
  static HttpHelper() {
    // Older versions of .NET Framework do not negotiate TLS 1.2+ by default, but the download hosts require it.
    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
  }

  // GitHub requests that applications set a user agent string that identifies the application
  // so that they can contact use if there are problems.
  public static string UserAgent => $"RAWeb.Server.Installer/{InstallerVersion.Current}";

  public static string GetString(string url) {
    using var client = new WebClient();
    client.Headers.Add("User-Agent", UserAgent);
    return client.DownloadString(url);
  }

  public static Stream OpenRead(string url) {
    var request = (HttpWebRequest)WebRequest.Create(url);
    request.UserAgent = UserAgent;
    var response = request.GetResponse();
    return response.GetResponseStream();
  }
}
