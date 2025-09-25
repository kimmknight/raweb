using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;
using RAWebServer.Utilities;

namespace RAWebServer.Api {
  public partial class AppInitDetailsController : ApiController {
    [HttpGet]
    [Route("")]
    public IHttpActionResult CompileDetails() {
      var iisBase = VirtualPathUtility.ToAbsolute("~/");
      var appBase = iisBase + "";

      // get the authenticated user from the cookie
      var authCookieHandler = new AuthCookieHandler();
      var userInfo = authCookieHandler.GetUserInformation(HttpContext.Current.Request);
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
      var combineTerminalServersModeEnabled = string.IsNullOrEmpty(ConfigurationManager.AppSettings["CombineTerminalServersModeEnabled"]) ? (bool?)null : ConfigurationManager.AppSettings["CombineTerminalServersModeEnabled"] == "true";
      var favoritesEnabled = string.IsNullOrEmpty(ConfigurationManager.AppSettings["App.FavoritesEnabled"]) ? (bool?)null : ConfigurationManager.AppSettings["App.FavoritesEnabled"] == "true";
      var flatModeEnabled = string.IsNullOrEmpty(ConfigurationManager.AppSettings["App.FlatModeEnabled"]) ? (bool?)null : ConfigurationManager.AppSettings["App.FlatModeEnabled"] == "true";
      var hidePortsEnabled = string.IsNullOrEmpty(ConfigurationManager.AppSettings["App.HidePortsEnabled"]) ? (bool?)null : ConfigurationManager.AppSettings["App.HidePortsEnabled"] == "true";
      var iconBackgroundsEnabled = string.IsNullOrEmpty(ConfigurationManager.AppSettings["App.IconBackgroundsEnabled"]) ? (bool?)null : ConfigurationManager.AppSettings["App.IconBackgroundsEnabled"] == "true";
      var simpleModeEnabled = string.IsNullOrEmpty(ConfigurationManager.AppSettings["App.SimpleModeEnabled"]) ? (bool?)null : ConfigurationManager.AppSettings["App.SimpleModeEnabled"] == "true";
      var passwordChangeEnabled = string.IsNullOrEmpty(ConfigurationManager.AppSettings["PasswordChange.Enabled"]) ? (bool?)null : ConfigurationManager.AppSettings["PasswordChange.Enabled"] == "true";
      var anonymousAuthentication = ConfigurationManager.AppSettings["App.Auth.Anonymous"] == "always" ? "always" : (ConfigurationManager.AppSettings["App.Auth.Anonymous"] == "allow" ? "allow" : "never");
      var signedInUserGlobalAlerts = ConfigurationManager.AppSettings["App.Alerts.SignedInUser"];
      var policies = new {
        combineTerminalServersModeEnabled,
        favoritesEnabled,
        flatModeEnabled,
        hidePortsEnabled,
        iconBackgroundsEnabled,
        simpleModeEnabled,
        passwordChangeEnabled,
        anonymousAuthentication,
        signedInUserGlobalAlerts
      };

      // host information
      var resolver = new AliasResolver();
      var machineName = resolver.Resolve(System.Environment.MachineName);
      var envMachineName = System.Environment.MachineName;

      // version information
      var coreVersion = LocalVersions.GetApplicationVersionString(); // server
      var webVersion = LocalVersions.GetFrontendVersionString(); // web client

      return Ok(new {
        iisBase,
        appBase,
        authUser,
        userNamespace,
        terminalServerAliases,
        policies,
        machineName,
        envMachineName,
        coreVersion,
        webVersion,
      });
    }

    private Dictionary<string, string> GetTerminalServerAliases() {
      // "host1=alias1;host2=alias2"
      var raw = ConfigurationManager.AppSettings["TerminalServerAliases"] ?? "";

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
  }
}
