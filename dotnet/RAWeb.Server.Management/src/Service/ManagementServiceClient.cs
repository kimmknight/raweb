using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RAWeb.Server.Management;

/// <summary>
/// Combined service interface implemented by the RAWeb Management Service.
/// Use <see cref="ManagementServiceClient.Proxy"/> to obtain an instance.
/// </summary>
public interface IManagementServiceDirectClient : IManagedResourceService, IManagedSystemTerminalServerSettings {
}

/// <summary>
/// Returns an <see cref="IManagementServiceDirectClient"/> backed by the named-pipe
/// transport, or directly if the current process is running with elevated privileges.
/// Works on both net462 and net10.0-windows.
/// </summary>
public static class ManagementServiceClient {
  public static IManagementServiceDirectClient Proxy {
    get {
      if (ElevatedPrivileges.Check()) {
        // If we have elevated privileges, we can call the management code directly.
        return new ManagementServiceDirectClient();
      }

      return new ManagementServicePipeClient();
    }
  }
}

/// <summary>
/// Implements <see cref="IManagementServiceDirectClient"/> by calling the management classes
/// directly. Used when the current process is already running with elevated privileges,
/// avoiding an unnecessary round-trip through the named-pipe service.
/// Also used by the named-pipe server itself to dispatch incoming calls.
/// </summary>
public class ManagementServiceDirectClient : IManagementServiceDirectClient {
  public bool AreConnectionsAllowed() => SystemTerminalServerSettings.AreConnectionsAllowed;

  public void InitializeRegistryPaths(string? collectionName = null) {
    new SystemRemoteApps(collectionName).EnsureRegistryPathExists();
  }

  public void InitializeDesktopRegistryPaths(string collectionName) {
    var defaultDesktop = SystemDesktop.FromRegistry(collectionName, collectionName);
    if (defaultDesktop is null) {
      defaultDesktop = new SystemDesktop(collectionName, collectionName);
      defaultDesktop.WriteToRegistry();
    }
  }

  public void RestorePackagedAppIconPaths(string? collectionName) {
    new SystemRemoteApps(collectionName).GetAllRegisteredApps(restorePackagedAppIconPaths: true);
  }

  public InstalledApps ListInstalledApps(string? userSid = null) {
    var packagedApps = InstalledApps.FromAppPackages();
    var startMenuApps = InstalledApps.FromStartMenu();
    var userStartMenuApps = userSid is null ? [] : InstalledApps.FromStartMenu(new SecurityIdentifier(userSid));
    return new InstalledApps([.. packagedApps, .. startMenuApps, .. userStartMenuApps]);
  }

  public void WriteRemoteAppToRegistry(SystemRemoteApps.SystemRemoteApp app) {
    if (app is null) throw new ArgumentNullException(nameof(app));
    app.WriteToRegistry();
  }

  public void DeleteRemoteAppFromRegistry(SystemRemoteApps.SystemRemoteApp app) {
    if (app is null) throw new ArgumentNullException(nameof(app));
    app.DeleteFromRegistry();
  }

  public void WriteDesktopToRegistry(SystemDesktop desktop) {
    if (desktop is null) throw new ArgumentNullException(nameof(desktop));
    desktop.WriteToRegistry();
  }

  public void DeleteDesktopFromRegistry(SystemDesktop desktop) {
    if (desktop is null) throw new ArgumentNullException(nameof(desktop));
    desktop.DeleteFromRegistry();
  }

  public Stream GetWallpaperStream(SystemDesktop desktop, ManagedFileResource.ImageTheme theme, string? userSid) {
    if (desktop is null) throw new ArgumentNullException(nameof(desktop));
    return desktop.GetWallpaperStream(theme, userSid is null ? null : new SecurityIdentifier(userSid));
  }
}

