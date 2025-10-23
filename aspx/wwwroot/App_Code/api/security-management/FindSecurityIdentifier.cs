using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using System.Web.Http;
using RAWeb.Server.Management;

namespace RAWebServer.Api {
  public partial class SecurityManagementController : ApiController {

    /// <summary>
    /// Accepts a lookup string and attempts to find the corresponding
    /// resolved security identifier (SID) for a user or group.
    /// <br /><br />
    /// See <see cref="ResolvedSecurityIdentifier.FromLookupString(string, string?)"/> for details.
    /// </summary>
    /// <param name="sids"></param>
    /// <returns></returns>
    [HttpGet]
    [HttpPost]
    [Route("find-sid")]
    [RequireLocalAdministrator]
    public IHttpActionResult ResolveSecurityIdentifiers(string lookup, string domain = null) {
      if (!string.IsNullOrWhiteSpace(domain)) {
        domain = Environment.MachineName;
      }

      if (string.IsNullOrWhiteSpace(lookup)) {
        return BadRequest("No username, group name, user principal name, or SID provided");
      }


      try {
        var resolvedSid = ResolvedSecurityIdentifier.FromLookupString(lookup, domain);
        if (resolvedSid == null) {
          return NotFound();
        }

        return Ok(resolvedSid);
      }
      catch (Exception exception) {
        return InternalServerError(exception);
      }
    }
  }
}
