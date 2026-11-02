using Microsoft.AspNetCore.Http;

namespace RAWeb.Server.Utilities.Tests;

public class SameOriginUrlTests {
  private static HttpRequest CreateRequest(string? origin = null, string scheme = "https", string host = "raweb.contoso.com") {
    var context = new DefaultHttpContext();
    context.Request.Scheme = scheme;
    context.Request.Host = new HostString(host);
    if (origin is not null) {
      context.Request.Headers["Origin"] = origin;
    }
    return context.Request;
  }

  [Test]
  public async Task AcceptsUrlWithSameOriginAsOriginHeader() {
    var request = CreateRequest(origin: "https://raweb.contoso.com");

    await Assert.That(SameOriginUrl.IsSameOrigin(request, "https://raweb.contoso.com/RAWeb/login?mfacompleted=1")).IsTrue();
  }

  [Test]
  public async Task HostComparisonIgnoresCase() {
    var request = CreateRequest(origin: "https://raweb.contoso.com");

    await Assert.That(SameOriginUrl.IsSameOrigin(request, "https://RAWEB.Contoso.com/login")).IsTrue();
  }

  [Test]
  public async Task DefaultPortMatchesOmittedPort() {
    var request = CreateRequest(origin: "https://raweb.contoso.com");

    await Assert.That(SameOriginUrl.IsSameOrigin(request, "https://raweb.contoso.com:443/login")).IsTrue();
  }

  /// <summary>
  /// Behind a reverse proxy that terminates TLS, the server sees http and an internal host name,
  /// but the browser's origin (from the Origin header) is what the return URL must match.
  /// </summary>
  [Test]
  public async Task UsesOriginHeaderInsteadOfServerAddressBehindProxy() {
    var request = CreateRequest(origin: "https://raweb.contoso.com", scheme: "http", host: "internal-server:8080");

    await Assert.That(SameOriginUrl.IsSameOrigin(request, "https://raweb.contoso.com/login")).IsTrue();
  }

  [Test]
  public async Task FallsBackToServerAddressWithoutOriginHeader() {
    var request = CreateRequest(scheme: "https", host: "raweb.contoso.com");

    await Assert.That(SameOriginUrl.IsSameOrigin(request, "https://raweb.contoso.com/login")).IsTrue();
    await Assert.That(SameOriginUrl.IsSameOrigin(request, "https://evil.example/login")).IsFalse();
  }

  [Test]
  [Arguments("https://evil.example/login")] // different host
  [Arguments("https://raweb.contoso.com.evil.example/login")] // host suffix
  [Arguments("http://raweb.contoso.com/login")] // different scheme
  [Arguments("https://raweb.contoso.com:8443/login")] // different port
  [Arguments("https://raweb.contoso.com@evil.example/login")] // user information
  [Arguments("https://user@raweb.contoso.com/login")] // user information on the right host
  [Arguments("/login")] // relative URL
  [Arguments("//evil.example/login")] // protocol-relative URL
  [Arguments("javascript:alert(1)")] // not http(s)
  [Arguments("")]
  [Arguments(null)]
  public async Task RejectsUrlsFromOtherOrigins(string? url) {
    var request = CreateRequest(origin: "https://raweb.contoso.com");

    await Assert.That(SameOriginUrl.IsSameOrigin(request, url)).IsFalse();
  }

  [Test]
  public async Task RejectsEverythingWhenOriginHeaderIsOpaque() {
    var request = CreateRequest(origin: "null");

    await Assert.That(SameOriginUrl.IsSameOrigin(request, "https://raweb.contoso.com/login")).IsFalse();
  }
}
