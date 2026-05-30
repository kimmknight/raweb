using System.Buffers.Binary;

namespace RAWeb.Server.Middleware;


internal static class UseNegotiateToNtlmMiddleware {
  /// <summary>
  /// Adds middleware to the ASP.NET Core pipeline that rewrites incoming
  /// NTLM Authorization headers to Negotiate and outgoing Negotiate WWW-Authenticate
  /// and Negotiate Authorization headers to NTLM.
  /// <br /><br />
  /// This is necessary because the Microsoft.AspNetCore.Authentication.Negotiate
  /// handler only accepts the "Negotiate" prefix, not "NTLM". However, some
  /// clients (e.g. the Android Windows App) only accept the "NTLM" prefix and will
  /// not respond to a "Negotiate" challenge.
  /// <br /><br />
  /// Microsoft.AspNetCore.Authentication.Negotiate can directly read and write
  /// NTLM tokens without any additional wrapping, so all we need to do is
  /// rewrite the headers to use the correct prefix.
  /// <br /><br />
  /// The following steps are performed during the multi-phase NTLM handshake:
  /// <list type="number">
  ///   <item><description>If the outgoing WWW-Authenticate header is Negotiate (empty), rewrite it to NTLM (empty).</description></item>
  ///   <item><description>If the incoming Authorization header is NTLM &lt;type 1&gt;, rewrite it to Negotiate &lt;type 1&gt;.</description></item>
  ///   <item><description>If the outgoing WWW-Authenticate header is Negotiate &lt;type 2&gt;, rewrite it to NTLM &lt;type 2&gt;.</description></item>
  ///   <item><description>If the incoming Authorization header is Negotiate &lt;type 3&gt;, rewrite it to NTLM &lt;type 3&gt;.</description></item>
  ///   <item><description>If the outgoing WWW-Authenticate header is Negotiate (empty) and the incoming Authorization header is Negotiate &lt;type 3&gt;, rewrite both to NTLM.</description></item>
  /// </list>
  /// </summary>
  /// <param name="app"></param>
  internal static void UseRewriteNegotiateToNtlm(this WebApplication app) {
    app.Use(async (context, next) => {
      var incomingAuthorizationHeader = context.Request.Headers.Authorization.ToString();
      var timestamp = DateTime.UtcNow.ToString("o");

      NtlmMessageType incomingAuthorizationMessageType = NtlmMessageType.Indeterminate;
      if (TryParseNtlmAuthHeaderType(incomingAuthorizationHeader, out incomingAuthorizationMessageType)) {
        Console.WriteLine($"\n[{timestamp}] [Type: {incomingAuthorizationMessageType}] Incoming Authorization header: {incomingAuthorizationHeader}\n");

        // STEP 2: If the Authorization header is NTLM <type 1>, this is step 2.
        //         In that case, we need to replace the prefix with "Negotiate"
        //         so that Microsoft.AspNetCore.Authentication.Negotiate can handle it.
        //         The Negotiate handler from Microsoft.AspNetCore.Authentication.Negotiate
        //         is smart enough to directly handle NTLM tokens, but it only accepts the
        //         "Negotiate" prefix, not "NTLM".
        var isStep2 = incomingAuthorizationMessageType == NtlmMessageType.Negotiate;
        if (isStep2) {
          Console.WriteLine($"\n[{timestamp}] Step 2 detected: Rewriting Authorization header to Negotiate\n");
          context.Request.Headers.Remove("Authorization");
          context.Request.Headers.Append("Authorization", incomingAuthorizationHeader.Replace("NTLM", "Negotiate"));
        }

        // STEP 4: If the Authorization header is NTLM <type 3>, this is step 4.
        //         In that case, we need to replace the prefix with "Negotiate"
        //         so that Microsoft.AspNetCore.Authentication.Negotiate can handle it.
        var isStep4 = incomingAuthorizationMessageType == NtlmMessageType.Authenticate;
        if (isStep4) {
          Console.WriteLine($"\n[{timestamp}] Step 4 detected: Rewriting Authorization header to Negotiate\n");
          context.Request.Headers.Remove("Authorization");
          context.Request.Headers.Append("Authorization", incomingAuthorizationHeader.Replace("NTLM", "Negotiate"));
        }
      }

      // log the outgoing WWW-Authenticate headers for debugging
      context.Response.OnStarting(() => {
        var outgoingWWWAuthenticateHeader = context.Response.Headers.WWWAuthenticate.ToString();

        // STEP 1: If the authenticate header is Negotiate and the authorization header
        //         is empty, this is step 1. In that case, we only need to replace the
        //         authenticate header with "NTLM".
        //         This should cause the client to respond with step 2.
        var isStep1 = outgoingWWWAuthenticateHeader.StartsWith("Negotiate", StringComparison.OrdinalIgnoreCase) &&
                      string.IsNullOrEmpty(incomingAuthorizationHeader);
        if (isStep1) {
          Console.WriteLine($"\n[{timestamp}] Step 1 detected: Rewriting WWW-Authenticate header to NTLM\n");
          context.Response.Headers.Remove("WWW-Authenticate");
          context.Response.Headers.Append("WWW-Authenticate", "NTLM");
          return Task.CompletedTask;
        }

        // STEP 3: If the WWW-Authenticate header is Negotiate <type 2> and the Authorization header
        //         is Negotiate <type 1>, this is step 3.
        //         In this case, we need to re-write both headers to use the NTLM prefix.
        //         This should cause the client to respond with step 4.
        var isStep3 = outgoingWWWAuthenticateHeader.StartsWith("Negotiate", StringComparison.OrdinalIgnoreCase) &&
                      !string.IsNullOrEmpty(incomingAuthorizationHeader) &&
                      incomingAuthorizationMessageType == NtlmMessageType.Negotiate;
        if (isStep3) {
          Console.WriteLine($"\n[{timestamp}] Step 3 detected: Rewriting WWW-Authenticate and Authorization headers to NTLM\n");
          context.Response.Headers.Remove("WWW-Authenticate");
          context.Response.Headers.Append("WWW-Authenticate", outgoingWWWAuthenticateHeader.Replace("Negotiate", "NTLM"));
          context.Request.Headers.Remove("Authorization");
          context.Request.Headers.Append("Authorization", incomingAuthorizationHeader.Replace("Negotiate", "NTLM"));
          return Task.CompletedTask;
        }

        // STEP 5: If the WWW-Authenticate header is Negotiate (empty) and the Authorization header
        //         is Negotiate <type 3>, this is step 5.
        //         In this case, we need to re-write both headers to use the NTLM prefix.
        var isStep5 = outgoingWWWAuthenticateHeader.StartsWith("Negotiate", StringComparison.OrdinalIgnoreCase) &&
                      !string.IsNullOrEmpty(incomingAuthorizationHeader) &&
                      incomingAuthorizationMessageType == NtlmMessageType.Authenticate;
        if (isStep5) {
          Console.WriteLine($"\n[{timestamp}] Step 5 detected: Rewriting WWW-Authenticate and Authorization headers to NTLM\n");
          context.Response.Headers.Remove("WWW-Authenticate");
          context.Response.Headers.Append("WWW-Authenticate", outgoingWWWAuthenticateHeader.Replace("Negotiate", "NTLM"));
          context.Request.Headers.Remove("Authorization");
          context.Request.Headers.Append("Authorization", incomingAuthorizationHeader.Replace("Negotiate", "NTLM"));
          return Task.CompletedTask;
        }

        return Task.CompletedTask;
      });

      await next();
    });
  }

