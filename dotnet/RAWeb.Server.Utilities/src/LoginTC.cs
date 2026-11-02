using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace RAWeb.Server.Utilities;

public sealed class LoginTCAuth(string clientId, string apiSecret, string apiHostname, string redirectPath) {
  public string ClientId { get; init; } = clientId;
  private string ApiSecret { get; init; } = apiSecret;
  public string ApiHostname { get; init; } = apiHostname;
  public string RedirectPath { get; init; } = redirectPath;
  private static readonly HttpClient s_httpClient = new();
  private Hs512Jwt Jwt => new(ApiSecret);

  private string CreateClientAssertionJwt(string audience) {
    return Jwt.Create(new Dictionary<string, object> {
      ["iss"] = ClientId,
      ["sub"] = ClientId,
      ["aud"] = audience,
      ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds(),
      // backdate the issued-at time so that LoginTC does not reject the assertion
      // as issued in the future when this devices's clock is slightly ahead of LoginTC's servers
      ["iat"] = DateTimeOffset.UtcNow.AddSeconds(-60).ToUnixTimeSeconds(),
      ["jti"] = Guid.NewGuid().ToString()
    });
  }

  /// <summary>
  /// Performs a ping to ensure LoginTC configuration is valid.
  /// <br /><br />
  /// This method sends a request to the LoginTC ping endpoint to verify that
  /// the integration is properly configured and the service is reachable.
  /// </summary>
  /// <exception cref="InvalidOperationException"></exception>
  public void DoPing() {
    var clientAssertion = CreateClientAssertionJwt($"https://{ApiHostname}/oauth/mfa/{ClientId}/ping");

    var payload = new FormUrlEncodedContent([
        new KeyValuePair<string, string>("client_assertion", clientAssertion)
    ]);

    var response = s_httpClient
        .PostAsync($"https://{ApiHostname}/oauth/mfa/{ClientId}/ping", payload)
        .GetAwaiter()
        .GetResult();

    var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    if (!response.IsSuccessStatusCode) {
      var error_code = "unknown_error";
      var error_description = "No description available";
      try {
        var errorOutput = JsonNode.Parse(result)?.AsObject();
        error_code = (string?)errorOutput?["error"] ?? error_code;
        error_description = (string?)errorOutput?["error_description"] ?? error_description;
      }
      catch {
        throw new InvalidOperationException($"LoginTC ping failed: {result}");
      }

      throw new InvalidOperationException($"{error_description} [{error_code}]");
    }
  }

  /// <summary>
  /// Requests authorization from LoginTC for the specified username.
  /// If successful, returns the URL to which the client must be redirected
  /// to complete a LoginTC MFA challenge.
  /// </summary>
  /// <param name="username">The username without the domain. LoginTC prompts this user for MFA.</param>
  /// <param name="returnUrl">The absolute URL to which the client should navigate after successful
  /// MFA authorization. Its origin is used to build the redirect URI for LoginTC.</param>
  /// <returns>The LoginTC authorization URL to redirect the user to, and the state value that LoginTC
  /// will return to the redirect URI. The caller must bind the state value to the browser session and
  /// verify it when LoginTC redirects back.</returns>
  /// <exception cref="ArgumentException"></exception>
  /// <exception cref="InvalidOperationException"></exception>
  public AuthorizationRequest GetRequestAuthorizationEndpoint(string username, string returnUrl) {
    // the state includes a hash of the return URL so that VerifyResponse can confirm
    // that it receives the same return URL
    var state = OAuthState.Create(returnUrl);

    // ensure returnUrl is a valid absolute URL
    Uri returnUri;
    try {
      returnUri = new Uri(returnUrl, UriKind.Absolute);
    }
    catch (UriFormatException ex) {
      throw new ArgumentException("Return URL is not a valid URL.", ex);
    }

    // infer the origin from the return URL path
    var returnOrigin = returnUri.GetLeftPart(UriPartial.Authority);
    var redirectUrl = returnOrigin + RedirectPath;

    var responseType = "code";
    var scope = "openid";
    var requestJwt = Jwt.Create(new Dictionary<string, object> {
      ["response_type"] = responseType,
      ["scope"] = scope,
      ["exp"] = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds(),
      ["client_id"] = ClientId,
      ["redirect_uri"] = redirectUrl,
      ["state"] = state,
      ["login_hint"] = username,
      ["aud"] = $"https://{ApiHostname}/oauth/mfa/{ClientId}/authorize",
      ["iss"] = ClientId
    });

    var reqBody = new FormUrlEncodedContent([
        new KeyValuePair<string, string>("response_type", responseType),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("request", requestJwt),
            new KeyValuePair<string, string>("redirect_uri", redirectUrl),
            new KeyValuePair<string, string>("state", state),
            new KeyValuePair<string, string>("scope", scope)
    ]);

    var url = $"https://{ApiHostname}/oauth/mfa/{ClientId}/authorize?" + reqBody.ReadAsStringAsync().GetAwaiter().GetResult();
    return new AuthorizationRequest(url, state);
  }

  public record AuthorizationRequest(string Url, string State);

  public record VerifyResponseResult(string Username, string? ReturnUrl);

