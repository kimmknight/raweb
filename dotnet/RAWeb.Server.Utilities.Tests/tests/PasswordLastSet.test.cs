using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace RAWeb.Server.Utilities.Tests;

public class AuthTicketPasswordLastSetTests {
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

  private static readonly DateTime s_passwordSet = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

  private static AuthTicket CreateTicket(DateTime issueDate, DateTime? passwordLastSet) {
    return new AuthTicket(1, @"DOMAIN\jdoe", issueDate, issueDate.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser, passwordLastSet, AuthSessionStore.NewAuthSessionId()));
  }

  [Test]
  public async Task WasIssuedForOlderPassword_FalseWhenPasswordIsUnchanged() {
    var ticket = CreateTicket(s_passwordSet.AddDays(3), s_passwordSet);

    await Assert.That(ticket.WasIssuedForOlderPassword(s_passwordSet)).IsFalse();
  }

  [Test]
  public async Task WasIssuedForOlderPassword_TrueAfterPasswordChange() {
    var ticket = CreateTicket(s_passwordSet.AddDays(3), s_passwordSet);

    await Assert.That(ticket.WasIssuedForOlderPassword(s_passwordSet.AddDays(4))).IsTrue();
  }

  [Test]
  public async Task WasIssuedForOlderPassword_AllowsSmallDriftFromLocalAccounts() {
    var ticket = CreateTicket(s_passwordSet.AddDays(3), s_passwordSet);

    await Assert.That(ticket.WasIssuedForOlderPassword(s_passwordSet.AddSeconds(2))).IsFalse();
  }

  [Test]
  public async Task WasIssuedForOlderPassword_UsesStampInsteadOfIssueDate() {
    // the ticket was issued before the password-last-set time according to this server's clock,
    // but it recorded the same password, so a clock difference with the domain controller does not matter
    var ticket = CreateTicket(s_passwordSet.AddMinutes(-2), s_passwordSet);

    await Assert.That(ticket.WasIssuedForOlderPassword(s_passwordSet)).IsFalse();
  }

  [Test]
  public async Task WasIssuedForOlderPassword_FalseWhenCurrentValueIsUnknown() {
    var ticket = CreateTicket(s_passwordSet, s_passwordSet);

    await Assert.That(ticket.WasIssuedForOlderPassword(null)).IsFalse();
  }

  [Test]
  public async Task WasIssuedForOlderPassword_TrueWhenUserMustChangePassword() {
    var ticket = CreateTicket(s_passwordSet.AddDays(3), s_passwordSet);
    var mustChangePassword = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);

    await Assert.That(ticket.WasIssuedForOlderPassword(mustChangePassword)).IsTrue();
  }

  [Test]
  public async Task WasIssuedForOlderPassword_TicketWithoutStampUsesIssueDate() {
    var issuedAfterChange = CreateTicket(s_passwordSet.AddDays(1), null);
    var issuedBeforeChange = CreateTicket(s_passwordSet.AddDays(-1), null);

    await Assert.That(issuedAfterChange.WasIssuedForOlderPassword(s_passwordSet)).IsFalse();
    await Assert.That(issuedBeforeChange.WasIssuedForOlderPassword(s_passwordSet)).IsTrue();
  }

  [Test]
  public async Task EncryptedToken_RoundTripPreservesPasswordLastSet() {
    var ticket = CreateTicket(DateTime.UtcNow, s_passwordSet);

    var parsed = AuthTicket.FromEncryptedToken(ticket.ToEncryptedToken());

    await Assert.That(parsed.UserData.PasswordLastSet).IsEqualTo(s_passwordSet);
    await Assert.That(parsed.WasIssuedForOlderPassword(s_passwordSet)).IsFalse();
  }

  [Test]
  public async Task EncryptedToken_RoundTripWithoutPasswordLastSetIsNull() {
    var ticket = new AuthTicket(1, @"DOMAIN\jdoe", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30), false, new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser));

    var parsed = AuthTicket.FromEncryptedToken(ticket.ToEncryptedToken());

    await Assert.That(parsed.UserData.PasswordLastSet).IsNull();
    await Assert.That(parsed.UserData.Level).IsEqualTo(AuthTicketLevel.ReadOnlyUser);
  }

  [Test]
  public async Task FromUserInformation_RecordsPasswordLastSet() {
    var userInfo = new UserInformation("S-1-5-21-1", "jdoe", "DOMAIN", "John Doe", []) { PasswordLastSet = s_passwordSet };

    var ticket = AuthTicket.FromUserInformation(userInfo, AuthTicketLevel.ReadOnlyUser);

    await Assert.That(ticket.UserData.PasswordLastSet).IsEqualTo(s_passwordSet);
  }

  [Test]
  public async Task Renew_KeepsOldTicketsPasswordLastSet() {
    var oldTicket = CreateTicket(DateTime.UtcNow.AddMinutes(-10), s_passwordSet);
    var userInfo = new UserInformation("S-1-5-21-1", "jdoe", "DOMAIN", "John Doe", []) { PasswordLastSet = null };

    var renewed = oldTicket.Renew(userInfo, DateTime.UtcNow.AddMonths(1));

    await Assert.That(renewed.UserData.PasswordLastSet).IsEqualTo(s_passwordSet);
  }

  [Test]
  public async Task Renew_TicketWithoutPasswordLastSetGetsCurrentValue() {
    var oldTicket = CreateTicket(DateTime.UtcNow.AddMinutes(-10), null);
    var userInfo = new UserInformation("S-1-5-21-1", "jdoe", "DOMAIN", "John Doe", []) { PasswordLastSet = s_passwordSet };

    var renewed = oldTicket.Renew(userInfo, DateTime.UtcNow.AddMonths(1));

    await Assert.That(renewed.UserData.PasswordLastSet).IsEqualTo(s_passwordSet);
  }

  [Test]
  public async Task Renew_ExtendsExpirationAndKeepsUserAndLevel() {
    var oldTicket = CreateTicket(DateTime.UtcNow.AddMinutes(-10), s_passwordSet);
    var userInfo = new UserInformation("S-1-5-21-1", "jdoe", "DOMAIN", "John Doe", [], AuthTicketLevel.ReadOnlyUser) { PasswordLastSet = s_passwordSet };
    var expiration = DateTime.UtcNow.AddMonths(1);

    var renewed = oldTicket.Renew(userInfo, expiration);

    await Assert.That(renewed.Name).IsEqualTo(oldTicket.Name);
    await Assert.That(renewed.UserData.Level).IsEqualTo(AuthTicketLevel.ReadOnlyUser);
    await Assert.That(renewed.Expiration).IsEqualTo(expiration);
    await Assert.That(renewed.IssueDate).IsGreaterThan(oldTicket.IssueDate);
  }

  [Test]
  public async Task Renew_RejectsUserFromAnotherTicket() {
    var oldTicket = CreateTicket(DateTime.UtcNow.AddMinutes(-10), s_passwordSet);
    var otherUser = new UserInformation("S-1-5-21-2", "someoneelse", "DOMAIN", "Someone Else", []) { PasswordLastSet = s_passwordSet };

    await Assert.That(() => oldTicket.Renew(otherUser, DateTime.UtcNow.AddMonths(1))).Throws<ArgumentException>();
  }
}

