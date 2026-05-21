using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.DataProtection;
using RAWeb.Server.Api;
using RAWeb.Server.Management;
using RAWeb.Server.Middleware;
using RAWeb.Server.Utilities;

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseIISIntegration();

builder.Services.ConfigureHttpJsonOptions(options => {
  options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
  options.SerializerOptions.TypeInfoResolverChain.Add(WebApiJsonSerializerContext.Default);
});

builder.Services.AddTransient(_ => ManagementServiceClient.Proxy);

builder.Services.AddDataProtection()
    .ProtectKeysWithDpapi()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Constants.AppDataFolderPath, "DataProtection-Keys")))
    .SetApplicationName("RAWeb.Server");

// ensure that the App_Data folder and its default contents exist before the application starts
var assembly = Assembly.GetExecutingAssembly();
assembly.GetManifestResourceNames()
  .Where(x => x.StartsWith("defaultappdata/"))
  .Select(resourceName => {
    var fileName = resourceName.Substring("defaultappdata/".Length);
    var filePath = Path.Combine(Constants.AppDataFolderPath, fileName);
    return (resourceName, filePath);
  })
  .Where(info => !File.Exists(info.filePath))
  .ToList()
  .ForEach(info => {
    var (resourceName, filePath) = info;

    // create the directory if it does not exist
    var directory = Path.GetDirectoryName(filePath);
    if (directory is not null && !Directory.Exists(directory)) {
      Directory.CreateDirectory(directory);
    }

    // copy the embedded resource to the file system
    using var resourceStream = assembly.GetManifestResourceStream(resourceName);
    if (resourceStream is not null) {
      using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
      resourceStream.CopyTo(fileStream);
    }
  });
AppId.Initialize();

// Use Windows Authentication for any endpoint with .RequireAuthorization("WindowsAuth")
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
builder.AddWindowsAuthorizationPolicy();

var app = builder.Build();
var pathBase = Environment.GetEnvironmentVariable("APP_PATH_BASE");
if (!string.IsNullOrEmpty(pathBase)) {
  app.UseForcedPathBase(pathBase);
}
app.UseWebSockets();
app.RegisterWebApi();
app.UseAuthUserStaleWhileRevalidate();
app.UseWindowsRadcCapture();
app.UseWorkspaceDiscovery();
app.UseEmbeddedFrontendResources();

// app.MapGet("/", () => {
//   var currentVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
//   return Results.Text($"RAWeb Server\nv{currentVersion}");
// });

app.MapGet("/resources", () => {
  var assembly = Assembly.GetExecutingAssembly();
  var resourceNames = assembly.GetManifestResourceNames();

  var output = string.Join("\n", resourceNames.OrderBy(x => x));
  return Results.Text(output);
});


// Set the console title
Console.Title = "RAWeb Server"; // for Windows Console Host
Console.Write("\x1b]0;RAWeb Server\x07"); // for other terminals

app.Run();


[JsonSerializable(typeof(string))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }
