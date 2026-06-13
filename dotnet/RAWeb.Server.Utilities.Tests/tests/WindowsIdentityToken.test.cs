using System.Security.Principal;

namespace RAWeb.Server.Utilities.Tests;

public class WindowsIdentityTokenTests {
  private static readonly WindowsIdentity s_currentIdentity = WindowsIdentity.GetCurrent();

  [Test]
  public async Task IsLocalAdministrator_DoesNotThrowForCurrentProcessToken() {
    // The actual result depends on whether the test runner is elevated
    // and whether the user is a member of the Administrators group,
    // so we just verify that there is no exception.
    WindowsIdentityToken.IsLocalAdministrator(s_currentIdentity.Token);
  }

  [Test]
  public async Task IsLocalAdministrator_ExtensionMatchesStaticMethodForCurrentIdentity() {
    var fromExtension = s_currentIdentity.IsLocalAdministrator;
    var fromStaticMethod = WindowsIdentityToken.IsLocalAdministrator(s_currentIdentity.Token);

    await Assert.That(fromExtension).IsEqualTo(fromStaticMethod);
  }
}
