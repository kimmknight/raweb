namespace RAWeb.Server.Utilities.Tests;

public class GroupInformationTests {
  [Test]
  public async Task EscapedDN_ReturnsNullWhenDnIsNull() {
    var group = new GroupInformation("GroupName", "S-1-5-32-544", null);

    await Assert.That(group.EscapedDN).IsNull();
  }

  [Test]
  public async Task EscapedDN_ReturnsNullWhenDnIsWhitespace() {
    var group = new GroupInformation("GroupName", "S-1-5-32-544", "   ");

    await Assert.That(group.EscapedDN).IsNull();
  }

  [Test]
  public async Task EscapedDN_EscapesAsterisk() {
    var group = new GroupInformation("GroupName", "S-1-5-32-544", "CN=test*group");

    await Assert.That(group.EscapedDN).IsEqualTo(@"CN=test\2Agroup");
  }

  [Test]
  public async Task EscapedDN_EscapesOpenParen() {
    var group = new GroupInformation("GroupName", "S-1-5-32-544", "CN=test(group");

    await Assert.That(group.EscapedDN).IsEqualTo(@"CN=test\28group");
  }

  [Test]
  public async Task EscapedDN_EscapesCloseParen() {
    var group = new GroupInformation("GroupName", "S-1-5-32-544", "CN=test)group");

    await Assert.That(group.EscapedDN).IsEqualTo(@"CN=test\29group");
  }

  [Test]
  public async Task EscapedDN_EscapesBackslash() {
    var group = new GroupInformation("GroupName", "S-1-5-32-544", @"CN=test\group");

    await Assert.That(group.EscapedDN).IsEqualTo(@"CN=test\5Cgroup");
  }

  [Test]
  public async Task EscapedDN_ReturnsUnchangedStringWithNoSpecialChars() {
    var group = new GroupInformation("GroupName", "S-1-5-32-544", "CN=TestGroup,DC=example,DC=com");

    await Assert.That(group.EscapedDN).IsEqualTo("CN=TestGroup,DC=example,DC=com");
  }

  [Test]
  public async Task EscapedDN_EscapesAllSpecialCharsInCombinedString() {
    var group = new GroupInformation("GroupName", "S-1-5-32-544", @"CN=test*(group)\name");

    await Assert.That(group.EscapedDN).IsEqualTo(@"CN=test\2A\28group\29\5Cname");
  }

  [Test]
  public async Task ResolveLocalizedGroupName_ReturnsGroupNameForValidSid() {
    var sid = "S-1-5-32-544"; // SID for Administrators group
    var groupName = GroupInformation.ResolveLocalizedGroupName(sid);

    await Assert.That(groupName).IsEqualTo("Administrators");
  }

  [Test]
  public async Task ResolveLocalizedGroupName_ReturnsSidStringForUnknownSid() {
    var invalidSid = "S-1-5-32-999"; // Unknown SID
    var groupName = GroupInformation.ResolveLocalizedGroupName(invalidSid);

    await Assert.That(groupName).IsEqualTo(invalidSid);
  }

  [Test]

  public async Task GroupInformation_PrefersManuallySpecifiedNameOverResolvedName() {
    var sid = "S-1-5-32-544"; // SID for Administrators group
    var manualName = "CustomGroupName";
    var group = new GroupInformation(manualName, sid);

    await Assert.That(group.Name).IsEqualTo(manualName);
  }
}
