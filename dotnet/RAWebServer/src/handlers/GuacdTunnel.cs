using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;
using RAWeb.Server.Utilities;

namespace RAWebServer.Handlers {
    public class GuacdTunnel : HttpTaskAsyncHandler {
        public override bool IsReusable { get { return false; } }

        public override Task ProcessRequestAsync(HttpContext context) {
            if (context.IsWebSocketRequest) {
                var requestedProtocols = context.WebSocketRequestedProtocols;

                // the protocol must be "guacamole"
                if (!requestedProtocols.Contains("guacamole")) {
                    context.Response.StatusCode = 400;
                    context.Response.Write("WebSocket subprotocol 'guacamole' required.");
                    return Task.FromResult<object>(null);
                }

                var options = new AspNetWebSocketOptions {
                    SubProtocol = "guacamole"
                };
                context.AcceptWebSocketRequest(ProcessWebSocket, options);
            }
            else {
                context.Response.StatusCode = 400;
                context.Response.Write("WebSocket only.");
            }

            return Task.FromResult<object>(null);
        }

        private static string GuacEncode(params string[] parts) {
            return string.Join(",", parts.Select(p => (p ?? "").Length + "." + (p ?? ""))) + ";";
        }

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

        private static string ReadGuacdReply(NetworkStream stream) {
            var buffer = new byte[4096];
            var sb = new StringBuilder();
            int bytesRead;

            // Read until we encounter a semicolon, indicating the end of the instruction
            do {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                sb.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
            } while (!sb.ToString().Contains(";"));

            return sb.ToString();
        }

        private static string ReadReadyMessage(string message) {
            var parts = GuacDecode(message);
            if (parts.Count < 1 || parts[0] != "ready" || parts.Count > 2)
                throw new ArgumentException("Not a valid ready instruction.");

            return parts[1];
        }

        private async Task ProcessWebSocket(AspNetWebSocketContext wsContext) {
            var ws = wsContext.WebSocket;
            Console.WriteLine("GuacdTunnel: WebSocket session started.");

            // sends a message to the browser via WebSocket.
            Task sendToBrowser(string message) => ws.SendAsync(
                new ArraySegment<byte>(Encoding.ASCII.GetBytes(message)),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );

            // disconnects the browser websocket
            async Task disconnectBrowser(string reason = null) {
                await sendToBrowser(GuacEncode("disconnect"));
                await ws.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    reason,
                    CancellationToken.None
                );
            }

            // waits for a message from the browser via WebSocket.
            async Task<string> receiveFromBrowser(string startsWith = null) {
                var buffer = new byte[8192];
                var result = await ws.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );



                if (result.MessageType == WebSocketMessageType.Close) {
                    return null;
                }

                var message = Encoding.ASCII.GetString(buffer, 0, result.Count);
                if (startsWith != null) {
                    if (!message.StartsWith(startsWith)) {
                        return null;
                    }
                }

                return message;
            }

            // check for a user
            var userInfo = UserInformation.FromHttpRequestSafe(HttpContext.Current.Request);
            if (userInfo == null) {
                // send anauthorized user error
                await sendToBrowser(GuacEncode("error", "You are not authenticated.", "401"));
                await disconnectBrowser();
                return;
            }

            // check that there is a resource available and that the user has permission to access it
            var resourcePath = wsContext.QueryString["rPath"];
            var resourceFrom = wsContext.QueryString["rFrom"];
            if (string.IsNullOrEmpty(resourcePath) || string.IsNullOrEmpty(resourceFrom)) {
                await sendToBrowser(GuacEncode("error", "Resource path and source must be specified.", "10000"));
                await disconnectBrowser();
                return;
            }

