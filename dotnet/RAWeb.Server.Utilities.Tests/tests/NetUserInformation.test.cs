using System.Security.Principal;

namespace RAWeb.Server.Utilities.Tests;

public class NetUserInformationTests {
  // Resolve the current user once for all tests to avoid repeated calls to WindowsIdentity.GetCurrent().
  private static readonly WindowsIdentity s_currentIdentity = WindowsIdentity.GetCurrent();
  private static readonly string s_currentUserSid = s_currentIdentity.User!.Value;
  private static readonly string s_currentUserName = Environment.UserName;

  [Test]
  public async Task GetSidFromAccountName_ReturnsNullForNonExistentUser() {
    var sid = NetUserInformation.GetSidFromAccountName("nonexistent_user_xyz_12345");

    await Assert.That(sid).IsNull();
  }

  [Test]
  public async Task GetSidFromAccountName_ReturnsNonNullForCurrentUser() {
    var sid = NetUserInformation.GetSidFromAccountName(s_currentUserName);

    await Assert.That(sid).IsNotNull();
  }

  [Test]
  public async Task GetSidFromAccountName_ReturnedSidMatchesCurrentUserSid() {
    var sid = NetUserInformation.GetSidFromAccountName(s_currentUserName);

    await Assert.That(sid).IsNotNull();
    await Assert.That(sid!.Value).IsEqualTo(s_currentUserSid);
  }

  [Test]
  public async Task GetSidFromAccountName_ReturnedSidIsValidSecurityIdentifier() {
    var sid = NetUserInformation.GetSidFromAccountName(s_currentUserName);

    await Assert.That(sid).IsNotNull();
    await Assert.That(sid!.IsAccountSid()).IsTrue();
  }

  [Test]
  public async Task GetFullName_ThrowsForNonExistentUser() {
    await Assert.ThrowsAsync<System.ComponentModel.Win32Exception>(() => Task.Run(() => {
      NetUserInformation.GetFullName(null, "nonexistent_xyz_12345");
    }));
  }

  [Test]
  public async Task GetFullName_ReturnsStringForCurrentUser() {
    var fullName = NetUserInformation.GetFullName(null, s_currentUserName);

    // Full name may be empty if not configured, but it will always be a string
    await Assert.That(fullName).IsNotNull();
  }

  [Test]
  public async Task ChangeCredentials_ReturnsFalseAndErrorMessageForBadCredentials() {
    var (success, error) = NetUserInformation.ChangeCredentials("nonexistent_user_xyz", "wrongpwd", "newpwd", ".");

    await Assert.That(success).IsFalse();
    await Assert.That(error).IsNotNull();
  }

  [Test]
  public async Task ChangeCredentials_ErrorMessageIsNonEmpty() {
    var (_, error) = NetUserInformation.ChangeCredentials("nonexistent_user_xyz", "wrongpwd", "newpwd", ".");

    await Assert.That(string.IsNullOrEmpty(error)).IsFalse();
  }

  [Test]
  public async Task ChangeCredentials_ReturnsFalseForNonExistentLocalUser() {
    var (success, error) = NetUserInformation.ChangeCredentials("nonexistent_user_xyz", "wrongpwd", "newpwd", Environment.MachineName);

    await Assert.That(success).IsFalse();
    await Assert.That(error).IsNotNull();
  }

  [Test]
  public async Task IsUserLocalAdministrator_DoesNotThrowForCurrentUserSid() {
    // The actual result depends on whether the test runner is elevated,
    // so we just verify that there is no exception.
    NetUserInformation.IsUserLocalAdministrator(s_currentUserSid);
  }

  [Test]
  public async Task IsUserLocalUser_DoesNotThrowForCurrentUserSid() {
    NetUserInformation.IsUserLocalUser(s_currentUserSid);
  }

  [Test]
  public async Task IsUserLocalAdministrator_ReturnsFalseForNonExistentSid() {
    var result = NetUserInformation.IsUserLocalAdministrator("S-1-5-21-1234567890-1234567890-1234567890-9999");

    await Assert.That(result).IsFalse();
  }

  [Test]
  public async Task IsUserLocalUser_ReturnsFalseForNonExistentSid() {
    var result = NetUserInformation.IsUserLocalUser("S-1-5-21-1234567890-1234567890-1234567890-9999");

    await Assert.That(result).IsFalse();
  }

  [Test]
  public async Task GetLocalGroupMemberships_ReturnsNonNullForCurrentUser() {
    var groups = NetUserInformation.GetLocalGroupMemberships(s_currentIdentity.User!);

    await Assert.That(groups).IsNotNull();
  }

  [Test]
  public async Task GetLocalGroupMemberships_ReturnsEmptyArrayForNonExistentUserSid() {
    var fakeSid = new SecurityIdentifier("S-1-5-21-1234567890-1234567890-1234567890-9999");

    var groups = NetUserInformation.GetLocalGroupMemberships(fakeSid);

    await Assert.That(groups.Length).IsEqualTo(0);
  }

  [Test]
  public async Task GetLocalGroupMemberships_EachGroupHasNonEmptyNameAndSid() {
    var groups = NetUserInformation.GetLocalGroupMemberships(s_currentIdentity.User!);

    foreach (var group in groups) {
      await Assert.That(string.IsNullOrEmpty(group.Name)).IsFalse();
      await Assert.That(string.IsNullOrEmpty(group.Sid)).IsFalse();
    }
  }

  [Test]
  public async Task GetLocalGroupMemberships_EachGroupSidIsValidFormat() {
    var groups = NetUserInformation.GetLocalGroupMemberships(s_currentIdentity.User!);

    foreach (var group in groups) {
      await Assert.That(group.Sid.StartsWith("S-")).IsTrue();
    }
  }
}
