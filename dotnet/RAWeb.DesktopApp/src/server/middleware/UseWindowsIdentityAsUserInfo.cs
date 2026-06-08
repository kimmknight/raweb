using System;
using Microsoft.AspNetCore.Builder;
using RAWeb.Server.Utilities;

namespace RAWeb.DesktopApp.InternalServer;

internal static class UseWindowsIdentityAsUserInfoMiddleware {
  /// <summary>
  /// This middleware populates the HttpContext with UserInformation
  /// based on the current Windows user.
  /// <br/><br/>
  /// Since UserInformation.FromHttpRequest checks the HttpContext
  /// for existing UserInformation before doing any lookups, this
  /// allows us to effectively spoof the authenticated user as the
  /// current Windows user for any requests that go through this middleware.
  /// </summary>
  /// <param name="app"></param>
  internal static void UseWindowsIdentityAsUserInfo(this WebApplication app) {
    app.Use(async (context, next) => {
      var currentWindowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
      var ticket = AuthTicket.FromWindowsIdentity(currentWindowsIdentity);
      var userInfo = UserInformation.FromDownLevelLogonName(ticket.Name);

      // UserInformation.FromHttpRequest always checks the http
      // context before doing lookups
      if (userInfo is not null) {
        context.Items[UserInformation.UserInformationContextKey] = userInfo;
      }

      // for good measure, also set the cookie with the auth ticket
      var cookie = ticket.ToCookie("/");
      context.Response.Cookies.Append(cookie.Name, cookie.Value, new Microsoft.AspNetCore.Http.CookieOptions {
        Path = cookie.Path,
        HttpOnly = cookie.HttpOnly,
        Secure = cookie.Secure,
        Expires = cookie.Expires == DateTime.MinValue ? null : (DateTimeOffset?)cookie.Expires
      });

      await next();
    });
  }
}
