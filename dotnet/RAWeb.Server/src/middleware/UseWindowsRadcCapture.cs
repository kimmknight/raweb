namespace RAWeb.Server.Middleware;

internal static class UseWindowsRadcCaptureMiddleware {

  /// <summary>
  /// This middleware captures paths that match the Windows Server- and
  /// Azure-style webfeed URLs that Windows RemoteApp and Desktop
  /// Connections (RADC) tries to append to the end of a feed URL
  /// if it is not an exact match.
  /// </summary>
  /// <param name="app"></param>
  internal static void UseWindowsRadcCapture(this WebApplication app) {
    app.Use(async (context, next) => {
      var pathBase = context.Request.PathBase.Value ?? string.Empty;
      var path = context.Request.Path.ToString();

      if (
          path.EndsWith("/Feed/webfeed.aspx", StringComparison.OrdinalIgnoreCase) || // RDWeb default location
          path.EndsWith("/RDWeb/Feed/webfeed.aspx", StringComparison.OrdinalIgnoreCase) || // RDWeb default location
          path.EndsWith("/api/feeddiscovery/webfeeddiscovery.aspx", StringComparison.OrdinalIgnoreCase) || // Azure Virtual Desktop (classic) default location
          path.EndsWith("/api/arm/feeddiscovery", StringComparison.OrdinalIgnoreCase) // Azure Virtual Desktop default location
      ) {
        // redirect to our workspace endpoint
        context.Response.Redirect($"{pathBase}/api/workspace");
        return;
      }

      await next(context);
    });
  }
}