  /// <summary>
  /// Verifies the authorization response from LoginTC. The code and state
  /// parameters are provided by LoginTC as query parameters to the redirect URL.
  /// </summary>
  /// <param name="code">An authorization code that can be exchanged for an
  /// ID token from LoginTC. The ID token includes whether the authorization was
  /// valid.</param>
  /// <param name="state">The state value that LoginTC returned to the redirect URI.</param>
  /// <param name="returnUrl">The return URL that was passed to <see cref="GetRequestAuthorizationEndpoint"/>.
  /// Its origin is used to build the redirect URI, which must match the one used in the authorization request.
  /// The state contains a hash of the original return URL, so a different return URL is rejected.</param>
  /// <returns></returns>
  /// <exception cref="ArgumentException"></exception>
  /// <exception cref="InvalidOperationException"></exception>
  public VerifyResponseResult VerifyResponse(string code, string state, string returnUrl) {
    if (string.IsNullOrEmpty(code)) {
      throw new ArgumentException("Authorization code cannot be null or empty.", nameof(code));
    }
    if (!OAuthState.MatchesReturnUrl(state, returnUrl)) {
      throw new ArgumentException("The return URL does not match the one used in the authorization request.", nameof(returnUrl));
    }

    // ensure returnUrl is a valid absolute URL
    Uri returnUri;
    try {
      returnUri = new Uri(returnUrl, UriKind.Absolute);
    }
    catch (UriFormatException ex) {
      throw new ArgumentException("Return URL is not a valid URL.", ex);
    }

    // infer the origin from the return URL path
    var returnOrigin = returnUri.GetLeftPart(UriPartial.Authority);
    var redirectUrl = returnOrigin + RedirectPath;

    var clientAssertionJwt = CreateClientAssertionJwt($"https://{ApiHostname}/oauth/mfa/{ClientId}/token");

    var tokenRequestBody = new FormUrlEncodedContent([
        new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", redirectUrl),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
            new KeyValuePair<string, string>("client_assertion", clientAssertionJwt)
    ]);

    var tokenResponse = s_httpClient
        .PostAsync($"https://{ApiHostname}/oauth/mfa/{ClientId}/token", tokenRequestBody)
        .GetAwaiter()
        .GetResult();

    var tokenResult = tokenResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();

    if (!tokenResponse.IsSuccessStatusCode) {
      var error_code = "unknown_error";
      var error_description = "No description available";
      try {
        var errorOutput = JsonNode.Parse(tokenResult)?.AsObject();
        error_code = (string?)errorOutput?["error"] ?? error_code;
        error_description = (string?)errorOutput?["error_description"] ?? error_description;
      }
      catch {
        throw new InvalidOperationException($"LoginTC access token request failed: {tokenResult}");
      }

      throw new InvalidOperationException($"{error_description} [{error_code}]");
    }
    if (tokenResult is null) {
      throw new InvalidOperationException("LoginTC access token request returned an empty response");
    }

    var output = JsonNode.Parse(tokenResult)?.AsObject();
    if (output == null) {
      throw new InvalidOperationException("LoginTC access token request response is empty or invalid");
    }

    if ((string?)output["token_type"] != "Bearer") {
      throw new InvalidOperationException("LoginTC access token request did not return a Bearer token");
    }

    var accessToken = (string?)output["access_token"];
    if (accessToken is null) {
      throw new InvalidOperationException("LoginTC access token request did not return an access token");
    }

    var idToken = (string?)output["id_token"];
    if (idToken is null) {
      throw new InvalidOperationException("LoginTC access token request did not return an ID token");
    }

    // parse the ID token as a JWT
    var idTokenData = Jwt.VerifyAndDecode(idToken);
    if (idTokenData is null) {
      throw new InvalidOperationException("LoginTC ID token is invalid");
    }

    var preferredUsername = (string?)idTokenData["preferred_username"];
    if (string.IsNullOrWhiteSpace(preferredUsername)) {
      throw new InvalidOperationException("LoginTC ID token did not contain a preferred_username value");
    }

    var issuer = (string?)idTokenData["iss"];
    if (issuer != $"https://{ApiHostname}/oauth/mfa/{ClientId}") {
      throw new InvalidOperationException("LoginTC ID token contained an invalid issuer");
    }

    var audience = (string?)idTokenData["aud"];
    if (audience != ClientId) {
      throw new InvalidOperationException("LoginTC ID token contained an invalid audience");
    }

    var exp = idTokenData["exp"]?.GetValue<long>() ?? 0;
    if (DateTimeOffset.FromUnixTimeSeconds(exp) < DateTimeOffset.UtcNow) {
      throw new InvalidOperationException("LoginTC ID token has expired");
    }

    var authTime = idTokenData["auth_time"]?.GetValue<long>() ?? 0;
    if (DateTimeOffset.FromUnixTimeSeconds(authTime) < DateTimeOffset.UtcNow.AddMinutes(-5)) {
      throw new InvalidOperationException("LoginTC ID token authentication time is too old");
    }

    return new VerifyResponseResult(
        Username: preferredUsername,
        ReturnUrl: returnUri.ToString()
    );
  }

}
