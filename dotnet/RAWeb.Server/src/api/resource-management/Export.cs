using System.IO.Compression;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class ExportResourceEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/management/resources/export-registered/{*identifier}", (Delegate)Handle);
  }

  /// <summary>
  /// Exports a registered resources as a .resource file.
  /// </summary>
  /// <param name="identifier">The key for the RemoteApp in the registry or the file name of a managed .resource file in App_Data/managed-resources</param>
  private static async Task<IResult> Handle(string identifier, HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null || !userInfo.AuthTicketLevel.IsAdmin) {
      return Results.Forbid();
    }

    var resources = GetRegisteredAppsEndpoint.GetPopulatedManagedResources(ctx);
    var resource = resources.GetByIdentifier(identifier);

    using var outputStream = new MemoryStream();
    var resourceFile = resource.ToResourceFile();
    using (resourceFile.archive) {
      using var outputArchive = new ZipArchive(outputStream, ZipArchiveMode.Create, true);
      foreach (var sourceEntry in resourceFile.archive.Entries) {
        var newEntry = outputArchive.CreateEntry(sourceEntry.FullName);
        newEntry.LastWriteTime = sourceEntry.LastWriteTime;
        using var sourceStream = sourceEntry.Open();
        using var destinationStream = newEntry.Open();
        await sourceStream.CopyToAsync(destinationStream);
      }
    }

    ctx.Response.ContentType = "application/x-tsresource";
    ctx.Response.Headers.ContentDisposition = $"attachment; filename=\"{resource.Name}.tsresource\"";

    outputStream.Position = 0;
    await outputStream.CopyToAsync(ctx.Response.Body);

    return Results.Empty;
  }
}
