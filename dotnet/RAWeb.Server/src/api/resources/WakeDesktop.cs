using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;
using static RAWeb.Server.Utilities.ResourceContentsResolver;

namespace RAWeb.Server.Api;

internal static class WakeDesktopEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapPost("/api/resources/wake/{*path}", Handle).RequireAuthorization("WindowsAuth");
  }

  private static IResult Handle(HttpContext ctx, string? path = null) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null) {
      return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(path)) {
      return Results.BadRequest("Resource identifier is required.");
    }

    // Attempt to resolve the resource from Registry (since Desktops are there)
    var resolved = ResourceContentsResolver.ResolveResource(userInfo, path, ResourceOrigin.RegistryDesktop);
    
    if (resolved is FailedResourceResult failed) {
      return Results.Problem(failed.ErrorMessage, statusCode: (int)failed.PermissionHttpStatus);
    }

    if (resolved is ResolvedResourceResult result) {
      // Find the MacAddress. We need to query the desktop from SystemDesktop directly.
      var collectionName = PoliciesManager.RawPolicies["App.Publishing.CollectionName"] ?? "";
      var desktop = SystemDesktop.FromRegistry(collectionName, path);

      if (desktop == null) {
        return Results.NotFound("Desktop not found.");
      }

      var macAddress = desktop.MacAddress;
      if (string.IsNullOrWhiteSpace(macAddress)) {
        return Results.BadRequest("No MAC Address configured for this desktop.");
      }

      try {
        WakeOnLan(macAddress);
        return Results.Ok(new { message = "Magic packet sent successfully." });
      } catch (Exception ex) {
        return Results.Problem($"Failed to send Wake-on-LAN packet: {ex.Message}");
      }
    }

    return Results.NotFound("Resource not found.");
  }

  private static void WakeOnLan(string macAddress) {
    // Remove non-hex characters
    macAddress = Regex.Replace(macAddress, "[^0-9a-fA-F]", "");
    if (macAddress.Length != 12) {
      throw new ArgumentException("Invalid MAC address format.");
    }

    var macBytes = new byte[6];
    for (int i = 0; i < 6; i++) {
      macBytes[i] = Convert.ToByte(macAddress.Substring(i * 2, 2), 16);
    }

    // Create WOL packet (6 bytes of 0xFF followed by 16 repetitions of the MAC address)
    var packet = new byte[6 + (16 * 6)];
    for (int i = 0; i < 6; i++) {
      packet[i] = 0xFF;
    }
    for (int i = 1; i <= 16; i++) {
      Array.Copy(macBytes, 0, packet, i * 6, 6);
    }

    using var client = new UdpClient();
    client.EnableBroadcast = true;
    client.Send(packet, packet.Length, new IPEndPoint(IPAddress.Broadcast, 9));
  }
}
