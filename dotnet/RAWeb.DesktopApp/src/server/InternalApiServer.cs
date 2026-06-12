using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RAWeb.Server.Api;
using RAWeb.Server.Middleware;
using RAWeb.Server.Utilities;

namespace RAWeb.DesktopApp.InternalServer;

internal sealed class ServerUtils {
  /// <summary>
  /// Find a port that is currently free.
  /// </summary>
  private static int FindFreePort() {
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    return ((IPEndPoint)listener.LocalEndpoint).Port;
  }

  /// <summary>
  /// Generates a random secret that the a client will need to provide
  /// in order for the internal API server to accept the request.
  /// <br/><br/>
  /// Use <see cref="AuthSecretCookieName"/> for the name of the cookie.
  /// </summary>
  private static string GenerateAuthSecret() {
    var bytes = new byte[32];
    RandomNumberGenerator.Fill(bytes);
    return Convert.ToBase64String(bytes);
  }

  public const string AuthSecretCookieName = "RAWeb-InternalApi-AuthSecret";

  private static void PopulateAppDataFolder(WebApplicationBuilder builder) {
    // AppRoot defaults to the folder of the executable, but since the desktop
    // app may be running in packaged mode (MSIX), we need to make sure that
    // this path is set to a location that is always writable.
    Constants.AppRoot = AppStorage.StorageFolder.Path;

    FileSystemInitializer.EnsureAppDataFolderContents();

    builder.Services.AddDataProtection()
        .ProtectKeysWithDpapi()
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Constants.AppDataFolderPath, "DataProtection-Keys")))
        .SetApplicationName("RAWeb.Server");

  }

  /// <summary>
  /// Starts an internal ASP.NET Core server that hosts the RAWeb API
  /// and serves the embedded frontend resources.
  /// </summary>
  /// <remarks>
  /// This is a pared-down version of the server that is used in RAWeb.Server's
  /// Program.cs.
  /// <br/><br/>
  /// This server requires a cookie that verifies that the request is coming
  /// from within the desktop app. All other requests are rejected with a 403
  /// status code.
  /// <br/><br/>
  /// This server always spoofs the authenticated user as the current Windows user.
  /// </remarks>
  /// <returns></returns>
  public static async Task<(WebApplication App, string BaseUrl, string AuthSecret)> StartServer(
    (string, string)? styleTagContent = null,
    (string, string)? scriptTagContent = null,
    Func<(string, string)>? getStyleTagContent = null,
    Func<(string, string)>? getScriptTagContent = null
  ) {
    var builder = WebApplication.CreateBuilder();

    // confirgure the server to listen on the free port
    var port = FindFreePort();
    builder.WebHost.ConfigureKestrel(k => {
      k.Listen(IPAddress.Loopback, port);
    });

    // add the JSON serializers from RAWeb.Server
    builder.Services.ConfigureHttpJsonOptions(options => {
      options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
      options.SerializerOptions.TypeInfoResolverChain.Add(WebApiJsonSerializerContext.Default);
    });

    // populate the App_Data folder
    PopulateAppDataFolder(builder);
    PoliciesManager.Set("App.Auth.Anonymous", "always"); // hides user from titlebar, among other effects

    var app = builder.Build();

    // block requests that do not have the correct auth secret cookie
    var authSecret = GenerateAuthSecret();
    app.UseRequireCookieValue(AuthSecretCookieName, authSecret);

    if (styleTagContent != null) {
      app.UseHeadInjection(UseHeadInjectionMiddleware.InjectionType.Style, styleTagContent.Value.Item1, styleTagContent.Value.Item2);
    }
    if (getStyleTagContent != null) {
      var (content, id) = getStyleTagContent();
      app.UseHeadInjection(UseHeadInjectionMiddleware.InjectionType.Style, content, id);
    }

    if (scriptTagContent != null) {
      app.UseHeadInjection(UseHeadInjectionMiddleware.InjectionType.Script, scriptTagContent.Value.Item1, scriptTagContent.Value.Item2);
    }
    if (getScriptTagContent != null) {
      var (content, id) = getScriptTagContent();
      app.UseHeadInjection(UseHeadInjectionMiddleware.InjectionType.Script, content, id);
    }

    app.UseLocalStoragePersistence(AppStorage.StorageFolder);
    app.UseWindowsIdentityAsUserInfo();
    app.UseDisableRegistryResourceManagement();
    app.RegisterWebApi();
    app.UseEmbeddedFrontendResources();

    await app.StartAsync();

    return (app, $"http://localhost:{port}/", authSecret);
  }
}