            string rdpContents;
            try {
                var resolvedResource = ResourceContentsResolver.ResolveResource(userInfo, resourcePath, resourceFrom);
                if (resolvedResource is ResourceContentsResolver.FailedResourceResult failedResource) {
                    if (failedResource.PermissionHttpStatus == System.Net.HttpStatusCode.NotFound) {
                        await sendToBrowser(GuacEncode("error", "The requested resource was not found.", "516"));
                        await disconnectBrowser();
                        return;
                    }

                    await sendToBrowser(GuacEncode("error", "You are not authorized to access this resource.", ((int)failedResource.PermissionHttpStatus).ToString()));
                    await disconnectBrowser();
                    return;
                }
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

            // check the certificate of the target server
            var shouldIgnoreCertificateErrors = wsContext.QueryString["ignoreCertErrors"] == "true";
            if (shouldIgnoreCertificateErrors == false) {
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
                }
                catch (SocketException ex) {
                    if (ex.SocketErrorCode == SocketError.HostNotFound ||
                        ex.SocketErrorCode == SocketError.NoData) {
                        await sendToBrowser(GuacEncode("error", "The specified remote host could not be reached.", "10010"));
                        await disconnectBrowser();
                        return;
                    }
                    await sendToBrowser(GuacEncode("error", "Error checking server certificate: " + ex.Message, "10009"));
                    await disconnectBrowser();
                    return;
                }
            }

            // wait for credentials from the browser
            await sendToBrowser(GuacEncode("raweb-demand-credentials"));
            var domainMessage = await receiveFromBrowser("6.domain");
            var usernameMessage = await receiveFromBrowser("8.username");
            var passwordMessage = await receiveFromBrowser("8.password");
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
            var widthMessage = await receiveFromBrowser("12.displayWidth");
            var heightMessage = await receiveFromBrowser("13.displayHeight");
            var dpiMessage = await receiveFromBrowser("10.displayDPI");
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
            var timezoneMessage = await receiveFromBrowser("8.timezone");
            var timezone = GuacDecode(timezoneMessage).ElementAtOrDefault(1) ?? "UTC";

            // if there is a gateway hostname, get it
            var gatewayHostname = GetRdpFileProperty("gatewayhostname:s:");
            var gatewayPort = "443";
            if (!string.IsNullOrEmpty(gatewayHostname)) {
                if (gatewayHostname.Contains(":")) {
                    var parts = gatewayHostname.Split(':');
                    gatewayHostname = parts[0];
                    gatewayPort = parts[1];
                }
            }

            // if there is a gateway, demand credentials for it
            string gatewayDomain = null;
            string gatewayUsername = null;
            string gatewayPassword = null;
            if (!string.IsNullOrEmpty(gatewayHostname)) {
                await sendToBrowser(GuacEncode("raweb-demand-gateway-credentials"));
                var gwDomainMessage = await receiveFromBrowser("14.gateway-domain");
                var gwUsernameMessage = await receiveFromBrowser("16.gateway-username");
                var gwPasswordMessage = await receiveFromBrowser("16.gateway-password");
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
            }

