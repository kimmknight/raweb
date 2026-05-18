using System.Net.NetworkInformation;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class CompileDetailsEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/app-init-details", Handle);
  }

  private static IResult Handle(HttpContext ctx) {
    var iisBase = ctx.Request.PathBase.HasValue ? ctx.Request.PathBase + "/" : "/";
    var appBase = iisBase + "";

    // get the authenticated user from the cookie
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    var authUser = userInfo is not null
        ? new AppInitAuthUser(
            Username: userInfo.Username,
            Domain: userInfo.Domain,
            FullName: userInfo.FullName ?? userInfo.Username,
            IsLocalAdministrator: userInfo.IsLocalAdministrator
        )
        : new AppInitAuthUser(
            Username: "UNAUTHENTICATED",
            Domain: "RAWEB",
            FullName: "Unauthenticated",
            IsLocalAdministrator: false
        );
    var userNamespace = userInfo is null ? "RAWEB:UNAUTHENTICATED" : (userInfo.Domain + ":" + userInfo.Username);

    // expose the terminal server aliases
    var terminalServerAliases = GetTerminalServerAliases();

    // app-related policies
    var combineTerminalServersModeEnabled = string.IsNullOrEmpty(PoliciesManager.RawPolicies["CombineTerminalServersModeEnabled"]) ? (bool?)null : PoliciesManager.RawPolicies["CombineTerminalServersModeEnabled"] == "true";
    var favoritesEnabled = string.IsNullOrEmpty(PoliciesManager.RawPolicies["App.FavoritesEnabled"]) ? (bool?)null : PoliciesManager.RawPolicies["App.FavoritesEnabled"] == "true";
    var openConnectionsInNewWindowEnabled = string.IsNullOrEmpty(PoliciesManager.RawPolicies["App.OpenConnectionsInNewWindowEnabled"]) ? (bool?)null : PoliciesManager.RawPolicies["App.OpenConnectionsInNewWindowEnabled"] == "true";
    var flatModeEnabled = string.IsNullOrEmpty(PoliciesManager.RawPolicies["App.FlatModeEnabled"]) ? (bool?)null : PoliciesManager.RawPolicies["App.FlatModeEnabled"] == "true";
    var hidePortsEnabled = string.IsNullOrEmpty(PoliciesManager.RawPolicies["App.HidePortsEnabled"]) ? (bool?)null : PoliciesManager.RawPolicies["App.HidePortsEnabled"] == "true";
    var iconBackgroundsEnabled = string.IsNullOrEmpty(PoliciesManager.RawPolicies["App.IconBackgroundsEnabled"]) ? (bool?)null : PoliciesManager.RawPolicies["App.IconBackgroundsEnabled"] == "true";
    var simpleModeEnabled = string.IsNullOrEmpty(PoliciesManager.RawPolicies["App.SimpleModeEnabled"]) ? (bool?)null : PoliciesManager.RawPolicies["App.SimpleModeEnabled"] == "true";
    var passwordChangeEnabled = string.IsNullOrEmpty(PoliciesManager.RawPolicies["PasswordChange.Enabled"]) ? (bool?)null : PoliciesManager.RawPolicies["PasswordChange.Enabled"] == "true";
    var anonymousAuthentication = PoliciesManager.RawPolicies["App.Auth.Anonymous"] == "always" ? "always" : (PoliciesManager.RawPolicies["App.Auth.Anonymous"] == "allow" ? "allow" : "never");
    var signedInUserGlobalAlerts = PoliciesManager.RawPolicies["App.Alerts.SignedInUser"];
    var workspaceAuthBlocked = PoliciesManager.RawPolicies["WorkspaceAuth.Block"] == "true";
    var rdpFileConnMethod = PoliciesManager.RawPolicies["App.ConnectionMethod.RdpFileDownload.Enabled"] != "false";
    var rdpProtocolUriConnMethod = PoliciesManager.RawPolicies["App.ConnectionMethod.RdpProtocol.Enabled"] != "false";
    var policies = new AppInitPolicies(
        combineTerminalServersModeEnabled,
        favoritesEnabled,
        openConnectionsInNewWindowEnabled,
        flatModeEnabled,
        hidePortsEnabled,
        iconBackgroundsEnabled,
        simpleModeEnabled,
        passwordChangeEnabled,
        anonymousAuthentication,
        signedInUserGlobalAlerts,
        workspaceAuthBlocked,
        new AppInitConnectionMethods(rdpFileConnMethod, rdpProtocolUriConnMethod)
    );

    // host information
    var resolver = new AliasResolver();
    var machineName = resolver.Resolve(Environment.MachineName);
    var envMachineName = Environment.MachineName;
    var envFQDN = envMachineName + "." + GetDnsDomainName();

    // version information
    var coreVersion = LocalVersions.GetServerVersionString();
    var webVersion = LocalVersions.GetWebClientVersionString();

    // capabilities reporting
    var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
    var supportsFqdnRedirect = true;
    var supportsGuacdWebClient = SupportsGuacd;
    var supportsWsl2 = Guacd.IsWindowsSubsystemForLinuxSupported;
    var supportsTerminalServerConnections = false;
    try {
      supportsTerminalServerConnections = ManagementServiceClient.Proxy.AreConnectionsAllowed();
    }
    catch { }
    var capabilities = new AppInitCapabilities(
        supportsCentralizedPublishing,
        supportsFqdnRedirect,
        supportsGuacdWebClient,
        supportsWsl2,
        supportsTerminalServerConnections
    );

    return Results.Ok(new AppInitDetailsResponse(
        iisBase,
        appBase,
        authUser,
        userNamespace,
        terminalServerAliases,
        policies,
        machineName,
        envMachineName,
        envFQDN,
        coreVersion,
        webVersion,
        capabilities
    ));
  }

  private static Dictionary<string, string> GetTerminalServerAliases() {
    // "host1=alias1;host2=alias2"
    var raw = PoliciesManager.RawPolicies["TerminalServerAliases"] ?? "";

    var dict = raw
        .Split(';')
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Select(s => s.Split('='))
        .ToDictionary(
            parts => parts[0].Trim(),
            parts => parts.Length > 1 ? parts[1].Trim() : ""
        );

    return dict;
  }

  private static bool SupportsGuacd {
    get {
      var guacdEnabled = PoliciesManager.RawPolicies["GuacdWebClient.Enabled"] != "false";
      var guacdAddress = PoliciesManager.RawPolicies["GuacdWebClient.Address"];
      var guacdMethod = PoliciesManager.RawPolicies["GuacdWebClient.Method"] == "external" ? "external" : "container";

      if (!guacdEnabled) {
        return false;
      }

      // if the method is container, WSL must be installed and supported
      if (guacdMethod == "container") {
        return Guacd.IsWindowsSubsystemForLinuxInstalled && Guacd.IsWindowsSubsystemForLinuxSupported;
      }

      // if the method is external, it must be a valid address
      if (string.IsNullOrWhiteSpace(guacdAddress)) {
        return false;
      }
      var addressParts = guacdAddress.Split(':');
      if (addressParts.Length != 2) {
        return false;
      }
      var portStr = addressParts[1];
      if (!int.TryParse(portStr, out _)) {
        return false;
      }
      return true;
    }
  }

  /// <summary>
  /// Gets the DNS domain name of the current machine's domain.
  /// <br /><br />
  /// This replaces the COM-based <c>Domain.GetCurrentDomain().Name</c> call,
  /// which is incompatible with Native AOT. The DNS domain name from
  /// <see cref="IPGlobalProperties"/> is equivalent for domain-joined machines.
  /// </summary>
  /// <returns>The DNS domain name, or "local" if the machine is not domain-joined or the domain cannot be reached.</returns>
  private static string GetDnsDomainName() {
    static bool IsDomainJoined() {
      return IPGlobalProperties.GetIPGlobalProperties().DomainName.Length > 0;
    }
    if (!IsDomainJoined()) {
      return "local";
    }

    try {
      return IPGlobalProperties.GetIPGlobalProperties().DomainName;
    }
    catch {
      return "local";
    }
  }
}

