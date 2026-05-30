namespace RAWeb.Server.Utilities.Tests;

public class UserCacheDatabaseHelperTests {
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

  [Test]
  public async Task GetUser_ReturnsNullForMissingUser() {
    var helper = new UserCacheDatabaseHelper(_dbName);

    var user = helper.GetUser(userSid: "S-1-5-21-9999", maxAge: 0);

    await Assert.That(user).IsNull();
  }

  [Test]
  public async Task GetUser_ByUsernameAndDomain_ReturnsNullForMissingUser() {
    var helper = new UserCacheDatabaseHelper(_dbName);

    var user = helper.GetUser(username: "nobody_xyz", domain: "NODOMAIN", maxAge: 0);

    await Assert.That(user).IsNull();
  }

  [Test]
  public async Task StoreAndGetUser_RoundTripPreservesFieldsBySid() {
    var helper = new UserCacheDatabaseHelper(_dbName);
    var stored = new UserInformation("S-1-5-21-1234", "jdoe", "DOMAIN", "John Doe", []);
    helper.StoreUser(stored);

    var retrieved = helper.GetUser(userSid: "S-1-5-21-1234", maxAge: 0);

    await Assert.That(retrieved).IsNotNull();
    await Assert.That(retrieved!.Sid).IsEqualTo("S-1-5-21-1234");
    await Assert.That(retrieved.Username).IsEqualTo("jdoe");
    await Assert.That(retrieved.Domain).IsEqualTo("DOMAIN");
    await Assert.That(retrieved.FullName).IsEqualTo("John Doe");
  }

  [Test]
  public async Task StoreAndGetUser_RetrieveByUsernameAndDomain() {
    var helper = new UserCacheDatabaseHelper(_dbName);
    var stored = new UserInformation("S-1-5-21-1234", "jdoe", "DOMAIN");
    helper.StoreUser(stored);

    var retrieved = helper.GetUser(username: "jdoe", domain: "DOMAIN", maxAge: 0);

    await Assert.That(retrieved).IsNotNull();
    await Assert.That(retrieved!.Sid).IsEqualTo("S-1-5-21-1234");
  }

  [Test]
  public async Task StoreAndGetUser_GroupsArePreserved() {
    var helper = new UserCacheDatabaseHelper(_dbName);
    GroupInformation[] groups = [
      new("Administrators", "S-1-5-32-544"),
      new("Users", "S-1-5-32-545"),
    ];

    var stored = new UserInformation("S-1-5-21-1234", "jdoe", "DOMAIN", null, groups);
    helper.StoreUser(stored);

    var retrieved = helper.GetUser(userSid: "S-1-5-21-1234", maxAge: 0);

    await Assert.That(retrieved).IsNotNull();
    await Assert.That(retrieved!.Groups.Length).IsEqualTo(2);
  }

  [Test]
  public async Task StoreAndGetUser_GroupSidsMatchAfterRoundTrip() {
    var helper = new UserCacheDatabaseHelper(_dbName);
    GroupInformation[] groups = [new("Administrators", "S-1-5-32-544")];
    var stored = new UserInformation("S-1-5-21-1234", "jdoe", "DOMAIN", null, groups);
    helper.StoreUser(stored);

    var retrieved = helper.GetUser(userSid: "S-1-5-21-1234", maxAge: 0);

    await Assert.That(retrieved!.Groups[0].Sid).IsEqualTo("S-1-5-32-544");
  }

  [Test]
  public async Task StoreUser_UpdatesExistingUserOnConflict() {
    var helper = new UserCacheDatabaseHelper(_dbName);
    helper.StoreUser(new UserInformation("S-1-5-21-1234", "jdoe", "DOMAIN", "Old Name", []));
    helper.StoreUser(new UserInformation("S-1-5-21-1234", "jdoe", "DOMAIN", "New Name", []));

    var retrieved = helper.GetUser(userSid: "S-1-5-21-1234", maxAge: 0);

    await Assert.That(retrieved!.FullName).IsEqualTo("New Name");
  }

