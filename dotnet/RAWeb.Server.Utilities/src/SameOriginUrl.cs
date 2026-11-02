using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace RAWeb.Server.Utilities;

/// <summary>
/// Checks the URLs to which a client asks RAWeb to redirect (e.g., the return URL after MFA).
/// RAWeb must only redirect to its own pages so that it cannot be used to send users to another site
/// or to send MFA responses to a host that an attacker controls.
/// </summary>
public static class SameOriginUrl {
  /// <summary>
  /// Whether <paramref name="url"/> is an absolute http or https URL with the same origin
  /// (scheme, host, and port) as the page that sent the request.
  /// <br /><br />
  /// The page's origin comes from the request's Origin header, which browsers send with requests
  /// such as POST fetches and which reflects the address the user sees, even when RAWeb runs behind
  /// a reverse proxy that changes the scheme, host, or port. When there is no Origin header,
  /// the request's own scheme and host are used instead.
  /// <br /><br />
  /// URLs with user information (e.g., <c>https://user@host/</c>) return false as a precaution.
  /// RAWeb never puts credentials in its URLs.
  /// </summary>
  public static bool IsSameOrigin(HttpRequest request, [NotNullWhen(true)] string? url) {
    if (string.IsNullOrWhiteSpace(url) || !TryParseHttpUrl(url, out var uri) || !string.IsNullOrEmpty(uri.UserInfo)) {
      return false;
    }

    if (!TryGetExpectedOrigin(request, out var expectedOrigin)) {
      return false;
    }

    return string.Equals(uri.Scheme, expectedOrigin.Scheme, StringComparison.OrdinalIgnoreCase)
      && string.Equals(uri.IdnHost, expectedOrigin.IdnHost, StringComparison.OrdinalIgnoreCase)
      && uri.Port == expectedOrigin.Port;
  }

  private static bool TryGetExpectedOrigin(HttpRequest request, out Uri origin) {
    var originHeader = request.Headers["Origin"].ToString();
    if (!string.IsNullOrEmpty(originHeader)) {
      // an Origin header that is not an http(s) origin (e.g., "null" from a sandboxed page) never matches
      return TryParseHttpUrl(originHeader, out origin);
    }

    return TryParseHttpUrl($"{request.Scheme}://{request.Host}", out origin);
  }

  private static bool TryParseHttpUrl(string value, out Uri uri) {
    if (Uri.TryCreate(value, UriKind.Absolute, out var parsed) && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps)) {
      uri = parsed;
      return true;
    }

    uri = null!;
    return false;
  }
}
