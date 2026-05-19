using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class WebApi {
  /// <summary>
  /// Registers all of the Web API endpoints on the web application.
  /// </summary>
  internal static void RegisterWebApi(this WebApplication app) {
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
    GetSystemImageEndpoint.Map(app);
    GetResourceEndpoint.Map(app);
    GetImageEndpoint.Map(app);
    GetInjectFileEndpoint.Map(app);
    ListInjectFilesEndpoint.Map(app);
    CompileDetailsEndpoint.Map(app);
    GuacdTunnelEndpoint.Map(app);
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
internal partial class WebApiJsonSerializerContext : JsonSerializerContext { }
