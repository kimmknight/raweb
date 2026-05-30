using System.Diagnostics;

namespace RAWeb.Server.Utilities.Tests;

public class SignInTests {
  [Test]
  public async Task ErrorCodes_HaveExpectedWindowsValues() {
    var logonFailure = SignIn.ERROR_LOGON_FAILURE;
    var accountRestriction = SignIn.ERROR_ACCOUNT_RESTRICTION;
    var invalidLogonHours = SignIn.ERROR_INVALID_LOGON_HOURS;
    var invalidWorkstation = SignIn.ERROR_INVALID_WORKSTATION;
    var passwordExpired = SignIn.ERROR_PASSWORD_EXPIRED;
    var accountDisabled = SignIn.ERROR_ACCOUNT_DISABLED;
    var passwordMustChange = SignIn.ERROR_PASSWORD_MUST_CHANGE;

    await Assert.That(logonFailure).IsEqualTo(1326);
    await Assert.That(accountRestriction).IsEqualTo(1327);
    await Assert.That(invalidLogonHours).IsEqualTo(1328);
    await Assert.That(invalidWorkstation).IsEqualTo(1329);
    await Assert.That(passwordExpired).IsEqualTo(1330);
    await Assert.That(accountDisabled).IsEqualTo(1331);
    await Assert.That(passwordMustChange).IsEqualTo(1907);
  }

  [Test]
  public async Task GetDomainName_ReturnsNonNullNonEmptyString() {
    var domain = SignIn.GetDomainName();

    await Assert.That(domain).IsNotNull();
    await Assert.That(domain).IsNotEqualTo("");
  }

  [Test]
  public async Task GetDomainName_ReturnsMachineNameOrDomainName() {
    var domain = SignIn.GetDomainName();

    var isMachineName = domain.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
    var looksDomainLike = domain.Contains('.');
    await Assert.That(isMachineName || looksDomainLike).IsTrue();
  }

  [Test]
  public async Task ValidateCredentials_BadLocalCredentials_ThrowsValidateCredentialsException() {
    await Assert.ThrowsAsync<ValidateCredentialsException>(() => Task.Run(() => {
      SignIn.ValidateCredentials("nonexistent_user_xyz_12345", "wrongpassword", ".");
    }));
  }

  [Test]
  public async Task ValidateCredentials_BadLocalCredentials_MessageIsIncorrectUsernameOrPassword() {
    ValidateCredentialsException? caught = null;
    try {
      SignIn.ValidateCredentials("nonexistent_user_xyz_12345", "wrongpassword", ".");
    }
    catch (ValidateCredentialsException ex) {
      caught = ex;
    }

    await Assert.That(caught).IsNotNull();
    await Assert.That(caught!.Message).IsEqualTo("login.incorrectUsernameOrPassword");
  }

  [Test]
  public async Task ValidateCredentials_EmptyDomain_IsTreatedAsLocalAccount() {
    await Assert.ThrowsAsync<ValidateCredentialsException>(() => Task.Run(() => {
      SignIn.ValidateCredentials("nonexistent_user_xyz_12345", "wrongpassword", "");
    }));
  }

  [Test]
  public async Task ValidateCredentials_EmptyDomain_MessageIsIncorrectUsernameOrPassword() {
    ValidateCredentialsException? caught = null;
    try {
      SignIn.ValidateCredentials("nonexistent_user_xyz_12345", "wrongpassword", "");
    }
    catch (ValidateCredentialsException ex) {
      caught = ex;
    }

    await Assert.That(caught).IsNotNull();
    await Assert.That(caught!.Message).IsEqualTo("login.incorrectUsernameOrPassword");
  }

  [Test]
  public async Task ValidateCredentials_MachineName_IsTreatedAsLocalAccount() {
    await Assert.ThrowsAsync<ValidateCredentialsException>(() => Task.Run(() => {
      SignIn.ValidateCredentials("nonexistent_user_xyz_12345", "wrongpassword", Environment.MachineName);
    }));
  }

