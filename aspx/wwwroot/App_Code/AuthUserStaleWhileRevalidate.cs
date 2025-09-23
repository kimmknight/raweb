using AuthUtilities;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

public class AuthUserStaleWhileRevalidate : IHttpModule
{

  // thread-safe dictionary to debounce per user
  private static readonly ConcurrentDictionary<string, AsyncDebouncer> debouncers =
      new ConcurrentDictionary<string, AsyncDebouncer>();

  public void Init(HttpApplication context)
  {
    context.EndRequest += new EventHandler(OnEndRequest);
  }

  private void Log(Exception ex)
  {
    if (ex != null)
    {
      var details = new System.Text.StringBuilder();
      var current = ex;
      while (current != null)
      {
        details.AppendLine(current.GetType().FullName);
        details.AppendLine(current.Message);
        details.AppendLine(current.StackTrace); // will include line numbers if PDB is present
        current = current.InnerException;
      }

      Log(details.ToString());
    }
  }
  private void Log(string message)
  {
    if (!string.IsNullOrEmpty(message))
    {
      string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
      message = "[" + timestamp + "] " + message;
      var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data/background_errors.log");
      System.IO.File.AppendAllText(logPath, message + Environment.NewLine);
    }
  }

  private void OnEndRequest(object sender, EventArgs e)
  {
    var application = (HttpApplication)sender;
    HttpContext context = application.Context;

    var userInfo = GetContextUserInformation(context);

    // if the user is authenticated, kick off a background task to revalidate
    // the user information, if needed
    if (ShouldRevalidate(userInfo))
    {

      // get the debouncer for this user or create a new one
      var debouncer = debouncers.GetOrAdd(userInfo.Sid, sid => new AsyncDebouncer());

      Task.Run(() =>
      {

        debouncer.DebounceAsync(5000, () =>
        {
          // run GetUserInformationFromPrincipalContext, which also updates the cache
          var authCookieHandler = new AuthUtilities.AuthCookieHandler();
          try
          {
            authCookieHandler.GetUserInformationFromPrincipalContext(userInfo.Username, userInfo.Domain);
          }
          catch (System.DirectoryServices.AccountManagement.PrincipalServerDownException ex)
          {
            Log("Could not reach domain controller to revalidate user " + userInfo.Domain + "\\" + userInfo.Username + ": " + ex.Message);
          }

          // clean up this debouncer
          AsyncDebouncer _;
          debouncers.TryRemove(userInfo.Sid, out _);

          return Task.CompletedTask;
        })
          // log  exceptions from the debounce task
          .ContinueWith(task => Log(task.Exception), TaskContinuationOptions.OnlyOnFaulted);
      });
    }
  }

  private bool ShouldRevalidate(UserInformation userInfo)
  {

    var isAuthenticated = userInfo != null && !userInfo.IsAnonymousUser;
    if (!isAuthenticated)
    {
      return false;
    }

    bool userCacheEnabled = System.Configuration.ConfigurationManager.AppSettings["UserCache.Enabled"] == "true";
    return userCacheEnabled;
  }

  private UserInformation GetContextUserInformation(HttpContext context)
  {
    return context.Items["UserInformation"] as AuthUtilities.UserInformation;
  }

  public void Dispose()
  {
    // Clean up any resources if necessary
  }
}

public class AsyncDebouncer
{
  private CancellationTokenSource _cts;
  private readonly object _lock = new object();

  public async Task DebounceAsync(int delayMs, Func<Task> action)
  {
    CancellationTokenSource cts;

    lock (_lock)
    {
      if (_cts != null)
      {
        _cts.Cancel();
        _cts.Dispose();
      }

      _cts = new CancellationTokenSource();
      cts = _cts;
    }

    try
    {
      // wait for debounce window, canceled if another call arrives
      await Task.Delay(delayMs, cts.Token);

      if (!cts.IsCancellationRequested)
      {
        await action();
      }
    }
    catch (OperationCanceledException)
    {
      // expected when debounce resets
    }
  }
}
