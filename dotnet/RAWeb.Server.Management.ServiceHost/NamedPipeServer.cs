using System;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RAWeb.Server.Management;

/// <summary>
/// A lightweight named-pipe server that exposes management operations
/// defined in <see cref="IManagementServiceHost"/> to unprivileged RAWeb processes.
/// </summary>
/// <remarks>
/// Protocol: one connection per call.
///   request  → UTF-8 JSON line terminated with \n
///   response → UTF-8 JSON line terminated with \n
///              (GetWallpaperStream additionally writes contentLength raw bytes)
///
/// Access is restricted by the pipe access rules to LocalSystem, local administrators,
/// and the IIS AppPool identity.
/// </remarks>
/// <param name="appPoolName">
/// The IIS application pool name passed by the installer via --app-pool.
/// Defaults to "raweb" for backwards compatibility for older manual
/// installations that use the legacy defaullt app pool name.
/// </summary></param>
public class NamedPipeServer(string appPoolName = "raweb") {
  public static string PipeName(string appPoolName) => $"raweb-management-{appPoolName}";

  private readonly string _appPoolName = appPoolName;

  /// <summary>
  /// Whether the server is currently accepting new connections.
  /// </summary>
  private volatile bool _acceptingNewConnections;

  private NamedPipeServerStream? _waitingPipe;

  /// <summary>
  /// Starts the named pipe server.
  /// The server will continue running until <see cref="Stop"/> is called.
  /// </summary>
  public void Start() {
    _acceptingNewConnections = true;
    new Thread(ListenLoop) { IsBackground = true }.Start();
  }

  /// <summary>
  /// Stops the named pipe server. Any active connections will be allowed to complete,
  /// but no new connections will be accepted.
  /// After calling this method, the server cannot be restarted.
  /// </summary>
  public void Stop() {
    _acceptingNewConnections = false;
    try { _waitingPipe?.Close(); } catch { }
  }

  /// <summary>
  /// The main listener loop. It creates a new named pipe and waits for a connection.
  /// When a client connects, it spawns a new thread to handle the connection and goes
  /// back to waiting for the next connection. The loop continues until <see cref="Stop"/> is called.
  /// </summary>
  private void ListenLoop() {
    while (_acceptingNewConnections) {
      NamedPipeServerStream? pipe = null;
      try {
        pipe = CreatePipe();
        _waitingPipe = pipe;
        pipe.WaitForConnection();
        _waitingPipe = null;

        var p = pipe;
        pipe = null;
        new Thread(() => Handle(p)) { IsBackground = true }.Start();
      }
      catch when (!_acceptingNewConnections) {
        pipe?.Dispose();
        break;
      }
      catch {
        pipe?.Dispose();
      }
    }
  }

  private NamedPipeServerStream CreatePipe() {
    var security = new PipeSecurity();

    // always grant LocalSystem full control of the service
    security.AddAccessRule(new PipeAccessRule(
      new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
      PipeAccessRights.FullControl, AccessControlType.Allow)
    );

    // also allow local administrators access to the service
    security.AddAccessRule(new PipeAccessRule(
      new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
      PipeAccessRights.ReadWrite, AccessControlType.Allow)
    );

#if RELEASE
    // the IIS AppPool identity also needs access to the service
    // so that RAWeb's server process can use the management service
    try {
      security.AddAccessRule(
        new PipeAccessRule(
          $"IIS AppPool\\{_appPoolName}",
          PipeAccessRights.ReadWrite, AccessControlType.Allow
        )
      );
    }
    catch { }
#else
    // In development, we still have to register the service with sc.exe,
    // but the web server runs as the developer user. So that the service
    // does not deny access to the developer user, we grant all users
    // access.
    // CAUTION: developers should always disable the service when it is
    // not needed
    security.AddAccessRule(
      new PipeAccessRule(
        new SecurityIdentifier(WellKnownSidType.InteractiveSid, null),
        PipeAccessRights.ReadWrite, AccessControlType.Allow
      )
    );
#endif

    return new NamedPipeServerStream(
        PipeName(_appPoolName),
        PipeDirection.InOut,
        NamedPipeServerStream.MaxAllowedServerInstances,
        PipeTransmissionMode.Byte,
        PipeOptions.None,
        4096, 4096, security
      );
  }

  private static readonly JsonSerializer s_serializer = JsonSerializer.CreateDefault();