public record AppInitAuthUser(string Username, string Domain, string? FullName, bool IsLocalAdministrator);
public record AppInitConnectionMethods(bool RdpFile, bool RdpProtocolUri);
public record AppInitPolicies(
    bool? CombineTerminalServersModeEnabled,
    bool? FavoritesEnabled,
    bool? OpenConnectionsInNewWindowEnabled,
    bool? FlatModeEnabled,
    bool? HidePortsEnabled,
    bool? IconBackgroundsEnabled,
    bool? SimpleModeEnabled,
    bool? PasswordChangeEnabled,
    string AnonymousAuthentication,
    string? SignedInUserGlobalAlerts,
    bool WorkspaceAuthBlocked,
    AppInitConnectionMethods ConnectionMethods
);
public record AppInitCapabilities(
    bool SupportsCentralizedPublishing,
    bool SupportsFqdnRedirect,
    bool SupportsGuacdWebClient,
    bool SupportsWsl2,
    bool SupportsTerminalServerConnections
);
public record AppInitDetailsResponse(
    string IisBase,
    string AppBase,
    AppInitAuthUser AuthUser,
    string UserNamespace,
    Dictionary<string, string> TerminalServerAliases,
    AppInitPolicies Policies,
    string MachineName,
    string EnvMachineName,
    string EnvFQDN,
    string? CoreVersion,
    string? WebVersion,
    AppInitCapabilities Capabilities
);
