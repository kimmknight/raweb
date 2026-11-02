using Microsoft.AspNetCore.Http;

namespace RAWeb.Server.Utilities.Tests;

public class CrossSiteRequestTests {
  private static HttpRequest CreateRequest(string method, string host = "raweb.example.com", string? fetchSite = null, string? fetchMode = null, string? fetchDest = null, string? origin = null, bool isWebSocket = false, string path = "/") {
    var context = new DefaultHttpContext();
    context.Request.Method = method;
    context.Request.Scheme = "https";
    context.Request.Host = new HostString(host);
    context.Request.Path = path;
    if (fetchSite is not null) {
      context.Request.Headers["Sec-Fetch-Site"] = fetchSite;
    }
    if (fetchMode is not null) {
      context.Request.Headers["Sec-Fetch-Mode"] = fetchMode;
    }
    if (fetchDest is not null) {
      context.Request.Headers["Sec-Fetch-Dest"] = fetchDest;
    }
    if (origin is not null) {
      context.Request.Headers["Origin"] = origin;
    }
    if (isWebSocket) {
      context.Request.Headers["Connection"] = "Upgrade";
      context.Request.Headers["Upgrade"] = "websocket";
    }
    return context.Request;
  }

  /// <summary>
  /// Links to RAWeb and redirects back from MFA providers are top-level navigations.
  /// </summary>
  [Test]
  [Arguments("cross-site")]
  [Arguments("same-site")]
  public async Task ShouldBlock_FalseForTopLevelNavigationFromAnotherOrigin(string fetchSite) {
    var request = CreateRequest("GET", fetchSite: fetchSite, fetchMode: "navigate", fetchDest: "document");

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsFalse();
  }

  /// <summary>
  /// RAWeb's service worker passes navigations on with fetch(event.request), which changes Sec-Fetch-Dest to "empty"
  /// but keeps the "navigate" mode and the cross-site Sec-Fetch-Site value (e.g., when an MFA provider redirects back to RAWeb).
  /// </summary>
  [Test]
  [Arguments("cross-site")]
  [Arguments("same-site")]
  public async Task ShouldBlock_FalseForTopLevelNavigationPassedOnByServiceWorker(string fetchSite) {
    var request = CreateRequest("GET", fetchSite: fetchSite, fetchMode: "navigate", fetchDest: "empty");

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsFalse();
  }

  [Test]
  public async Task ShouldBlock_TrueForTopLevelNavigationPassedOnByServiceWorkerToManagementApi() {
    var request = CreateRequest("GET", fetchSite: "cross-site", fetchMode: "navigate", fetchDest: "empty", path: "/api/management/resources/icon");

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsTrue();
  }

  /// <summary>
  /// Pages on other origins can only send "empty" requests with fetch() and XMLHttpRequest, which never use the "navigate" mode.
  /// </summary>
  [Test]
  [Arguments("cors")]
  [Arguments("no-cors")]
  [Arguments("same-origin")]
  public async Task ShouldBlock_TrueForEmptyDestinationWithoutNavigateMode(string fetchMode) {
    var request = CreateRequest("GET", fetchSite: "cross-site", fetchMode: fetchMode, fetchDest: "empty");

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsTrue();
  }

  /// <summary>
  /// Some management endpoints contact a path or host from the request with the server's credentials,
  /// so a link on another site must not be able to make an administrator's browser call them.
  /// </summary>
  [Test]
  [Arguments("cross-site", "/api/management/resources/icon")]
  [Arguments("same-site", "/api/management/security/find-sid")]
  [Arguments("cross-site", "/API/Management/resources/icon/indices")]
  public async Task ShouldBlock_TrueForTopLevelNavigationToManagementApiFromAnotherOrigin(string fetchSite, string path) {
    var request = CreateRequest("GET", fetchSite: fetchSite, fetchMode: "navigate", fetchDest: "document", path: path);

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsTrue();
  }

  [Test]
  [Arguments("same-origin")]
  [Arguments("none")]
  public async Task ShouldBlock_FalseForSameOriginOrUserInitiatedManagementApiRequest(string fetchSite) {
    var request = CreateRequest("GET", fetchSite: fetchSite, fetchMode: "navigate", fetchDest: "document", path: "/api/management/resources/icon");

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsFalse();
  }

  /// <summary>
  /// Only the management API itself is blocked, not other paths that start with the same characters.
  /// </summary>
  [Test]
  public async Task ShouldBlock_FalseForTopLevelNavigationToPathThatOnlyStartsLikeManagementApi() {
    var request = CreateRequest("GET", fetchSite: "cross-site", fetchMode: "navigate", fetchDest: "document", path: "/api/management-help");

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsFalse();
  }

  /// <summary>
  /// Another origin can see whether these requests succeed (e.g., an image's load and error events),
  /// so they must not reach endpoints that use the user's authentication cookie.
  /// </summary>
  [Test]
  [Arguments("cors", "empty")]
  [Arguments("no-cors", "image")]
  [Arguments("no-cors", "script")]
  [Arguments("no-cors", "style")]
  public async Task ShouldBlock_TrueForSubresourceGetFromAnotherOrigin(string fetchMode, string fetchDest) {
    var request = CreateRequest("GET", fetchSite: "same-site", fetchMode: fetchMode, fetchDest: fetchDest);

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsTrue();
  }

