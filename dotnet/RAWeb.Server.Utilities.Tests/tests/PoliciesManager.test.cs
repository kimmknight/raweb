using static RAWeb.Server.Utilities.PoliciesManager;

namespace RAWeb.Server.Utilities.Tests;

public class PoliciesDictionaryTests {
  [Test]
  public async Task Indexer_ReturnsNullForMissingKey() {
    var dict = new PoliciesDictionary();

    await Assert.That(dict["missing"]).IsNull();
  }

  [Test]
  public async Task Indexer_ReturnsValueForExistingKey() {
    var dict = new PoliciesDictionary(new Dictionary<string, string> { ["key"] = "value" });

    await Assert.That(dict["key"]).IsEqualTo("value");
  }

  [Test]
  public async Task LoginTCMfa_ReturnsNullWhenEnablementKeyMissing() {
    var dict = new PoliciesDictionary();

    await Assert.That(dict.LoginTCMfa).IsNull();
  }

  [Test]
  public async Task LoginTCMfa_ReturnsNullWhenEnabledIsFalse() {
    var dict = new PoliciesDictionary(new Dictionary<string, string> {
      ["App.Auth.MFA.LoginTC.Enabled"] = "false",
      ["App.Auth.MFA.LoginTC"] = "id:secret@host@DOMAIN1"
    });

    await Assert.That(dict.LoginTCMfa).IsNull();
  }

  [Test]
  public async Task LoginTCMfa_ParsesSingleConnectionString() {
    var dict = new PoliciesDictionary(new Dictionary<string, string> {
      ["App.Auth.MFA.LoginTC.Enabled"] = "true",
      ["App.Auth.MFA.LoginTC"] = "myClientId:mySecret@login.example.com@DOMAIN1,DOMAIN2"
    });

    var result = dict.LoginTCMfa;

    await Assert.That(result).IsNotNull();
    await Assert.That(result!.Length).IsEqualTo(1);
    await Assert.That(result[0].ClientId).IsEqualTo("myClientId");
    await Assert.That(result[0].SecretKey).IsEqualTo("mySecret");
    await Assert.That(result[0].Hostname).IsEqualTo("login.example.com");
    await Assert.That(result[0].Domains.Length).IsEqualTo(2);
    await Assert.That(result[0].Domains[0]).IsEqualTo("DOMAIN1");
    await Assert.That(result[0].Domains[1]).IsEqualTo("DOMAIN2");
    await Assert.That(result[0].RedirectPath).IsEqualTo("/api/auth/logintc/callback");
  }

  [Test]
  public async Task LoginTCMfa_ParsesMultipleConnectionStrings() {
    var dict = new PoliciesDictionary(new Dictionary<string, string> {
      ["App.Auth.MFA.LoginTC.Enabled"] = "true",
      ["App.Auth.MFA.LoginTC"] = "id1:secret1@host1@DOMAIN1;id2:secret2@host2@DOMAIN2"
    });

    var result = dict.LoginTCMfa;

    await Assert.That(result).IsNotNull();
    await Assert.That(result!.Length).IsEqualTo(2);
    await Assert.That(result[0].ClientId).IsEqualTo("id1");
    await Assert.That(result[1].ClientId).IsEqualTo("id2");
  }

  [Test]
  public async Task LoginTCMfa_SkipsInvalidConnectionStrings() {
    var dict = new PoliciesDictionary(new Dictionary<string, string> {
      ["App.Auth.MFA.LoginTC.Enabled"] = "true",
      ["App.Auth.MFA.LoginTC"] = "invalid;id1:secret1@host1@DOMAIN1"
    });

    var result = dict.LoginTCMfa;

    await Assert.That(result).IsNotNull();
    await Assert.That(result!.Length).IsEqualTo(1);
    await Assert.That(result[0].ClientId).IsEqualTo("id1");
  }

  [Test]
  public async Task LoginTCMfa_HandlesMissingDomainsPart() {
    var dict = new PoliciesDictionary(new Dictionary<string, string> {
      ["App.Auth.MFA.LoginTC.Enabled"] = "true",
      ["App.Auth.MFA.LoginTC"] = "myClientId:mySecret@login.example.com"
    });

    var result = dict.LoginTCMfa;

    await Assert.That(result).IsNotNull();
    await Assert.That(result!.Length).IsEqualTo(1);
    await Assert.That(result[0].Domains.Length).IsEqualTo(0);
  }

  [Test]
  public async Task DuoMfa_ReturnsNullWhenNotEnabled() {
    var dict = new PoliciesDictionary();

    await Assert.That(dict.DuoMfa).IsNull();
  }

