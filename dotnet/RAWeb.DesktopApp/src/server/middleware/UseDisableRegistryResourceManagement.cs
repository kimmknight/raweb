using Microsoft.AspNetCore.Builder;

namespace RAWeb.DesktopApp.InternalServer;

internal static class UseDisableRegistryResourceManagementMiddleware {
  internal static void UseDisableRegistryResourceManagement(this WebApplication app) {
    app.Use(async (context, next) => {
      context.Items["c.disableListInstalledApps"] = true;
      context.Items["c.disableManageRegistryApps"] = true;
      context.Items["c.disableReadRegistryApps"] = true;
      context.Items["c.disableCheckIfConnectionsAreAllowed"] = true;
      await next(context);
    });
  }
}
