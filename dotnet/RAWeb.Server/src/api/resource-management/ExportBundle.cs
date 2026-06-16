using System.IO.Compression;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class ExportResourcesEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/management/resources/export-registered", (Delegate)Handle);
  }

  /// <summary>
  /// Exports all registered resources as a zip file containing .resource files for
  /// each resource.
  /// </summary>
  private static async Task<IResult> Handle(HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null || !userInfo.IsLocalAdministrator) {
      return Results.Forbid();
    }

    var resources = GetRegisteredAppsEndpoint.GetPopulatedManagedResources(ctx);

    var tempFilePath = Path.GetTempFileName();
    try {
      using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
      using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create)) {

        // track the entry names we've already used to avoid conflicts within the zip file
        var insertedEntryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // convert each resource into an in-memory .resource file and add it to the export zip
        foreach (var resource in resources) {
          try {
            var resourceFile = resource.ToResourceFile();

            // prefer the resource's name, appending incremented numbers if there are conflicts
            var entryName = resource.Name;
            var entryNameWithoutExtension = Path.GetFileNameWithoutExtension(entryName);
            var increment = 0;
            while (insertedEntryNames.Contains(entryName)) {
              entryName = $"{entryNameWithoutExtension} ({increment})";
            }

            var entry = archive.CreateEntry($"{entryName}.tsresource", CompressionLevel.NoCompression);
            entry.LastWriteTime = resourceFile.archive.Entries
              .Select(e => e.LastWriteTime)
              .OrderByDescending(t => t)
              .FirstOrDefault(entry.LastWriteTime);
            using (var entryStream = entry.Open())
            using (var innerZip = new ZipArchive(entryStream, ZipArchiveMode.Create, true)) {
              foreach (var sourceEntry in resourceFile.archive.Entries) {
                var newEntry = innerZip.CreateEntry(sourceEntry.FullName);
                newEntry.LastWriteTime = sourceEntry.LastWriteTime;
                using var sourceStream = sourceEntry.Open();
                using var destinationStream = newEntry.Open();
                await sourceStream.CopyToAsync(destinationStream);
              }
            }
          }
          catch (Exception ex) {
            Console.WriteLine($"Failed to export resource {resource.Identifier}: {ex}");
          }
        }
      }

      ctx.Response.ContentType = "application/x-tsresourcebundle";
      var resolver = new AliasResolver();
      var machineName = resolver.Resolve(Environment.MachineName);
      ctx.Response.Headers.ContentDisposition = $"attachment; filename=\"{machineName}.tsresourcebundle\"";

      using var exportStream = File.OpenRead(tempFilePath);
      await exportStream.CopyToAsync(ctx.Response.Body);
    }
    finally {
      File.Delete(tempFilePath);
    }

    return Results.Empty;
  }
}
