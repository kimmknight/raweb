using System;
using System.Security.Cryptography;
using System.Text;

namespace RAWeb.Server.Utilities;

/// <summary>
/// Creates and checks the OAuth state values used for MFA providers (Duo and LoginTC).
/// <br /><br />
/// A state value has the form <c>&lt;random&gt;.&lt;hash&gt;</c>, where the random part makes the
/// state unguessable and the hash is the SHA-256 hash of the return URL used in the authorization
/// request. When the provider redirects back, <see cref="MatchesReturnUrl"/> confirms that the
/// return URL used to verify the response is the same one used to start the request.
/// </summary>
internal static class OAuthState {
  /// <summary>
  /// Creates a new state value for the specified return URL.
  /// </summary>
  public static string Create(string returnUrl) {
    var random = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    return $"{random}.{HashReturnUrl(returnUrl)}";
  }

  /// <summary>
  /// Determines whether the state value was created for the specified return URL.
  /// </summary>
  public static bool MatchesReturnUrl(string state, string returnUrl) {
    var separatorIndex = state.LastIndexOf('.');
    if (separatorIndex <= 0) {
      return false;
    }

    var expectedHash = Encoding.ASCII.GetBytes(state[(separatorIndex + 1)..]);
    var actualHash = Encoding.ASCII.GetBytes(HashReturnUrl(returnUrl));
    return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
  }

  private static string HashReturnUrl(string returnUrl) {
    return Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(returnUrl)));
  }

  private static string Base64UrlEncode(byte[] bytes) {
    return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
  }
}
