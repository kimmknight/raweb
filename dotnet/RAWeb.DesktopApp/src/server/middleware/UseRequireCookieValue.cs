using Microsoft.AspNetCore.Builder;

namespace RAWeb.DesktopApp.InternalServer;

internal static class UseRequireCookieValueMiddleware {
  internal static void UseRequireCookieValue(this WebApplication app, string cookieName, string requiredValue) {
    app.Use(async (context, next) => {
      if (!context.Request.Cookies.TryGetValue(cookieName, out var foundCookieValue) || foundCookieValue != requiredValue) {
        context.Abort();
        return;
      }
      await next(context);
    });
  }
}
