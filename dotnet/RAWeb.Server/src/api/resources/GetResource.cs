using RAWeb.Server.Utilities;
using static RAWeb.Server.Utilities.ResourceContentsResolver;

namespace RAWeb.Server.Api;

internal static class GetResourceEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/resources/{*path}", Handle);
    app.MapGet("/get-rdp.aspx", Handle);
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="path">relative path to the rdp file, or the name of the registry key</param>
  /// <param name="from">rdp or registry</param>
  /// <returns></returns>
  private static IResult Handle(HttpContext ctx, string? path = null, string from = "rdp") {
    // get authentication information
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null) {
      return Results.Unauthorized();
    }

    // resolve the resource based on the parameters
    ResourceOrigin? fromEnum = from.ToLowerInvariant() switch {
      "rdp" => ResourceOrigin.Rdp,
      "registry" => ResourceOrigin.Registry,
      "mr" => ResourceOrigin.ManagedResource,
      "registrydesktop" => ResourceOrigin.RegistryDesktop,
      _ => null,
    };
    if (fromEnum is null) {
      return Results.BadRequest("Parameter 'from' must be either 'rdp', 'mr', 'registry', or 'registryDesktop'.");
    }

    var resolved = ResolveResource(userInfo!, path ?? "", fromEnum.Value);

    // if the resource resolution failed, return the appropriate error response
    // (can fail due to permissions or invalid parameters)
    if (resolved is FailedResourceResult failed) {
      if (failed.ErrorMessage is not null) {
        return Results.Problem(failed.ErrorMessage, statusCode: (int)failed.PermissionHttpStatus);
      }
      else {
        return Results.StatusCode((int)failed.PermissionHttpStatus);
      }
    }

    // if the resource was resolved successfully, return the RDP file
    if (resolved is ResolvedResourceResult result) {
      return Results.File(
        System.Text.Encoding.UTF8.GetBytes(result.RdpFileContents),
        "application/x-rdp",
        result.FileName
      );
    }

    throw new Exception("Unrecognized ResourceResult type.");
  }
}
