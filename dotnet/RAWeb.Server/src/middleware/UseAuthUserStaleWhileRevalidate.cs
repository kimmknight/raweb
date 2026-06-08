using System.Collections.Concurrent;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Middleware;

public static class UseAuthUserStaleWhileRevalidateMiddleware {

  // thread-safe dictionary to debounce per user
  private static readonly ConcurrentDictionary<string, AsyncDebouncer> s_debouncers = new();

  /// <summary>
  /// Middleware to revalidate authenticated user information in the background after
  /// each request, if needed. This allows us to keep user information up-to-date without
  /// adding latency to requests. The revalidation is debounced per user to avoid excessive
  /// processing for users making multiple requests in a short period of time.
  /// </summary>
  /// <param name="app"></param>
  public static void UseAuthUserStaleWhileRevalidate(this WebApplication app) {
    app.Use(async (context, next) => {
      await next(context);

      // if the user is authenticated, kick off a background task to revalidate
      // the user information, if needed
      var userInfo = GetHttpContextUserInformation(context.Request.HttpContext);
      if (!ShouldRevalidate(userInfo)) {
        return;
      }

      // get the debouncer for this user or create a new one
      var debouncer = s_debouncers.GetOrAdd(userInfo!.Sid, _ => new AsyncDebouncer());

      _ = Task.Run(() =>

        debouncer.DebounceAsync(5000, () => {
          // run UserInformation.FromPrincipal, which also updates the cache
          try {
            UserInformation.FromPrincipal(userInfo.Username, userInfo.Domain);
          }
          catch (Exception ex) {
            LogError($"Could not revalidate user {userInfo.Domain}\\{userInfo.Username}: {ex.Message}");
          }

          // clean up this debouncer
          s_debouncers.TryRemove(userInfo.Sid, out _);

          return Task.CompletedTask;
        })
        // log exceptions from the debounce task
        .ContinueWith(task => LogError(task.Exception?.ToString() ?? string.Empty), TaskContinuationOptions.OnlyOnFaulted)
      );
    });
  }

  private static bool ShouldRevalidate(UserInformation? userInfo) {
    var isAuthenticated = userInfo is not null && !userInfo.IsAnonymousUser;
    if (!isAuthenticated) {
      return false;
    }

    var userCacheEnabled = PoliciesManager.RawPolicies["UserCache.Enabled"] == "true";
    return userCacheEnabled;
  }

  private static UserInformation? GetHttpContextUserInformation(HttpContext context) {
    return context.Items["UserInformation"] as UserInformation;
  }

  private static void LogError(string message) {
    if (string.IsNullOrEmpty(message)) {
      return;
    }
    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    var logPath = Path.Combine(Constants.AppDataFolderPath, "background_errors.log");
    File.AppendAllText(logPath, $"[{timestamp}] {message}{Environment.NewLine}");
  }
}
