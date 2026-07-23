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
    var issueDate = new DateTime(2026, 6, 1, 12, 0, 0);
    var expiration = new DateTime(2026, 6, 1, 12, 30, 0);
    var original = new AuthTicket(1, @"DOMAIN\jdoe", issueDate, expiration, true, new AuthTicketUserData(AuthTicketLevel.ReadOnlyAdmin));

    var token = original.ToEncryptedToken();
    var restored = AuthTicket.FromEncryptedToken(token);

    await Assert.That(restored.Version).IsEqualTo(1);
    await Assert.That(restored.Name).IsEqualTo(@"DOMAIN\jdoe");
    await Assert.That(restored.IssueDate.Ticks).IsEqualTo(issueDate.Ticks);
    await Assert.That(restored.Expiration.Ticks).IsEqualTo(expiration.Ticks);
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
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.Now, DateTime.Now.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));
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
    await Assert.That(restoredTicket.Expiration).IsEqualTo(expiration);
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
    );

    var ticket = AuthTicket.FromUserInformation(userInfo, AuthTicketLevel.ReadOnlyAdmin);

    await Assert.That(ticket).IsNotNull();
    await Assert.That(ticket.Name).IsEqualTo($@"{userInfo.Domain}\{userInfo.Username}");
    await Assert.That(ticket.UserData.Level).IsEqualTo(AuthTicketLevel.ReadOnlyAdmin);
    await Assert.That(ticket.IsPersistent).IsFalse();
    await Assert.That(ticket.IssueDate).IsLessThanOrEqualTo(DateTime.Now);
    await Assert.That(ticket.Expiration).IsGreaterThan(ticket.IssueDate);
  }

  [Test]
  public async Task FromWindowsIdentity_ReturnsTicketWithCorrectFields() {
    var identity = System.Security.Principal.WindowsIdentity.GetCurrent();

    var ticket = AuthTicket.FromWindowsIdentity(identity, AuthTicketLevel.ReadOnlyAdmin);

    await Assert.That(ticket).IsNotNull();
    await Assert.That(ticket.Name).IsEqualTo(identity.Name);
    await Assert.That(ticket.UserData.Level).IsEqualTo(AuthTicketLevel.ReadOnlyAdmin);
    await Assert.That(ticket.IsPersistent).IsFalse();
    await Assert.That(ticket.IssueDate).IsLessThanOrEqualTo(DateTime.Now);
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
    );

    var ticket = AuthTicket.FromUserInformation(userInfo, AuthTicketLevel.ReadOnlyUser);

    await Assert.That(ticket.UserData.Level).IsEqualTo(AuthTicketLevel.ReadOnlyUser);
  }

  [Test]
  public async Task FromUserInformation_UsesReadAndWriteAdminLevelWhenSpecified() {
    var userInfo = new UserInformation(
      sid: "S-1-4-447-2",
      username: "jdoe",
      domain: "DOMAIN"
    );

    var ticket = AuthTicket.FromUserInformation(userInfo, AuthTicketLevel.ReadAndWriteAdmin);

    await Assert.That(ticket.UserData.Level).IsEqualTo(AuthTicketLevel.ReadAndWriteAdmin);
  }

  [Test]
  public async Task ToCookie_SetsExpiresToTicketExpiration() {
    var expiration = DateTime.Now.AddMinutes(30);
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.Now, expiration, false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));

    var cookie = ticket.ToCookie("/app");

    await Assert.That(cookie.Expires).IsEqualTo(expiration);
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
