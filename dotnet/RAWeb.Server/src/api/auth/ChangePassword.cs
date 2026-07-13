using System.Text.Json.Serialization;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class ChangePasswordEndpoint {
    internal static void Map(IEndpointRouteBuilder app) {
        app.MapPost("/api/auth/change-password", Handle);
    }

    private static IResult Handle(ChangePasswordBody body) {
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

        // attempt to change the credentials for the user
        var (success, errorMessage) = NetUserInformation.ChangeCredentials(username, body.OldPassword ?? "", body.NewPassword ?? "", domain);

        if (success) {
            return Results.Ok(new ChangePasswordSuccessResponse(true, username, domain));
        }
        else {
            return Results.BadRequest(
                new ChangePasswordFailResponse(false, errorMessage, domain)
            );
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