  [Test]
  public async Task DuoMfa_ParsesSingleConnectionString() {
    var dict = new PoliciesDictionary(new Dictionary<string, string> {
      ["App.Auth.MFA.Duo.Enabled"] = "true",
      ["App.Auth.MFA.Duo"] = "duoClientId:duoSecret@api.duosecurity.com@DOMAIN1"
    });

    var result = dict.DuoMfa;

    await Assert.That(result).IsNotNull();
    await Assert.That(result!.Length).IsEqualTo(1);
    await Assert.That(result[0].ClientId).IsEqualTo("duoClientId");
    await Assert.That(result[0].SecretKey).IsEqualTo("duoSecret");
    await Assert.That(result[0].Hostname).IsEqualTo("api.duosecurity.com");
    await Assert.That(result[0].Domains.Length).IsEqualTo(1);
    await Assert.That(result[0].Domains[0]).IsEqualTo("DOMAIN1");
    await Assert.That(result[0].RedirectPath).IsEqualTo("/api/auth/duo/callback");
  }

  [Test]
  public async Task DuoMfa_ParsesMultipleConnectionStrings() {
    var dict = new PoliciesDictionary(new Dictionary<string, string> {
      ["App.Auth.MFA.Duo.Enabled"] = "true",
      ["App.Auth.MFA.Duo"] = "id1:secret1@host1@DOMAIN1;id2:secret2@host2@DOMAIN2"
    });

    var result = dict.DuoMfa;

    await Assert.That(result).IsNotNull();
    await Assert.That(result!.Length).IsEqualTo(2);
    await Assert.That(result[0].ClientId).IsEqualTo("id1");
    await Assert.That(result[1].ClientId).IsEqualTo("id2");
  }
}

[NotInParallel]
public class PoliciesManagerTests {
  private string _tempFile = null!;

  [Before(Test)]
  public void SetupTempFile() {
    _tempFile = Path.GetTempFileName();
    File.Delete(_tempFile);
    _ = new PoliciesManager(_tempFile);
  }

  [After(Test)]
  public void CleanupTempFile() {
    if (File.Exists(_tempFile)) {
      File.Delete(_tempFile);
    }
  }

  [Test]
  public async Task RawPolicies_ReturnsEmptyDictionaryWhenFileDoesNotExist() {
    var policies = RawPolicies;

    await Assert.That(policies.Value.Count).IsEqualTo(0);
  }

  [Test]
  public async Task RawPolicies_ParsesKeyValuePairsFromExistingFile() {
    await File.WriteAllTextAsync(_tempFile, """
      <?xml version="1.0" encoding="utf-8" standalone="yes"?>
      <appSettings>
        <add key="key1" value="value1" />
        <add key="key2" value="value2" />
      </appSettings>
      """);

    var policies = RawPolicies;

    await Assert.That(policies["key1"]).IsEqualTo("value1");
    await Assert.That(policies["key2"]).IsEqualTo("value2");
  }

  [Test]
  public async Task Set_CreatesFileAndAddsKeyValue() {
    Set("myKey", "myValue");

    await Assert.That(File.Exists(_tempFile)).IsTrue();
    var policies = RawPolicies;
    await Assert.That(policies["myKey"]).IsEqualTo("myValue");
  }

  [Test]
  public async Task Set_UpdatesExistingKey() {
    Set("myKey", "initialValue");
    Set("myKey", "updatedValue");

    var policies = RawPolicies;
    await Assert.That(policies["myKey"]).IsEqualTo("updatedValue");
  }

  [Test]
  public async Task Set_RemovesKeyWhenValueIsEmpty() {
    Set("myKey", "someValue");
    Set("myKey", "");

    var policies = RawPolicies;
    await Assert.That(policies["myKey"]).IsNull();
  }

  [Test]
  public async Task Set_WritesMultipleKeysAlphabetically() {
    Set("zebra", "z");
    Set("apple", "a");
    Set("mango", "m");

    var content = await File.ReadAllTextAsync(_tempFile);
    var appleIndex = content.IndexOf("apple");
    var mangoIndex = content.IndexOf("mango");
    var zebraIndex = content.IndexOf("zebra");

    await Assert.That(appleIndex).IsLessThan(mangoIndex);
    await Assert.That(mangoIndex).IsLessThan(zebraIndex);
  }

  [Test]
  public async Task Remove_RemovesExistingKey() {
    Set("keyToRemove", "value");
    Remove("keyToRemove");

    var policies = RawPolicies;
    await Assert.That(policies["keyToRemove"]).IsNull();
  }

  [Test]
  public async Task Remove_DoesNotThrowWhenKeyDoesNotExist() {
    Remove("nonExistentKey");

    var policies = RawPolicies;
    await Assert.That(policies.Value.Count).IsEqualTo(0);
  }

