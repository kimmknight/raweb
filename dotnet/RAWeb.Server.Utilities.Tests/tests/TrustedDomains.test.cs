namespace RAWeb.Server.Utilities.Tests;

public class TrustedDomainsTests {
  [Test]
  [Arguments(null)]
  [Arguments("")]
  [Arguments(" ")]
  [Arguments(".")]
  public async Task IsAllowedLogonDomain_AllowsLocalMachineValues(string? domain) {
    await Assert.That(TrustedDomains.IsAllowedLogonDomain(domain)).IsTrue();
  }

  [Test]
  public async Task IsAllowedLogonDomain_AllowsMachineNameInAnyCase() {
    await Assert.That(TrustedDomains.IsAllowedLogonDomain(Environment.MachineName)).IsTrue();
    await Assert.That(TrustedDomains.IsAllowedLogonDomain(Environment.MachineName.ToLowerInvariant())).IsTrue();
  }

  [Test]
  public async Task IsAllowedLogonDomain_AllowsDefaultSignInDomain() {
    await Assert.That(TrustedDomains.IsAllowedLogonDomain(SignIn.GetDomainName())).IsTrue();
  }

  [Test]
  [Arguments("attacker.example")]
  [Arguments("10.0.0.5")]
  [Arguments("\\\\attacker.example")]
  public async Task IsAllowedLogonDomain_RejectsUnknownDomains(string domain) {
    await Assert.That(TrustedDomains.IsAllowedLogonDomain(domain)).IsFalse();
  }

  [Test]
  public async Task IsAllowedLogonDomain_MatchesAllowedNamesIgnoringCaseAndTrailingDot() {
    var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CONTOSO", "contoso.com" };

    await Assert.That(TrustedDomains.IsAllowedLogonDomain("contoso", allowed)).IsTrue();
    await Assert.That(TrustedDomains.IsAllowedLogonDomain("Contoso.COM.", allowed)).IsTrue();
    await Assert.That(TrustedDomains.IsAllowedLogonDomain(" contoso.com ", allowed)).IsTrue();
  }

  [Test]
  public async Task IsAllowedLogonDomain_RejectsNamesThatOnlyResembleAllowedNames() {
    var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "contoso.com" };

    await Assert.That(TrustedDomains.IsAllowedLogonDomain("evilcontoso.com", allowed)).IsFalse();
    await Assert.That(TrustedDomains.IsAllowedLogonDomain("contoso.com.attacker.example", allowed)).IsFalse();
    await Assert.That(TrustedDomains.IsAllowedLogonDomain("sub.contoso.com", allowed)).IsFalse();
  }

  [Test]
  [Arguments("contoso.com.", "contoso.com")]
  [Arguments(" contoso ", "contoso")]
  [Arguments(".", ".")]
  public async Task NormalizeDomainName_TrimsWhitespaceAndTrailingDot(string input, string expected) {
    await Assert.That(TrustedDomains.NormalizeDomainName(input)).IsEqualTo(expected);
  }

  [Test]
  public async Task ValidateCredentials_RejectsUnknownDomainBeforeContactingIt() {
    var exception = Assert.Throws<ValidateCredentialsException>(
      () => SignIn.ValidateCredentials("user", "password", "attacker.example")
    );

    await Assert.That(exception.Message).IsEqualTo("login.server.unfoundDomain");
  }

  [Test]
  public async Task ChangeCredentials_RejectsUnknownDomainBeforeContactingIt() {
    var (success, error) = NetUserInformation.ChangeCredentials("user", "old", "new", "attacker.example");

    await Assert.That(success).IsFalse();
    await Assert.That(error).IsNotNull();
  }
}
