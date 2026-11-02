using System.Security.AccessControl;
using System.Security.Principal;
using RAWeb.Server.Management;

namespace RAWeb.Server.Utilities.Tests;

public class SecurityDescriptorCanReadTests {
  private static readonly SecurityIdentifier s_user = new("S-1-5-21-1000000000-2000000000-3000000000-1001");
  private static readonly SecurityIdentifier s_otherUser = new("S-1-5-21-1000000000-2000000000-3000000000-1002");
  private static readonly SecurityIdentifier s_domainUsers = new("S-1-5-21-1000000000-2000000000-3000000000-513");
  private static readonly SecurityIdentifier s_contractors = new("S-1-5-21-1000000000-2000000000-3000000000-2001");
  private static readonly SecurityIdentifier s_unrelatedGroup = new("S-1-5-21-1000000000-2000000000-3000000000-2002");

  private static readonly SecurityIdentifier[] s_userGroups = [s_domainUsers, s_contractors];

  /// <summary>
  /// Creates a security descriptor the same way the managed resource security dialog does.
  /// </summary>
  private static RawSecurityDescriptor FromDialog(SecurityIdentifier[] allowed, SecurityIdentifier[]? denied = null) {
    var dto = new SecurityDescriptionDTO(
      [.. allowed.Select(sid => sid.Value)],
      [.. (denied ?? []).Select(sid => sid.Value)]
    );
    return dto.ToRawSecurityDescriptor()!;
  }

  [Test]
  public async Task TrueWhenUserIsAllowed() {
    var descriptor = FromDialog(allowed: [s_user]);
    await Assert.That(descriptor.CanRead(s_user, s_userGroups)).IsTrue();
  }

  [Test]
  public async Task TrueWhenAGroupOfTheUserIsAllowed() {
    var descriptor = FromDialog(allowed: [s_domainUsers]);
    await Assert.That(descriptor.CanRead(s_user, s_userGroups)).IsTrue();
  }

  [Test]
  public async Task FalseWhenNeitherTheUserNorTheirGroupsAreAllowed() {
    var descriptor = FromDialog(allowed: [s_otherUser, s_unrelatedGroup]);
    await Assert.That(descriptor.CanRead(s_user, s_userGroups)).IsFalse();
  }

  [Test]
  public async Task FalseWhenThereAreNoEntries() {
    var descriptor = new RawSecurityDescriptor("D:");
    await Assert.That(descriptor.CanRead(s_user, s_userGroups)).IsFalse();
  }

  /// <summary>
  /// A user who is in an allowed group and a denied group must be denied
  /// (e.g., allow Domain Users and deny Contractors).
  /// </summary>
  [Test]
  public async Task FalseWhenAGroupOfTheUserIsDeniedAndAnotherGroupIsAllowed() {
    var descriptor = FromDialog(allowed: [s_domainUsers], denied: [s_contractors]);
    await Assert.That(descriptor.CanRead(s_user, s_userGroups)).IsFalse();
  }

  [Test]
  public async Task FalseWhenUserIsDeniedAndAGroupOfTheUserIsAllowed() {
    var descriptor = FromDialog(allowed: [s_domainUsers], denied: [s_user]);
    await Assert.That(descriptor.CanRead(s_user, s_userGroups)).IsFalse();
  }

  [Test]
  public async Task FalseWhenAGroupOfTheUserIsDeniedAndUserIsAllowed() {
    var descriptor = FromDialog(allowed: [s_user], denied: [s_contractors]);
    await Assert.That(descriptor.CanRead(s_user, s_userGroups)).IsFalse();
  }

  [Test]
  public async Task FalseWhenUserIsAllowedAndDenied() {
    var descriptor = FromDialog(allowed: [s_user], denied: [s_user]);
    await Assert.That(descriptor.CanRead(s_user, s_userGroups)).IsFalse();
  }

  [Test]
  public async Task TrueWhenOnlyAnUnrelatedGroupIsDenied() {
    var descriptor = FromDialog(allowed: [s_domainUsers], denied: [s_unrelatedGroup, s_otherUser]);
    await Assert.That(descriptor.CanRead(s_user, s_userGroups)).IsTrue();
  }

  /// <summary>
  /// Registry security descriptors may deny broader rights than ReadData
  /// (FR = FILE_GENERIC_READ, FA = FILE_ALL_ACCESS). These include ReadData, so they must also deny.
  /// </summary>
  [Test]
  [Arguments("FR")]
  [Arguments("FA")]
  public async Task FalseWhenAGroupOfTheUserIsDeniedBroaderRights(string deniedRights) {
    var descriptor = new RawSecurityDescriptor($"D:(D;;{deniedRights};;;{s_contractors})(A;;FR;;;{s_domainUsers})");
    await Assert.That(descriptor.CanRead(s_user, s_userGroups)).IsFalse();
  }

  /// <summary>
  /// Denying a right that does not include ReadData (FW = FILE_GENERIC_WRITE) must not deny reading.
  /// </summary>
  [Test]
  public async Task TrueWhenAGroupOfTheUserIsOnlyDeniedWriteAccess() {
    var descriptor = new RawSecurityDescriptor($"D:(D;;FW;;;{s_contractors})(A;;FR;;;{s_domainUsers})");
    await Assert.That(descriptor.CanRead(s_user, s_userGroups)).IsTrue();
  }

  [Test]
  public async Task FalseWhenUserIsOnlyAllowedWriteAccess() {
    var descriptor = new RawSecurityDescriptor($"D:(A;;FW;;;{s_user})");
    await Assert.That(descriptor.CanRead(s_user, s_userGroups)).IsFalse();
  }
}
