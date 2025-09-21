using AuthUtilities;
using System;
using System.IO;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace RAWebServer.Api
{
  /// <summary>
  /// A web API for RAWeb Server. This web API expects reads controllers
  /// from the api sub-folder. Controllers should have route prefixes
  /// (e.g. <c>[RoutePrefix("api/auth")]</c>) that declare where they
  /// are used in the API.
  /// </summary>
  public static class WebApi
  {
    public static void Register(HttpConfiguration config)
    {
      // enable attribute routing
      config.MapHttpAttributeRoutes();

      // disable XML formatting
      config.Formatters.Remove(config.Formatters.XmlFormatter);

      // enable plain text formatting with higher priority than JSON
      // so that all plain text bodies are read as strings
      config.Formatters.Insert(0, new TextMediaTypeFormatter());
    }
  }

  /// <summary>
  /// An authorization attribute that only allows access to authenticated users.
  /// <br/><br/>
  /// If the user is not authenticated, a 401 Unauthorized response is returned.
  /// <br/><br/>
  /// Usage:
  /// <code>
  /// [RequireAuthentication]
  /// public IHttpActionResult SomeMethod() { ... }
  /// </code>
  /// </summary>
  public class RequireAuthenticationAttribute : AuthorizeAttribute
  {
    protected override bool IsAuthorized(HttpActionContext actionContext)
    {
      var authCookieHandler = new AuthCookieHandler();
      var userInfo = authCookieHandler.GetUserInformationSafe(HttpContext.Current.Request);

      return userInfo != null; // only allow if authenticated
    }

    protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
    {
      HttpContext.Current.Response.StatusCode = 401;
      HttpContext.Current.Response.End();
    }
  }

  /// <summary>
  /// An authorization attribute that only allows access to local administrators.
  /// <br/><br/>
  /// Requires the user to be authenticated before checking for local admin status.
  /// If the user is not authenticated, a 401 Unauthorized response is returned.
  /// If the user is authenticated but not a local admin, a 403 Forbidden response is returned.
  /// <br/><br/>
  /// Usage:
  /// <code>
  /// [RequireLocalAdministrator]
  /// public IHttpActionResult SomeMethod() { ... }
  /// </code>
  /// </summary>
  public class RequireLocalAdministratorAttribute : AuthorizeAttribute
  {
    protected override bool IsAuthorized(HttpActionContext actionContext)
    {
      var authCookieHandler = new AuthCookieHandler();
      var userInfo = authCookieHandler.GetUserInformationSafe(HttpContext.Current.Request);

      if (userInfo == null)
      {
        return false;
      }

      return userInfo.IsLocalAdministrator;
    }

    protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
    {
      var authCookieHandler = new AuthCookieHandler();
      var userInfo = authCookieHandler.GetUserInformationSafe(HttpContext.Current.Request);

      if (userInfo == null)
      {
        HttpContext.Current.Response.StatusCode = 401;
        HttpContext.Current.Response.End();
      }

      if (!userInfo.IsLocalAdministrator)
      {
        HttpContext.Current.Response.StatusCode = 403;
        HttpContext.Current.Response.End();
      }
    }
  }

  /// <summary>
  /// A formatter that allows us to read plain text bodies in Web API methods.
  /// <br/><br/>
  /// Adapted from: https://stackoverflow.com/a/29914360/9861747
  /// </summary>
  public class TextMediaTypeFormatter : MediaTypeFormatter
  {
    public TextMediaTypeFormatter()
    {
      SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));
    }

    public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
    {
      var taskCompletionSource = new TaskCompletionSource<object>();
      try
      {
        var memoryStream = new MemoryStream();
        readStream.CopyTo(memoryStream);
        var s = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
        taskCompletionSource.SetResult(s);
      }
      catch (Exception e)
      {
        taskCompletionSource.SetException(e);
      }
      return taskCompletionSource.Task;
    }

    public override bool CanReadType(Type type)
    {
      // only support reading plain strings
      return type == typeof(string);
    }

    public override bool CanWriteType(Type type)
    {
      return false;
    }
  }
}
