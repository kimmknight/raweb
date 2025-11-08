using System;
using System.Web;
using System.Web.Http;

namespace RAWebServer.Api {
  public partial class AuthController : ApiController {
    [HttpGet]
    [Route("clear")]
    public IHttpActionResult Clear() {
      var context = HttpContext.Current;
      if (context == null) {
        return BadRequest("No HTTP context");
      }

      // clear server-session if it exists
      if (context.Session != null) {
        context.Session.Clear();
        context.Session.Abandon();
      }

      // expire auth cookie on the client
      if (context.Request.Cookies[".ASPXAUTH"] != null) {
        var authCookie = new HttpCookie(".ASPXAUTH") {
          Path = VirtualPathUtility.ToAbsolute("~/"),
          Expires = DateTime.Now.AddDays(-1)
        };
        context.Response.Cookies.Add(authCookie);
      }

      return Ok(new { cleared = true });
    }
  }
}
