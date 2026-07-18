using System;
using System.Linq;
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
      var userInfo = UserInformation.FromWindowsIdentity(currentWindowsIdentity, AuthTicketLevel.ReadAndWriteAdmin);

      if (userInfo is null) {
        await next();
        return;
      }

      // include the administrators group even if the user is not a local admin
      // so that the user can add and edit file resources
      userInfo.Groups = [.. userInfo.Groups, new GroupInformation("S-1-5-32-544")];

      // UserInformation.FromHttpRequest always checks the http
      // context before doing lookups
      context.Items[UserInformation.UserInformationContextKey] = userInfo;

      // for good measure, also set the cookie with the auth ticket
      var ticket = AuthTicket.FromUserInformation(userInfo, AuthTicketLevel.ReadAndWriteAdmin);
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
