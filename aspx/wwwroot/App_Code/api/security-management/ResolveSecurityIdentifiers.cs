using System.Collections.Generic;
using System.Web.Http;
using RAWeb.Server.Management;

namespace RAWebServer.Api {
    public partial class SecurityManagementController : ApiController {

        /// <summary>
        /// Resolves a list of security identifiers (SIDs) to their corresponding
        /// account domains, usernames, and display names.
        /// <br /><br />
        /// See <see cref="ResolvedSecurityIdentifiers.FromSidStrings(string[], out List{string})"/> for details.
        /// </summary>
        /// <param name="sids"></param>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        [Route("resolve-sids")]
        [RequireLocalAdministrator]
        public IHttpActionResult ResolveSecurityIdentifiers(string[] sids) {
            List<string> invalidOrUnfoundSids;
            var resolvedSids = ResolvedSecurityIdentifiers.FromSidStrings(sids, out invalidOrUnfoundSids);

            return Ok(new {
                ResolvedSids = resolvedSids,
                InvalidOrUnfoundSids = invalidOrUnfoundSids
            });
        }
    }
}
