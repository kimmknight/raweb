using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace RAWeb.Server.Utilities.Tests;

public class AuthSessionStoreTests {
  private string _dbName = "";

  [Before(Test)]
  public void Setup() {
    _dbName = "test_sessions_" + Guid.NewGuid().ToString("N")[..8];
  }

  [After(Test)]
  public void Cleanup() {
    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    var dbFilePath = Path.Combine(Constants.AppDataFolderPath, _dbName + ".sqlite");
    if (File.Exists(dbFilePath)) {
      File.Delete(dbFilePath);
    }
  }

  private static AuthTicket CreateTicket(string? authSessionId, DateTime? expiration = null, string name = @"DOMAIN\jdoe") {
    var issueDate = DateTime.UtcNow;
    return new AuthTicket(1, name, issueDate, expiration ?? issueDate.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser, null, authSessionId));
  }

  [Test]
  public async Task NewAuthSessionId_IsRandomAndUrlSafe() {
    var first = AuthSessionStore.NewAuthSessionId();
    var second = AuthSessionStore.NewAuthSessionId();

    await Assert.That(first).IsNotEqualTo(second);
    await Assert.That(first.Length).IsGreaterThanOrEqualTo(22);
    await Assert.That(first.Any(c => c is '+' or '/' or '=')).IsFalse();
  }

  [Test]
  public async Task IsActive_FalseBeforeAdd() {
    var store = new AuthSessionStore(_dbName);

    await Assert.That(store.IsActive(CreateTicket("session-1"))).IsFalse();
  }

  [Test]
  public async Task IsActive_TrueAfterAdd() {
    var store = new AuthSessionStore(_dbName);
    var ticket = CreateTicket("session-1");

    store.Add(ticket);

    await Assert.That(store.IsActive(ticket)).IsTrue();
  }

  [Test]
  public async Task IsActive_FalseAfterRemove() {
    var store = new AuthSessionStore(_dbName);
    var ticket = CreateTicket("session-1");
    store.Add(ticket);

    store.Remove(ticket);

    await Assert.That(store.IsActive(ticket)).IsFalse();
  }

  [Test]
  public async Task Remove_OnlyEndsThatSession() {
    var store = new AuthSessionStore(_dbName);
    var first = CreateTicket("session-1");
    var second = CreateTicket("session-2");
    store.Add(first);
    store.Add(second);

    store.Remove(first);

    await Assert.That(store.IsActive(first)).IsFalse();
    await Assert.That(store.IsActive(second)).IsTrue();
  }

  [Test]
  public async Task IsActive_FalseForExpiredSession() {
    var store = new AuthSessionStore(_dbName);
    var ticket = CreateTicket("session-1", DateTime.UtcNow.AddSeconds(-1));

    store.Add(ticket);

    await Assert.That(store.IsActive(ticket)).IsFalse();
  }

  [Test]
  public async Task IsActive_FalseWhenSessionBelongsToAnotherUser() {
    var store = new AuthSessionStore(_dbName);
    store.Add(CreateTicket("session-1", name: @"DOMAIN\jdoe"));

    await Assert.That(store.IsActive(CreateTicket("session-1", name: @"DOMAIN\someoneelse"))).IsFalse();
  }

  [Test]
  public async Task IsActive_IgnoresCaseOfUserName() {
    var store = new AuthSessionStore(_dbName);
    store.Add(CreateTicket("session-1", name: @"DOMAIN\jdoe"));

    await Assert.That(store.IsActive(CreateTicket("session-1", name: @"domain\JDOE"))).IsTrue();
  }

  [Test]
  public async Task IsActive_FalseForTicketWithoutAuthSessionId() {
    var store = new AuthSessionStore(_dbName);
    var ticket = CreateTicket(null);

    store.Add(ticket);

    await Assert.That(store.IsActive(ticket)).IsFalse();
  }

  [Test]
  public async Task Extend_UpdatesExpirationOfExistingSession() {
    var store = new AuthSessionStore(_dbName);
    store.Add(CreateTicket("session-1", DateTime.UtcNow.AddMinutes(1)));

    var extended = store.Extend(CreateTicket("session-1", DateTime.UtcNow.AddMonths(1)));

    await Assert.That(extended).IsTrue();
  }

  [Test]
  public async Task Extend_DoesNotAddEndedSession() {
    var store = new AuthSessionStore(_dbName);
    var ticket = CreateTicket("session-1");
    store.Add(ticket);
    store.Remove(ticket);

    var extended = store.Extend(CreateTicket("session-1", DateTime.UtcNow.AddMonths(1)));

    await Assert.That(extended).IsFalse();
    await Assert.That(store.IsActive(ticket)).IsFalse();
  }

  [Test]
  public async Task Add_DeletesExpiredSessions() {
    var store = new AuthSessionStore(_dbName);
    var expired = CreateTicket("expired", DateTime.UtcNow.AddSeconds(-1));
    store.Add(expired);

    store.Add(CreateTicket("session-2"));

    // extending fails because the expired session was deleted
    await Assert.That(store.Extend(CreateTicket("expired", DateTime.UtcNow.AddMonths(1)))).IsFalse();
  }
}