public class UserCachePasswordLastSetTests {
  private string _dbName = "";

  [Before(Test)]
  public void Setup() {
    _dbName = "test_usercache_" + Guid.NewGuid().ToString("N")[..8];
  }

  [After(Test)]
  public void Cleanup() {
    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    var dbFilePath = Path.Combine(Constants.AppDataFolderPath, _dbName + ".sqlite");
    if (File.Exists(dbFilePath)) {
      File.Delete(dbFilePath);
    }
  }

  private static readonly DateTime s_passwordSet = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

  [Test]
  public async Task StoreUser_RoundTripPreservesPasswordLastSet() {
    var helper = new UserCacheDatabaseHelper(_dbName);
    helper.StoreUser(new UserInformation("S-1-5-21-1", "jdoe", "DOMAIN", "John Doe", []) { PasswordLastSet = s_passwordSet });

    var user = helper.GetUser(userSid: "S-1-5-21-1", maxAge: 0);

    await Assert.That(user!.PasswordLastSet).IsEqualTo(s_passwordSet);
  }

  [Test]
  public async Task StoreUser_WithoutPasswordLastSetKeepsCachedValue() {
    var helper = new UserCacheDatabaseHelper(_dbName);
    helper.StoreUser(new UserInformation("S-1-5-21-1", "jdoe", "DOMAIN", "John Doe", []) { PasswordLastSet = s_passwordSet });

    helper.StoreUser("S-1-5-21-1", "jdoe", "DOMAIN", "John Doe", []);
    var user = helper.GetUser(userSid: "S-1-5-21-1", maxAge: 0);

    await Assert.That(user!.PasswordLastSet).IsEqualTo(s_passwordSet);
  }

