using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Web;
using RAWeb.Server.Utilities;

namespace RAWebServer.Modules {
  public class AuthUserStaleWhileRevalidate : IHttpModule {

    // thread-safe dictionary to debounce per user
    private static readonly ConcurrentDictionary<string, AsyncDebouncer> s_debouncers = new();

    public void Init(HttpApplication context) {
      context.EndRequest += new EventHandler(OnEndRequest);
    }

    private void Log(Exception ex) {
      if (ex != null) {
        var details = new System.Text.StringBuilder();
        var current = ex;
        while (current != null) {
          details.AppendLine(current.GetType().FullName);
          details.AppendLine(current.Message);
          details.AppendLine(current.StackTrace); // will include line numbers if PDB is present
          current = current.InnerException;
        }

        Log(details.ToString());
      }
    }

    private void Log(string message) {
      if (!string.IsNullOrEmpty(message)) {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        message = "[" + timestamp + "] " + message;
        var logPath = System.IO.Path.Combine(Constants.AppDataFolderPath, "background_errors.log");
        System.IO.File.AppendAllText(logPath, message + Environment.NewLine);
      }
    }

    private void OnEndRequest(object sender, EventArgs e) {
      var application = (HttpApplication)sender;
      var context = application.Context;

      var userInfo = GetContextUserInformation(context);

      // if the user is authenticated, kick off a background task to revalidate
      // the user information, if needed
      if (ShouldRevalidate(userInfo)) {

        // get the debouncer for this user or create a new one
        var debouncer = s_debouncers.GetOrAdd(userInfo.Sid, sid => new AsyncDebouncer());

        Task.Run(() => {

          debouncer.DebounceAsync(5000, () => {
            // run UserInformation.FromPrincipal, which also updates the cache
            try {
              UserInformation.FromPrincipal(userInfo.Username, userInfo.Domain);
            }
            catch (System.DirectoryServices.AccountManagement.PrincipalServerDownException ex) {
              Log("Could not reach domain controller to revalidate user " + userInfo.Domain + "\\" + userInfo.Username + ": " + ex.Message);
            }

            // clean up this debouncer
            AsyncDebouncer _;
            s_debouncers.TryRemove(userInfo.Sid, out _);

            return Task.CompletedTask;
          })
            // log  exceptions from the debounce task
            .ContinueWith(task => Log(task.Exception), TaskContinuationOptions.OnlyOnFaulted);
        });
      }
    }

    private bool ShouldRevalidate(UserInformation userInfo) {
      var isAuthenticated = userInfo != null && !userInfo.IsAnonymousUser;
      if (!isAuthenticated) {
        return false;
      }

      var userCacheEnabled = PoliciesManager.RawPolicies["UserCache.Enabled"] == "true";
      return userCacheEnabled;
    }

    private UserInformation GetContextUserInformation(HttpContext context) {
      return context.Items["UserInformation"] as UserInformation;
    }

    public void Dispose() { }
  }
}