  [Test]
  public async Task ValidateCredentials_MachineName_MessageIsIncorrectUsernameOrPassword() {
    ValidateCredentialsException? caught = null;
    try {
      SignIn.ValidateCredentials("nonexistent_user_xyz_12345", "wrongpassword", Environment.MachineName);
    }
    catch (ValidateCredentialsException ex) {
      caught = ex;
    }

    await Assert.That(caught).IsNotNull();
    await Assert.That(caught!.Message).IsEqualTo("login.incorrectUsernameOrPassword");
  }

  [Test]
  public async Task ValidateCredentials_BadDomain_ThrowsValidateCredentialsException() {
    await Assert.ThrowsAsync<ValidateCredentialsException>(() => Task.Run(() => {
      SignIn.ValidateCredentials("user", "pass", "nonexistent_domain_xyz_12345");
    }));
  }

  [Test]
  public async Task ValidateCredentials_BadDomain_MessageIsUnfoundDomain() {
    ValidateCredentialsException? caught = null;
    try {
      SignIn.ValidateCredentials("user", "pass", "nonexistent_domain_xyz_12345");
    }
    catch (ValidateCredentialsException ex) {
      caught = ex;
    }

    await Assert.That(caught).IsNotNull();
    await Assert.That(caught!.Message).IsEqualTo("login.server.unfoundDomain");
  }

  [Test]
  public async Task ValidateCredentials_ValidLocalCredentials_ReturnsNonInvalidToken() {
    await Assert.That(Management.ElevatedPrivileges.Check()).IsTrue()
        .Because("creating a temporary local user requires elevated privileges");

    Console.WriteLine("Creating temporary local user with cryptographically random password for testing credentials...");
    var username = "RAWebTest_" + Guid.NewGuid().ToString("N")[..8];
    var password = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)).Replace("/", "A").Replace("+", "B").Replace("=", "C");
    try {
      Console.WriteLine($"Creating temporary local user '{username}' for testing credentials.");

      var psi = new ProcessStartInfo {
        FileName = "powershell.exe",
        Arguments = $"-NoProfile -Command \"$secpwd = New-Object System.Security.SecureString; '{password}'.ToCharArray() | ForEach-Object {{ $secpwd.AppendChar($_) }}; New-LocalUser -Name '{username}' -Password $secpwd -PasswordNeverExpires\"",
        CreateNoWindow = true,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
      };

      var process = Process.Start(psi);
      if (process is null) {
        throw new Exception("Failed to start PowerShell process.");
      }

      Console.WriteLine($"Waiting for user creation to complete for '{username}'...");

      if (!process.WaitForExit(5000)) {
        process.Kill();
        throw new TimeoutException("PowerShell New-LocalUser command timed out");
      }

      var output = process.StandardOutput.ReadToEnd();
      var error = process.StandardError.ReadToEnd();
      if (!string.IsNullOrWhiteSpace(output)) {
        Console.WriteLine($"PowerShell output: {output}");
      }
      if (!string.IsNullOrWhiteSpace(error)) {
        Console.Error.WriteLine($"PowerShell error: {error}");
      }

      Console.WriteLine($"Created temporary local user '{username}' for testing credentials.");
      await Assert.That(process.ExitCode).IsEqualTo(0).Because("New-LocalUser must succeed before testing credentials");

      using var token = SignIn.ValidateCredentials(username, password, ".");

      await Assert.That(token.IsInvalid).IsFalse();
    }
    finally {
      Console.WriteLine($"Deleting temporary local user '{username}' after testing credentials.");
      var psi = new ProcessStartInfo {
        FileName = "powershell.exe",
        Arguments = $"-NoProfile -Command \"Remove-LocalUser -Name '{username}'\"",
        CreateNoWindow = true,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
      };

      var process = Process.Start(psi);
      if (process is not null) {
        if (!process.WaitForExit(5000)) {
          process.Kill();
          throw new TimeoutException("PowerShell Remove-LocalUser command timed out");
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        if (!string.IsNullOrWhiteSpace(output)) {
          Console.WriteLine($"PowerShell output: {output}");
        }
        if (!string.IsNullOrWhiteSpace(error)) {
          Console.Error.WriteLine($"PowerShell error: {error}");
        }

        Console.WriteLine($"Deleted temporary local user '{username}' after testing credentials.");
      }
      else {
        Console.Error.WriteLine($"Failed to start PowerShell process to delete temporary local user '{username}' after testing credentials.");
      }

    }
  }
}
