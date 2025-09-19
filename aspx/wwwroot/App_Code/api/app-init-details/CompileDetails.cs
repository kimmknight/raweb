using AliasUtilities;
using AuthUtilities;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Configuration;


namespace RAWebServer.Api
{
  public partial class AppInitDetailsController : ApiController
  {
    [HttpGet]
    [Route("")]
    public IHttpActionResult CompileDetails()
    {
      string iisBase = VirtualPathUtility.ToAbsolute("~/");
      string appBase = iisBase + "";

      // get the authenticated user from the cookie
      AuthCookieHandler authCookieHandler = new AuthUtilities.AuthCookieHandler();
      var userInfo = authCookieHandler.GetUserInformation(HttpContext.Current.Request);
      var authUser = userInfo != null ? new
      {
        username = userInfo.Username,
        domain = userInfo.Domain,
        fullName = userInfo.FullName,
        isLocalAdministrator = userInfo.IsLocalAdministrator,
      } :
      new
      {
        username = "UNAUTHENTICATED",
        domain = "RAWEB",
        fullName = "Unauthenticated",
        isLocalAdministrator = false,
      };
      string userNamespace = userInfo == null ? "RAWEB:UNAUTHENTICATED" : (userInfo.Domain + ":" + userInfo.Username);

      // expose the terminal server aliases
      var terminalServerAliases = GetTerminalServerAliases();

      // app-related policies
      bool? combineTerminalServersModeEnabled = string.IsNullOrEmpty(ConfigurationManager.AppSettings["CombineTerminalServersModeEnabled"]) ? (bool?)null : ConfigurationManager.AppSettings["CombineTerminalServersModeEnabled"] == "true";
      bool? favoritesEnabled = string.IsNullOrEmpty(ConfigurationManager.AppSettings["App.Favorites.Enabled"]) ? (bool?)null : ConfigurationManager.AppSettings["App.Favorites.Enabled"] == "true";
      bool? flatModeEnabled = string.IsNullOrEmpty(ConfigurationManager.AppSettings["App.FlatModeEnabled"]) ? (bool?)null : ConfigurationManager.AppSettings["App.FlatModeEnabled"] == "true";
      bool? hidePortsEnabled = string.IsNullOrEmpty(ConfigurationManager.AppSettings["App.HidePortsEnabled"]) ? (bool?)null : ConfigurationManager.AppSettings["App.HidePortsEnabled"] == "true";
      bool? iconBackgroundsEnabled = string.IsNullOrEmpty(ConfigurationManager.AppSettings["App.IconBackgroundsEnabled"]) ? (bool?)null : ConfigurationManager.AppSettings["App.IconBackgroundsEnabled"] == "true";
      bool? simpleModeEnabled = string.IsNullOrEmpty(ConfigurationManager.AppSettings["App.SimpleModeEnabled"]) ? (bool?)null : ConfigurationManager.AppSettings["App.SimpleModeEnabled"] == "true";
      bool? passwordChangeEnabled = string.IsNullOrEmpty(ConfigurationManager.AppSettings["PasswordChange.Enabled"]) ? (bool?)null : ConfigurationManager.AppSettings["PasswordChange.Enabled"] == "true";
      string signedInUserGlobalAlerts = ConfigurationManager.AppSettings["App.Alerts.SignedInUser"];
      var policies = new
      {
        combineTerminalServersModeEnabled,
        favoritesEnabled,
        flatModeEnabled,
        hidePortsEnabled,
        iconBackgroundsEnabled,
        simpleModeEnabled,
        passwordChangeEnabled,
        signedInUserGlobalAlerts
      };

      // host information
      AliasResolver resolver = new AliasResolver();
      var machineName = resolver.Resolve(System.Environment.MachineName);
      var envMachineName = System.Environment.MachineName;

      // version information
      var coreVersion = VersionUtilities.LocalVersions.GetApplicationVersionString(); // server
      var webVersion = VersionUtilities.LocalVersions.GetFrontendVersionString(); // web client

      return Ok(new
      {
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

    private Dictionary<string, string> GetTerminalServerAliases()
    {
      // "host1=alias1;host2=alias2"
      string raw = ConfigurationManager.AppSettings["TerminalServerAliases"] ?? "";

      Dictionary<string, string> dict = raw
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
