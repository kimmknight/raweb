using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;
using RAWeb.Server.Utilities;

namespace RAWebServer.Handlers;

public class GuacdTunnel : HttpTaskAsyncHandler {
    public override bool IsReusable => false;

    private static ConcurrentHashSet<string> s_activeConnectionIds = new ConcurrentHashSet<string>();

    public override Task ProcessRequestAsync(HttpContext context) {
        if (context.IsWebSocketRequest) {
            var requestedProtocols = context.WebSocketRequestedProtocols;

            // the protocol must be "guacamole"
            if (!requestedProtocols.Contains("guacamole")) {
                _logger.WriteLogline("WebSocket connection rejected due to missing 'guacamole' subprotocol.");
                context.Response.StatusCode = 400;
                context.Response.Write("WebSocket subprotocol 'guacamole' required.");
                return Task.FromResult<object>(null);
            }

            var options = new AspNetWebSocketOptions {
                SubProtocol = "guacamole"
            };
            _logger.WriteLogline("Accepting WebSocket connection with 'guacamole' subprotocol.");

            // NOTE: Even though ProcessWebSocket will check auth, we need to block unauthenticated
            // connections to prevent attacks from flooding guacd with unauthenticated connections.
            var userInfo = UserInformation.FromHttpRequestSafe(context.Request);
            if (userInfo == null) {
                _logger.WriteLogline("WebSocket connection rejected due to unauthenticated user.");
                context.Response.StatusCode = 401;
                context.Response.Write("Authentication required.");
                return Task.CompletedTask;
            }

            context.AcceptWebSocketRequest(async websocketContext => await ProcessWebSocket(websocketContext, context), options);
        }
        else {
            _logger.WriteLogline("Received non-WebSocket request to GuacdTunnel handler.");
            context.Response.StatusCode = 400;
            context.Response.Write("WebSocket only.");
        }

        return Task.FromResult<object>(null);
    }

    private static string GuacEncode(params string[] parts) {
        return string.Join(",", parts.Select(p => (p ?? "").Length + "." + (p ?? ""))) + ";";
    }

    readonly Logger _logger = new("guacd-tunnel");

    /// <summary>
    /// Decodes a Guacamole protocol instruction string into its component parts.
    /// Each part is prefixed by its length and a dot, and the entire instruction ends with a semicolon.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private static List<string> GuacDecode(string data) {
        var parts = new List<string>();
        var index = 0;
        while (index < data.Length) {
            var dotPos = data.IndexOf('.', index);
            if (dotPos == -1)
                break;

            var lenStr = data.Substring(index, dotPos - index);
            if (!int.TryParse(lenStr, out var len))
                break;

            var start = dotPos + 1;
            if (start + len > data.Length)
                break;

            var part = data.Substring(start, len);
            parts.Add(part);

            index = start + len + 1; // skip the trailing ';' or ','
        }

