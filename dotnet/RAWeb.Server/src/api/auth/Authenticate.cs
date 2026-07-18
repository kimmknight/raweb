using System.Text.Json.Serialization;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class AuthenticateEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapPost("/api/auth/authenticate", HandleAuthenticate);
    app.MapPost("/api/auth/authenticate/sudo", HandleSudoAuthenticate);
    app.MapGet("/api/auth/duo/callback", HandleDuoCallback);
    app.MapGet("/api/auth/logintc/callback", HandleLoginTCCallback);
    app.MapGet("/api/auth/login", HandleTestLoginForm);
  }

  private static IResult HandleAuthenticate(ValidateCredentialsBody body, HttpContext ctx) {
    if (ShouldAuthenticateAnonymously(body.Username)) {
      var ticket = AuthTicket.FromUserInformation(UserInformation.AnonymousUser, AuthTicketLevel.ReadOnlyUser);
      return CreateAuthCookieResponse(ctx, "anonymous", "RAWEB", ticket);
    }

    var credentials = new ParsedCredentials(body.Username, body.Password);

    try {
      // check if the username and password are valid for the domain
      using (var userToken = SignIn.ValidateCredentials(credentials.Username, credentials.Password, credentials.Domain)) {
        var ticket = AuthTicket.FromLogonToken(userToken.DangerousGetHandle(), maxLevel: AuthTicketLevel.ReadOnlyAdmin);

        // update the credentials to have the correct case for username and domain
        var userInfo = UserInformation.FromDownLevelLogonName(ticket.Name, ticket.UserData.Level);
        credentials.Username = userInfo!.Username;
        credentials.Domain = userInfo.Domain;

        // if MFA is enabled, redirect to the MFA prompt instead of setting the auth cookie directly
        var mfaResult = TriggerMultiFactorAuthenticationPrompt(ctx, credentials, body.ReturnUrl);
        if (mfaResult is not null) {
          return mfaResult;
        }

        return CreateAuthCookieResponse(ctx, credentials.Username, credentials.Domain, ticket);
      }
    }
    catch (ValidateCredentialsException ex) {
      return Results.Ok(
          new AuthResponse {
            Success = false,
            Error = ex.Message,
            Domain = credentials.Domain
          }
      );
    }
  }

  private static IResult HandleSudoAuthenticate(ValidateCredentialsBody body, HttpContext ctx) {
    if (ShouldAuthenticateAnonymously(body.Username)) {
      return Results.Ok(
          new AuthResponse {
            Success = false,
            Error = "login.sudoAnonymousBlocked",
            Domain = "RAWEB"
          }
      );
    }

    var credentials = new ParsedCredentials(body.Username, body.Password);

    try {
      // check if the username and password are valid for the domain
      using (var userToken = SignIn.ValidateCredentials(credentials.Username, credentials.Password, credentials.Domain)) {
        var ticket = AuthTicket.FromLogonToken(userToken.DangerousGetHandle(), maxLevel: AuthTicketLevel.ReadAndWriteAdmin);

        // since this is elevated, use a short-lived ticket/token
        ticket.Expiration = DateTime.UtcNow.AddHours(1);

        // update the credentials to have the correct case for username and domain
        var userInfo = UserInformation.FromDownLevelLogonName(ticket.Name, ticket.UserData.Level);
        credentials.Username = userInfo!.Username;
        credentials.Domain = userInfo.Domain;

        // only allow administrators
        if (!userInfo.IsLocalAdministrator) {
          throw new ValidateCredentialsException("login.sudoRequiresAdmin");
        }
        // confirm that the current user is the same as the user being authenticated for sudo
        var currentUser = UserInformation.FromHttpRequestSafe(ctx.Request);
        if (currentUser is null || currentUser.Domain != credentials.Domain || currentUser.Username != credentials.Username) {
          throw new ValidateCredentialsException("login.sudoUserMismatch");
        }

        return CreateAuthCookieResponse(ctx, credentials.Username, credentials.Domain, ticket, isSudo: true);
      }
    }
    catch (ValidateCredentialsException ex) {
      return Results.Ok(
          new AuthResponse {
            Success = false,
            Error = ex.Message,
            Domain = credentials.Domain
          }
      );
    }
  }

  /// <summary>
  /// A simple login form for testing the authentication endpoint
  /// during development. This is not intended for production use,
  /// but it may be helpful for quickly isolating login issues for
  /// users who are having issues with the web app login flow.
  /// </summary>
  private static IResult HandleTestLoginForm(HttpContext ctx) {
    var authUser = UserInformation.FromHttpRequestSafe(ctx.Request);

    return Results.Content(
      $$"""
      <!DOCTYPE html>
      <html>
        <head>
          <title>Sign In</title>
        </head>
        <body style="font-family: sans-serif; max-width: 400px; margin: auto; display: flex; flex-direction: column; align-items: stretch; justify-content: center; height: 100dvh; gap: 8px;">
          <div id="login" style="padding: 0 16px; border: 1px solid lightgray;">
            <h2>Sign In</h2>
            <p style="color: darkred;">This form is for testing the authentication endpoint. It is not intended for production use.</p>
            <form id="form">
              <label>Username: <input name="username" required /></label><br /><br />
              <label>Password: <input name="password" type="password" required /></label><br /><br />
              <button type="submit">Sign In</button>
              <p id="msg"></p>
            </form>
          </div>
          {{(authUser is not null
              ? $$"""
                <div style="padding: 4px 16px; border: 1px solid lightgray; font-size: 90%;">
                  <p>Currently authenticated as: {{authUser.Domain}}\{{authUser.Username}}</p>
                </div>
                """
              : ""
)}}
          <script>
            document.getElementById('form').onsubmit = async (event) => {
              event.preventDefault();
              const formData = new FormData(event.target);
              const res = await fetch('/api/auth/authenticate', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ 
                  username: formData.get('username'),
                  password: formData.get('password'),
                  returnUrl: window.location.href,
                }),
              });
              const msg = document.getElementById('msg');
              if (res.ok) {
                const data = await res.json();
                msg.textContent = data.success ? 'Credentials validated for: ' + data.username : 'Error: ' + data.error;
                msg.style.color = data.success ? 'green' : 'red';
                if (data.mfa_redirect) {
                  const mfaSpanElem = document.createElement('span');
                  const mfaLinkElem = document.createElement('a');
                  mfaLinkElem.href = data.mfa_redirect;
                  mfaLinkElem.textContent = 'Click here to complete multi-factor authentication';
                  mfaSpanElem.appendChild(document.createElement('br'));
                  mfaSpanElem.appendChild(mfaLinkElem);
                  msg.appendChild(mfaSpanElem);
                }
              } else {
                msg.textContent = 'Authentication failed (' + res.status + ')';
                msg.style.color = 'red';
              }
            };
          </script>
        </body>
      </html>
      """,
      "text/html"
    );
  }

  /// <summary>
  /// Reads the response from Duo MFA and creates an authentication cookie if
  /// the response indicates a successful authentication and contains a valid
  /// state (which should be an absolute URL to redirect to after setting the auth cookie).
  /// </summary>
  /// <param name="state"></param>
  /// <param name="code"></param>
  /// <returns></returns>
  private static IResult HandleDuoCallback(HttpContext ctx, string state, string code) {
    var domain = state.Split('‾')[0]; // the user's domain is prepended to the state with a delimiter

    try {
      var duoPolicy = PoliciesManager.GetDuoMfaPolicyForDomain(domain);
      var redirectPath = ctx.Request.PathBase + duoPolicy!.RedirectPath; // append the path prefix for RAWeb
      var duoAuth = new DuoAuth(duoPolicy.ClientId, duoPolicy.SecretKey, duoPolicy.Hostname, redirectPath);

      var result = duoAuth.VerifyResponse(code, state);
      var userInfo = UserInformation.FromDownLevelLogonName(domain + "\\" + result.Username, AuthTicketLevel.ReadOnlyUser);
      var ticket = AuthTicket.FromUserInformation(userInfo!, userInfo!.IsLocalAdministrator ? AuthTicketLevel.ReadOnlyAdmin : AuthTicketLevel.ReadOnlyUser);

      return CreateAuthCookieResponse(ctx, userInfo!.Username, userInfo.Domain, ticket, result.ReturnUrl);
    }
    catch (Exception ex) {
      return Results.Ok(ex.Message);
    }
  }

  /// <summary>
  /// Reads the response from LoginTC MFA and creates an authentication cookie if
  /// the response indicates a successful authentication and contains a valid
  /// state (which should be an absolute URL to redirect to after setting the auth cookie).
  /// </summary>
  /// <param name="state"></param>
  /// <param name="code"></param>
  /// <returns></returns>
  private static IResult HandleLoginTCCallback(HttpContext ctx, string state, string? code = null, string? error = null, string? error_description = null) {
    var domain = state.Split('‾')[0]; // the user's domain is prepended to the state with a delimiter

    try {
      var loginTcPolicy = PoliciesManager.GetLoginTCMfaPolicyForDomain(domain);
      var redirectPath = ctx.Request.PathBase + loginTcPolicy!.RedirectPath; // append the path prefix for RAWeb
      var loginTcAuth = new LoginTCAuth(loginTcPolicy.ClientId, loginTcPolicy.SecretKey, loginTcPolicy.Hostname, redirectPath);

      if (error == "access_denied" && error_description == "User cancelled authentication") {
        var returnUrlPart = state.Split('‾')[1]; // the return URL is appended to the state with a delimiter

        try {
          // redirect to the logout page
          var returnUri = new UriBuilder(returnUrlPart) {
            Path = ctx.Request.PathBase + "/logoff"
          }.Uri;
          return Results.Redirect(returnUri.ToString());
        }
        catch (UriFormatException) {
          // if the return URL is not a valid absolute URI, use the regular error response instead of redirecting
        }
      }

      if (error is not null) {
        return Results.Ok(error_description);
      }

      var result = loginTcAuth.VerifyResponse(code!, state);
      var userInfo = UserInformation.FromDownLevelLogonName(domain + "\\" + result.Username, AuthTicketLevel.ReadOnlyUser);
      var ticket = AuthTicket.FromUserInformation(userInfo!, userInfo!.IsLocalAdministrator ? AuthTicketLevel.ReadOnlyAdmin : AuthTicketLevel.ReadOnlyUser);

      return CreateAuthCookieResponse(ctx, userInfo!.Username, userInfo.Domain, ticket, result.ReturnUrl);
    }
    catch (Exception ex) {
      return Results.Ok(ex.Message);
    }
  }

  /// <summary>
  /// Triggers the multi-factor authentication prompt if at least one MFA provider is configured.
  /// <br /><br />
  /// If no MFA providers are configured, this method returns null.
  /// </summary>
  /// <param name="credentials"></param>
  /// <param name="returnUrl">The absolute URL that the web app should go to after MFA is completed.</param>
  /// <returns></returns>
  private static IResult? TriggerMultiFactorAuthenticationPrompt(HttpContext ctx, ParsedCredentials credentials, string? returnUrl) {
    // if Duo MFA is enabled, redirect to Duo authorization endpoint
    var duoPolicy = PoliciesManager.GetDuoMfaPolicyForDomain(credentials.Domain, credentials.Username);
    if (duoPolicy is not null) {
      try {
        var redirectPath = ctx.Request.PathBase + duoPolicy.RedirectPath; // append the path prefix for RAWeb
        var duoAuth = new DuoAuth(duoPolicy.ClientId, duoPolicy.SecretKey, duoPolicy.Hostname, redirectPath);
        duoAuth.DoHealthCheck();
        var redirectUrl = duoAuth.GetRequestAuthorizationEndpoint(credentials.Domain, credentials.Username, returnUrl ?? "");
        return Results.Ok(
          new AuthResponse {
            Success = true,
            Username = credentials.Username,
            MfaRedirect = redirectUrl,
            Domain = credentials.Domain
          }
        );
      }
      catch (Exception ex) {
        System.Diagnostics.Debug.WriteLine("Duo health check or authorization request failed: " + ex.Message);
        return Results.Ok(
          new AuthResponse {
            Success = false,
            Error = ex.Message,
            Domain = credentials.Domain
          }
        );
      }
    }

    // if LoginTC MFA is enabled, redirect to LoginTC authorization endpoint
    var loginTcPolicy = PoliciesManager.GetLoginTCMfaPolicyForDomain(credentials.Domain, credentials.Username);
    if (loginTcPolicy is not null) {
      try {
        var redirectPath = ctx.Request.PathBase + loginTcPolicy.RedirectPath; // append the path prefix for RAWeb
        var loginTcAuth = new LoginTCAuth(loginTcPolicy.ClientId, loginTcPolicy.SecretKey, loginTcPolicy.Hostname, redirectPath);
        loginTcAuth.DoPing();
        var redirectUrl = loginTcAuth.GetRequestAuthorizationEndpoint(credentials.Domain, credentials.Username, returnUrl ?? "");
        return Results.Ok(
          new AuthResponse {
            Success = true,
            Username = credentials.Username,
            MfaRedirect = redirectUrl,
            Domain = credentials.Domain
          }
        );
      }
      catch (Exception ex) {
        System.Diagnostics.Debug.WriteLine("LoginTC ping or authorization request failed: " + ex.Message);
        return Results.Ok(
          new AuthResponse {
            Success = false,
            Error = ex.Message,
            Domain = credentials.Domain
          }
        );
      }
    }

    return null;
  }

  private static bool ShouldAuthenticateAnonymously(string? username) {
    var anonSetting = PoliciesManager.RawPolicies["App.Auth.Anonymous"];
    return anonSetting == "always" || (anonSetting == "allow" && username == "RAWEB\\anonymous");
  }

  private class ParsedCredentials {
    public string Domain { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }

    public ParsedCredentials(string? username, string? password) {
      Password = password ?? "";

      // if the username contains a domain, split it to get the username and domain separately
      if (username?.Contains('\\') == true) {
        var parts = username.Split(['\\'], 2);
        Domain = parts[0]; // the part before the backslash is the domain
        Username = parts[1]; // the part after the backslash is the username
      }
      else {
        Domain = SignIn.GetDomainName();
        Username = username ?? "";
      }
    }
  }

  private static IResult CreateAuthCookieResponse(HttpContext ctx, string username, string domain, AuthTicket ticket, string? returnUrl = null, bool isSudo = false) {
    var cookiePath = ctx.Request.PathBase.HasValue ? ctx.Request.PathBase + "/" : "/"; // set the path to the application root
    var cookie = ticket.ToCookie(cookiePath, cookieName: isSudo ? Constants.SudoAuthCookieName : null);

    ctx.Response.Cookies.Append(cookie.Name, cookie.Value, new CookieOptions {
      Path = cookie.Path,
      HttpOnly = cookie.HttpOnly,
      Secure = cookie.Secure,
      Expires = cookie.Expires == DateTime.MinValue ? null : (DateTimeOffset?)cookie.Expires
    });

    if (!string.IsNullOrEmpty(returnUrl)) {
      return Results.Redirect(returnUrl);
    }

    return Results.Ok(
      new AuthResponse {
        Success = true,
        Username = username,
        Domain = domain
      }
    );
  }
}

public class ValidateCredentialsBody {
  public string? Username { get; set; }
  public string? Password { get; set; }
  [JsonPropertyName("returnUrl")] public string? ReturnUrl { get; set; } // nullable
}

public class AuthResponse {
  [JsonPropertyName("success")]
  public bool Success { get; init; }

  [JsonPropertyName("username"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Username { get; init; }

  [JsonPropertyName("domain")]
  public string? Domain { get; init; }

  [JsonPropertyName("error"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Error { get; init; }

  [JsonPropertyName("mfa_redirect"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? MfaRedirect { get; init; }
}