  [Test]
  public async Task GetLoginTCMfaPolicyForDomain_ReturnsNullWhenNoPoliciesExist() {
    var result = GetLoginTCMfaPolicyForDomain("DOMAIN1");

    await Assert.That(result).IsNull();
  }

  [Test]
  public async Task GetLoginTCMfaPolicyForDomain_ReturnsNullWhenNoDomainMatch() {
    Set("App.Auth.MFA.LoginTC.Enabled", "true");
    Set("App.Auth.MFA.LoginTC", "id:secret@host@DOMAIN1");

    var result = GetLoginTCMfaPolicyForDomain("OTHER_DOMAIN");

    await Assert.That(result).IsNull();
  }

  [Test]
  public async Task GetLoginTCMfaPolicyForDomain_ReturnsExactDomainMatch() {
    Set("App.Auth.MFA.LoginTC.Enabled", "true");
    Set("App.Auth.MFA.LoginTC", "id:secret@host@DOMAIN1");

    var result = GetLoginTCMfaPolicyForDomain("DOMAIN1");

    await Assert.That(result).IsNotNull();
    await Assert.That(result!.ClientId).IsEqualTo("id");
  }

  [Test]
  public async Task GetLoginTCMfaPolicyForDomain_ReturnsWildcardMatchWhenNoExactMatch() {
    Set("App.Auth.MFA.LoginTC.Enabled", "true");
    Set("App.Auth.MFA.LoginTC", "id:secret@host@*");

    var result = GetLoginTCMfaPolicyForDomain("ANY_DOMAIN");

    await Assert.That(result).IsNotNull();
  }

  [Test]
  public async Task GetLoginTCMfaPolicyForDomain_PrefersExactMatchOverWildcard() {
    Set("App.Auth.MFA.LoginTC.Enabled", "true");
    Set("App.Auth.MFA.LoginTC", "wildId:wildSecret@wildHost@*;exactId:exactSecret@exactHost@DOMAIN1");

    var result = GetLoginTCMfaPolicyForDomain("DOMAIN1");

    await Assert.That(result).IsNotNull();
    await Assert.That(result!.ClientId).IsEqualTo("exactId");
  }

  [Test]
  public async Task GetLoginTCMfaPolicyForDomain_ReturnsNullWhenUsernameExcludedByDomainBackslashUsername() {
    Set("App.Auth.MFA.LoginTC.Enabled", "true");
    Set("App.Auth.MFA.LoginTC", "id:secret@host@DOMAIN1");
    Set("App.Auth.MFA.LoginTC.Excluded", "DOMAIN1\\jdoe");

    var result = GetLoginTCMfaPolicyForDomain("DOMAIN1", "jdoe");

    await Assert.That(result).IsNull();
  }

  [Test]
  public async Task GetLoginTCMfaPolicyForDomain_DomainExclusionMatchIsCaseInsensitive() {
    Set("App.Auth.MFA.LoginTC.Enabled", "true");
    Set("App.Auth.MFA.LoginTC", "id:secret@host@DOMAIN1");
    Set("App.Auth.MFA.LoginTC.Excluded", "domain1\\jdoe");

    var result = GetLoginTCMfaPolicyForDomain("DOMAIN1", "jdoe");

    await Assert.That(result).IsNull();
  }

  [Test]
  public async Task GetLoginTCMfaPolicyForDomain_UsernameExclusionMatchIsCaseSensitive() {
    Set("App.Auth.MFA.LoginTC.Enabled", "true");
    Set("App.Auth.MFA.LoginTC", "id:secret@host@DOMAIN1");
    Set("App.Auth.MFA.LoginTC.Excluded", "DOMAIN1\\jdoe");

    var result = GetLoginTCMfaPolicyForDomain("DOMAIN1", "JDOE");

    await Assert.That(result).IsNotNull();
  }

  [Test]
  public async Task GetLoginTCMfaPolicyForDomain_ReturnsNullWhenUsernameExcludedByDotNotation() {
    Set("App.Auth.MFA.LoginTC.Enabled", "true");
    Set("App.Auth.MFA.LoginTC", "id:secret@host@DOMAIN1");
    Set("App.Auth.MFA.LoginTC.Excluded", ".\\jdoe");

    var result = GetLoginTCMfaPolicyForDomain("DOMAIN1", "jdoe");

    await Assert.That(result).IsNull();
  }

