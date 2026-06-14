using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

public static class WebApi {
  private static bool s_shouldRegisterAuthWithApp = false;

  /// <summary>
  /// Registers all of the Web API endpoints on the web application.
  /// </summary>
  public static void RegisterWebApi(this WebApplication app) {
    if (s_shouldRegisterAuthWithApp) {
      app.UseAuthentication();
      app.UseAuthorization();
    }
    else {
      Console.WriteLine("Warning: Web API registered without authentication. Call builder.AddWindowsAuthorizationPolicy() before building the app to enable authentication for the Web API.");
    }
    InitializeAuthTicketProtection(app);
    MapEndpoints(app);
  }

  private static void MapEndpoints(WebApplication app) {
    GetDomainNameEndpoint.Map(app);
    AuthenticateEndpoint.Map(app);
    AuthenticateWorkspaceEndpoint.Map(app);
    ChangePasswordEndpoint.Map(app);
    ClearEndpoint.Map(app);
    MeEndpoint.Map(app);
    GetAppSettingEndpoint.Map(app);
    GetAppSettingsEndpoint.Map(app);
    SetAppSettingEndpoint.Map(app);
    GetWorkspaceEndpoint.Map(app);
    GetReconnectEndpoint.Map(app);
    FindSecurityIdentifierEndpoint.Map(app);
    ListLocationsEndpoint.Map(app);
    ResolveSecurityIdentifiersEndpoint.Map(app);
    GetRegisteredAppsEndpoint.Map(app);
    GetRegisteredAppEndpoint.Map(app);
    RegisterAppEndpoint.Map(app);
    ModifyAppEndpoint.Map(app);
    UnregisterAppEndpoint.Map(app);
    GetPossibleAppsEndpoint.Map(app);
    CountImageIndicesEndpoint.Map(app);
    ExportResourcesEndpoint.Map(app);
    GetSystemImageEndpoint.Map(app);
    GetResourceEndpoint.Map(app);
    GetImageEndpoint.Map(app);
    GetInjectFileEndpoint.Map(app);
    ListInjectFilesEndpoint.Map(app);
    CompileDetailsEndpoint.Map(app);
    GuacdTunnelEndpoint.Map(app);

    // add a simple endpoint to return the current version of the RAWeb Server
    app.MapGet("/api", () => {
      var currentVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
      return Results.Text($"RAWeb Server\nv{currentVersion}");
    });

    // in development mode, add an endpoint to list all embedded resources in the assembly
    if (app.Environment.IsDevelopment()) {
      app.MapGet("/api/_internal/resources", () => {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        var output = string.Join("\n", resourceNames.OrderBy(x => x));
        return Results.Text(output);
      });
    }
  }

  /// <summary>
  /// Initializes the protection and unprotection methods for AuthTicket using the
  /// apps' data protection provider. This allows AuthTickets to be securely encrypted and
  /// decrypted.
  /// </summary>
  /// <details>
  /// This method requires <c>builder.Services.AddDataProtection()</c> to be called in
  /// to register the data protection services.
  /// </details>
  /// <param name="app"></param>
  private static void InitializeAuthTicketProtection(WebApplication app) {
    var dataProtector = app.Services.GetRequiredService<IDataProtectionProvider>()
        .CreateProtector("RAWeb.AuthTicket");
    AuthTicket.InitializeProtection(
        protect: plaintext => {
          var cipherBytes = dataProtector.Protect(Encoding.UTF8.GetBytes(plaintext));
          return Convert.ToBase64String(cipherBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        },
        unprotect: token => {
          var base64 = token.Replace('-', '+').Replace('_', '/');
          base64 += (base64.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
          var plainBytes = dataProtector.Unprotect(Convert.FromBase64String(base64));
          return Encoding.UTF8.GetString(plainBytes);
        }
    );
  }

  /// <summary>
  /// Adds a Windows Authentication policy that can be used to protect endpoints that require
  /// Windows Authentication.
  /// <br /><br />
  /// This policy can be applied to endpoints using <c>.RequireAuthorization("WindowsAuth")</c>.
  /// </summary>
  /// <param name="builder"></param>
  public static void AddWindowsAuthorizationPolicy(this WebApplicationBuilder builder) {
    builder.Services.AddAuthorizationBuilder()
      .AddPolicy("WindowsAuth", policy => {
        // if anonymous authentication is in always mode,
        // then we should skip requiring Windows Authentication
        var anonSetting = PoliciesManager.RawPolicies["App.Auth.Anonymous"];
        if (anonSetting != "always") {
          policy.AddAuthenticationSchemes(NegotiateDefaults.AuthenticationScheme);
        }
        policy.RequireAuthenticatedUser();
      });
    s_shouldRegisterAuthWithApp = true;
  }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(DomainResponse))]
[JsonSerializable(typeof(InstalledApps))]
[JsonSerializable(typeof(UserInformation))]
[JsonSerializable(typeof(ResolvedSecurityIdentifier))]
[JsonSerializable(typeof(ResolveSidsResponse))]
[JsonSerializable(typeof(ClearResponse))]
[JsonSerializable(typeof(ValidateCredentialsBody))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(WorkspaceBlockedResponse))]
[JsonSerializable(typeof(ChangePasswordBody))]
[JsonSerializable(typeof(ChangePasswordSuccessResponse))]
[JsonSerializable(typeof(ChangePasswordFailResponse))]
[JsonSerializable(typeof(AppSettingResponse))]
[JsonSerializable(typeof(AppInitDetailsResponse))]
[JsonSerializable(typeof(AppInitAuthUser))]
[JsonSerializable(typeof(AppInitPolicies))]
[JsonSerializable(typeof(AppInitCapabilities))]
[JsonSerializable(typeof(AppInitConnectionMethods))]
[JsonSerializable(typeof(InjectFileItem))]
[JsonSerializable(typeof(InjectFileItem[]))]

// management resource types used by the resource-management API endpoints
// (duplicated here because of casing differences compared to ManagementJsonContext)
// TODO: make casing consistent (camelCase) for all endpoints and then switch to using ManagementJsonContext
[JsonSerializable(typeof(ManagedResource))]
[JsonSerializable(typeof(ManagedResources))]
[JsonSerializable(typeof(ManagedFileResource))]
[JsonSerializable(typeof(SystemRemoteApps.SystemRemoteApp))]
[JsonSerializable(typeof(SystemDesktop))]
[JsonSerializable(typeof(SecurityDescriptionDTO))]
[JsonSerializable(typeof(RemoteAppProperties))]
[JsonSerializable(typeof(PartialManagedResource))]
[JsonSerializable(typeof(PartialRemoteAppProperties))]
// end of management resource types
public partial class WebApiJsonSerializerContext : JsonSerializerContext { }