        return parts;
    }

    private record ArgsInstruction(GuacProtocolVersion Version, string[] AcceptedParameterNames);
    private enum GuacProtocolVersion {
        VERSION_1_0_0 = 1_0_0,
        VERSION_1_1_0 = 1_1_0,
        VERSION_1_3_0 = 1_3_0,
        VERSION_1_5_0 = 1_5_0,
    }
    private static ArgsInstruction ParseArgsInstruction(string instruction) {
        var parts = GuacDecode(instruction);
        if (parts.Count < 2 || parts[0] != "args")
            throw new ArgumentException("Not a valid args instruction.");

        var version = parts[1];
        GuacProtocolVersion protoVersion = version switch {
            "VERSION_1_5_0" => GuacProtocolVersion.VERSION_1_5_0,
            _ => throw new ArgumentException("Unsupported Guacamole protocol version: " + version),
        };
        var acceptedParameterNames = parts.GetRange(2, parts.Count - 2).ToArray();

        return new ArgsInstruction(protoVersion, acceptedParameterNames);
    }

    /// <summary>
    /// Reads a reply from guacd over the provided network stream.
    /// This method blocks until a full instruction is received.
    /// A full instruction ends with a semicolon.
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    private static async Task<string> ReadGuacdReply(NetworkStream stream) {
        var buffer = new byte[4096];
        var sb = new StringBuilder();
        int bytesRead;

        // read until we encounter a semicolon, indicating the end of the instruction
        do {
            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            sb.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
        } while (!sb.ToString().Contains(";"));

        return sb.ToString();
    }

    /// <summary>
    /// Parses a "ready" instruction from guacd and returns the connection ID.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static string ReadReadyMessage(string message) {
        var parts = GuacDecode(message);
        if (parts.Count != 2 || parts[0] != "ready") {
            throw new ArgumentException("Not a valid ready instruction.");
        }

        return parts[1];
    }

    private async Task ProcessWebSocket(AspNetWebSocketContext wsContext, HttpContext httpContext) {
        var ws = wsContext.WebSocket;
        var currentConnectionId = null as string;
        try {

            // start sending nop instructions every 10 seconds to keep the connection alive
            var nopCts = new CancellationTokenSource();
            _ = Task.Run(async () => {
                while (!nopCts.Token.IsCancellationRequested) {
                    await Task.Delay(TimeSpan.FromSeconds(10), nopCts.Token);
                    if (!nopCts.Token.IsCancellationRequested) {
                        await sendToBrowser(GuacEncode("nop"));
                    }
                }
            }, nopCts.Token);

            /// <summary>
            /// Sends a message to the browser via WebSocket.
            /// </summary>
            Task sendToBrowser(string message) => ws.SendAsync(
                new ArraySegment<byte>(Encoding.ASCII.GetBytes(message)),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );

            /// <summary>
            /// Disconnects the browser websocket.
            /// </summary>
            async Task disconnectBrowser(string reason = null) {
                await sendToBrowser(GuacEncode("disconnect"));
                nopCts.Cancel();
                await ws.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    reason,
                    CancellationToken.None
                );
            }

            /// <summary>
            /// Waits until the specified instruction is received from the browser via WebSocket.
            /// </summary>
            async Task<string> receiveFromBrowser(string instructionName) {
                var recvBuffer = new StringBuilder();

                /// <summary>
                /// Looks through the receive buffer to see if it ends with a full instruction.
                /// If so, it provides the full instruction and removes it from the buffer.
                /// </summary>
                bool BufferEndsContainsFullInstruction(out string instruction) {
                    instruction = null;
                    var scan = 0;

                    while (true) {
                        // find next "<len>."
                        var dotPos = recvBuffer.ToString().IndexOf('.', scan);
                        if (dotPos <= scan)
                            return false; // need more data

                        var lenStr = recvBuffer.ToString(scan, dotPos - scan);
                        if (!int.TryParse(lenStr, out var len))
                            return false; // malformed/partial

                        var payloadStart = dotPos + 1;
                        var payloadEnd = payloadStart + len; // exclusive
                        if (payloadEnd >= recvBuffer.Length)
                            return false; // need more data

                        var sep = recvBuffer[payloadEnd];
                        if (sep != ',' && sep != ';')
                            return false; // malformed

                        // advance scan past this part (+ separator)
                        scan = payloadEnd + 1;

                        if (sep == ';') {
                            // complete instruction from buffer start through current separator
                            instruction = recvBuffer.ToString(0, scan);
                            recvBuffer.Remove(0, scan);
                            return true;
                        }
                        // sep == ',' -> keep looping to finish the instruction
                    }
                }

                while (true) {
                    if (BufferEndsContainsFullInstruction(out var instruction)) {
                        // confirm that this is the instruction we are looking for
                        var parts = GuacDecode(instruction);
                        if (parts.Count > 0 && parts[0] == instructionName) {
                            return instruction;
                        }

                        // keep looking
                        continue;
                    }

                    // continue to populate the receive buffer
                    var buffer = new byte[8192];
                    var result = await ws.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None
                    );

                    if (result.MessageType == WebSocketMessageType.Close) {
                        return null;
                    }

                    recvBuffer.Append(Encoding.ASCII.GetString(buffer, 0, result.Count));
                }
            }

            // check for a user
            var userInfo = UserInformation.FromHttpRequestSafe(httpContext.Request);
            if (userInfo == null) {
                // send anauthorized user error
                await sendToBrowser(GuacEncode("error", "You are not authenticated.", "401"));
                await disconnectBrowser();
                return;
            }
            _logger.WriteLogline($"User '{userInfo.Username}' authenticated successfully.");

            // check that there is a resource available and that the user has permission to access it
            var resourcePath = wsContext.QueryString["rPath"];
            var resourceFrom = wsContext.QueryString["rFrom"];
            if (string.IsNullOrEmpty(resourcePath) || string.IsNullOrEmpty(resourceFrom)) {
                await sendToBrowser(GuacEncode("error", "Resource path and source must be specified.", "10000"));
                await disconnectBrowser();
                return;
            }
            _logger.WriteLogline($"User '{userInfo.Username}' is requesting resource at path '{resourcePath}' from '{resourceFrom}'.");

            ResourceOrigin? resourceOrigin = resourceFrom.ToLowerInvariant() switch {
                "rdp" => ResourceOrigin.Rdp,
                "registry" => ResourceOrigin.Registry,
                "mr" => ResourceOrigin.ManagedResource,
                "registrydesktop" => ResourceOrigin.RegistryDesktop,
                _ => null,
            };
            if (resourceOrigin is null) {
                await sendToBrowser(GuacEncode("error", "The requested resource was not found.", "516"));
                await disconnectBrowser();
                return;
            }

            string rdpContents;
            try {
                var resolvedResource = ResourceContentsResolver.ResolveResource(userInfo, resourcePath, resourceOrigin.Value);
                if (resolvedResource is ResourceContentsResolver.FailedResourceResult failedResource) {
                    _logger.WriteLogline($"Failed to resolve resource for user '{userInfo.Username}': {failedResource.ErrorMessage}");

                    if (failedResource.PermissionHttpStatus == HttpStatusCode.NotFound) {
                        await sendToBrowser(GuacEncode("error", "The requested resource was not found.", "516"));
                        await disconnectBrowser();
                        return;
                    }

                    await sendToBrowser(GuacEncode("error", "You are not authorized to access this resource.", ((int)failedResource.PermissionHttpStatus).ToString()));
                    await disconnectBrowser();
                    return;
                }

                _logger.WriteLogline($"Resource resolved successfully for user '{userInfo.Username}'. Preparing RDP connection...");
                rdpContents = (resolvedResource as ResourceContentsResolver.ResolvedResourceResult).RdpFileContents;
            }
            catch (Exception ex) {
                await sendToBrowser(GuacEncode("error", "Error resolving resource: " + ex.Message, "10012"));
                await disconnectBrowser();
                return;
            }

            // helper function to quickly get RDP properties
            string GetRdpFileProperty(string propertyName) {
                return Resource.Utilities.GetRdpStringProperty(rdpContents, propertyName);
            }

            // ensure that there is a full address in the RDP file
            var fullAddress = GetRdpFileProperty("full address:s:") ?? GetRdpFileProperty("alternate full address:s");
            if (string.IsNullOrEmpty(fullAddress)) {
                _logger.WriteLogline($"RDP file for user '{userInfo.Username}' is missing the full address property.");
                await sendToBrowser(GuacEncode("error", "The RDP file is missing the full address property.", "10001"));
                await disconnectBrowser();
                return;
            }

            // if there is a port in the full address, use it; otherwise, get the port property or default to 3389
            var port = GetRdpFileProperty("server port:i:") ?? "3389";
            if (fullAddress.Contains(":")) {
                var parts = fullAddress.Split(':');
                fullAddress = parts[0];
                port = parts[1];
            }
            if (string.IsNullOrWhiteSpace(port)) {
                port = "3389";
            }
            _logger.WriteLogline($"Extracted connection details - Address: {fullAddress}, Port: {port}");

            // check the certificate of the target server
            var shouldIgnoreCertificateErrors = wsContext.QueryString["ignoreCertErrors"] == "true";
            if (!shouldIgnoreCertificateErrors) {
                try {
                    try {
                        var (cert, policyErrors) = CheckCertificateDetails(fullAddress, port);
                        if (cert == null) {
                            await sendToBrowser(GuacEncode("error", "Failed to retrieve the server's SSL certificate.", "10002"));
                            await disconnectBrowser();
                            return;
                        }
                        if (policyErrors != SslPolicyErrors.None) {
                            var certDetails = new StringBuilder();
                            certDetails.AppendLine("The server's SSL certificate is untrusted.");
                            certDetails.AppendLine("");
                            certDetails.AppendLine("Connection details:");
                            certDetails.AppendLine($"  Address: {fullAddress}");
                            certDetails.AppendLine($"  Port: {port}");
                            certDetails.AppendLine("");
                            certDetails.AppendLine("Certificate details:");
                            certDetails.AppendLine($"  Subject: {cert.Subject}");
                            certDetails.AppendLine($"  Issuer: {cert.Issuer}");
                            certDetails.AppendLine($"  Valid From: {cert.NotBefore}");
                            certDetails.AppendLine($"  Valid To: {cert.NotAfter}");
                            certDetails.AppendLine($"  Thumbprint: {cert.Thumbprint}");
                            certDetails.AppendLine($"  Policy Errors: {policyErrors}");
                            await sendToBrowser(GuacEncode("error", certDetails.ToString(), "10003"));
                            await disconnectBrowser();
                            return;
                        }
                        cert.Dispose();
                    }
                    catch (AggregateException ex) {
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }
                }
                catch (SocketException ex) {
                    if (ex.SocketErrorCode == SocketError.HostNotFound ||
                        ex.SocketErrorCode == SocketError.NoData ||
                        ex.SocketErrorCode == SocketError.HostUnreachable ||
                        ex.SocketErrorCode == SocketError.HostDown) {
                        await sendToBrowser(GuacEncode("error", "The specified remote host could not be reached.", "10010"));
                        await disconnectBrowser();
                        return;
                    }
                    if (ex.SocketErrorCode == SocketError.ConnectionRefused) {
                        await sendToBrowser(GuacEncode("error", "The specified remote host refused the connection.", "10027"));
                        await disconnectBrowser();
                        return;
                    }
                    await sendToBrowser(GuacEncode("error", "Error checking server certificate: " + ex.Message, "10009"));
                    await disconnectBrowser();
                    return;
                }
                catch (TimeoutException ex) {
                    await sendToBrowser(GuacEncode("error", "Timeout while checking server certificate: " + ex.Message, "10026"));
                    await disconnectBrowser();
                    return;
                }
                catch (Exception ex) {
                    await sendToBrowser(GuacEncode("error", "Error checking server certificate: " + ex.Message, "10009"));
                    await sendToBrowser(GuacEncode("raweb-console-error", $"{ex.Message}", $"{ex}", "19999"));
                    await disconnectBrowser();
                    return;
                }
            }

            // if the address is a hostname, resolve it to an IPv4 address.
            try {
                fullAddress = ResolveToIpv4(fullAddress)?.ToString() ?? fullAddress;
                _logger.WriteLogline($"Resolved address to IPv4: {fullAddress}");
            }
            catch (Exception ex) {
                await sendToBrowser(GuacEncode("error", "Failed to resolve hostname to an IPv4 address: " + ex.Message, "10032"));
                _logger.WriteLogline($"Failed to resolve hostname '{fullAddress}' to an IPv4 address: {ex.Message}");
            }

            // wait for credentials from the browser
            await sendToBrowser(GuacEncode("raweb-demand-credentials"));
            var domainMessage = await receiveFromBrowser("domain");
            var usernameMessage = await receiveFromBrowser("username");
            var passwordMessage = await receiveFromBrowser("password");
            if (domainMessage == null || usernameMessage == null || passwordMessage == null) {
                await sendToBrowser(GuacEncode("error", "Failed to receive credentials from the client.", "10004"));
                await disconnectBrowser();
                return;
            }
            var domain = GuacDecode(domainMessage).ElementAtOrDefault(1) ?? "";
            var username = GuacDecode(usernameMessage).ElementAtOrDefault(1) ?? "";
            var password = GuacDecode(passwordMessage).ElementAtOrDefault(1) ?? "";
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) {
                await sendToBrowser(GuacEncode("error", "Domain, username, and password must be provided.", "10005"));
                await disconnectBrowser();
                return;
            }

            // wait for the initial display resolution message from the browser
            await sendToBrowser(GuacEncode("raweb-demand-display-info"));
            var widthMessage = await receiveFromBrowser("displayWidth");
            var heightMessage = await receiveFromBrowser("displayHeight");
            var dpiMessage = await receiveFromBrowser("displayDPI");
            if (widthMessage == null || heightMessage == null || dpiMessage == null) {
                await sendToBrowser(GuacEncode("error", "Failed to receive display info from the client.", "10006"));
                await disconnectBrowser();
                return;
            }
            var displayWidth = GuacDecode(widthMessage).ElementAtOrDefault(1) ?? "1024";
            var displayHeight = GuacDecode(heightMessage).ElementAtOrDefault(1) ?? "768";
            var displayDpi = GuacDecode(dpiMessage).ElementAtOrDefault(1) ?? "96";

            // wait for the IANA timezone name from the browser
            await sendToBrowser(GuacEncode("raweb-demand-timezone"));
            var timezoneMessage = await receiveFromBrowser("timezone");
            var timezone = GuacDecode(timezoneMessage).ElementAtOrDefault(1) ?? "UTC";

            // if there is a gateway hostname, get it
            var gatewayHostname = GetRdpFileProperty("gatewayhostname:s:");
            var gatewayPort = "443";
            if (!string.IsNullOrEmpty(gatewayHostname) && gatewayHostname.Contains(":")) {
                var parts = gatewayHostname.Split(':');
                gatewayHostname = parts[0];
                gatewayPort = parts[1];
            }

            // if there is a gateway, demand credentials for it
            string gatewayDomain = null;
            string gatewayUsername = null;
            string gatewayPassword = null;
            if (!string.IsNullOrEmpty(gatewayHostname)) {
                await sendToBrowser(GuacEncode("raweb-demand-gateway-credentials"));
                var gwDomainMessage = await receiveFromBrowser("gateway-domain");
                var gwUsernameMessage = await receiveFromBrowser("gateway-username");
                var gwPasswordMessage = await receiveFromBrowser("gateway-password");
                if (gwUsernameMessage == null || gwPasswordMessage == null) {
                    await sendToBrowser(GuacEncode("error", "Failed to receive gateway credentials from the client.", "10007"));
                    await disconnectBrowser();
                    return;
                }
                var gwDomain = GuacDecode(gwDomainMessage).ElementAtOrDefault(1) ?? "";
                var gwUsername = GuacDecode(gwUsernameMessage).ElementAtOrDefault(1) ?? "";
                var gwPassword = GuacDecode(gwPasswordMessage).ElementAtOrDefault(1) ?? "";
                if (string.IsNullOrEmpty(gwUsername) || string.IsNullOrEmpty(gwPassword)) {
                    await sendToBrowser(GuacEncode("error", "Gateway username and password must be provided.", "10008"));
                    await disconnectBrowser();
                    return;
                }

                // set gateway credentials
                gatewayDomain = gwDomain;
                gatewayUsername = gwUsername;
                gatewayPassword = gwPassword;

                _logger.WriteLogline($"Gateway credentials resolved - Hostname: {gatewayHostname}, Port: {gatewayPort}, Domain: {gatewayDomain}, Username: {gatewayUsername}");
            }

            try {
                var guacdMethod = PoliciesManager.RawPolicies["GuacdWebClient.Method"];
                if (guacdMethod == "container" && !Guacd.IsWindowsSubsystemForLinuxSupported) {
                    guacdMethod = "external";
                }

                string guacdAddress;

                // use an external guacd if specified
                if (guacdMethod == "external") {
                    guacdAddress = PoliciesManager.RawPolicies["GuacdWebClient.Address"];
                }

                // start the internal guacd
                else {
                    try {

                        // install the guacd distribution if it's not already installed
                        if (!Guacd.IsGuacdDistributionInstalled) {
                            await sendToBrowser(GuacEncode("raweb-msg-installing-service"));
                            Guacd.InstallGuacd();
                            if (!Guacd.IsGuacdDistributionInstalled) {
                                await sendToBrowser(GuacEncode("error", "Failed to install the remote desktop proxy service.", "10017"));
                                await disconnectBrowser();
                                return;
                            }
                        }

                        Guacd.RequestStart();
                        await sendToBrowser(GuacEncode("raweb-msg-starting-service"));
                        Guacd.WaitUntilRunning(TimeSpan.FromSeconds(30));
                        await sendToBrowser(GuacEncode("raweb-msg-service-started"));
                    }
                    catch (TimeoutException) {
                        await sendToBrowser(GuacEncode("error", "The remote desktop proxy service did not start in time.", "10014"));
                        await disconnectBrowser();
                        return;
                    }
                    catch (GuacdDistributionMissingException) {
                        await sendToBrowser(GuacEncode("error", "The remote desktop proxy service is not installed on the server.", "10015"));
                        await disconnectBrowser();
                        return;
                    }
                    catch (WindowsSubsystemForLinuxMissingException) {
                        await sendToBrowser(GuacEncode("error", "The Windows Subsystem for Linux is not installed on the server.", "10016"));
                        await disconnectBrowser();
                        return;
                    }
                    catch (GuacdInstallFailedException) {
                        await sendToBrowser(GuacEncode("error", "The remote desktop proxy service failed to install.", "10022"));
                        await disconnectBrowser();
                        return;
                    }
                    catch (MissingOptionalComponentException) {
                        await sendToBrowser(GuacEncode("error", "The Windows Subsystem for Linux optional component is not installed on the server.", "10023"));
                        await disconnectBrowser();
                        return;
                    }
                    catch (VirtualMachinePlatformMissingException) {
                        await sendToBrowser(GuacEncode("error", "The Virtual Machine Platform optional component is not installed on the server.", "10024"));
                        await disconnectBrowser();
                        return;
                    }
                    catch (VirtualMachinePlatformUnavailableException) {
                        await sendToBrowser(GuacEncode("error", "The Virtual Machine Platform is unavailable.", "10028"));
                        await disconnectBrowser();
                        return;
                    }
                    catch (UnknownWslErrorCodeException) {
                        await sendToBrowser(GuacEncode("error", $"An error with the Windows Subsystem for Linux prevented the remote desktop proxy service from installing or starting.", "10025"));
                        await disconnectBrowser();
                        return;
                    }
                    catch (GuacdStoppingTimeoutException) {
                        await sendToBrowser(GuacEncode("error", "The remote desktop proxy service is stopping and cannot be started at this time.", "10035"));
                        await disconnectBrowser();
                        return;
                    }
                    catch (Exception ex) {
                        if (!Guacd.IsRunning) {
                            _logger.WriteLogline($"Failed to start guacd: {ex}");
                            await sendToBrowser(GuacEncode("error", "The remote desktop proxy service failed to start.", "10013"));
                            await sendToBrowser(GuacEncode("raweb-console-error", $"{ex.Message}", $"{ex}", "19999"));
                            await disconnectBrowser();
                            return;
                        }

                        // even though an exception was thrown, guacd is running, so we can continue
                        _logger.WriteLogline($"Exception occurred while starting guacd, but guacd is running: {ex}");
                    }

                    guacdAddress = Guacd.IpAddress + ":4822";
                }

                _logger.WriteLogline($"Connecting to guacd at {guacdAddress} using method '{guacdMethod}'.");


                var guacdAddressParts = guacdAddress.Split(':');
                if (guacdAddressParts.Length != 2) {
                    await sendToBrowser(GuacEncode("error", "Guacd address is not properly configured.", "10011"));
                    await disconnectBrowser();
                    return;
                }
                var guacdHostname = guacdAddressParts[0];
                var guacdPort = int.TryParse(guacdAddressParts[1], out var p) ? p : 4822;

                try {
                    using (var guacd = new TcpClient(guacdHostname, guacdPort))
                    using (var stream = guacd.GetStream()) {
                        // tell guacd we want to use rdp
                        await stream.WriteAsync(Encoding.ASCII.GetBytes(GuacEncode("select", "rdp")), 0,
                            GuacEncode("select", "rdp").Length);
                        await stream.FlushAsync();

                        // read what guacd sends back
                        var reply = await ReadGuacdReply(stream);
                        var argsInstruction = ParseArgsInstruction(reply);
                        if (argsInstruction.Version != GuacProtocolVersion.VERSION_1_5_0) {
                            await sendToBrowser(GuacEncode("error", "The web client is using an unsupported Guacamole protocol version: " + argsInstruction.Version + ".", "10033"));
                            await disconnectBrowser();
                            return;
                        }

                        string[] defaultAudio = ["audio/L16"];
                        string[] defaultVideo = null;
                        string[] defaultImage = ["image/webp", "image/jpeg"];
                        string defaultTimezone = null;

                        // since guacd does not support all RemoteApp parameters, we must
                        // end with an error if the below conditions are not met
                        var isRemoteApp = GetRdpFileProperty("remoteapplicationmode:i:") == "1";
                        if (isRemoteApp) {
                            var hasFileParameter = !string.IsNullOrWhiteSpace(GetRdpFileProperty("remoteapplicationfile:s:"));
                            if (hasFileParameter) {
                                await sendToBrowser(GuacEncode("error", "The specified connection file must not specify a file to open on the terminal server", "10018"));
                                await disconnectBrowser();
                                return;
                            }
                            var hasProgramParameter = !string.IsNullOrWhiteSpace(GetRdpFileProperty("remoteapplicationprogram:s:"));
                            if (!hasProgramParameter) {
                                await sendToBrowser(GuacEncode("error", "The specified connection file must specify a program to open on the terminal server", "10019"));
                                await disconnectBrowser();
                                return;
                            }
                            var expandsCommandLineOnTerminalServer = GetRdpFileProperty("remoteapplicationexpandcmdline:i:") != "0";
                            if (!expandsCommandLineOnTerminalServer) {
                                await sendToBrowser(GuacEncode("error", "The specified connection file must not expand the command line parameters on the terminal server.", "10020"));
                                await disconnectBrowser();
                                return;
                            }
                        }

                        // connections to packaged apps must use the program explorer.exe
                        var isPackagedApp = GetRdpFileProperty("remoteapplicationcmdline:s:").StartsWith("shell:AppsFolder");
                        if (isPackagedApp) {
                            var remoteAppProgram = GetRdpFileProperty("remoteapplicationprogram:s:");
                            if (remoteAppProgram != @"C:\Windows\explorer.exe") {
                                await sendToBrowser(GuacEncode("error", @"Connections to packaged applications must connect via C:\Windows\explorer.exe.", "10021"));
                                await disconnectBrowser();
                                return;
                            }
                        }

                        // respond with the connection parameters
                        var sb = new StringBuilder();
                        sb.Append(GuacEncode("size", displayWidth, displayHeight, displayDpi));
                        sb.Append(GuacEncode("audio", string.Join(",", defaultAudio)));
                        sb.Append(GuacEncode("video", string.Join(",", defaultVideo ?? Array.Empty<string>())));
                        sb.Append(GuacEncode("image", string.Join(",", defaultImage)));
                        sb.Append(GuacEncode("timezone", defaultTimezone));
                        var connectArgs = argsInstruction.AcceptedParameterNames.Select(paramName => {

                            return paramName switch {
                                // auth + security settings
                                "hostname" => fullAddress,
                                "port" => port,
                                "domain" => domain,
                                "username" => username,
                                "password" => password,
                                "security" => "any",
                                "ignore-cert" => shouldIgnoreCertificateErrors ? "true" : "false",
                                // session settings
                                "client-name" => "RAWeb",
                                "console" => "true",
                                "timezone" => timezone,
                                // display settings
                                "color-depth" => "32", // guacd always uses 32-bit color depth
                                "width" => GetRdpFileProperty("desktopwidth:i:"),
                                "height" => GetRdpFileProperty("desktopheight:i:"),
                                "dpi" => displayDpi,
                                "resize-method" => "display-update",
                                // device redirection
                                "disable-audio" => GetRdpFileProperty("audiomode:i:") == "1" ? "true" : "false",
                                "enable-audio-input" => GetRdpFileProperty("audiocapturemode:i:") == "1" ? "true" : "false",
                                "enable-touch" => "true",
                                "enable-printing" => "false", // the guacd image does not include the software for print-to-PDF support
                                "printer-name" => "RAWeb Print-to-PDF",
                                "enable-drive" => "false",
                                "disable-download" => "false",
                                "disable-upload" => "true",
                                "disable-copy" => "false",
                                "disable-paste" => "false",
                                // gateway settings
                                "gateway-hostname" => gatewayHostname,
                                "gateway-port" => gatewayPort,
                                "gateway-domain" => gatewayDomain,
                                "gateway-username" => gatewayUsername,
                                "gateway-password" => gatewayPassword,
                                // performance flags
                                "enable-wallpaper" => GetRdpFileProperty("disable wallpaper:i:") == "0" ? "true" : "false", // default disable
                                "enable-theming" => GetRdpFileProperty("disable themes:i:") == "0" ? "true" : "false", // default disable
                                "enable-font-smoothing" => GetRdpFileProperty("allow font smoothing:i:") == "0" ? "false" : "true", // default enable
                                "enable-full-window-drag" => GetRdpFileProperty("disable full window drag:i:") == "1" ? "false" : "true", // default enable
                                "enable-desktop-composition" => GetRdpFileProperty("allow desktop composition:i:") == "1" ? "true" : "false", // default disable
                                "enable-menu-animations" => GetRdpFileProperty("disable menu anims:i:") == "0" ? "false" : "true", // default disable
                                "disable-bitmap-caching" => GetRdpFileProperty("bitmapcachepersistenable:i:") == "0" ? "true" : "false", // default enable
                                "disable-gfx" => isRemoteApp ? "false" : "true",
                                // RemoteApp
                                "remote-app" => GetRdpFileProperty("remoteapplicationprogram:s:"),
                                "remote-app-args" => GetRdpFileProperty("remoteapplicationcmdline:s:"),
                                _ => ""
                            };
                        });
                        sb.Append(GuacEncode(["connect", $"VERSION_{argsInstruction.Version}", .. connectArgs]));

                        await stream.WriteAsync(Encoding.ASCII.GetBytes(sb.ToString()), 0, sb.ToString().Length);
                        await stream.FlushAsync();

                        // check for read message from guacd
                        reply = await ReadGuacdReply(stream);
                        currentConnectionId = ReadReadyMessage(reply);
                        s_activeConnectionIds.TryAdd(currentConnectionId);

                        // Relay guacd -> browser
                        var fromGuacd = Task.Run(async () => {
                            // we no longer need to send nop messages
                            // since guacd will now handle that internally
                            nopCts.Cancel();

                            var collector = new MessageCollector(stream, 8192);

                            while (ws.State == WebSocketState.Open) {
                                var msg = await collector.ReadUntilSemicolonAsync();
                                if (string.IsNullOrEmpty(msg)) {
                                    break;
                                }

                                await ws.SendAsync(
                                    new ArraySegment<byte>(Encoding.ASCII.GetBytes(msg)),
                                    WebSocketMessageType.Text,
                                    true,
                                    CancellationToken.None);
                            }

                            _logger.WriteLogline($"Guacd -> browser connection closed for user '{userInfo.Username}' and resource '{resourcePath}'.");
                        });

                        // Browser -> guacd (clipboard, mouse, keyboard, etc.)
                        var toGuacd = Task.Run(async () => {
                            var buf = new byte[8192];
                            while (ws.State == WebSocketState.Open) {
                                var res = await ws.ReceiveAsync(
                                    new ArraySegment<byte>(buf),
                                    CancellationToken.None);

                                if (res.MessageType == WebSocketMessageType.Close) {
                                    _logger.WriteLogline($"Browser -> guacd connection closed by client for user '{userInfo.Username}' and resource '{resourcePath}'.");
                                    break;
                                }
                                await stream.WriteAsync(buf, 0, res.Count);
                            }
                        });

                        // wait for either direction to close
                        await Task.WhenAny(toGuacd, fromGuacd);

                        // observe exceptions from both tasks to prevent unobserved task exceptions
                        toGuacd.Catch(ex => _logger.WriteLogline($"toGuacd task faulted: {ex.Message}"));
                        fromGuacd.Catch(ex => _logger.WriteLogline($"fromGuacd task faulted: {ex.Message}"));
                    }
                }
                catch (SocketException ex) {
                    _logger.WriteLogline($"SocketException: Unable to reach guacd server at {guacdHostname}:{guacdPort} - {ex.Message}");
                    if (ex.SocketErrorCode == SocketError.HostNotFound ||
                        ex.SocketErrorCode == SocketError.NoData ||
                        ex.SocketErrorCode == SocketError.HostUnreachable ||
                        ex.SocketErrorCode == SocketError.HostDown) {
                        await sendToBrowser(GuacEncode("error", "The guacd server could not be reached.", "10029"));
                        await disconnectBrowser();
                        return;
                    }
                    if (ex.SocketErrorCode == SocketError.ConnectionRefused) {
                        await sendToBrowser(GuacEncode("error", "The guacd server refused the connection.", "10030"));
                        await disconnectBrowser();
                        return;
                    }
                    await sendToBrowser(GuacEncode("error", "An unexpected error occurred when attempting to connect to the guacd server.", "10031"));
                    _logger.WriteLogline($"An unexpected error occurred when attempting to connect to the guacd server (SocketException): {ex}");
                    await disconnectBrowser();
                    return;
                }
            }
            catch (Exception ex) {
                Console.WriteLine("GuacdTunnel: Exception - " + ex);
                _logger.WriteLogline("An error occurred during the remote desktop session: " + ex.Message);
                await sendToBrowser(GuacEncode("error", "An unexpected error occurred during the remote desktop session.", "10034"));
            }
            finally {
                _logger.WriteLogline($"Remote desktop session ended for user '{userInfo.Username}' and resource '{resourcePath}'.");
                s_activeConnectionIds.TryRemove(currentConnectionId);
            }
        }
        finally {
            if (ws != null && ws.State == WebSocketState.Open) {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }

            ScheduleDelayedCleanup();
        }
    }

    private static AsyncDebouncer s_guacdStopDebouncer = new AsyncDebouncer();
    private static AsyncDebouncer s_guacdUninstallDebouncer = new AsyncDebouncer();

    /// <summary>
    /// Schedules a delayed cleanup task to stop and uninstall guacd.
    /// <br /><br />
    /// This method uses debouncing to ensure that the cleanup actions are only
    /// performed after a specified delay, and only if there are no active connections.
    /// </summary>
    private void ScheduleDelayedCleanup() {
        var FIVE_MINUTES_AS_MS = (int)TimeSpan.FromMinutes(5).TotalMilliseconds;
        var FIFTEEN_MINUTES_AS_MS = (int)TimeSpan.FromMinutes(15).TotalMilliseconds;

        // if there are no more active connections, request guacd stop
        // so that WSL does not remain running unnecessarily
        if (s_activeConnectionIds.Count == 0) {
            s_guacdStopDebouncer.DebounceAsync(FIVE_MINUTES_AS_MS, () => {
                // after the wait period, confirm there are still no active connections
                if (s_activeConnectionIds.Count == 0) {
                    return Guacd.Stop();
                }
                return Task.CompletedTask;
            }).Catch(ex => {
                _logger.WriteLogline("Error while attempting to stop guacd after all connections closed: " + ex);
            });
        }

        // similarly, remove the guacd distribution if there are no active connections
        if (s_activeConnectionIds.Count == 0) {
            s_guacdUninstallDebouncer.DebounceAsync(FIFTEEN_MINUTES_AS_MS, () => {
                // after the wait period, confirm there are still no active connections
                if (s_activeConnectionIds.Count == 0 && Guacd.IsWindowsSubsystemForLinuxInstalled) {
                    Guacd.UninstallGuacd();
                }
                return Task.CompletedTask;
            }).Catch(ex => {
                _logger.WriteLogline("Error while attempting to uninstall guacd after all connections closed: " + ex);
            });
        }
    }

    /// Reads until at least one ';' is read. Returns the data up to (and including)
    /// the last semicolon found so far. If multiple complete instructions exist,
    /// all are returned. Partial remainders are kept internally for the next call.
    internal sealed class MessageCollector(Stream stream, int bufferSize = 65536) {
        private readonly Stream _stream = stream;
        private readonly StringBuilder _buffer = new();
        private readonly byte[] _readBuf = new byte[bufferSize];

        public async Task<string> ReadUntilSemicolonAsync(CancellationToken token = default) {
            while (true) {
                var buf = _buffer.ToString();
                if (buf.Contains(';')) {
                    // Take everything up to last ';'
                    var lastSemi = buf.LastIndexOf(';');
                    var complete = buf[..(lastSemi + 1)];
                    _buffer.Remove(0, lastSemi + 1);
                    return complete;
                }

                var n = await _stream.ReadAsync(_readBuf, 0, _readBuf.Length, token);
                if (n == 0) {
                    // connection closed
                    return _buffer.Length > 0 ? buf : null;
                }

                _buffer.Append(Encoding.ASCII.GetString(_readBuf, 0, n));
            }
        }
    }

    /// <summary>
    /// Checks the SSL certificate details of the specified host and port.
    /// Returns the certificate and any SSL policy errors encountered during validation.
    /// </summary>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    internal static (X509Certificate2, SslPolicyErrors?) CheckCertificateDetails(string host, string port, int timeoutMilliseconds = 6000) {
        // connect to the server
        using var tcpClient = new TcpClient();
        var connectTask = tcpClient.ConnectAsync(host, int.Parse(port));
        if (!connectTask.Wait(timeoutMilliseconds)) {
            throw new TimeoutException($"Connection to {host}:{port} timed out.");
        }

        // establish an SSL stream, preserving any certificate errors
        SslPolicyErrors policyErrors = SslPolicyErrors.None;
        using var sslStream = new SslStream(
            tcpClient.GetStream(),
            false,
            (sender, certificate, chain, errors) => {
                policyErrors = errors;
                return true;
            }
        );

        // perform an SSL handshake so we can retrieve the certificate
        sslStream.AuthenticateAsClient(
        host,
        null,
        SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
        checkCertificateRevocation: false
    );

        // get the remote certificate
        var remoteCert = sslStream.RemoteCertificate;
        if (remoteCert == null) {
            Console.WriteLine("No certificate retrieved.");
            return (null, null);
        }

        return (new X509Certificate2(remoteCert), policyErrors);
    }

    private static IPAddress ResolveToIpv4(string hostname) {
        return Dns.GetHostAddresses(hostname).FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);
    }
}

public static class TaskExtensions {
    /// <summary>
    /// Catches exceptions from a Task and invokes the provided error handler.
    /// <br /><br />
    /// If no error handler is provided, exceptions are suppressed.
    /// </summary>
    /// <param name="task"></param>
    /// <param name="onError"></param>
    public static void Catch(this Task task, Action<Exception> onError = null) {
        if (task == null) return;
        task.ContinueWith(t => {
            var ex = t.Exception?.Flatten().InnerException ?? t.Exception;
            try { onError?.Invoke(ex); }
            catch { /* swallow logging exceptions */ }
        },
        TaskContinuationOptions.OnlyOnFaulted);
    }
}
