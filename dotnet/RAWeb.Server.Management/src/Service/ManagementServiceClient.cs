using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RAWeb.Server.Management;

/// <summary>
/// Combined service interface implemented by the RAWeb Management Service.
/// Use <see cref="ManagementServiceClient.Proxy"/> to obtain an instance.
/// </summary>
public interface IManagementServiceHost : IManagedResourceService, IManagedSystemTerminalServerSettings {
}

/// <summary>
/// Returns an <see cref="IManagementServiceHost"/> backed by the named-pipe
/// transport. Works on both net462 and net10.0-windows.
/// </summary>
public static class ManagementServiceClient {
  public static IManagementServiceHost Proxy => new ManagementServicePipeClient();
}

/// <summary>
/// Implements <see cref="IManagementServiceHost"/> by communicating with the
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
internal class ManagementServicePipeClient : IManagementServiceHost {
  private static string AppPoolName => Environment.GetEnvironmentVariable("APP_POOL_ID") ?? "raweb";
  private static string PipeAppPoolName => AppPoolName == "IISExpressAppPool" ? "raweb" : AppPoolName;
  private static string PipeName => $"raweb-management-{PipeAppPoolName}";

  private const int ConnectTimeoutMs = 5000;

  public bool AreConnectionsAllowed() {
    var response = Call("AreConnectionsAllowed");
    return response.Value<bool>("data");
  }

  public void InitializeRegistryPaths(string? collectionName = null) {
    Call("InitializeRegistryPaths", new JObject { ["collectionName"] = collectionName });
  }

  public void InitializeDesktopRegistryPaths(string collectionName) {
    Call("InitializeDesktopRegistryPaths", new JObject { ["collectionName"] = collectionName });
  }


  public void RestorePackagedAppIconPaths(string? collectionName) {
    Call("RestorePackagedAppIconPaths", new JObject { ["collectionName"] = collectionName });
  }

  public InstalledApps ListInstalledApps(string? userSid = null) {
    var response = Call("ListInstalledApps", new JObject { ["userSid"] = userSid });
    var items = response["data"]!.ToObject<InstalledApp[]>(JsonSerializer.CreateDefault()) ?? [];
    return new InstalledApps([.. items]);
  }

  public void WriteRemoteAppToRegistry(SystemRemoteApps.SystemRemoteApp app) {
    Call("WriteRemoteAppToRegistry", new JObject { ["app"] = JObject.FromObject(app) });
  }


  public void DeleteRemoteAppFromRegistry(SystemRemoteApps.SystemRemoteApp app) {
    Call("DeleteRemoteAppFromRegistry", new JObject { ["app"] = JObject.FromObject(app) });
  }

  public void WriteDesktopToRegistry(SystemDesktop desktop) {
    Call("WriteDesktopToRegistry", new JObject { ["desktop"] = JObject.FromObject(desktop) });
  }

  public void DeleteDesktopFromRegistry(SystemDesktop desktop) {
    Call("DeleteDesktopFromRegistry", new JObject { ["desktop"] = JObject.FromObject(desktop) });
  }

  public Stream GetWallpaperStream(SystemDesktop desktop, ManagedFileResource.ImageTheme theme, string? userSid) {
    return CallForStream("GetWallpaperStream", new JObject {
      ["desktop"] = JObject.FromObject(desktop),
      ["theme"] = (int)theme,
      ["userSid"] = userSid,
    });
  }

  /// <summary>
  /// Requests the return value of a management service method call.
  /// </summary>
  /// <param name="method"></param>
  /// <param name="parameters"></param>
  /// <returns></returns>
  private JObject Call(string method, JObject? parameters = null) {
    using var pipe = OpenPipe();
    SendRequest(pipe, new JObject {
      ["method"] = method,
      ["parameters"] = parameters ?? [],
    });
    return ReadResponseLine(pipe);
  }

  /// <summary>
  /// Requests a stream from the management service. The service will first send a JSON
  /// object containing the content length, followed by the raw stream bytes.
  /// 
  /// Only methods that return a stream (e.g. GetWallpaperStream) should use this helper.
  /// All other methods should use the Call() helper method.
  /// </summary>
  /// <param name="method"></param>
  /// <param name="parameters"></param>
  /// <returns></returns>
  /// <exception cref="EndOfStreamException"></exception>
  private MemoryStream CallForStream(string method, JObject? parameters = null) {
    // Ask the service to prepare a stream.
    using var pipe = OpenPipe();
    SendRequest(pipe, new JObject {
      ["method"] = method,
      ["parameters"] = parameters ?? [],
    });

    // The service will respond with a JSON object containing the content length,
    // followed by the raw stream bytes. We first read the JSON response to get
    // the content length.
    var response = ReadResponseLine(pipe);
    var contentLength = response.Value<int>("contentLength");

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
  /// <returns></returns>
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
    return pipe;
  }

  /// <summary>
  /// Sends a JSON request object to the service over the given pipe.
  /// </summary>
  private static void SendRequest(Stream pipe, JObject request) {
    var bytes = Encoding.UTF8.GetBytes(request.ToString(Formatting.None) + "\n");
    pipe.Write(bytes, 0, bytes.Length);
  }

  /// <summary>
  /// Reads a single line of JSON from the pipe, parses it, and checks for an "ok" status.
  /// If the "ok" property is not true, an exception is thrown with the error
  /// message from the "error" property (or a default message if "error" is not provided).
  /// </summary>
  /// <param name="pipe"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  private static JObject ReadResponseLine(Stream pipe) {
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
    var json = JObject.Parse(sb.ToString());
    if (json.Value<bool>("ok") != true) {
      throw new InvalidOperationException(
        // If an error message is provided, expose it to the consumer as the exception message.
        json.Value<string>("error") ?? "Management service call failed."
      );
    }
    return json;
  }
}

/// <summary>
/// Thrown when the RAWeb Management Service cannot be reached.
/// </summary>
public class EndpointNotFoundException(string message) : Exception(message) { }
