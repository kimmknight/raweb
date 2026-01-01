using System;
using System.Collections.Generic;
using System.Net.Http;
using JWT.Builder;
using Newtonsoft.Json;

namespace RAWeb.Server.Utilities;

public sealed class DuoAuth(string clientId, string apiSecret, string apiHostname, string redirectPath) {
  public string ClientId { get; init; } = clientId;
  private string ApiSecret { get; init; } = apiSecret;
  public string ApiHostname { get; init; } = apiHostname;
  public string RedirectPath { get; init; } = redirectPath;
  private static readonly HttpClient s_httpClient = new();
  private static readonly AliasResolver s_aliasResolver = new();

  /// <summary>
  /// Performs a health check to ensure Duo configuration is valid.
  /// <br /><br />
  /// This method sends a POST request to Duo's health check endpoint
  /// with the client ID and client assertion to verify that the
  /// integration is properly configured and the service is reachable.
  /// <br /><br />
  /// See <see cref="https://duo.com/docs/oauthapi#:~:text=Endpoints-,Health%20Check,-To%20ensure%20that"/>
  /// </summary>
  /// <exception cref="InvalidOperationException"></exception>
  public void DoHealthCheck() {
    var clientAssertion = JwtBuilder.Create()
        .WithAlgorithm(new JWT.Algorithms.HMACSHA512Algorithm())
        .WithSecret(ApiSecret)
        .AddClaim("iss", ClientId)
        .AddClaim("sub", ClientId)
        .AddClaim("aud", $"https://{ApiHostname}/oauth/v1/health_check")
        .AddClaim("exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds())
        .AddClaim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        .AddClaim("jti", Guid.NewGuid().ToString())
        .Encode();

    var payload = new FormUrlEncodedContent([
          new KeyValuePair<string, string>("client_id", ClientId),
          new KeyValuePair<string, string>("client_assertion", clientAssertion)
        ]);

    var response = s_httpClient
        .PostAsync($"https://{ApiHostname}/oauth/v1/health_check", payload)
        .GetAwaiter()
        .GetResult();

    var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    if (!response.IsSuccessStatusCode) {
      throw new InvalidOperationException($"Duo health check failed: {result}");
    }
    if (result is null) {
      throw new InvalidOperationException("Duo health check returned an empty response");
    }

    // parse the output
    var output = JsonConvert.DeserializeObject<dynamic>(result);
    if (output == null) {
      throw new InvalidOperationException("Duo health check response is empty or invalid");
    }

    // only return if status is OK
    if (output.stat == "OK") {
      return;
    }

    // otherwise, show error details
    else if (output.stat == "FAIL") {
      string error_code = output.code;
      string error_message = output.message;
      string error_message_detail = output.message_detail;
      throw new InvalidOperationException($"Duo health check failed: {error_code} - {error_message}: {error_message_detail}");
    }
    else {
      throw new InvalidOperationException("Duo health check returned an unexpected response");
    }
  }

  /// <summary>
  /// Requests authorization from Duo for the specified username.
  /// If successful, this method returns an authorization code that can be used
  /// to obtain an access token.
  /// <br /><br />
  /// See <see cref="https://duo.com/docs/oauthapi#:~:text=error.%0A%7D-,Authorization%20Request,-This%20endpoint%20processes"/> 
  /// </summary>
  /// <param name="downLevelLoginName">The domain and username in the format DOMAIN\USERNAME.
  /// Duo exposes this to the server after a successful MFA action, which allows us to
  /// create a <see cref="UserInformation"/>  object and then create a authorization token cookie.</param>
  /// <param name="returnUrl">The URL to which the client should navigate after successful
  /// MFA authorization. This is used as the state variable in the request to Duo, which
  /// allows us to use the value when redirecting back to the web app after a successful
  /// response from Duo.</param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public string GetRequestAuthorizationEndpoint(string downLevelLoginName, string returnUrl) {

    if (returnUrl.Length < 16) {
      throw new ArgumentException("Return URL must be at least 16 characters long to be used as state value.");
    }
    if (returnUrl.Length > 1024) {
      throw new ArgumentException("Return URL must be at most 1024 characters long to be used as state value.");
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
    var RedirectUrl = returnOrigin + RedirectPath;

    // build the request JWT
    var responseType = "code";
    var scope = "openid";
    var state = returnUrl;
    var requestJwt = JwtBuilder.Create()
      .WithAlgorithm(new JWT.Algorithms.HMACSHA512Algorithm())
      .WithSecret(ApiSecret)
      .AddClaim("response_type", responseType)
      .AddClaim("scope", scope)
      .AddClaim("exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds())
      .AddClaim("client_id", ClientId)
      .AddClaim("redirect_uri", RedirectUrl)
      .AddClaim("state", state)
      .AddClaim("duo_uname", downLevelLoginName)
      .AddClaim("aud", $"https://{ApiHostname}/oauth/v1/authorize")
      .AddClaim("dest_app_name", $"RAWeb on {s_aliasResolver.Resolve(Environment.MachineName)}")
      .AddClaim("dest_app_id", AppId.ToGuid().ToString())
      .AddClaim("iss", ClientId)
      .Encode();

    var reqBody = new FormUrlEncodedContent(new[]
            {
              new KeyValuePair<string, string>("response_type", responseType),
              new KeyValuePair<string, string>("client_id", ClientId),
              new KeyValuePair<string, string>("request", requestJwt),
              new KeyValuePair<string, string>("redirect_uri", RedirectUrl),
              new KeyValuePair<string, string>("state", state),
              new KeyValuePair<string, string>("scope", scope)
            });

    // provide the URL to which the client must be redirected in order
    // to complete a successful MFA authorization
    var url = $"https://{ApiHostname}/oauth/v1/authorize?" + reqBody.ReadAsStringAsync().GetAwaiter().GetResult();
    return url;
  }

  public record VerifyResponseResult(string DownLevelLoginName, string? ReturnUrl);

  /// <summary>
  /// Verifies the authorization response from Duo. The code and state
  /// parameters are provided by Duo as query parameters to the redirect URL.
  /// </summary>
  /// <param name="code">An authorization code that can be exchanged for an
  /// ID token from Duo. The ID token includes whether the authorization was
  /// valid.</param>
  /// <param name="state">The URL to which the client should redirect if the
  /// code value is valid.</param>
  /// <returns></returns>
  /// <exception cref="ArgumentException"></exception>
  /// <exception cref="InvalidOperationException"></exception>
  public VerifyResponseResult VerifyResponse(string code, string state) {
    // ensure state is a valid absolute URL
    Uri returnUri;
    try {
      returnUri = new Uri(state, UriKind.Absolute);
    }
    catch (UriFormatException ex) {
      throw new ArgumentException("Return URL is not a valid URL.", ex);
    }

    // infer the origin from the return URL path
    var returnOrigin = returnUri.GetLeftPart(UriPartial.Authority);
    var RedirectUrl = returnOrigin + RedirectPath;

    // exchange the authorization code for an ID token

    var clientAssertionJwt = JwtBuilder.Create()
      .WithAlgorithm(new JWT.Algorithms.HMACSHA512Algorithm())
      .WithSecret(ApiSecret)
      .AddClaim("iss", ClientId)
      .AddClaim("sub", ClientId)
      .AddClaim("aud", $"https://{ApiHostname}/oauth/v1/token")
      .AddClaim("exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds())
      .AddClaim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
      .AddClaim("jti", Guid.NewGuid().ToString())
      .Encode();

    var tokenRequestBody = new FormUrlEncodedContent([
      new KeyValuePair<string, string>("grant_type", "authorization_code"),
      new KeyValuePair<string, string>("code", code),
      new KeyValuePair<string, string>("redirect_uri", RedirectUrl),
      new KeyValuePair<string, string>("client_id", ClientId),
      new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
      new KeyValuePair<string, string>("client_assertion", clientAssertionJwt)
    ]);

    // exchange the authorization code for an ID token
    var tokenResponse = s_httpClient
      .PostAsync($"https://{ApiHostname}/oauth/v1/token", tokenRequestBody)
      .GetAwaiter()
      .GetResult();

    var tokenResult = tokenResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();

    // if the response indicates an error, attempt to parse
    // the error details from the response body and throw an exception
    if (!tokenResponse.IsSuccessStatusCode) {

      // parse the error details from the json response
      var error_code = "unknown_error";
      var error_description = "No description available";
      try {
        var errorOutput = JsonConvert.DeserializeObject<dynamic>(tokenResult);
        if (errorOutput != null) {
          error_code = errorOutput.error;
          error_description = errorOutput.error_description;
        }
      }
      catch {
        // if parsing fails, just include the raw string response
        throw new InvalidOperationException($"Duo access token request failed: {tokenResult}");
      }

      throw new InvalidOperationException($"{error_description} [{error_code}]");

    }
    if (tokenResult is null) {
      throw new InvalidOperationException("Duo access token request returned an empty response");
    }


    // parse the response json
    var output = JsonConvert.DeserializeObject<dynamic>(tokenResult);
    if (output == null) {
      throw new InvalidOperationException("Duo access token request response is empty or invalid");
    }

    if (output.token_type != "Bearer") {
      throw new InvalidOperationException("Duo access token request did not return a Bearer token");
    }

    string? accessToken = output.access_token;
    if (accessToken is null) {
      throw new InvalidOperationException("Duo access token request did not return an access token");
    }

    string? idToken = output.id_token;
    if (idToken is null) {
      throw new InvalidOperationException("Duo access token request did not return an ID token");
    }

    // parse the ID token as a JWT
    var idTokenJson = JwtBuilder.Create()
      .WithAlgorithm(new JWT.Algorithms.HMACSHA512Algorithm())
      .WithSecret(ApiSecret)
      .MustVerifySignature()
      .Decode(idToken);
    var idTokenData = JsonConvert.DeserializeObject<Dictionary<string, object>>(idTokenJson);
    if (idTokenData is null) {
      throw new InvalidOperationException("Duo ID token is invalid");
    }

    var downLevelLoginName = idTokenData["preferred_username"] as string; ;
    if (string.IsNullOrEmpty(downLevelLoginName)) {
      throw new InvalidOperationException("Duo ID token did not contain a preferred_username value");
    }

    // validate issuer
    var issuer = idTokenData["iss"] as string;
    if (issuer != $"https://{ApiHostname}/oauth/v1/token") {
      throw new InvalidOperationException("Duo ID token contained an invalid issuer");
    }

    // validate audience
    var audience = idTokenData["aud"] as string;
    if (audience != ClientId) {
      throw new InvalidOperationException("Duo ID token contained an invalid audience");
    }

    // ensure not expired
    var exp = Convert.ToInt64(idTokenData["exp"]);
    var expDateTime = DateTimeOffset.FromUnixTimeSeconds(exp);
    if (expDateTime < DateTimeOffset.UtcNow) {
      throw new InvalidOperationException("Duo ID token has expired");
    }

    // ensure that auth_time is within the last 5 minutes
    var authTime = Convert.ToInt64(idTokenData["auth_time"]);
    var authDateTime = DateTimeOffset.FromUnixTimeSeconds(authTime);
    if (authDateTime < DateTimeOffset.UtcNow.AddMinutes(-5)) {
      throw new InvalidOperationException("Duo ID token authentication time is too old");
    }

    return new VerifyResponseResult(
      DownLevelLoginName: downLevelLoginName,
      ReturnUrl: returnUri.ToString()
    );
  }
}