/// <summary>
/// Implements <see cref="IManagementServiceDirectClient"/> by communicating with the
/// RAWeb Management Service over a local named pipe.
/// </summary>
/// <remarks>
/// We use this custom JSON-over-named-pipe communication method instead
/// of Windows Communication Foundation (WCF) because it works on both
/// .NET Framework 4.6.2 and .NET 10.0, and it supports Native AOT.
///
/// One pipe connection is opened per call. The pipe name is
/// "raweb-management-{appPoolName}" where appPoolName is read from
/// the APP_POOL_ID environment variable (default: "raweb").
/// </remarks>
internal class ManagementServicePipeClient : IManagementServiceDirectClient {
  private static string AppPoolName => Environment.GetEnvironmentVariable("APP_POOL_ID") ?? "raweb-kestral";
  private static string PipeAppPoolName => AppPoolName == "IISExpressAppPool" ? "raweb-iisexpress" : AppPoolName;
  private static string PipeName => $"raweb-management-{PipeAppPoolName}";

  private const int ConnectTimeoutMs = 5000;

  public bool AreConnectionsAllowed() {
    var response = Call("AreConnectionsAllowed");
    return response["data"]?.GetValue<bool>() ?? false;
  }

  public void InitializeRegistryPaths(string? collectionName = null) {
    Call("InitializeRegistryPaths", new JsonObject { ["collectionName"] = collectionName });
  }

  public void InitializeDesktopRegistryPaths(string collectionName) {
    Call("InitializeDesktopRegistryPaths", new JsonObject { ["collectionName"] = collectionName });
  }

  public void RestorePackagedAppIconPaths(string? collectionName) {
    Call("RestorePackagedAppIconPaths", new JsonObject { ["collectionName"] = collectionName });
  }

  public InstalledApps ListInstalledApps(string? userSid = null) {
    var response = Call("ListInstalledApps", new JsonObject { ["userSid"] = userSid });
    var items = response["data"]?.Deserialize(ManagementJsonContext.Default.InstalledAppArray) ?? [];
    return new InstalledApps([.. items]);
  }

  public void WriteRemoteAppToRegistry(SystemRemoteApps.SystemRemoteApp app) {
    Call("WriteRemoteAppToRegistry", new JsonObject {
      ["app"] = JsonSerializer.SerializeToNode(app, ManagementJsonContext.Default.SystemRemoteApp)
    });
  }

  public void DeleteRemoteAppFromRegistry(SystemRemoteApps.SystemRemoteApp app) {
    Call("DeleteRemoteAppFromRegistry", new JsonObject {
      ["app"] = JsonSerializer.SerializeToNode(app, ManagementJsonContext.Default.SystemRemoteApp)
    });
  }

  public void WriteDesktopToRegistry(SystemDesktop desktop) {
    Call("WriteDesktopToRegistry", new JsonObject {
      ["desktop"] = JsonSerializer.SerializeToNode(desktop, ManagementJsonContext.Default.SystemDesktop)
    });
  }

  public void DeleteDesktopFromRegistry(SystemDesktop desktop) {
    Call("DeleteDesktopFromRegistry", new JsonObject {
      ["desktop"] = JsonSerializer.SerializeToNode(desktop, ManagementJsonContext.Default.SystemDesktop)
    });
  }

  public Stream GetWallpaperStream(SystemDesktop desktop, ManagedFileResource.ImageTheme theme, string? userSid) {
    return CallForStream("GetWallpaperStream", new JsonObject {
      ["desktop"] = JsonSerializer.SerializeToNode(desktop, ManagementJsonContext.Default.SystemDesktop),
      ["theme"] = (int)theme,
      ["userSid"] = userSid,
    });
  }

  /// <summary>
  /// Requests the return value of a management service method call.
  /// </summary>
  private JsonObject Call(string method, JsonObject? parameters = null) {
    using var pipe = OpenPipe();
    var request = new JsonObject { ["method"] = method };
    if (parameters is not null) {
      foreach (var prop in parameters) {
        request[prop.Key] = prop.Value?.DeepClone();
      }
    }
    SendRequest(pipe, request);
    return ReadResponseLine(pipe);
  }

