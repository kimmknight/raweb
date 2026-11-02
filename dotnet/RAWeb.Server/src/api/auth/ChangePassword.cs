using System.Text.Json.Serialization;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class ChangePasswordEndpoint {
    internal static void Map(IEndpointRouteBuilder app) {
        app.MapPost("/api/auth/change-password", Handle);
    }

    private static IResult Handle(ChangePasswordBody body, HttpContext ctx) {
        if (PoliciesManager.RawPolicies["PasswordChange.Enabled"] == "false") {
            return Results.Json(
                new ChangePasswordFailResponse(false, "Password change is disabled."),
                WebApiJsonSerializerContext.Default.ChangePasswordFailResponse,
                statusCode: 401
            );
        }

        // if the username contains a domain, split it to get the username and domain separately
        string domain;
        var username = body.Username ?? "";
        if (username.Contains('\\')) {
            var parts = username.Split(['\\'], 2);
            domain = parts[0]; // the part before the backslash is the domain
            username = parts[1]; // the part after the backslash is the username
        }
        else {
            domain = SignIn.GetDomainName();
        }

        if (string.IsNullOrEmpty(username)) {
            return Results.BadRequest(
                new ChangePasswordFailResponse(false, "Username must be provided.", domain)
            );
        }

        // read the browser's session before the password changes because the
        // session is rejected afterward (it was signed in with the old password)
        var sessionUser = UserInformation.FromHttpRequestSafe(ctx.Request);

        // attempt to change the credentials for the user
        var (success, errorMessage) = NetUserInformation.ChangeCredentials(username, body.OldPassword ?? "", body.NewPassword ?? "", domain);

        if (success) {
            KeepSessionForNewPassword(ctx, sessionUser, username, body.NewPassword ?? "", domain);
            return Results.Ok(new ChangePasswordSuccessResponse(true, username, domain));
        }
        else {
            return Results.BadRequest(
                new ChangePasswordFailResponse(false, errorMessage, domain)
            );
        }
    }

    /// <summary>
    /// Updates the user cache with the account's new password-last-set time and keeps this browser
    /// signed in despite the password change.
    /// <br /><br />
    /// Changing the password ends every session that was signed in with the old password
    /// (see <see cref="AuthTicket.WasIssuedForOlderPassword"/>). Signing in with the new password
    /// updates the user cache with the new password-last-set value, so the other sessions end on
    /// their next request instead of when the cached value expires.
    /// <br /><br />
    /// If this browser was already signed in as the same account, it gets a new authentication
    /// cookie for the new password. A browser that was not signed in does not get a cookie, so
    /// changing a password cannot be used to sign in without MFA.
    /// </summary>
    /// <param name="sessionUser">The browser's signed-in user from before the password changed</param>
    private static void KeepSessionForNewPassword(HttpContext ctx, UserInformation? sessionUser, string username, string newPassword, string domain) {
        try {
            using var userToken = SignIn.ValidateCredentials(username, newPassword, domain);
            using var identity = new System.Security.Principal.WindowsIdentity(userToken.DangerousGetHandle());

            // creating the ticket looks up the new password-last-set value and stores it in the user cache
            var maxLevel = AuthTicket.IsSudoPolicyEnabled ? AuthTicketLevel.ReadOnlyAdmin : AuthTicketLevel.ReadAndWriteAdmin;
            var ticket = AuthTicket.FromWindowsIdentity(identity, maxLevel);

            // compare SIDs because the domain may be written differently (e.g., contoso.com instead of CONTOSO)
            var isSameAccount = sessionUser is not null && !sessionUser.IsAnonymousUser && identity.User?.Value == sessionUser.Sid;
            if (isSameAccount) {
                AuthenticateEndpoint.StartAuthSession(ctx, ticket);
            }
        }
        catch (Exception ex) {
            // the password was changed, so do not report an error; the browser will need to sign in again
            System.Diagnostics.Debug.WriteLine("Could not sign in with the new password: " + ex.Message);
        }
    }
}

public class ChangePasswordBody {
    public string? Username { get; set; }
    public string? OldPassword { get; set; }
    public string? NewPassword { get; set; }
}

public class ChangePasswordSuccessResponse(bool success, string username, string domain) {
    [JsonPropertyName("success")] public bool Success { get; } = success;
    [JsonPropertyName("username")] public string Username { get; } = username;
    [JsonPropertyName("domain")] public string Domain { get; } = domain;
}

public class ChangePasswordFailResponse(bool success, string? error, string? domain = null) {
    [JsonPropertyName("success")] public bool Success { get; } = success;
    [JsonPropertyName("error")] public string? Error { get; } = error;
    [JsonPropertyName("domain"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Domain { get; } = domain;
}
