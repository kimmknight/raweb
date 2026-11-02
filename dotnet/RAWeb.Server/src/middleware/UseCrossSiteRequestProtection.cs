using RAWeb.Server.Utilities;

namespace RAWeb.Server.Middleware;

internal static class UseCrossSiteRequestProtectionMiddleware {
  /// <summary>
  /// Rejects requests that pages on other origins send through the user's browser except
  /// top-level navigations (e.g., following a link to RAWeb).
  /// 
  /// This prevents cross-site request forgery, where a page on another site uses
  /// the user's RAWeb cookies (e.g., to change policies) and prevents other pages
  /// from loading RAWeb URLs to learn about the user.
  /// <br /><br />
  /// It also tells browsers to only allow RAWeb's own pages to show RAWeb in a frame,
  /// which prevents clickjacking attacks.
  /// <br /><br />
  /// See <see cref="CrossSiteRequest.ShouldBlock"/> for which requests are rejected.
  /// </summary>
  /// <param name="app"></param>
  internal static void UseCrossSiteRequestProtection(this WebApplication app) {
    app.Use(async (context, next) => {
      context.Response.Headers.ContentSecurityPolicy = "frame-ancestors 'self'";
      context.Response.Headers.XFrameOptions = "SAMEORIGIN";

      if (CrossSiteRequest.ShouldBlock(context.Request)) {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("Cross-site requests are not allowed.");
        return;
      }

      await next(context);
    });
  }
}
