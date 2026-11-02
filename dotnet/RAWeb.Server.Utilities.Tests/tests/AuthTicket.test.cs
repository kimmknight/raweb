using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace RAWeb.Server.Utilities.Tests;

public class AuthTicketTests {
  [Before(Class)]
  public static Task SetupProtection(ClassHookContext context) {
    var dataProtectionProvider = new EphemeralDataProtectionProvider();
    var dataProtector = dataProtectionProvider.CreateProtector("AuthTicket");


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
    return Task.CompletedTask;
  }

  [Test]
  public async Task ToString_ReturnsName() {
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.Now, DateTime.Now.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));

    await Assert.That(ticket.ToString()).IsEqualTo(@"DOMAIN\jdoe");
  }

  [Test]
  public async Task ToEncryptedToken_ReturnsNonEmptyString() {
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.Now, DateTime.Now.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));

    var token = ticket.ToEncryptedToken();
    Console.WriteLine($"Encrypted Token: {token}");

    await Assert.That(token).IsNotNull();
    await Assert.That(token).IsNotEmpty();
  }

  [Test]
  public async Task RoundTrip_PreservesAllFields() {
    var issueDate = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    var expiration = new DateTime(2026, 6, 1, 12, 30, 0, DateTimeKind.Utc);
    var original = new AuthTicket(1, @"DOMAIN\jdoe", issueDate, expiration, true, new AuthTicketUserData(AuthTicketLevel.ReadOnlyAdmin));

    var token = original.ToEncryptedToken();
    var restored = AuthTicket.FromEncryptedToken(token);

    await Assert.That(restored.Version).IsEqualTo(1);
    await Assert.That(restored.Name).IsEqualTo(@"DOMAIN\jdoe");
    await Assert.That(restored.IssueDate.Kind).IsEqualTo(DateTimeKind.Utc);
    await Assert.That(restored.IssueDate).IsEqualTo(issueDate);
    await Assert.That(restored.Expiration.Kind).IsEqualTo(DateTimeKind.Utc);
    await Assert.That(restored.Expiration).IsEqualTo(expiration);
    await Assert.That(restored.IsPersistent).IsTrue();
    await Assert.That(restored.UserData.Level).IsEqualTo(AuthTicketLevel.ReadOnlyAdmin);
  }

  [Test]
  public async Task ToCookie_UsesDefaultAuthCookieName() {
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.Now, DateTime.Now.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));

    var cookie = ticket.ToCookie("/app");

    await Assert.That(cookie.Name).IsEqualTo(Constants.DefaultAuthCookieName);
    await Assert.That(cookie.Path).IsEqualTo("/app");
    await Assert.That(cookie.HttpOnly).IsTrue();
  }

  [Test]
  public async Task ToCookie_UsesProvidedCookieName() {
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.Now, DateTime.Now.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));

    var cookie = ticket.ToCookie("/app", "mycookie");

    await Assert.That(cookie.Name).IsEqualTo("mycookie");
  }

  [Test]
  public async Task ToCookie_ThrowsWhenCookieValueExceeds4096Bytes() {
    var longName = new string('X', 4100);
    var ticket = new AuthTicket(1, longName, DateTime.Now, DateTime.Now.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));

    var action = () => ticket.ToCookie("/app");

    await Assert.That(action).ThrowsException();
  }

  [Test]
  public async Task FromHttpRequestCookie_ReturnsNullForMissingCookie() {
    var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
    var request = context.Request;

    request.Headers["Cookie"] = "othercookie=othervalue";

    var ticket = AuthTicket.FromHttpRequestCookie(request, Constants.DefaultAuthCookieName);

    await Assert.That(ticket).IsNull();
  }

  [Test]
  public async Task FromHttpRequestCookie_ReturnsTicketForValidCookie() {
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.Now, DateTime.Now.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser, null, AuthSessionStore.NewAuthSessionId()));
    new AuthSessionStore().Add(ticket); // tickets are only accepted while their sign-in session exists
    var cookie = ticket.ToCookie("/app");

    var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
    var request = context.Request;

    request.Headers["Cookie"] = $"{cookie.Name}={cookie.Value};othercookie=othervalue";

    var restoredTicket = AuthTicket.FromHttpRequestCookie(request, Constants.DefaultAuthCookieName);

    await Assert.That(restoredTicket).IsNotNull();
    await Assert.That(restoredTicket!.Name).IsEqualTo(@"DOMAIN\jdoe");
  }

  [Test]
  public async Task FromHttpRequestCookie_ReturnsNullForInvalidCookie() {
    var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
    var request = context.Request;

    var requestCookies = new Dictionary<string, string> {
      { Constants.DefaultAuthCookieName, "invalidtoken" }
    };
    request.Cookies = new Microsoft.AspNetCore.Http.Internal.RequestCookieCollection(requestCookies);

    var ticket = AuthTicket.FromHttpRequestCookie(request, Constants.DefaultAuthCookieName);

    await Assert.That(ticket).IsNull();
  }

  [Test]
  public async Task FromEncryptedToken_ThrowsForInvalidToken() {
    var invalidToken = "invalidtoken";

    await Assert.ThrowsAsync<InvalidTicketException>(() => Task.Run(() => {
      AuthTicket.FromEncryptedToken(invalidToken);
    }));
  }

  /// <summary>
  /// Consumers are reponsible for checking that the ticket is not expired.
  /// </summary>
  /// <returns></returns>
  [Test]
  public async Task FromEncryptedToken_AllowsExpiredTicket() {
    var issueDate = DateTime.Now.AddMinutes(-60);
    var expiration = DateTime.Now.AddMinutes(-30);
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", issueDate, expiration, false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));
    var token = ticket.ToEncryptedToken();

    var restoredTicket = AuthTicket.FromEncryptedToken(token);

    await Assert.That(restoredTicket).IsNotNull();
    await Assert.That(restoredTicket!.Name).IsEqualTo(@"DOMAIN\jdoe");
    await Assert.That(restoredTicket.Expiration).IsEqualTo(expiration.ToUniversalTime());
    await Assert.That(restoredTicket.IsExpired).IsTrue();
  }

  [Test]
  public async Task IsExpired_FalseBeforeExpiration() {
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));

    await Assert.That(ticket.IsExpired).IsFalse();
  }

  /// <summary>
  /// Local and UTC expiration times must be compared as the same instant.
  /// </summary>
  [Test]
  public async Task IsExpired_HandlesLocalTimes() {
    var notExpired = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.Now, DateTime.Now.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));
    var expired = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.Now.AddMinutes(-60), DateTime.Now.AddMinutes(-30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));

    await Assert.That(notExpired.IsExpired).IsFalse();
    await Assert.That(expired.IsExpired).IsTrue();
  }

  [Test]
  public async Task FromHttpRequestCookie_ReturnsNullForExpiredCookie() {
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(-1), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));
    var cookie = ticket.ToCookie("/app");

    var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
    context.Request.Headers["Cookie"] = $"{cookie.Name}={cookie.Value}";

    var restoredTicket = AuthTicket.FromHttpRequestCookie(context.Request, Constants.DefaultAuthCookieName);

    await Assert.That(restoredTicket).IsNull();
  }

  [Test]
  public async Task FromHttpRequestCookie_ReturnsNullForExpiredSudoCookie() {
    var ticket = new AuthTicket(1, @"DOMAIN\admin1", DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(-1), false, new AuthTicketUserData(AuthTicketLevel.ReadAndWriteAdmin));
    var cookie = ticket.ToCookie("/app", Constants.SudoAuthCookieName);

    var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
    context.Request.Headers["Cookie"] = $"{cookie.Name}={cookie.Value}";

    var restoredTicket = AuthTicket.FromHttpRequestCookie(context.Request, Constants.SudoAuthCookieName);

    await Assert.That(restoredTicket).IsNull();
  }

  [Test]
  public async Task FromEncryptedToken_ThrowsForTamperedTicket() {
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.Now, DateTime.Now.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));
    var token = ticket.ToEncryptedToken();

    // simulate tampering by changing the last character of the token to something different
    var lastChar = token[token.Length - 1];
    var tamperedToken = token.Substring(0, token.Length - 1) + (lastChar == 'X' ? 'Y' : 'X');

    await Assert.ThrowsAsync<InvalidTicketException>(() => Task.Run(() => {
      AuthTicket.FromEncryptedToken(tamperedToken);
    }));
  }

  [Test]
  public async Task FromUserInformation_ReturnsTicketWithCorrectFields() {
    var userInfo = new UserInformation(
      sid: "S-1-4-447-2",
      username: "jdoe",
      domain: "DOMAIN",
      fullName: "J. Doe",
      groups: [new GroupInformation("S-1-4-447-3", "Group1")]
    ) {
      PasswordLastSet = null // DOMAIN does not exist, so skip the directory lookup
    };

    var ticket = AuthTicket.FromUserInformation(userInfo, AuthTicketLevel.ReadOnlyAdmin);

    await Assert.That(ticket).IsNotNull();
    await Assert.That(ticket.Name).IsEqualTo($@"{userInfo.Domain}\{userInfo.Username}");
    await Assert.That(ticket.UserData.Level).IsEqualTo(AuthTicketLevel.ReadOnlyAdmin);
    await Assert.That(ticket.IsPersistent).IsFalse();
    await Assert.That(ticket.IssueDate.Kind).IsEqualTo(DateTimeKind.Utc);
    await Assert.That(ticket.IssueDate).IsLessThanOrEqualTo(DateTime.UtcNow);
    await Assert.That(ticket.Expiration).IsGreaterThan(ticket.IssueDate);
    await Assert.That(ticket.IsExpired).IsFalse();
  }

  [Test]
  public async Task FromWindowsIdentity_ReturnsTicketWithCorrectFields() {
    var identity = System.Security.Principal.WindowsIdentity.GetCurrent();

    var ticket = AuthTicket.FromWindowsIdentity(identity, AuthTicketLevel.ReadOnlyAdmin);

    await Assert.That(ticket).IsNotNull();
    await Assert.That(ticket.Name).IsEqualTo(identity.Name);
    await Assert.That(ticket.UserData.Level).IsEqualTo(AuthTicketLevel.ReadOnlyAdmin);
    await Assert.That(ticket.IsPersistent).IsFalse();
    await Assert.That(ticket.IssueDate).IsLessThanOrEqualTo(DateTime.UtcNow);
    await Assert.That(ticket.Expiration).IsGreaterThan(ticket.IssueDate);
  }

  [Test]
  public async Task FromWindowsIdentity_MaxLevelReadOnlyUser_NeverGrantsAdminLevel() {
    var identity = System.Security.Principal.WindowsIdentity.GetCurrent();

    var ticket = AuthTicket.FromWindowsIdentity(identity, AuthTicketLevel.ReadOnlyUser);

    await Assert.That(ticket.UserData.Level).IsEqualTo(AuthTicketLevel.ReadOnlyUser);
  }

  [Test]
  public async Task Constructor_DefaultsUserDataWhenNull() {
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.Now, DateTime.Now.AddMinutes(30), false, null);

    await Assert.That(ticket.UserData).IsNotNull();
    await Assert.That(ticket.UserData.Level).IsEqualTo(AuthTicketLevel.ReadOnlyUser);
  }

  [Test]
  public async Task FromUserInformation_UsesReadOnlyUserLevelWhenSpecified() {
    var userInfo = new UserInformation(
      sid: "S-1-4-447-2",
      username: "jdoe",
      domain: "DOMAIN"
    ) {
      PasswordLastSet = null // DOMAIN does not exist, so skip the directory lookup
    };

    var ticket = AuthTicket.FromUserInformation(userInfo, AuthTicketLevel.ReadOnlyUser);

    await Assert.That(ticket.UserData.Level).IsEqualTo(AuthTicketLevel.ReadOnlyUser);
  }

  [Test]
  public async Task FromUserInformation_UsesReadAndWriteAdminLevelWhenSpecified() {
    var userInfo = new UserInformation(
      sid: "S-1-4-447-2",
      username: "jdoe",
      domain: "DOMAIN"
    ) {
      PasswordLastSet = null // DOMAIN does not exist, so skip the directory lookup
    };

    var ticket = AuthTicket.FromUserInformation(userInfo, AuthTicketLevel.ReadAndWriteAdmin);

    await Assert.That(ticket.UserData.Level).IsEqualTo(AuthTicketLevel.ReadAndWriteAdmin);
  }

  [Test]
  public async Task ToCookie_SetsExpiresToTicketExpiration() {
    var expiration = DateTime.Now.AddMinutes(30);
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.Now, expiration, false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));

    var cookie = ticket.ToCookie("/app");

    // the ticket stores the expiration in UTC
    await Assert.That(cookie.Expires).IsEqualTo(expiration.ToUniversalTime());
  }

  [Test]
  public async Task ToCookie_DefaultCookieUsesLaxSameSite() {
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));

    var cookie = ticket.ToCookie("/app");

    await Assert.That(cookie.SameSite).IsEqualTo(Microsoft.AspNetCore.Http.SameSiteMode.Lax);
  }

  [Test]
  public async Task ToCookie_SudoCookieUsesStrictSameSite() {
    var ticket = new AuthTicket(1, @"DOMAIN\admin1", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), false, new AuthTicketUserData(AuthTicketLevel.ReadAndWriteAdmin));

    var cookie = ticket.ToCookie("/app", Constants.SudoAuthCookieName);

    await Assert.That(cookie.SameSite).IsEqualTo(Microsoft.AspNetCore.Http.SameSiteMode.Strict);
  }

  [Test]
  public async Task Constructor_ConvertsLocalDatesToUtc() {
    var issueDate = DateTime.Now;
    var expiration = issueDate.AddMinutes(30);

    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", issueDate, expiration, false, null);

    await Assert.That(ticket.IssueDate.Kind).IsEqualTo(DateTimeKind.Utc);
    await Assert.That(ticket.IssueDate).IsEqualTo(issueDate.ToUniversalTime());
    await Assert.That(ticket.Expiration.Kind).IsEqualTo(DateTimeKind.Utc);
    await Assert.That(ticket.Expiration).IsEqualTo(expiration.ToUniversalTime());
  }

  [Test]
  public async Task Constructor_ThrowsForUnspecifiedDates() {
    var unspecified = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Unspecified);

    await Assert.That(() => new AuthTicket(1, @"DOMAIN\jdoe", unspecified, DateTime.UtcNow, false, null)).Throws<ArgumentException>();
    await Assert.That(() => new AuthTicket(1, @"DOMAIN\jdoe", DateTime.UtcNow, unspecified, false, null)).Throws<ArgumentException>();
  }

  [Test]
  public async Task ExpirationSetter_ConvertsLocalDatesToUtcAndThrowsForUnspecifiedDates() {
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30), false, null);
    var localExpiration = DateTime.Now.AddMonths(1);

    ticket.Expiration = localExpiration;

    await Assert.That(ticket.Expiration.Kind).IsEqualTo(DateTimeKind.Utc);
    await Assert.That(ticket.Expiration).IsEqualTo(localExpiration.ToUniversalTime());
    await Assert.That(() => ticket.Expiration = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Unspecified)).Throws<ArgumentException>();
  }

  [Test]
  public async Task AuthTicketCookie_DefaultsExpiresToMinValueWhenNotProvided() {
    var cookie = new AuthTicketCookie("cookiename", "cookievalue", "/app");

    await Assert.That(cookie.Expires).IsEqualTo(DateTime.MinValue);
  }

  [Test]
  public async Task AuthTicketCookie_UsesProvidedExpires() {
    var expires = DateTime.Now.AddDays(1);
    var cookie = new AuthTicketCookie("cookiename", "cookievalue", "/app", expires);

    await Assert.That(cookie.Expires).IsEqualTo(expires);
  }

  [Test]
  public async Task RoundTrip_PreservesReadOnlyWorkspaceUserLevel() {
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.Now, DateTime.Now.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyWorkspaceUser));

    var restored = AuthTicket.FromEncryptedToken(ticket.ToEncryptedToken());

    await Assert.That(restored.UserData.Level).IsEqualTo(AuthTicketLevel.ReadOnlyWorkspaceUser);
  }

  [Test]
  public async Task FromWindowsIdentity_MaxLevelReadOnlyWorkspaceUser_UsesWorkspaceUserLevel() {
    var identity = System.Security.Principal.WindowsIdentity.GetCurrent();

    var ticket = AuthTicket.FromWindowsIdentity(identity, AuthTicketLevel.ReadOnlyWorkspaceUser);

    await Assert.That(ticket.UserData.Level).IsEqualTo(AuthTicketLevel.ReadOnlyWorkspaceUser);
  }

  [Test]
  public async Task UserInformation_DoesNotUpgradeReadOnlyWorkspaceUserLevel() {
    var userInfo = new UserInformation("S-1-4-447-2", "jdoe", "DOMAIN", AuthTicketLevel.ReadOnlyWorkspaceUser);

    await Assert.That(userInfo.AuthTicketLevel).IsEqualTo(AuthTicketLevel.ReadOnlyWorkspaceUser);
    await Assert.That(userInfo.AuthTicketLevel.IsAdmin).IsFalse();
  }

  private static Microsoft.AspNetCore.Http.DefaultHttpContext CreateContextWithAuthCookie(AuthTicket ticket) {
    // tickets are only accepted while their session exists, so start it like the sign-in endpoints do
    new AuthSessionStore().Add(ticket);

    var cookie = ticket.ToCookie("/");
    var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
    context.Request.Headers["Cookie"] = $"{cookie.Name}={cookie.Value}";
    return context;
  }

  [Test]
  public async Task FromHttpRequest_RejectsWorkspaceTicketWhenNotAllowed() {
    var ticket = AuthTicket.FromUserInformation(UserInformation.AnonymousUser, AuthTicketLevel.ReadOnlyWorkspaceUser);
    var context = CreateContextWithAuthCookie(ticket);

    var userInfo = UserInformation.FromHttpRequest(context.Request);

    await Assert.That(userInfo).IsNull();
  }

  [Test]
  public async Task FromHttpRequest_AcceptsWorkspaceTicketWhenAllowed() {
    var ticket = AuthTicket.FromUserInformation(UserInformation.AnonymousUser, AuthTicketLevel.ReadOnlyWorkspaceUser);
    var context = CreateContextWithAuthCookie(ticket);
    AuthTicket.AllowWorkspaceAuthTicket(context);

    var userInfo = UserInformation.FromHttpRequest(context.Request);

    await Assert.That(userInfo).IsNotNull();
    await Assert.That(userInfo!.Sid).IsEqualTo(UserInformation.AnonymousUser.Sid);
  }

  [Test]
  public async Task FromHttpRequest_AcceptsFullTicketWhenWorkspaceTicketsAreNotAllowed() {
    var ticket = AuthTicket.FromUserInformation(UserInformation.AnonymousUser, AuthTicketLevel.ReadOnlyUser);
    var context = CreateContextWithAuthCookie(ticket);

    var userInfo = UserInformation.FromHttpRequest(context.Request);

    await Assert.That(userInfo).IsNotNull();
  }

  /// <summary>
  /// The sudo policy is enabled by default, so a ReadAndWriteAdmin ticket in the main
  /// auth cookie (such as a sudo ticket copied into it) must not grant write access.
  /// </summary>
  [Test]
  public async Task MainAuthTicketLevel_CapsReadAndWriteAdminWhenSudoPolicyIsEnabled() {
    var ticket = new AuthTicket(1, @"DOMAIN\admin1", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), false, new AuthTicketUserData(AuthTicketLevel.ReadAndWriteAdmin));

    await Assert.That(AuthTicket.IsSudoPolicyEnabled).IsTrue();
    await Assert.That(ticket.MainAuthTicketLevel).IsEqualTo(AuthTicketLevel.ReadOnlyAdmin);
  }

  [Test]
  public async Task MainAuthTicketLevel_DoesNotChangeLowerLevels() {
    foreach (var level in new[] { AuthTicketLevel.ReadOnlyWorkspaceUser, AuthTicketLevel.ReadOnlyUser, AuthTicketLevel.ReadOnlyAdmin }) {
      var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30), false, new AuthTicketUserData(level));

      await Assert.That(ticket.MainAuthTicketLevel).IsEqualTo(level);
    }
  }

  [Test]
  public async Task IsSudoTicketFor_TrueForSameUserWithReadAndWriteAdminLevel() {
    var session = new AuthTicket(1, @"DOMAIN\admin1", DateTime.Now, DateTime.Now.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyAdmin));
    var sudo = new AuthTicket(1, @"DOMAIN\admin1", DateTime.Now, DateTime.Now.AddHours(1), false, new AuthTicketUserData(AuthTicketLevel.ReadAndWriteAdmin));

    await Assert.That(sudo.IsSudoTicketFor(session)).IsTrue();
  }

  [Test]
  public async Task IsSudoTicketFor_IgnoresAccountNameCase() {
    var session = new AuthTicket(1, @"DOMAIN\Admin1", DateTime.Now, DateTime.Now.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyAdmin));
    var sudo = new AuthTicket(1, @"domain\admin1", DateTime.Now, DateTime.Now.AddHours(1), false, new AuthTicketUserData(AuthTicketLevel.ReadAndWriteAdmin));

    await Assert.That(sudo.IsSudoTicketFor(session)).IsTrue();
  }

  [Test]
  public async Task IsSudoTicketFor_FalseForDifferentUser() {
    var session = new AuthTicket(1, @"DOMAIN\admin1", DateTime.Now, DateTime.Now.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyAdmin));
    var sudo = new AuthTicket(1, @"DOMAIN\admin2", DateTime.Now, DateTime.Now.AddHours(1), false, new AuthTicketUserData(AuthTicketLevel.ReadAndWriteAdmin));

    await Assert.That(sudo.IsSudoTicketFor(session)).IsFalse();
  }

  [Test]
  public async Task IsSudoTicketFor_FalseForLevelBelowReadAndWriteAdmin() {
    var session = new AuthTicket(1, @"DOMAIN\admin1", DateTime.Now, DateTime.Now.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyAdmin));
    var sudo = new AuthTicket(1, @"DOMAIN\admin1", DateTime.Now, DateTime.Now.AddHours(1), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyAdmin));

    await Assert.That(sudo.IsSudoTicketFor(session)).IsFalse();
  }

  [Test]
  public async Task IsAdmin_FalseForReadOnlyUser() {
    await Assert.That(AuthTicketLevel.ReadOnlyUser.IsAdmin).IsFalse();
  }

  [Test]
  public async Task IsAdmin_TrueForReadOnlyAdmin() {
    await Assert.That(AuthTicketLevel.ReadOnlyAdmin.IsAdmin).IsTrue();
  }

  [Test]
  public async Task IsAdmin_TrueForReadAndWriteAdmin() {
    await Assert.That(AuthTicketLevel.ReadAndWriteAdmin.IsAdmin).IsTrue();
  }
}
