namespace RAWeb.Server.Utilities.Tests;

public class UserInformationTests {
  [Test]
  public async Task IsAnonymousUser_TrueForAnonymousSid() {
    var user = new UserInformation("S-1-4-447-1", "anonymous", "RAWEB");

    await Assert.That(user.IsAnonymousUser).IsTrue();
  }

  [Test]
  public async Task IsAnonymousUser_FalseForNormalSid() {
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN");

    await Assert.That(user.IsAnonymousUser).IsFalse();
  }

  [Test]
  public async Task IsRemoteDesktopUser_TrueWhenGroupContainsRDUsersSid() {
    GroupInformation[] groups = [new("Remote Desktop Users", "S-1-5-32-555")];
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", null, groups);

    await Assert.That(user.IsRemoteDesktopUser).IsTrue();
  }

  [Test]
  public async Task IsRemoteDesktopUser_TrueWhenRDUsersIsOneOfManyGroups() {
    GroupInformation[] groups = [
      new("Users", "S-1-5-32-545"),
      new("Remote Desktop Users", "S-1-5-32-555"),
      new("Guests", "S-1-5-32-546"),
    ];
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", null, groups);

    await Assert.That(user.IsRemoteDesktopUser).IsTrue();
  }

  [Test]
  public async Task IsRemoteDesktopUser_FalseWhenNoGroupsPresent() {
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN");

    await Assert.That(user.IsRemoteDesktopUser).IsFalse();
  }

  [Test]
  public async Task IsRemoteDesktopUser_FalseWhenGroupsExistButNoneAreRDUsers() {
    GroupInformation[] groups = [new("Administrators", "S-1-5-32-544")];
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", null, groups);

    await Assert.That(user.IsRemoteDesktopUser).IsFalse();
  }

  [Test]
  public async Task IsLocalAdministrator_TrueWhenGroupContainsAdmin() {
    GroupInformation[] groups = [new("Administrators", "S-1-5-32-544")];
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", null, groups);

    await Assert.That(user.IsLocalAdministrator).IsTrue();
  }

  [Test]
  public async Task IsLocalAdministrator_TrueWhenAdminIsOneOfManyGroups() {
    GroupInformation[] groups = [
      new("Remote Desktop Users", "S-1-5-32-555"),
      new("Administrators", "S-1-5-32-544"),
    ];
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", null, groups);

    await Assert.That(user.IsLocalAdministrator).IsTrue();
  }

  [Test]
  public async Task IsLocalAdministrator_FalseWhenNoGroupsPresent() {
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN");

    await Assert.That(user.IsLocalAdministrator).IsFalse();
  }

  [Test]
  public async Task IsLocalAdministrator_FalseWhenGroupsExistButNoneAreAdmin() {
    GroupInformation[] groups = [new("Remote Desktop Users", "S-1-5-32-555")];
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", null, groups);

    await Assert.That(user.IsLocalAdministrator).IsFalse();
  }

  [Test]
  public async Task FullName_DefaultsToUsernameWhenNotProvided() {
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN");

    await Assert.That(user.FullName).IsEqualTo("jdoe");
  }

  [Test]
  public async Task FullName_DefaultsToUsernameWhenNullIsPassed() {
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", null, []);

    await Assert.That(user.FullName).IsEqualTo("jdoe");
  }

  [Test]
  public async Task FullName_DefaultsToUsernameWhenEmptyStringIsPassed() {
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", "", []);

    await Assert.That(user.FullName).IsEqualTo("jdoe");
  }

  [Test]
  public async Task FullName_UsesProvidedValueWhenNonEmpty() {
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", "John Doe", []);

    await Assert.That(user.FullName).IsEqualTo("John Doe");
  }

  [Test]
  public async Task Groups_ReturnsSuppliedArray() {
    GroupInformation[] groups = [
      new("Administrators", "S-1-5-32-544"),
      new("Users", "S-1-5-32-545"),
    ];
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", null, groups);

    await Assert.That(user.Groups.Length).IsEqualTo(2);
    await Assert.That(user.Groups[0].Sid).IsEqualTo(groups[0].Sid);
    await Assert.That(user.Groups[1].Sid).IsEqualTo(groups[1].Sid);
  }

  [Test]
  public async Task Groups_EmptyForShortConstructor() {
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN");

    await Assert.That(user.Groups.Length).IsEqualTo(0);
  }

  [Test]
  public async Task ToString_ContainsUsernameAndDomain() {
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN");
    var str = user.ToString();

    await Assert.That(str.Contains("Username: jdoe")).IsTrue();
    await Assert.That(str.Contains("Domain: DOMAIN")).IsTrue();
  }

  [Test]
  public async Task ToString_SaysNoneWhenNoGroups() {
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN");

    await Assert.That(user.ToString().Contains("Groups: None")).IsTrue();
  }

