using System;
using System.Collections.Generic;
using System.Net.Http;
using JWT.Builder;
using Newtonsoft.Json;

namespace RAWeb.Server.Utilities;

public sealed class LoginTCAuth(string clientId, string apiSecret, string apiHostname, string redirectPath) {
    public string ClientId { get; init; } = clientId;
    private string ApiSecret { get; init; } = apiSecret;
    public string ApiHostname { get; init; } = apiHostname;
    public string RedirectPath { get; init; } = redirectPath;
    private static readonly HttpClient s_httpClient = new();
    private static readonly AliasResolver s_aliasResolver = new();

    /// <summary>
    /// Performs a ping to ensure LoginTC configuration is valid.
    /// <br /><br />
    /// This method sends a request to the LoginTC ping endpoint to verify that
    /// the integration is properly configured and the service is reachable.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void DoPing() {
        var clientAssertion = JwtBuilder.Create()
            .WithAlgorithm(new JWT.Algorithms.HMACSHA512Algorithm())
            .WithSecret(ApiSecret)
            .AddClaim("iss", ClientId)
            .AddClaim("sub", ClientId)
            .AddClaim("aud", $"https://{ApiHostname}/oauth/mfa/{ClientId}/ping")
            .AddClaim("exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds())
            .AddClaim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            .AddClaim("jti", Guid.NewGuid().ToString())
            .Encode();

        var payload = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_assertion", clientAssertion)
        ]);

        var response = s_httpClient
            .PostAsync($"https://{ApiHostname}/oauth/mfa/{ClientId}/ping", payload)
            .GetAwaiter()
            .GetResult();

        var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        Console.WriteLine($"LoginTC ping response: {result}");
        if (!response.IsSuccessStatusCode) {
            var error_code = "unknown_error";
            var error_description = "No description available";
            try {
                var errorOutput = JsonConvert.DeserializeObject<dynamic>(result);
                if (errorOutput != null) {
                    error_code = errorOutput.error;
                    error_description = errorOutput.error_description;
                }
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
    /// <param name="domain">The user's domain.</param>
    /// <param name="username">The username without the domain.
    /// LoginTC exposes this after a successful MFA action, which allows us to
    /// create a <see cref="UserInformation"/> object and then create an authorization token cookie.</param>
    /// <param name="returnUrl">The URL to which the client should navigate after successful
    /// MFA authorization. This is encoded in the OIDC state parameter so we can redirect
    /// back to the web app after a successful response from LoginTC.</param>
    /// <returns>The LoginTC authorization URL to redirect the user to.</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public string GetRequestAuthorizationEndpoint(string domain, string username, string returnUrl) {

        if (returnUrl.Length < 16) {
            throw new ArgumentException("Return URL must be at least 16 characters long to be used as state value.", nameof(returnUrl));
        }
        if (returnUrl.Length > 1024) {
            throw new ArgumentException("Return URL must be at most 1024 characters long to be used as state value.", nameof(returnUrl));
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

        // build the state parameter by prepending the domain to the returnUrl
        var state = $"{domain}‾{returnUrl}";
        if (state.Length > 1024) {
            throw new ArgumentException($"The combined state value exceeds the maximum length of 1024 characters. The return URL must not be longer than {1024 - domain.Length - 1} characters.", nameof(returnUrl));
        }

        var responseType = "code";
        var scope = "openid";
        var requestJwt = JwtBuilder.Create()
            .WithAlgorithm(new JWT.Algorithms.HMACSHA512Algorithm())
            .WithSecret(ApiSecret)
            .AddClaim("response_type", responseType)
            .AddClaim("scope", scope)
            .AddClaim("exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds())
            .AddClaim("client_id", ClientId)
            .AddClaim("redirect_uri", redirectUrl)
            .AddClaim("state", state)
            .AddClaim("login_hint", username)
            .AddClaim("aud", $"https://{ApiHostname}/oauth/mfa/{ClientId}/authorize")
            .AddClaim("iss", ClientId)
            .Encode();

        var reqBody = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("response_type", responseType),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("request", requestJwt),
            new KeyValuePair<string, string>("redirect_uri", redirectUrl),
            new KeyValuePair<string, string>("state", state),
            new KeyValuePair<string, string>("scope", scope)
        ]);

        return $"https://{ApiHostname}/oauth/mfa/{ClientId}/authorize?" + reqBody.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    public record VerifyResponseResult(string Username, string? ReturnUrl);

    /// <summary>
    /// Verifies the authorization response from LoginTC. The code and state
    /// parameters are provided by LoginTC as query parameters to the redirect URL.
    /// </summary>
    /// <param name="code">An authorization code that can be exchanged for an
    /// ID token from LoginTC. The ID token includes whether the authorization was
    /// valid.</param>
    /// <param name="state">The user's domain and URL to which the client should redirect if the
    /// code value is valid, separated by an overscore character (‾).</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public VerifyResponseResult VerifyResponse(string code, string state) {
        if (string.IsNullOrEmpty(code)) {
            throw new ArgumentException("Authorization code cannot be null or empty.", nameof(code));
        }
        if (string.IsNullOrEmpty(state)) {
            throw new ArgumentException("State cannot be null or empty.", nameof(state));
        }

        // extract the return url from the state (domain‾returnUrl)
        var domainSeparatorIndex = state.IndexOf('‾');
        if (domainSeparatorIndex <= 0) {
            throw new ArgumentException("State value is invalid.", nameof(state));
        }
        var returnUrlPart = state.Substring(domainSeparatorIndex + 1);

        // ensure state contains a valid absolute URL
        Uri returnUri;
        try {
            returnUri = new Uri(returnUrlPart, UriKind.Absolute);
        }
        catch (UriFormatException ex) {
            throw new ArgumentException("Return URL is not a valid URL.", ex);
        }

        // infer the origin from the return URL path
        var returnOrigin = returnUri.GetLeftPart(UriPartial.Authority);
        var redirectUrl = returnOrigin + RedirectPath;

        var clientAssertionJwt = JwtBuilder.Create()
            .WithAlgorithm(new JWT.Algorithms.HMACSHA512Algorithm())
            .WithSecret(ApiSecret)
            .AddClaim("iss", ClientId)
            .AddClaim("sub", ClientId)
            .AddClaim("aud", $"https://{ApiHostname}/oauth/mfa/{ClientId}/token")
            .AddClaim("exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds())
            .AddClaim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            .AddClaim("jti", Guid.NewGuid().ToString())
            .Encode();

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
                var errorOutput = JsonConvert.DeserializeObject<dynamic>(tokenResult);
                if (errorOutput != null) {
                    error_code = errorOutput.error;
                    error_description = errorOutput.error_description;
                }
            }
            catch {
                throw new InvalidOperationException($"LoginTC access token request failed: {tokenResult}");
            }

            throw new InvalidOperationException($"{error_description} [{error_code}]");
        }
        if (tokenResult is null) {
            throw new InvalidOperationException("LoginTC access token request returned an empty response");
        }

        var output = JsonConvert.DeserializeObject<dynamic>(tokenResult);
        if (output == null) {
            throw new InvalidOperationException("LoginTC access token request response is empty or invalid");
        }

        if (output.token_type != "Bearer") {
            throw new InvalidOperationException("LoginTC access token request did not return a Bearer token");
        }

        string? accessToken = output.access_token;
        if (accessToken is null) {
            throw new InvalidOperationException("LoginTC access token request did not return an access token");
        }

        string? idToken = output.id_token;
        if (idToken is null) {
            throw new InvalidOperationException("LoginTC access token request did not return an ID token");
        }

        var idTokenJson = JwtBuilder.Create()
            .WithAlgorithm(new JWT.Algorithms.HMACSHA512Algorithm())
            .WithSecret(ApiSecret)
            .MustVerifySignature()
            .Decode(idToken);
        var idTokenData = JsonConvert.DeserializeObject<Dictionary<string, object>>(idTokenJson);
        if (idTokenData is null) {
            throw new InvalidOperationException("LoginTC ID token is invalid");
        }

        var preferredUsername = idTokenData["preferred_username"] as string;
        if (string.IsNullOrWhiteSpace(preferredUsername) || preferredUsername is null) {
            throw new InvalidOperationException("LoginTC ID token did not contain a preferred_username value");
        }

        var issuer = idTokenData["iss"] as string;
        if (issuer != $"https://{ApiHostname}/oauth/mfa/{ClientId}") {
            throw new InvalidOperationException("LoginTC ID token contained an invalid issuer");
        }

        var audience = idTokenData["aud"] as string;
        if (audience != ClientId) {
            throw new InvalidOperationException("LoginTC ID token contained an invalid audience");
        }

        var exp = Convert.ToInt64(idTokenData["exp"]);
        if (DateTimeOffset.FromUnixTimeSeconds(exp) < DateTimeOffset.UtcNow) {
            throw new InvalidOperationException("LoginTC ID token has expired");
        }

        var authTime = Convert.ToInt64(idTokenData["auth_time"]);
        if (DateTimeOffset.FromUnixTimeSeconds(authTime) < DateTimeOffset.UtcNow.AddMinutes(-5)) {
            throw new InvalidOperationException("LoginTC ID token authentication time is too old");
        }

        return new VerifyResponseResult(
            Username: preferredUsername,
            ReturnUrl: returnUri.ToString()
        );
    }
}