  internal enum NtlmMessageType {
    Indeterminate = 0,
    Negotiate = 1, // client -> server
    Challenge = 2, // server -> client
    Authenticate = 3 // client -> server
  }

  /// <summary>
  /// Parses the NTLM message type from the given byte array.
  ///
  /// All NTLM messages start with <c>"NTLMSSP\0"</c> followed by a 32-bit integer 
  /// indicating the message type. The message type can be:
  /// <list type="bullet">
  ///   <item><description>0: Indeterminate (there is no message yet or something went wrong)</description></item>
  ///   <item><description>1: Negotiate (client → server)</description></item>
  ///   <item><description>2: Challenge (server → client)</description></item>
  ///   <item><description>3: Authenticate (client → server)</description></item>
  /// </list>
  /// </summary>
  /// <param name="data">The raw NTLM message bytes.</param>
  /// <returns>The parsed NTLM message type.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if the message type cannot be determined.
  /// </exception>
  private static NtlmMessageType ParseNtlmMessageType(ReadOnlySpan<byte> data) {
    // all NTLM messages start with "NTLMSSP\0"
    if (data.Length < 12 || !data[..8].SequenceEqual("NTLMSSP\0"u8)) {
      throw new InvalidOperationException("Not a valid NTLM message.");
    }

    // message type is a uint32 at offset 8
    var messageType = BinaryPrimitives.ReadInt32LittleEndian(data[8..12]);
    return messageType switch {
      0 => NtlmMessageType.Indeterminate,
      1 => NtlmMessageType.Negotiate,
      2 => NtlmMessageType.Challenge,
      3 => NtlmMessageType.Authenticate,
      _ => throw new InvalidOperationException($"Unknown NTLM message type: {messageType}")
    };
  }

  /// <summary>
  /// Tries to parse the NTLM message type from the given Authorization header value.
  /// <br /><br />
  /// See the parameter documentation for the expected forms of the header value
  /// and the possible output message types.
  /// </summary>
  /// <param name="headerValue">
  /// The header value can be in one of the following forms:
  /// <list type="bullet">
  ///   <item><description>NTLM</description></item>
  ///   <item><description>Negotiate</description></item>
  ///   <item><description>NTLM &lt;base64-encoded-ntlm-token&gt;</description></item>
  ///   <item><description>Negotiate &lt;base64-encoded-ntlm-token&gt;</description></item>
  /// </list>
  /// </param>
  /// <param name="messageType">
  /// The parsed message type.
  /// </param>
  /// <returns>Whether parsing was successful</returns>
  internal static bool TryParseNtlmAuthHeaderType(string headerValue, out NtlmMessageType messageType) {
    messageType = NtlmMessageType.Indeterminate;
    var startsWithNtlmOrNegotiate = headerValue.StartsWith("NTLM", StringComparison.OrdinalIgnoreCase) ||
                                    headerValue.StartsWith("Negotiate", StringComparison.OrdinalIgnoreCase);
    if (!startsWithNtlmOrNegotiate) {
      return false;
    }

    var isIndeterminate = headerValue.Equals("NTLM", StringComparison.OrdinalIgnoreCase) ||
                          headerValue.Equals("Negotiate", StringComparison.OrdinalIgnoreCase);
    if (isIndeterminate) {
      messageType = NtlmMessageType.Indeterminate;
      return true;
    }

    var message = headerValue.StartsWith("NTLM ", StringComparison.OrdinalIgnoreCase) ?
                    headerValue["NTLM ".Length..].Trim() :
                    headerValue["Negotiate ".Length..].Trim();
    var messageBytes = Convert.FromBase64String(message);

    try {
      messageType = ParseNtlmMessageType(messageBytes);
      return true;
    }
    catch {
      return false;
    }
  }

}