  [Test]
  public async Task ToString_ListsGroupNamesWhenGroupsPresent() {
    GroupInformation[] groups = [new("Administrators", "S-1-5-32-544")];
    var user = new UserInformation("S-1-5-21-1000", "jdoe", "DOMAIN", null, groups);

    await Assert.That(user.ToString().Contains("\n  - Administrators")).IsTrue();
  }

  [Test]
  public async Task AnonymousUser_HasCorrectSid() {
    await Assert.That(UserInformation.AnonymousUser.Sid).IsEqualTo("S-1-4-447-1");
  }

  [Test]
  public async Task AnonymousUser_IsAnonymousUser() {
    await Assert.That(UserInformation.AnonymousUser.IsAnonymousUser).IsTrue();
  }

  [Test]
  public async Task AnonymousUser_HasCorrectUsername() {
    await Assert.That(UserInformation.AnonymousUser.Username).IsEqualTo("anonymous");
  }

  [Test]
  public async Task AnonymousUser_HasCorrectDomain() {
    await Assert.That(UserInformation.AnonymousUser.Domain).IsEqualTo("RAWEB");
  }

  [Test]
  public async Task AnonymousUser_FullNameIsAnonymousUser() {
    await Assert.That(UserInformation.AnonymousUser.FullName).IsEqualTo("Anonymous User");
  }

  [Test]
  public async Task AnonymousUser_GroupsContainEveryone() {
    var everyone = UserInformation.AnonymousUser.Groups.FirstOrDefault(g => g.Sid == "S-1-1-0");

    await Assert.That(everyone).IsNotNull();
  }

  [Test]
  public async Task AnonymousUser_GroupsContainAuthenticatedUsers() {
    var authenticatedUsers = UserInformation.AnonymousUser.Groups.FirstOrDefault(g => g.Sid == "S-1-5-11");

    await Assert.That(authenticatedUsers).IsNotNull();
  }

  [Test]
  public async Task IncludedSpecialIdentityGroups_ContainsEveryone() {
    var everyone = UserInformation.IncludedSpecialIdentityGroups.FirstOrDefault(g => g.Sid == "S-1-1-0");

    await Assert.That(everyone).IsNotNull();
  }

  [Test]
  public async Task IncludedSpecialIdentityGroups_ContainsAuthenticatedUsers() {
    var authenticatedUsers = UserInformation.IncludedSpecialIdentityGroups.FirstOrDefault(g => g.Sid == "S-1-5-11");

    await Assert.That(authenticatedUsers).IsNotNull();
  }

  [Test]
  public async Task FromDownLevelLogonName_ThrowsForNull() {
    var action = () => UserInformation.FromDownLevelLogonName(null!);

    await Assert.That(action).ThrowsException();
  }

  [Test]
  public async Task FromDownLevelLogonName_ThrowsForInputWithoutBackslash() {
    var action = () => UserInformation.FromDownLevelLogonName("justausername");

    await Assert.That(action).ThrowsException();
  }

  [Test]
  public async Task FromDownLevelLogonName_ThrowsForEmptyUsernamePart() {
    var action = () => UserInformation.FromDownLevelLogonName(@"DOMAIN\");

    await Assert.That(action).ThrowsException();
  }

  [Test]
  public async Task FromDownLevelLogonName_ThrowsForEmptyDomainPart() {
    var action = () => UserInformation.FromDownLevelLogonName(@"\username");

    await Assert.That(action).ThrowsException();
  }

  [Test]
  public async Task FromDownLevelLogonName_ReturnsAnonymousUserForAnonymousAccount() {
    var user = UserInformation.FromDownLevelLogonName(@"RAWEB\anonymous");

    await Assert.That(user).IsNotNull();
    await Assert.That(user!.IsAnonymousUser).IsTrue();
  }

  [Test]
  public async Task FromDownLevelLogonName_ReturnsAnonymousUserForIISUser() {
    var user = UserInformation.FromDownLevelLogonName(@"NT AUTHORITY\IUSR");

    await Assert.That(user).IsNotNull();
    await Assert.That(user!.IsAnonymousUser).IsTrue();
  }

  [Test]
  public async Task FromDownLevelLogonName_ReturnsAnonymousUserForIisAppPoolRaweb() {
    var user = UserInformation.FromDownLevelLogonName(@"IIS APPPOOL\raweb");

    await Assert.That(user).IsNotNull();
    await Assert.That(user!.IsAnonymousUser).IsTrue();
  }

  [Test]
  public async Task FromDownLevelLogonName_ReturnsNullForNonExistentLocalUser() {
    var user = UserInformation.FromDownLevelLogonName($@"{Environment.MachineName}\nonexistent_xyz_12345");

    await Assert.That(user).IsNull();
  }
}
