using System.Net;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// Confirms the freshly installed application actually answers requests.
/// </summary>
public static class HealthCheck {
  public const int Attempts = 10;

  public static bool Poll(string url, InstallLog log, CancellationToken cancellationToken) {
    // Self-signed certificates are likely to be used, so certificate validation is intentionally bypassed
    // for this check only. Nothing sensitive is sent; the request is an unauthenticated GET to localhost.
    var previousCallback = ServicePointManager.ServerCertificateValidationCallback;
    ServicePointManager.ServerCertificateValidationCallback = (_, _, _, _) => true;
    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

    try {
      log.Detail($"Performing health check against {url}...");

      for (var attempt = 1; attempt <= Attempts; attempt++) {
        cancellationToken.ThrowIfCancellationRequested();

        try {
          var request = (HttpWebRequest)WebRequest.Create(url);
          request.Timeout = 5000;
          using var response = (HttpWebResponse)request.GetResponse();
          log.Detail($"  Passed ({(int)response.StatusCode} {response.StatusDescription}).");
          return true;
        }
        catch (WebException exception) {
          log.Detail($"  Error: {exception.InnerException?.Message ?? exception.Message}");
        }

        if (attempt < Attempts) {
          log.Detail($"Attempt {attempt}/{Attempts} - retrying in 3 s...");
          Thread.Sleep(3000);
        }
      }

      log.Error($"  Health check failed after {Attempts} attempts.");
      return false;
    }
    finally {
      ServicePointManager.ServerCertificateValidationCallback = previousCallback;
    }
  }
}