            try {
                var guacdAddress = PoliciesManager.RawPolicies["GuacdWebClient.Address"];
                var guacdAddressParts = guacdAddress.Split(':');
                if (guacdAddressParts.Length != 2) {
                    await sendToBrowser(GuacEncode("error", "Guacd address is not properly configured.", "10011"));
                    await disconnectBrowser();
                }
                var guacdHostname = guacdAddressParts[0];
                var guacdPort = int.TryParse(guacdAddressParts[1], out var p) ? p : 4822;

                using (var guacd = new TcpClient(guacdHostname, guacdPort))
                using (var stream = guacd.GetStream()) {
                    Console.WriteLine("GuacdTunnel: Connected to guacd.");

                    // tell guacd we want to use rdp
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(GuacEncode("select", "rdp")), 0,
                        GuacEncode("select", "rdp").Length);
                    await stream.FlushAsync();
                    Console.WriteLine("GuacdTunnel: Sent select to guacd.");

                    // read what guacd sends back
                    var reply = ReadGuacdReply(stream);
                    var argsInstruction = ParseArgsInstruction(reply);
                    if (argsInstruction.Version != GuacProtocolVersion.VERSION_1_5_0) {
                        throw new ArgumentException("Unsupported Guacamole protocol version: " + argsInstruction.Version);
                    }

                    string[] defaultAudio = ["audio/L16"];
                    string[] defaultVideo = null;
                    string[] defaultImage = ["image/png", "image/jpeg"];
                    string defaultTimezone = null;

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
                            "color-depth" => "16",
                            "width" => GetRdpFileProperty("desktopwidth:i:"),
                            "height" => GetRdpFileProperty("desktopheight:i:"),
                            "dpi" => displayDpi,
                            "resize-method" => "display-update",
                            // device redirection
                            "disable-audio" => GetRdpFileProperty("audiomode:i:") == "1" ? "true" : "false",
                            "enable-audio-input" => GetRdpFileProperty("audiocapturemode:i:") == "1" ? "true" : "false",
                            "enable-touch" => "true",
                            "enable-printing" => "true",
                            "printer-name" => "RAWeb Print-to-PDF",
                            "enable-drive" => "false",
                            "disable-download" => "false",
                            "disable-upload" => "true",
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
                            _ => ""
                        };
                    });
                    sb.Append(GuacEncode(["connect", $"VERSION_{argsInstruction.Version}", .. connectArgs]));

                    await stream.WriteAsync(Encoding.ASCII.GetBytes(sb.ToString()), 0, sb.ToString().Length);
                    await stream.FlushAsync();

                    // check for read message from guacd
                    reply = ReadGuacdReply(stream);
                    var connectionId = ReadReadyMessage(reply);
                    Console.WriteLine("GuacdTunnel: Connection established with ID " + connectionId);

                    // Relay guacd -> browser
                    var fromGuacd = Task.Run(async () => {
                        var collector = new MessageCollector(stream, 8192);

                        while (ws.State == WebSocketState.Open) {
                            var msg = await collector.ReadUntilSemicolonAsync();
                            if (string.IsNullOrEmpty(msg)) {
                                Console.WriteLine("GuacdTunnel: guacd closed stream.");
                                break;
                            }

                            await ws.SendAsync(
                                new ArraySegment<byte>(Encoding.ASCII.GetBytes(msg)),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None);
                        }

                        Console.WriteLine("GuacdTunnel: fromGuacd task ending.");
                    });

                    // Browser -> guacd (clipboard, mouse, keyboard, etc.)
                    var toGuacd = Task.Run(async () => {
                        var buf = new byte[8192];
                        while (ws.State == WebSocketState.Open) {
                            var res = await ws.ReceiveAsync(
                                new ArraySegment<byte>(buf),
                                CancellationToken.None);

                            if (res.MessageType == WebSocketMessageType.Close) {
                                Console.WriteLine("GuacdTunnel: browser closed WebSocket.");
                                break;
                            }
                            await stream.WriteAsync(buf, 0, res.Count);
                        }

                        Console.WriteLine("GuacdTunnel: toGuacd task ending.");
                    });

                    await Task.WhenAny(toGuacd, fromGuacd);
                }
            }
            catch (Exception ex) {
                Console.WriteLine("GuacdTunnel: Exception - " + ex);
            }
            finally {
                if (ws != null && ws.State == WebSocketState.Open) {
                    ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait();
                }

                Console.WriteLine("GuacdTunnel: Session ended.");
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
                    if (_buffer.ToString().Contains(';')) {
                        // Take everything up to last ';'
                        var buf = _buffer.ToString();
                        var lastSemi = buf.LastIndexOf(';');
                        var complete = buf[..(lastSemi + 1)];
                        _buffer.Remove(0, lastSemi + 1);
                        return complete;
                    }

                    var n = await _stream.ReadAsync(_readBuf, 0, _readBuf.Length, token);
                    if (n == 0) {
                        // connection closed
                        return _buffer.Length > 0 ? _buffer.ToString() : null;
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
        internal static (X509Certificate2, SslPolicyErrors?) CheckCertificateDetails(string host, string port) {
            // connect to the server
            using var tcpClient = new TcpClient();
            tcpClient.Connect(host, int.Parse(port));


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
            try {
                sslStream.AuthenticateAsClient(
                host,
                null,
                SslProtocols.Tls12,
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
            catch (Exception ex) {
                Console.WriteLine($"Handshake failed: {ex.Message}");
            }

            return (null, null);
        }
    }
}