public class AuthTicketSessionTests {
  [Before(Class)]
  public static Task SetupProtection(ClassHookContext context) {
    var dataProtector = new EphemeralDataProtectionProvider().CreateProtector("AuthTicket");
    AuthTicket.InitializeProtection(
        protect: plaintext => Convert.ToBase64String(dataProtector.Protect(Encoding.UTF8.GetBytes(plaintext))).TrimEnd('=').Replace('+', '-').Replace('/', '_'),
        unprotect: token => {
          var base64 = token.Replace('-', '+').Replace('_', '/');
          base64 += (base64.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
          return Encoding.UTF8.GetString(dataProtector.Unprotect(Convert.FromBase64String(base64)));
        }
    );
    return Task.CompletedTask;
  }

  private static Microsoft.AspNetCore.Http.HttpRequest CreateRequestWithCookie(AuthTicket ticket) {
    var cookie = ticket.ToCookie("/");
    var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
    context.Request.Headers["Cookie"] = $"{cookie.Name}={cookie.Value}";
    return context.Request;
  }

  private static UserInformation CreateUser() {
    return new UserInformation("S-1-5-21-1", "jdoe", "DOMAIN", "John Doe", []) { PasswordLastSet = null };
  }

  [Test]
  public async Task FromUserInformation_AssignsNewAuthSessionId() {
    var first = AuthTicket.FromUserInformation(CreateUser(), AuthTicketLevel.ReadOnlyUser);
    var second = AuthTicket.FromUserInformation(CreateUser(), AuthTicketLevel.ReadOnlyUser);

    await Assert.That(first.UserData.AuthSessionId).IsNotNull();
    await Assert.That(first.UserData.AuthSessionId).IsNotEqualTo(second.UserData.AuthSessionId);
  }

  [Test]
  public async Task Renew_KeepsAuthSessionId() {
    var ticket = AuthTicket.FromUserInformation(CreateUser(), AuthTicketLevel.ReadOnlyUser);

    var renewed = ticket.Renew(CreateUser(), DateTime.UtcNow.AddMonths(1));

    await Assert.That(renewed.UserData.AuthSessionId).IsEqualTo(ticket.UserData.AuthSessionId);
  }

  [Test]
  public async Task Renew_ThrowsForTicketWithoutAuthSessionId() {
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));

    await Assert.That(() => ticket.Renew(CreateUser(), DateTime.UtcNow.AddMonths(1))).Throws<InvalidOperationException>();
  }

  [Test]
  public async Task EncryptedToken_RoundTripPreservesAuthSessionId() {
    var ticket = AuthTicket.FromUserInformation(CreateUser(), AuthTicketLevel.ReadOnlyUser);

    var parsed = AuthTicket.FromEncryptedToken(ticket.ToEncryptedToken());

    await Assert.That(parsed.UserData.AuthSessionId).IsEqualTo(ticket.UserData.AuthSessionId);
  }

  [Test]
  public async Task FromHttpRequestCookie_RejectsTicketWhoseSessionWasNotStarted() {
    var ticket = AuthTicket.FromUserInformation(CreateUser(), AuthTicketLevel.ReadOnlyUser);

    await Assert.That(AuthTicket.FromHttpRequestCookie(CreateRequestWithCookie(ticket))).IsNull();
  }

  [Test]
  public async Task FromHttpRequestCookie_AcceptsTicketWithActiveSession() {
    var ticket = AuthTicket.FromUserInformation(CreateUser(), AuthTicketLevel.ReadOnlyUser);
    new AuthSessionStore().Add(ticket);

    var restored = AuthTicket.FromHttpRequestCookie(CreateRequestWithCookie(ticket));

    await Assert.That(restored).IsNotNull();
    await Assert.That(restored!.UserData.AuthSessionId).IsEqualTo(ticket.UserData.AuthSessionId);
  }

  [Test]
  public async Task FromHttpRequestCookie_RejectsTicketAfterSessionEnds() {
    var ticket = AuthTicket.FromUserInformation(CreateUser(), AuthTicketLevel.ReadOnlyUser);
    var store = new AuthSessionStore();
    store.Add(ticket);
    var request = CreateRequestWithCookie(ticket);

    store.Remove(ticket);

    await Assert.That(AuthTicket.FromHttpRequestCookie(request)).IsNull();
  }

  [Test]
  public async Task FromHttpRequestCookie_RejectsTicketWithoutAuthSessionIdFromOlderVersions() {
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));

    await Assert.That(AuthTicket.FromHttpRequestCookie(CreateRequestWithCookie(ticket))).IsNull();
  }
}
