using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using RAWeb.Server.Api;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options => {
  options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
  options.SerializerOptions.TypeInfoResolverChain.Add(WebApiJsonSerializerContext.Default);
});

builder.Services.AddTransient(_ => ManagementServiceClient.Proxy);

builder.Services.AddDataProtection()
    .ProtectKeysWithDpapi()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Constants.AppDataFolderPath, "DataProtection-Keys")))
    .SetApplicationName("RAWeb.Server");

AppId.Initialize();

var app = builder.Build();
app.RegisterWebApi();

app.MapGet("/", () => {
  var currentVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
  return Results.Text($"RAWeb Server\nv{currentVersion}");
});


app.Run();

[JsonSerializable(typeof(string))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }
