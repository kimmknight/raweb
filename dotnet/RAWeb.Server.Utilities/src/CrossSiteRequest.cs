using System;
using Microsoft.AspNetCore.Http;

namespace RAWeb.Server.Utilities;

/// <summary>
/// Detects requests that a browser sends to RAWeb on behalf of a page on another origin,
/// such as a form on another site that submits to RAWeb with the user's cookies
/// (cross-site request forgery), an image or script that loads a RAWeb URL to learn
/// about the user (cross-site leaks), or a frame that shows RAWeb (clickjacking).
/// </summary>
public static class CrossSiteRequest {
  /// <summary>
  /// Whether the request must be rejected because a page on another origin sent it.
  /// <br /><br />
  /// The only requests that are allowed from other origins are top-level navigations with GET
  /// (e.g., the MFA provider redirecting back to RAWeb). Their
  /// responses are shown to the user instead of the other page. All other requests from other
  /// origins are rejected, including:
  /// <list type="bullet">
  /// <item>requests that can change data (e.g., POST, PUT, PATCH, and DELETE),</item>
  /// <item>WebSocket handshakes,</item>
  /// <item>requests for subresources (e.g., images and scripts),</item>
  /// <item>requests to show RAWeb in a frame, and</item>
  /// <item>top-level navigations to the management API.</item>
  /// </list>
  /// </summary>
  public static bool ShouldBlock(HttpRequest request) {
    if (!IsFromAnotherOrigin(request)) {
      return false;
    }

    return !IsTopLevelNavigation(request) || IsManagementApiRequest(request);
  }

  /// <summary>
  /// Whether the request is for the management API, which only RAWeb's own pages use.
  /// <br /><br />
  /// Some management endpoints make the server contact a path or host from the request
  /// (e.g., an icon on a UNC path) with the server's own credentials. Even a top-level
  /// navigation from another origin must not reach them; Otherwise, a page on another
  /// site could make an administrator's browser send the server's credentials to a host
  /// that the malicious page chooses.
  /// </summary>
  private static bool IsManagementApiRequest(HttpRequest request) {
    return request.Path.StartsWithSegments("/api/management", StringComparison.OrdinalIgnoreCase);
  }

  private static bool IsWebSocketHandshake(HttpRequest request) {
    return request.Headers["Upgrade"].ToString().Contains("websocket", StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Whether the request is a GET request that navigates the whole browser tab or window
  /// (not a frame) to RAWeb.
  /// </summary>
  private static bool IsTopLevelNavigation(HttpRequest request) {
    if (!HttpMethods.IsGet(request.Method) || IsWebSocketHandshake(request)) {
      return false;
    }

    var fetchDest = request.Headers["Sec-Fetch-Dest"].ToString();
    var fetchMode = request.Headers["Sec-Fetch-Mode"].ToString();

    var isFromServiceWorker = fetchMode == "navigate" && fetchDest == "empty";
    var isOtherTopLevelNavigation = fetchMode == "navigate" && fetchDest == "document";

    return isFromServiceWorker || isOtherTopLevelNavigation;
  }

  /// <summary>
  /// Whether a browser sent the request from a page on another origin.
  /// <br /><br />
  /// Browsers set the Sec-Fetch-Site header, and pages cannot change it. Requests from other
  /// subdomains of the same site ("same-site") are treated as cross-origin because SameSite
  /// cookies do not protect against them.
  /// <br /><br />
  /// Older browsers that do not send Sec-Fetch-Site still send the Origin header with requests
  /// that can change data and with fetch requests from other origins, so it is compared with the
  /// request's host instead. Requests with neither header are allowed because they are either
  /// navigations in older browsers or requests that do not come from a page in a browser
  /// (e.g., Remote Desktop clients).
  /// </summary>
  private static bool IsFromAnotherOrigin(HttpRequest request) {
    var fetchSite = request.Headers["Sec-Fetch-Site"].ToString();
    if (!string.IsNullOrEmpty(fetchSite)) {
      // "none" means that the user started the request (e.g., from the address bar or a bookmark)
      return fetchSite != "same-origin" && fetchSite != "none";
    }

    var origin = request.Headers["Origin"].ToString();
    if (string.IsNullOrEmpty(origin)) {
      return false;
    }

    // an opaque origin ("null"), such as from a sandboxed iframe, is never the same origin
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri)) {
      return true;
    }

    var requestPort = request.Host.Port ?? (request.IsHttps ? 443 : 80);
    return !string.Equals(originUri.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase) || originUri.Port != requestPort;
  }
}