  /// <summary>
  /// Handles a client connection. It reads a single line of JSON from the client,
  /// interprets it as a method call, executes the corresponding method on a new
  /// instance of <see cref="SystemRemoteAppsServiceHost"/>, and replies with a
  /// single line of JSON indicating success or failure, and optionally includes a
  /// response value.
  /// </summary>
  private static void Handle(NamedPipeServerStream pipe) {
    using (pipe) {
      try {
        var line = ReadLine(pipe);
        if (string.IsNullOrEmpty(line)) return;

        var req = JObject.Parse(line);
        var method = req.Value<string>("method");
        var host = new SystemRemoteAppsServiceHost();

        switch (method) {
          case "AreConnectionsAllowed":
            Reply(pipe, new OkReply(host.AreConnectionsAllowed()));
            break;

          case "InitializeRegistryPaths":
            host.InitializeRegistryPaths(req.Value<string>("collectionName"));
            Reply(pipe, new OkReply());
            break;

          case "InitializeDesktopRegistryPaths":
            host.InitializeDesktopRegistryPaths(req.Value<string>("collectionName")!);
            Reply(pipe, new OkReply());
            break;

          case "RestorePackagedAppIconPaths":
            host.RestorePackagedAppIconPaths(req.Value<string>("collectionName"));
            Reply(pipe, new OkReply());
            break;

          case "ListInstalledApps": {
              var result = host.ListInstalledApps(req.Value<string>("userSid"));
              Reply(pipe, new OkReply(JArray.FromObject(result, s_serializer)));
              break;
            }

          case "WriteRemoteAppToRegistry": {
              var app = SystemRemoteApps.SystemRemoteApp.FromJSON(
                  (JObject)req["app"]!, s_serializer)!;
              host.WriteRemoteAppToRegistry(app);
              Reply(pipe, new OkReply());
              break;
            }

          case "DeleteRemoteAppFromRegistry": {
              var app = SystemRemoteApps.SystemRemoteApp.FromJSON(
                  (JObject)req["app"]!, s_serializer)!;
              host.DeleteRemoteAppFromRegistry(app);
              Reply(pipe, new OkReply());
              break;
            }

          case "WriteDesktopToRegistry": {
              var desktop = SystemDesktop.FromJSON((JObject)req["desktop"]!, s_serializer)!;
              host.WriteDesktopToRegistry(desktop);
              Reply(pipe, new OkReply());
              break;
            }

          case "DeleteDesktopFromRegistry": {
              var desktop = SystemDesktop.FromJSON((JObject)req["desktop"]!, s_serializer)!;
              host.DeleteDesktopFromRegistry(desktop);
              Reply(pipe, new OkReply());
              break;
            }

          case "GetWallpaperStream": {
              var desktop = SystemDesktop.FromJSON((JObject)req["desktop"]!, s_serializer)!;
              var theme = (ManagedFileResource.ImageTheme)req.Value<int>("theme");
              var userSid = req.Value<string>("userSid");
              using var wallpaperStream = host.GetWallpaperStream(desktop, theme, userSid);
              using var ms = new MemoryStream();
              wallpaperStream.CopyTo(ms);
              var bytes = ms.ToArray();
              Reply(pipe, new IncomingStreamReply(bytes.Length));
              pipe.Write(bytes, 0, bytes.Length);
              break;
            }

          default:
            Reply(pipe, new ErrorReply(new InvalidOperationException($"Unknown method: {method}")));
            break;
        }
      }
      catch (Exception ex) {
        try { Reply(pipe, new ErrorReply(ex)); } catch { }
      }
    }
  }

  private static string ReadLine(Stream stream) {
    var sb = new StringBuilder();
    var buf = new byte[1];
    while (true) {
      if (stream.Read(buf, 0, 1) == 0) break;
      if (buf[0] == (byte)'\n') break;
      if (buf[0] != (byte)'\r') sb.Append((char)buf[0]);
    }
    return sb.ToString();
  }

  /// <summary>
  /// Respond to the client with a single-line JSON message indicating
  /// success or failure, and optionally, including a response value.
  /// </summary>
  /// <param name="pipe"></param>
  /// <param name="response"></param>
  private static void Reply(Stream pipe, ReplyResponse response) {
    var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response) + "\n");
    pipe.Write(bytes, 0, bytes.Length);
  }

  private class ReplyResponse(bool ok) {
    public bool ok = ok;
  }
  private class OkReply(object? data = null) : ReplyResponse(true) {
    public object? data = data;
  }
  private class ErrorReply(Exception ex) : ReplyResponse(false) {
    public string error { get; set; } = ex.Message;
  }
  private class IncomingStreamReply(int contentLength) : ReplyResponse(true) {
    public int contentLength = contentLength;
  }
}
