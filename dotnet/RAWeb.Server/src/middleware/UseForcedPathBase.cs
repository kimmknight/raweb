namespace RAWeb.Server.Middleware;

internal static class UseForcedPathBaseMiddleware {
  /// <summary>
  /// This middleware forces all requests to be redirected to a URL that includes
  /// the specified base path.
  /// </summary>
  /// <param name="app"></param>
  /// <param name="pathBase"></param>
  internal static void UseForcedPathBase(this WebApplication app, string pathBase) {
    // This middleware strips the base path from context.Request.Path
    // and moves it to context.Request.PathBase
    app.UsePathBase(pathBase);

    app.Use(async (context, next) => {
      // if context.Request.PathBase is empty, that means the request did not include the base path.
      var requestMissingBasePath = !context.Request.PathBase.HasValue;

      // append the base path to the request path and redirect if it was missing from the request
      if (requestMissingBasePath) {
        context.Response.Redirect(pathBase + context.Request.Path + context.Request.QueryString);
        return;
      }

      // force the base path to use the exact specified case
      var basePathIsIncorrect = context.Request.PathBase.Value != pathBase;
      if (basePathIsIncorrect) {
        context.Response.Redirect(pathBase + context.Request.Path + context.Request.QueryString);
        return;
      }

      await next();
    });
  }
}
