using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;
using static RAWeb.Server.Utilities.ResourceContentsResolver;

namespace RAWeb.Server.Api;

internal static class WakeDesktopEndpoint {
  /// <summary>
  /// The ports to which Wake-on-LAN magic packets are conventionally sent.
  /// </summary>
  private static readonly int[] s_wakeOnLanPorts = [7, 9];

  internal static void Map(IEndpointRouteBuilder app) {
    app.MapPost("/api/resources/wake/{*path}", Handle);
  }

  /// <summary>
  /// Sends a Wake-on-LAN magic packet for the MAC address stored on a managed .resource file.
  /// <br /><br />
  /// Only managed .resource files are supported. Registry resources do not store a MAC address
  /// because if their host device is offline, that means the RAWeb server is also offline,
  /// which means there would be nothing available to send the magic packet.
  /// </summary>
  /// <param name="path">
  /// The path of the managed .resource file relative to App_Data, in the same form used by
  /// the resource file endpoint (e.g. <c>managed-resources/MyDevice.resource</c>). Paths
  /// outside of App_Data/managed-resources are rejected.
  /// </param>
  private static IResult Handle(HttpContext ctx, string? path = null) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null) {
      return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(path)) {
      return Results.BadRequest("A resource path is required.");
    }

    // resolve the resource so that the path is validated and the user's permission
    // to access the resource is checked before revealing anything about it
    ResourceResult resolved;
    try {
      resolved = ResolveResource(userInfo, path, ResourceOrigin.ManagedResource);
    }
    catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException) {
      return Results.NotFound();
    }
    if (resolved is FailedResourceResult failed) {
      return failed.ErrorMessage is not null
        ? Results.Problem(failed.ErrorMessage, statusCode: (int)failed.PermissionHttpStatus)
        : Results.StatusCode((int)failed.PermissionHttpStatus);
    }
    if (resolved is not ResolvedResourceResult) {
      return Results.NotFound();
    }

    // read the MAC address from the resource file
    var rootedPath = Path.GetFullPath(Path.Combine(Constants.AppDataFolderPath, path));
    var resource = ManagedFileResource.FromResourceFile(rootedPath);
    if (string.IsNullOrWhiteSpace(resource.MacAddress)) {
      return Results.Problem("No MAC address is configured for this resource.", statusCode: 400);
    }

    try {
      SendMagicPacket(resource.MacAddress);
      return Results.NoContent();
    }
    catch (Exception ex) {
      return Results.Problem($"Failed to send the Wake-on-LAN packet: {ex.Message}", statusCode: 500);
    }
  }

  /// <summary>
  /// Broadcasts a Wake-on-LAN magic packet for the specified MAC address.
  /// <br /><br />
  /// The packet is sent to the limited broadcast address as well as the directed broadcast
  /// address of every active local network, because routers and virtual switches do not
  /// always forward the limited broadcast address to the network that contains the target machine.
  /// </summary>
  /// <exception cref="ArgumentException">If the MAC address is not valid.</exception>
  private static void SendMagicPacket(string macAddress) {
    // a magic packet is 6 bytes of 0xFF followed by 16 repetitions of the MAC address
    var macBytes = MacAddresses.ToBytes(macAddress);
    var packet = new byte[6 + (16 * 6)];
    for (var i = 0; i < 6; i++) {
      packet[i] = 0xFF;
    }
    for (var repetition = 1; repetition <= 16; repetition++) {
      Array.Copy(macBytes, 0, packet, repetition * 6, 6);
    }

    using var client = new UdpClient { EnableBroadcast = true };

    Exception? lastException = null;
    var sentCount = 0;
    foreach (var address in GetBroadcastAddresses()) {
      foreach (var port in s_wakeOnLanPorts) {
        try {
          client.Send(packet, packet.Length, new IPEndPoint(address, port));
          sentCount++;
        }
        catch (SocketException ex) {
          // an unreachable network should not prevent the other networks from being tried
          lastException = ex;
        }
      }
    }

    if (sentCount == 0) {
      throw lastException ?? new InvalidOperationException("No network was available to broadcast on.");
    }
  }

  /// <summary>
  /// Gets the limited broadcast address plus the directed broadcast address
  /// of every active IPv4 network to which the server is attached.
  /// </summary>
  private static List<IPAddress> GetBroadcastAddresses() {
    // IPAddress.Broadcast is 255.255.255.255, but most routers silently drop packets
    // sent to that address
    var addresses = new List<IPAddress> { IPAddress.Broadcast };

    foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces()) {
      if (networkInterface.OperationalStatus != OperationalStatus.Up) {
        continue;
      }
      if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback) {
        continue;
      }

      foreach (var unicastAddress in networkInterface.GetIPProperties().UnicastAddresses) {
        if (unicastAddress.Address.AddressFamily != AddressFamily.InterNetwork) {
          continue;
        }
        if (unicastAddress.IPv4Mask is null) {
          continue;
        }

        // the directed broadcast address is the network address with every host bit set
        // (e.g. 192.168.0.x with subnet mask 255.255.255.0 has a network address of 
        //  192.168.0.1, useable host addresses of 192.168.0.1-102.168.0.254, and a
        //  broadcast address of 192.168.0.255)
        // (255 is the decimal form of 11111111, all bits in a byte set to 1)
        var addressBytes = unicastAddress.Address.GetAddressBytes();
        var maskBytes = unicastAddress.IPv4Mask.GetAddressBytes();
        if (addressBytes.Length != maskBytes.Length) {
          continue;
        }

        var broadcastBytes = new byte[addressBytes.Length];
        for (var i = 0; i < addressBytes.Length; i++) {
          broadcastBytes[i] = (byte)(addressBytes[i] | (byte)~maskBytes[i]);
        }

        var broadcastAddress = new IPAddress(broadcastBytes);
        if (!addresses.Contains(broadcastAddress)) {
          addresses.Add(broadcastAddress);
        }
      }
    }

    return addresses;
  }
}
