using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Web;
using System.Web.Http;
using RAWeb.Server.Utilities;

namespace RAWebServer.Api {
  public partial class AppInitDetailsController : ApiController {
    [HttpGet]
    [Route("")]
    public IHttpActionResult CompileDetails() {
      var iisBase = VirtualPathUtility.ToAbsolute("~/");
      var appBase = iisBase + "";

      // get the authenticated user from the cookie
      var userInfo = UserInformation.FromHttpRequest(HttpContext.Current.Request);
      var authUser = userInfo != null ? new {
        username = userInfo.Username,
        domain = userInfo.Domain,
        fullName = userInfo.FullName,
        isLocalAdministrator = userInfo.IsLocalAdministrator,
      } :
      new {
        username = "UNAUTHENTICATED",
        domain = "RAWEB",
        fullName = "Unauthenticated",
        isLocalAdministrator = false,
      };
      var userNamespace = userInfo == null ? "RAWEB:UNAUTHENTICATED" : (userInfo.Domain + ":" + userInfo.Username);

      // expose the terminal server aliases
      var terminalServerAliases = GetTerminalServerAliases();

      // app-related policies
      var combineTerminalServersModeEnabled = string.IsNullOrEmpty(PoliciesManager.RawPolicies["CombineTerminalServersModeEnabled"]) ? (bool?)null : PoliciesManager.RawPolicies["CombineTerminalServersModeEnabled"] == "true";
      var favoritesEnabled = string.IsNullOrEmpty(PoliciesManager.RawPolicies["App.FavoritesEnabled"]) ? (bool?)null : PoliciesManager.RawPolicies["App.FavoritesEnabled"] == "true";
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
      var policies = new {
        combineTerminalServersModeEnabled,
        favoritesEnabled,
        flatModeEnabled,
        hidePortsEnabled,
        iconBackgroundsEnabled,
        simpleModeEnabled,
        passwordChangeEnabled,
        anonymousAuthentication,
        signedInUserGlobalAlerts,
        workspaceAuthBlocked,
        connectionMethods = new {
          rdpFile = rdpFileConnMethod,
          rdpProtocolUri = rdpProtocolUriConnMethod,
        }
      };

      // host information
      var resolver = new AliasResolver();
      var machineName = resolver.Resolve(System.Environment.MachineName);
      var envMachineName = System.Environment.MachineName;
      var envFQDN = envMachineName + "." + GetDnsDomainName();

      // version information
      var coreVersion = LocalVersions.GetServerVersionString();
      var webVersion = LocalVersions.GetWebClientVersionString();

      // capabilities reporting
      var supportsCentralizedPublishing = PoliciesManager.RawPolicies["RegistryApps.Enabled"] != "true";
      var supportsFqdnRedirect = true;
      var supportsGuacdWebClient = PoliciesManager.RawPolicies["GuacdWebClient.Enabled"] == "true" && (PoliciesManager.RawPolicies["GuacdWebClient.Address"].Contains(":") || Guacd.IsGuacdDistributionInstalled);
      var capabilities = new {
        supportsCentralizedPublishing,
        supportsFqdnRedirect,
        supportsGuacdWebClient
      };

      return Ok(new {
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
      });
    }

    private Dictionary<string, string> GetTerminalServerAliases() {
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



    private string GetDnsDomainName() {
      static bool IsDomainJoined() {
        return IPGlobalProperties.GetIPGlobalProperties().DomainName.Length > 0;
      }
      if (!IsDomainJoined()) {
        return "local";
      }

      try {
        var domain = System.DirectoryServices.ActiveDirectory.Domain.GetCurrentDomain();
        return domain.Name;
      }
      catch {
        return "local";
      }
    }
  }
}
