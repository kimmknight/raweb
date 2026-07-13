using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;

namespace RAWeb.Server.Middleware;

internal static class UseWorkspaceDiscoveryMiddleware {
  internal static void UseWorkspaceDiscovery(this WebApplication app) {
    app.Use(async (context, next) => {
      var userAgent = context.Request.Headers.UserAgent.ToString();
      var pathBase = context.Request.PathBase.Value ?? string.Empty;

      // [Workspace Discovery - Part 1]
      // The macOS, iOS and Android clients use their own user agents when
      // testing whether the workspace URL is valid. In effect, they are testing
      // whether they receive a 401 Unauthorized response with a WWW-Authenticate
      // header for NTLM or Negotiate.
      var hasAspxAuthCookie = context.Request.Cookies[".ASPXAUTH"] is not null;
      if (userAgent is not null && !hasAspxAuthCookie) {
        var isMacosAddWorkspaceDialog = userAgent.StartsWith("com.microsoft.rdc.macos") && userAgent.Contains("RdCore/");
        var isIosAddWorkspaceDialog = userAgent.StartsWith("com.microsoft.rdc.ios") && userAgent.Contains("RdCore/");
        var isAndroidAddWorkspaceDialog = userAgent.StartsWith("com.microsoft.rdc.androidx") && userAgent.Contains("RdCore/");

        if (isMacosAddWorkspaceDialog || isIosAddWorkspaceDialog || isAndroidAddWorkspaceDialog) {
          await context.ChallengeAsync(NegotiateDefaults.AuthenticationScheme);
          return;
        }
      }

      // [Workspace Discovery - Part 2]
      // If it is a workspace client (e.g. Windows RADC or Windows App),
      // serve the workspace XML when no file or endpoint matches (excludes HTML files)
      // This is allows the client to handle cases where the workspace
      // URL entered by the user is not the exact correct URL.
      var isWorkspaceClient = userAgent?.StartsWith("TSWorkspace/2.0") ?? false;
      var endpoint = context.GetEndpoint();
      if (isWorkspaceClient && endpoint is null) {
        context.Response.Redirect($"{pathBase}/api/workspace");
        return;
      }

      await next(context);
    });
  }
}
