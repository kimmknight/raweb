using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.DataProtection;
using RAWeb.Server.Api;
using RAWeb.Server.Management;
using RAWeb.Server.Middleware;
using RAWeb.Server.Utilities;

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseIISIntegration();
builder.WebHost.UseKestrelHttpsConfiguration();

builder.Services.ConfigureHttpJsonOptions(options => {
  options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
  options.SerializerOptions.TypeInfoResolverChain.Add(WebApiJsonSerializerContext.Default);
});

builder.Services.AddTransient(_ => ManagementServiceClient.Proxy);

builder.Services.AddDataProtection()
    .ProtectKeysWithDpapi()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Constants.AppDataFolderPath, "DataProtection-Keys")))
    .SetApplicationName("RAWeb.Server");

FileSystemInitizalier.EnsureAppDataFolderContents();

// Use Windows Authentication for any endpoint with .RequireAuthorization("WindowsAuth")
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
builder.AddWindowsAuthorizationPolicy();

var app = builder.Build();
var pathBase = Environment.GetEnvironmentVariable("APP_PATH_BASE");
if (!string.IsNullOrEmpty(pathBase)) {
  app.UseForcedPathBase(pathBase);
}
app.UseRewriteNegotiateToNtlm();
app.UseWebSockets();
app.RegisterWebApi();
app.UseAuthUserStaleWhileRevalidate();
app.UseWindowsRadcCapture();
app.UseWorkspaceDiscovery();
app.UseEmbeddedFrontendResources();

// Set the console title
Console.Title = "RAWeb Server"; // for Windows Console Host
Console.Write("\x1b]0;RAWeb Server\x07\r"); // for other terminals


app.Run();


[JsonSerializable(typeof(string))]
public partial class AppJsonSerializerContext : JsonSerializerContext { }