  [Test]
  public async Task StoreUser_UpdatedGroups_OldGroupsAreRemoved() {
    var helper = new UserCacheDatabaseHelper(_dbName);
    GroupInformation[] originalGroups = [
      new("Administrators", "S-1-5-32-544"),
      new("Users", "S-1-5-32-545"),
    ];
    helper.StoreUser(new UserInformation("S-1-5-21-1234", "jdoe", "DOMAIN", null, originalGroups));

    GroupInformation[] newGroups = [new("Users", "S-1-5-32-545")];
    helper.StoreUser(new UserInformation("S-1-5-21-1234", "jdoe", "DOMAIN", null, newGroups));

    var retrieved = helper.GetUser(userSid: "S-1-5-21-1234", maxAge: 0);

    await Assert.That(retrieved!.Groups.Length).IsEqualTo(1);
    await Assert.That(retrieved.Groups[0].Sid).IsEqualTo("S-1-5-32-545");
  }

  [Test]
  public async Task StoreUser_NullUserInfo_Throws() {
    var helper = new UserCacheDatabaseHelper(_dbName);

    var action = () => helper.StoreUser(null!);

    await Assert.That(action).ThrowsException();
  }

  [Test]
  public async Task GetUser_WithNoIdentifier_Throws() {
    var helper = new UserCacheDatabaseHelper(_dbName);

    var action = () => helper.GetUser(maxAge: 0);

    await Assert.That(action).ThrowsException();
  }

  [Test]
  public async Task GetUser_MaxAgeZero_ReturnsUserRegardlessOfAge() {
    var helper = new UserCacheDatabaseHelper(_dbName);
    helper.StoreUser(new UserInformation("S-1-5-21-1234", "jdoe", "DOMAIN"));

    // maxAge=0 disables the staleness check
    var retrieved = helper.GetUser(userSid: "S-1-5-21-1234", maxAge: 0);

    await Assert.That(retrieved).IsNotNull();
  }

  [Test]
  public async Task GetUser_MaxAgePositive_ReturnsUserWhenJustStored() {
    var helper = new UserCacheDatabaseHelper(_dbName);
    helper.StoreUser(new UserInformation("S-1-5-21-1234", "jdoe", "DOMAIN"));

    // user was just stored, so it is within the 1-hour maxAge window
    var retrieved = helper.GetUser(userSid: "S-1-5-21-1234", maxAge: 3600);

    await Assert.That(retrieved).IsNotNull();
  }

  [Test]
  public async Task GetUser_MaxAgePositive_ReturnsNullForStaleUser() {
    var helper = new UserCacheDatabaseHelper(_dbName);
    helper.StoreUser(new UserInformation("S-1-5-21-1234", "jdoe", "DOMAIN"));

    // wait 2 seconds so that the user is older than the 1-second maxAge window
    await Task.Delay(2000);
    var retrieved = helper.GetUser(userSid: "S-1-5-21-1234", maxAge: 1);

    await Assert.That(retrieved).IsNull();
  }

  [Test]
  public async Task GetUser_WithEmptyGroups_ReturnsUserWithNoGroups() {
    var helper = new UserCacheDatabaseHelper(_dbName);
    helper.StoreUser(new UserInformation("S-1-5-21-1234", "jdoe", "DOMAIN", null, []));

    var retrieved = helper.GetUser(userSid: "S-1-5-21-1234", maxAge: 0);

    await Assert.That(retrieved).IsNotNull();
    await Assert.That(retrieved!.Groups.Length).IsEqualTo(0);
  }

  [Test]
  public async Task StoreUser_MultipleDifferentUsers_AreStoredIndependently() {
    var helper = new UserCacheDatabaseHelper(_dbName);
    helper.StoreUser(new UserInformation("S-1-5-21-1111", "alice", "DOMAIN"));
    helper.StoreUser(new UserInformation("S-1-5-21-2222", "bob", "DOMAIN"));

    var alice = helper.GetUser(userSid: "S-1-5-21-1111", maxAge: 0);
    var bob = helper.GetUser(userSid: "S-1-5-21-2222", maxAge: 0);

    await Assert.That(alice!.Username).IsEqualTo("alice");
    await Assert.That(bob!.Username).IsEqualTo("bob");
  }
}