  [Test]
  public async Task StoreUser_WithNewPasswordLastSetReplacesCachedValue() {
    var helper = new UserCacheDatabaseHelper(_dbName);
    helper.StoreUser(new UserInformation("S-1-5-21-1", "jdoe", "DOMAIN", "John Doe", []) { PasswordLastSet = s_passwordSet });

    helper.StoreUser(new UserInformation("S-1-5-21-1", "jdoe", "DOMAIN", "John Doe", []) { PasswordLastSet = s_passwordSet.AddDays(1) });
    var user = helper.GetUser(userSid: "S-1-5-21-1", maxAge: 0);

    await Assert.That(user!.PasswordLastSet).IsEqualTo(s_passwordSet.AddDays(1));
  }

  [Test]
  public async Task StoreUser_PreservesMustChangePasswordValue() {
    var helper = new UserCacheDatabaseHelper(_dbName);
    var mustChangePassword = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
    helper.StoreUser(new UserInformation("S-1-5-21-1", "jdoe", "DOMAIN", "John Doe", []) { PasswordLastSet = mustChangePassword });

    var user = helper.GetUser(userSid: "S-1-5-21-1", maxAge: 0);

    await Assert.That(user!.PasswordLastSet).IsEqualTo(mustChangePassword);
  }
}

public class UserInformationPasswordLastSetTests {
  [Test]
  public async Task PasswordLastSet_IsLookedUpAutomaticallyForLocalAccount() {
    var userInfo = new UserInformation("S-1-5-21-1", Environment.UserName, Environment.MachineName, null, []);
    NetUserInformation.TryGetLocalPasswordLastSet(Environment.UserName, out var expected);

    await Assert.That(userInfo.PasswordLastSet).IsNotNull();
    await Assert.That((userInfo.PasswordLastSet!.Value - expected).Duration()).IsLessThan(TimeSpan.FromSeconds(2));
  }

  [Test]
  public async Task PasswordLastSet_IsOnlyLookedUpOnce() {
    var userInfo = new UserInformation("S-1-5-21-1", Environment.UserName, Environment.MachineName, null, []);

    var first = userInfo.PasswordLastSet;
    await Task.Delay(1100);
    var second = userInfo.PasswordLastSet;

    await Assert.That(second).IsEqualTo(first);
  }

  [Test]
  public async Task PasswordLastSet_SetValueIsUsedInsteadOfLookup() {
    var setValue = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
    var userInfo = new UserInformation("S-1-5-21-1", Environment.UserName, Environment.MachineName, null, []) { PasswordLastSet = setValue };

    await Assert.That(userInfo.PasswordLastSet).IsEqualTo(setValue);
  }

  [Test]
  public async Task PasswordLastSet_SetToNullIsKeptAsNull() {
    var userInfo = new UserInformation("S-1-5-21-1", Environment.UserName, Environment.MachineName, null, []) { PasswordLastSet = null };

    await Assert.That(userInfo.PasswordLastSet).IsNull();
  }

  [Test]
  public async Task PasswordLastSet_IsNullForAnonymousUser() {
    await Assert.That(UserInformation.AnonymousUser.PasswordLastSet).IsNull();
  }

  [Test]
  public async Task AuthTicket_FromUserInformation_RecordsLookedUpValue() {
    var userInfo = new UserInformation("S-1-5-21-1", Environment.UserName, Environment.MachineName, null, []);

    var ticket = AuthTicket.FromUserInformation(userInfo, AuthTicketLevel.ReadOnlyUser);

    await Assert.That(ticket.UserData.PasswordLastSet).IsNotNull();
    await Assert.That(ticket.UserData.PasswordLastSet).IsEqualTo(userInfo.PasswordLastSet);
  }
}

public class LocalPasswordLastSetTests {
  [Test]
  public async Task TryGetLocalPasswordLastSet_ReturnsPastTimeForCurrentUser() {
    var found = NetUserInformation.TryGetLocalPasswordLastSet(Environment.UserName, out var passwordLastSet);

    await Assert.That(found).IsTrue();
    await Assert.That(passwordLastSet).IsLessThanOrEqualTo(DateTime.UtcNow);
    await Assert.That(passwordLastSet).IsGreaterThan(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));
  }

  [Test]
  public async Task TryGetLocalPasswordLastSet_IsStableBetweenCalls() {
    NetUserInformation.TryGetLocalPasswordLastSet(Environment.UserName, out var first);
    NetUserInformation.TryGetLocalPasswordLastSet(Environment.UserName, out var second);

    await Assert.That((second - first).Duration()).IsLessThan(TimeSpan.FromSeconds(2));
  }

  [Test]
  public async Task TryGetLocalPasswordLastSet_ReturnsFalseForMissingUser() {
    var found = NetUserInformation.TryGetLocalPasswordLastSet("nonexistent_xyz_12345", out _);

    await Assert.That(found).IsFalse();
  }
}