  [Test]
  public async Task GetLoginTCMfaPolicyForDomain_DotNotationUsernameMatchIsCaseInsensitive() {
    Set("App.Auth.MFA.LoginTC.Enabled", "true");
    Set("App.Auth.MFA.LoginTC", "id:secret@host@DOMAIN1");
    Set("App.Auth.MFA.LoginTC.Excluded", ".\\JDOE");

    var result = GetLoginTCMfaPolicyForDomain("DOMAIN1", "jdoe");

    await Assert.That(result).IsNull();
  }

  [Test]
  public async Task GetLoginTCMfaPolicyForDomain_NonExcludedUserHasReturnedPolicy() {
    Set("App.Auth.MFA.LoginTC.Enabled", "true");
    Set("App.Auth.MFA.LoginTC", "id:secret@host@DOMAIN1");
    Set("App.Auth.MFA.LoginTC.Excluded", "DOMAIN1\\otherUser");

    var result = GetLoginTCMfaPolicyForDomain("DOMAIN1", "jdoe");

    await Assert.That(result).IsNotNull();
  }

  [Test]
  public async Task GetDuoMfaPolicyForDomain_ReturnsNullWhenNoPoliciesExist() {
    var result = GetDuoMfaPolicyForDomain("DOMAIN1");

    await Assert.That(result).IsNull();
  }

  [Test]
  public async Task GetDuoMfaPolicyForDomain_ReturnsNullWhenNoDomainMatch() {
    Set("App.Auth.MFA.Duo.Enabled", "true");
    Set("App.Auth.MFA.Duo", "id:secret@api.duosecurity.com@DOMAIN1");

    var result = GetDuoMfaPolicyForDomain("OTHER_DOMAIN");

    await Assert.That(result).IsNull();
  }

  [Test]
  public async Task GetDuoMfaPolicyForDomain_ReturnsExactDomainMatch() {
    Set("App.Auth.MFA.Duo.Enabled", "true");
    Set("App.Auth.MFA.Duo", "id:secret@api.duosecurity.com@DOMAIN1");

    var result = GetDuoMfaPolicyForDomain("DOMAIN1");

    await Assert.That(result).IsNotNull();
    await Assert.That(result!.Hostname).IsEqualTo("api.duosecurity.com");
  }

  [Test]
  public async Task GetDuoMfaPolicyForDomain_ReturnsWildcardMatchWhenNoExactMatch() {
    Set("App.Auth.MFA.Duo.Enabled", "true");
    Set("App.Auth.MFA.Duo", "id:secret@api.duosecurity.com@*");

    var result = GetDuoMfaPolicyForDomain("ANY_DOMAIN");

    await Assert.That(result).IsNotNull();
  }

  [Test]
  public async Task GetDuoMfaPolicyForDomain_PrefersExactMatchOverWildcard() {
    Set("App.Auth.MFA.Duo.Enabled", "true");
    Set("App.Auth.MFA.Duo", "wildId:wildSecret@wildHost@*;exactId:exactSecret@exactHost@DOMAIN1");

    var result = GetDuoMfaPolicyForDomain("DOMAIN1");

    await Assert.That(result).IsNotNull();
    await Assert.That(result!.ClientId).IsEqualTo("exactId");
  }

  [Test]
  public async Task GetDuoMfaPolicyForDomain_ReturnsNullWhenUsernameExcludedByDomainBackslashUsername() {
    Set("App.Auth.MFA.Duo.Enabled", "true");
    Set("App.Auth.MFA.Duo", "id:secret@api.duosecurity.com@DOMAIN1");
    Set("App.Auth.MFA.Duo.Excluded", "DOMAIN1\\jdoe");

    var result = GetDuoMfaPolicyForDomain("DOMAIN1", "jdoe");

    await Assert.That(result).IsNull();
  }

  [Test]
  public async Task GetDuoMfaPolicyForDomain_ReturnsNullWhenUsernameExcludedByDotNotation() {
    Set("App.Auth.MFA.Duo.Enabled", "true");
    Set("App.Auth.MFA.Duo", "id:secret@api.duosecurity.com@DOMAIN1");
    Set("App.Auth.MFA.Duo.Excluded", ".\\jdoe");

    var result = GetDuoMfaPolicyForDomain("DOMAIN1", "jdoe");

    await Assert.That(result).IsNull();
  }

  [Test]
  public async Task GetDuoMfaPolicyForDomain_NonExcludedUserIsNotBlocked() {
    Set("App.Auth.MFA.Duo.Enabled", "true");
    Set("App.Auth.MFA.Duo", "id:secret@api.duosecurity.com@DOMAIN1");
    Set("App.Auth.MFA.Duo.Excluded", "DOMAIN1\\otherUser");

    var result = GetDuoMfaPolicyForDomain("DOMAIN1", "jdoe");

    await Assert.That(result).IsNotNull();
  }
}
