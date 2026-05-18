using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace RAWeb.Server.Utilities;

/// <summary>
/// Minimal HMAC-SHA512 JWT helper. Used in place of a JWT library because third-party
/// JWT libraries either require reflection (incompatible with Native AOT) or enforce a
/// minimum key size of 512 bits that not all provider secrets satisfy.
/// </summary>
internal sealed class Hs512Jwt(string secret) {
  private readonly byte[] _key = Encoding.UTF8.GetBytes(secret);

  /// <summary>
  /// Creates a compact JWT (header.payload.signature) signed with HMAC-SHA512.
  /// </summary>
  public string Create(IDictionary<string, object> claims) {
    var header = Base64UrlEncode(Encoding.UTF8.GetBytes("{\"alg\":\"HS512\",\"typ\":\"JWT\"}"));

    var payloadObj = new JObject();
    foreach (var kvp in claims) {
      payloadObj[kvp.Key] = kvp.Value switch {
        string s => (JToken)s,
        long l => (JToken)l,
        int i => (JToken)i,
        bool b => (JToken)b,
        _ => (JToken)(kvp.Value?.ToString() ?? "")
      };
    }
    var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadObj.ToString(Newtonsoft.Json.Formatting.None)));

    var signingInput = $"{header}.{payload}";
    using var hmac = new HMACSHA512(_key);
    var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput)));

    return $"{signingInput}.{signature}";
  }

  /// <summary>
  /// Verifies the HMAC-SHA512 signature of a compact JWT and returns its payload claims.
  /// </summary>
  /// <param name="token">The compact JWT string to verify and decode.</param>
  /// <returns>A JObject containing the claims from the token payload if the signature is valid.</returns>
  /// <exception cref="InvalidOperationException">Thrown if the token is not in a valid JWT format or if the signature verification fails.</exception>
  public JObject VerifyAndDecode(string token) {
    var parts = token.Split('.');
    if (parts.Length != 3) {
      throw new InvalidOperationException("ID token has an invalid JWT format");
    }

    var signingInput = $"{parts[0]}.{parts[1]}";
    using var hmac = new HMACSHA512(_key);
    var expectedSig = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput)));

    if (!CryptographicEquals(parts[2], expectedSig)) {
      throw new InvalidOperationException("ID token signature is invalid");
    }

    return JObject.Parse(Encoding.UTF8.GetString(Base64UrlDecode(parts[1])));
  }

  private static string Base64UrlEncode(byte[] bytes) {
    return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
  }

  private static byte[] Base64UrlDecode(string s) {
    s = s.Replace('-', '+').Replace('_', '/');
    s += (s.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
    return Convert.FromBase64String(s);
  }

  // Constant-time comparison to prevent timing attacks on signature bytes.
  private static bool CryptographicEquals(string a, string b) {
    if (a.Length != b.Length) return false;
    var result = 0;
    for (var i = 0; i < a.Length; i++) {
      result |= a[i] ^ b[i];
    }
    return result == 0;
  }
}