  /// <summary>
  /// Requests a stream from the management service. The service will first send a JSON
  /// object containing the content length, followed by the raw stream bytes.
  ///
  /// Only methods that return a stream (e.g. GetWallpaperStream) should use this helper.
  /// All other methods should use the Call() helper method.
  /// </summary>
  /// <exception cref="EndOfStreamException"></exception>
  private MemoryStream CallForStream(string method, JsonObject? parameters = null) {
    // Ask the service to prepare a stream.
    using var pipe = OpenPipe();
    var request = new JsonObject { ["method"] = method };
    if (parameters is not null) {
      foreach (var prop in parameters) {
        request[prop.Key] = prop.Value?.DeepClone();
      }
    }
    SendRequest(pipe, request);

    // The service will respond with a JSON object containing the content length,
    // followed by the raw stream bytes. We first read the JSON response to get
    // the content length.
    var response = ReadResponseLine(pipe);
    var contentLength = response["contentLength"]?.GetValue<int>() ?? 0;

    // Read the specified number of bytes from the pipe and return them as a MemoryStream.
    var bytes = new byte[contentLength];
    var totalRead = 0;
    while (totalRead < contentLength) {
      var n = pipe.Read(bytes, totalRead, contentLength - totalRead);
      if (n == 0) throw new EndOfStreamException("Unexpected EOF reading stream bytes.");
      totalRead += n;
    }
    return new MemoryStream(bytes);
  }

  /// <summary>
  /// Creates a connection to the management service named pipe.
  /// </summary>
  /// <exception cref="EndpointNotFoundException">If the connection was not made in time.</exception>
  private NamedPipeClientStream OpenPipe() {
    var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut);
    try {
      pipe.Connect(ConnectTimeoutMs);
    }
    catch (TimeoutException) {
      pipe.Dispose();
      throw new EndpointNotFoundException($"The RAWeb Management Service is not running (pipe: {PipeName}).");
    }
    catch (UnauthorizedAccessException) {
      pipe.Dispose();
      throw new PipeAccessDeniedException($"Access to the RAWeb Management Service was denied. Ensure that the current user has permission to access it (pipe: {PipeName}).");
    }
    return pipe;
  }

  /// <summary>
  /// Sends a JSON request object to the service over the given pipe.
  /// </summary>
  private static void SendRequest(Stream pipe, JsonObject request) {
    var bytes = Encoding.UTF8.GetBytes(request.ToJsonString() + "\n");
    pipe.Write(bytes, 0, bytes.Length);
  }

  /// <summary>
  /// Reads a single line of JSON from the pipe, parses it, and checks for an "ok" status.
  /// If the "ok" property is not true, an exception is thrown with the error
  /// message from the "error" property (or a default message if "error" is not provided).
  /// </summary>
  /// <exception cref="InvalidOperationException"></exception>
  private static JsonObject ReadResponseLine(Stream pipe) {
    // Read bytes until we get a newline, assuming that
    // a single JSON object is sent per line.
    var sb = new StringBuilder();
    var buf = new byte[1];
    while (true) {
      if (pipe.Read(buf, 0, 1) == 0) break;
      if (buf[0] == (byte)'\n') break;
      if (buf[0] != (byte)'\r') sb.Append((char)buf[0]);
    }

    // Parse the JSON response and check for an "ok" status.
    var json = JsonNode.Parse(sb.ToString())?.AsObject()
      ?? throw new InvalidOperationException("Management service returned an empty or invalid response.");
    if (json["ok"]?.GetValue<bool>() != true) {
      throw new InvalidOperationException(
        // If an error message is provided, expose it to the consumer as the exception message.
        (string?)json["error"] ?? "Management service call failed."
      );
    }
    return json;
  }
}

/// <summary>
/// Thrown when the RAWeb Management Service cannot be reached.
/// </summary>
public class EndpointNotFoundException(string message) : Exception(message) { }

public class PipeAccessDeniedException(string message) : Exception(message) { }