  /// <summary>
  /// Frames from another origin could be used for clickjacking.
  /// </summary>
  [Test]
  [Arguments("iframe")]
  [Arguments("frame")]
  [Arguments("object")]
  [Arguments("embed")]
  public async Task ShouldBlock_TrueForFrameNavigationFromAnotherOrigin(string fetchDest) {
    var request = CreateRequest("GET", fetchSite: "cross-site", fetchMode: "navigate", fetchDest: fetchDest);

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsTrue();
  }

  [Test]
  public async Task ShouldBlock_FalseForSameOriginSubresourceGet() {
    var request = CreateRequest("GET", fetchSite: "same-origin", fetchMode: "no-cors", fetchDest: "image");

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsFalse();
  }

  /// <summary>
  /// Older browsers do not send Fetch Metadata headers or the Origin header with GET navigations.
  /// </summary>
  [Test]
  public async Task ShouldBlock_FalseForGetWithoutFetchMetadataOrOrigin() {
    var request = CreateRequest("GET");

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsFalse();
  }

  /// <summary>
  /// Older browsers send the Origin header with fetch requests from other origins.
  /// </summary>
  [Test]
  public async Task ShouldBlock_TrueForGetWithOtherOriginAndNoFetchMetadata() {
    var request = CreateRequest("GET", origin: "https://evil.example.net");

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsTrue();
  }

  /// <summary>
  /// A form on another site that submits with POST is a top-level navigation,
  /// but it can change data.
  /// </summary>
  [Test]
  public async Task ShouldBlock_TrueForTopLevelPostNavigationFromAnotherOrigin() {
    var request = CreateRequest("POST", fetchSite: "cross-site", fetchMode: "navigate", fetchDest: "document");

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsTrue();
  }

  [Test]
  [Arguments("POST")]
  [Arguments("PUT")]
  [Arguments("PATCH")]
  [Arguments("DELETE")]
  public async Task ShouldBlock_TrueForCrossSiteUnsafeMethods(string method) {
    var request = CreateRequest(method, fetchSite: "cross-site");

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsTrue();
  }

  /// <summary>
  /// Another subdomain of the same site is not protected by SameSite cookies,
  /// so it must also be blocked.
  /// </summary>
  [Test]
  public async Task ShouldBlock_TrueForSameSitePost() {
    var request = CreateRequest("POST", fetchSite: "same-site");

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsTrue();
  }

  [Test]
  [Arguments("same-origin")]
  [Arguments("none")]
  public async Task ShouldBlock_FalseForSameOriginOrUserInitiatedPost(string fetchSite) {
    var request = CreateRequest("POST", fetchSite: fetchSite);

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsFalse();
  }

  /// <summary>
  /// Sec-Fetch-Site is computed by the browser, so it takes precedence over an Origin
  /// header that matches the host (e.g., when a reverse proxy changes the Host header).
  /// </summary>
  [Test]
  public async Task ShouldBlock_UsesSecFetchSiteBeforeOrigin() {
    var request = CreateRequest("POST", fetchSite: "same-origin", origin: "https://other-host.example.com");

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsFalse();
  }

  [Test]
  public async Task ShouldBlock_TrueForCrossSiteWebSocketHandshake() {
    var request = CreateRequest("GET", fetchSite: "cross-site", fetchMode: "websocket", fetchDest: "websocket", isWebSocket: true);

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsTrue();
  }

  [Test]
  public async Task ShouldBlock_FalseForSameOriginWebSocketHandshake() {
    var request = CreateRequest("GET", fetchSite: "same-origin", fetchMode: "websocket", fetchDest: "websocket", isWebSocket: true);

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsFalse();
  }

  /// <summary>
  /// Remote Desktop clients and other non-browser clients send neither header.
  /// </summary>
  [Test]
  public async Task ShouldBlock_FalseForPostWithoutSecFetchSiteOrOrigin() {
    var request = CreateRequest("POST");

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsFalse();
  }

  [Test]
  [Arguments("https://raweb.example.com")]
  [Arguments("https://RAWEB.example.com:443")]
  public async Task ShouldBlock_FalseForPostWithMatchingOrigin(string origin) {
    var request = CreateRequest("POST", origin: origin);

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsFalse();
  }

  [Test]
  [Arguments("https://evil.example.net")]
  [Arguments("https://raweb.example.com:8443")]
  [Arguments("null")]
  public async Task ShouldBlock_TrueForPostWithOtherOrigin(string origin) {
    var request = CreateRequest("POST", origin: origin);

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsTrue();
  }

  [Test]
  public async Task ShouldBlock_ComparesOriginPortWithRequestPort() {
    var request = CreateRequest("POST", host: "raweb.example.com:8443", origin: "https://raweb.example.com:8443");

    await Assert.That(CrossSiteRequest.ShouldBlock(request)).IsFalse();
  }
}
