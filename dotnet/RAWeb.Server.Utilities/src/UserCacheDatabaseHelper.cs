using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace RAWeb.Server.Utilities;

public sealed class UserCacheDatabaseHelper {
    private static bool s_schemaChecked = false; // shared across all instances
    private static readonly object s_schemaLock = new(); // thread safety
    private readonly string _dbPath;
    public UserCacheDatabaseHelper(string databaseName = "usercache") {
        var appDataPath = Constants.AppDataFolderPath;

        // ensure the App_Data directory exists
        if (!Directory.Exists(appDataPath)) {
            Directory.CreateDirectory(appDataPath);
        }

        // construct the database file pathy
        var dbFilePath = Path.Combine(appDataPath, string.Format("{0}.sqlite", databaseName));

        // construct the connection string
        _dbPath = $"Data Source={dbFilePath};";

        // only create the database file if it does not exist
        if (!File.Exists(dbFilePath)) {
            CreateTable();
        }

        // migrate the schema if needed
        if (s_schemaChecked)
            return;

        lock (s_schemaLock) {
            if (!s_schemaChecked) {
                EnsureUsersSchemaHasTimestamps();
                EnsureUsersSchemaHasPasswordLastSet();
                s_schemaChecked = true;
            }
        }
    }

    private void CreateTable() {
        using (var connection = new SqliteConnection(_dbPath)) {
            connection.Open();

            // create the users table to store basic user information for when the
            // domain cannot be reached
            var createUsersTableSql = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Sid TEXT PRIMARY KEY NOT NULL,
                    Username TEXT NOT NULL,
                    Domain TEXT NOT NULL,
                    FullName TEXT NOT NULL,
                    LastUpdated TEXT,
                    PasswordLastSet TEXT
                );";
            using (var command = new SqliteCommand(createUsersTableSql, connection)) {
                command.ExecuteNonQuery();
            }

            // create the groups table so we can store cached group memberships
            var createGroupsTableSql = @"
                CREATE TABLE IF NOT EXISTS Groups (
                    Sid TEXT PRIMARY KEY NOT NULL,
                    DisplayName TEXT NOT NULL
                );";
            using (var command = new SqliteCommand(createGroupsTableSql, connection)) {
                command.ExecuteNonQuery();
            }

            // Create Junction table for User-Group many-to-many relationship
            var createUserGroupMapTableSql = @"
                CREATE TABLE IF NOT EXISTS UserGroupMap (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserSid TEXT NOT NULL,
                    GroupSid TEXT NOT NULL,
                    FOREIGN KEY (UserSid) REFERENCES Users(Sid) ON DELETE CASCADE,
                    FOREIGN KEY (GroupSid) REFERENCES Groups(Sid) ON DELETE CASCADE,
                    UNIQUE(UserSid, GroupSid)
                );";
            using (var command = new SqliteCommand(createUserGroupMapTableSql, connection)) {
                command.ExecuteNonQuery();
            }
        }
    }

    /// <summary>
    /// Ensures that the Users table has a LastUpdated timestamp column.
    /// This column was added after RAWeb version 2025.9.11.0.
    /// </summary>
    private void EnsureUsersSchemaHasTimestamps() {
        using (var connection = new SqliteConnection(_dbPath)) {
            connection.Open();

            // check if Users.LastUpdated
            var hasLastUpdatedUser = false;
            using (var cmd = new SqliteCommand("PRAGMA table_info(Users);", connection))
            using (var reader = cmd.ExecuteReader()) {
                while (reader.Read()) {
                    if ((reader["name"] as string ?? string.Empty)
                        .Equals("LastUpdated", StringComparison.OrdinalIgnoreCase)) {
                        hasLastUpdatedUser = true;
                        break;
                    }
                }
            }

            // if needed, add the LastUpdated column to Users
            if (!hasLastUpdatedUser) {
                using (var cmd = new SqliteCommand(
                    "ALTER TABLE Users ADD COLUMN LastUpdated TEXT;",
                    connection)) {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    /// <summary>
    /// Ensures that the Users table has a PasswordLastSet column, which stores when the
    /// account's password was last changed (see <see cref="UserInformation.PasswordLastSet"/>).
    /// </summary>
    private void EnsureUsersSchemaHasPasswordLastSet() {
        using (var connection = new SqliteConnection(_dbPath)) {
            connection.Open();

            var hasPasswordLastSet = false;
            using (var cmd = new SqliteCommand("PRAGMA table_info(Users);", connection))
            using (var reader = cmd.ExecuteReader()) {
                while (reader.Read()) {
                    if ((reader["name"] as string ?? string.Empty)
                        .Equals("PasswordLastSet", StringComparison.OrdinalIgnoreCase)) {
                        hasPasswordLastSet = true;
                        break;
                    }
                }
            }

            if (!hasPasswordLastSet) {
                using (var cmd = new SqliteCommand("ALTER TABLE Users ADD COLUMN PasswordLastSet TEXT;", connection)) {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    /// <summary>
    /// Inserts a group into the cache if it does not exist or updates the group display name if it does.
    /// </summary>
    private static void InsertOrUpdateGroup(SqliteConnection connection, SqliteTransaction transaction, GroupInformation group) {
        var upsertGroupSql = @"
            INSERT OR IGNORE INTO Groups (Sid, DisplayName) VALUES (@Sid, @DisplayName);
            UPDATE Groups SET DisplayName = @DisplayName WHERE Sid = @Sid;
        ";
        using (var command = new SqliteCommand(upsertGroupSql, connection, transaction)) {
            command.Parameters.AddWithValue("@Sid", group.Sid);
            command.Parameters.AddWithValue("@DisplayName", group.Name);
            command.ExecuteNonQuery();
        }
    }

    /// <param name="passwordLastSet">
    /// When the account's password was last changed. If null, the cached value is kept
    /// because callers that do not look up the password information pass null.
    /// </param>
    public void StoreUser(string userSid, string username, string domain, string? fullName, List<GroupInformation> groups, DateTime? passwordLastSet = null) {
        using (var connection = new SqliteConnection(_dbPath)) {
            connection.Open();
            using (var transaction = connection.BeginTransaction()) {
                // insert the user if it does not exist or update the username, domain, and full name if it does
                var upsertUserSql = @"
                    INSERT INTO Users (Sid, Username, Domain, FullName, LastUpdated, PasswordLastSet)
                    VALUES (@Sid, @Username, @Domain, @FullName, @LastUpdated, @PasswordLastSet)
                    ON CONFLICT(Sid) DO UPDATE SET
                        Username=excluded.Username,
                        Domain=excluded.Domain,
                        FullName=excluded.FullName,
                        LastUpdated=excluded.LastUpdated,
                        PasswordLastSet=COALESCE(excluded.PasswordLastSet, Users.PasswordLastSet);
                ";
                using (var command = new SqliteCommand(upsertUserSql, connection, transaction)) {
                    command.Parameters.AddWithValue("@Sid", userSid);
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Domain", domain);
                    command.Parameters.AddWithValue("@FullName", fullName);
                    command.Parameters.AddWithValue("@LastUpdated", DateTime.UtcNow.ToString("o"));
                    command.Parameters.AddWithValue("@PasswordLastSet", passwordLastSet is null ? DBNull.Value : passwordLastSet.Value.ToUniversalTime().ToString("o"));
                    command.ExecuteNonQuery();
                }

                // insert/update cached groups and link them to the user
                if (groups != null && groups.Count > 0) {
                    var insertUserGroupMapSql = "INSERT OR IGNORE INTO UserGroupMap (UserSid, GroupSid) VALUES (@UserSid, @GroupSid);";

                    foreach (var group in groups) {
                        // ensure the group exists in the Groups table
                        InsertOrUpdateGroup(connection, transaction, group);

                        // insert the user-group mapping
                        using (var command = new SqliteCommand(insertUserGroupMapSql, connection, transaction)) {
                            command.Parameters.AddWithValue("@UserSid", userSid);
                            command.Parameters.AddWithValue("@GroupSid", group.Sid);
                            command.ExecuteNonQuery();
                        }
                    }
                }

                // remove any group maps that are no longer associated with the user
                var deleteUserGroupMapSql = @"
                    DELETE FROM UserGroupMap
                    WHERE UserSid = @UserSid AND GroupSid NOT IN (
                        SELECT Sid FROM Groups WHERE Sid IN (" + string.Join(",", groups?.Select(g => "\"" + g.Sid + "\"") ?? []) + @")
                    );
                ";
                using (var command = new SqliteCommand(deleteUserGroupMapSql, connection, transaction)) {
                    command.Parameters.AddWithValue("@UserSid", userSid);
                    command.ExecuteNonQuery();
                }

                // remove any groups that are no longer associated with any users
                var deleteUnusedGroupsSql = @"
                    DELETE FROM Groups
                    WHERE Sid NOT IN (
                        SELECT GroupSid FROM UserGroupMap
                    );
                ";
                using (var command = new SqliteCommand(deleteUnusedGroupsSql, connection, transaction)) {
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }
    }

    public void StoreUser(UserInformation userInfo) {
        if (userInfo == null) {
            throw new ArgumentNullException("User information cannot be null.");
        }

        var groupsList = new List<GroupInformation>();
        if (userInfo.Groups != null) {
            groupsList = userInfo.Groups.ToList();
        }

        // Store the user with their groups without looking up the password last set value
        StoreUser(userInfo.Sid, userInfo.Username, userInfo.Domain, userInfo.FullName, groupsList, userInfo.KnownPasswordLastSet);
    }

    /// <summary>
    /// Removes a user and their group memberships from the cache.
    /// <br /><br />
    /// Use this when an account can no longer sign in (e.g., it is disabled) so that the cache
    /// cannot be used for the account, including when the domain controller cannot be reached.
    /// </summary>
    public void RemoveUser(string username, string domain) {
        using (var connection = new SqliteConnection(_dbPath)) {
            connection.Open();
            using (var transaction = connection.BeginTransaction()) {
                var deleteSql = @"
                    DELETE FROM UserGroupMap WHERE UserSid IN (SELECT Sid FROM Users WHERE Username = @Username AND Domain = @Domain);
                    DELETE FROM Users WHERE Username = @Username AND Domain = @Domain;
                ";
                using (var command = new SqliteCommand(deleteSql, connection, transaction)) {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Domain", domain);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }
    }

    /// <summary>
    /// Retrieves a user's information from the cache by their SID or by their username and domain.
    /// <br /><br />
    /// If maxAge is greater than zero, only returns the user if their cached information
    /// is no older than maxAge seconds. If maxAge is zero or negative, returns the user
    /// regardless of age.
    /// </summary>
    /// <param name="userSid"></param>
    /// <param name="username"></param>
    /// <param name="domain"></param>
    /// <param name="maxAge">If the user information is older than this amount (in seconds), the user information will not be returned from the cache. Defaults to 59 seconds. Can be set with the "UserCache.StaleWhileRevaldate" app setting.</param>
    /// <returns>UserInformation</returns>
    /// <exception cref="ArgumentException"></exception>
    public UserInformation? GetUser(string? userSid = null, string? username = null, string? domain = null, int? maxAge = null) {
        // if maxAge is not provided, read it from app settings
        if (maxAge == null) {
            var appSettingValue = PoliciesManager.RawPolicies["UserCache.StaleWhileRevalidate"];
            var parseWasSuccess = int.TryParse(appSettingValue, out var configuredMaxAge);

            if (parseWasSuccess) {
                // if the configured value was a number less than zero, treat it as zero
                maxAge = Math.Max(0, configuredMaxAge);
            }
            else {
                // if the configured value was missing or invalid, default to 59 seconds
                maxAge = 59;
            }
        }

        UserInformation? userInfo = null;
        using (var connection = new SqliteConnection(_dbPath)) {
            connection.Open();

            // find the user's details in the database
            string selectUserSql;
            if (!string.IsNullOrEmpty(userSid)) {
                selectUserSql = "SELECT Sid, Username, Domain, FullName, LastUpdated, PasswordLastSet FROM Users WHERE Sid = @Sid;";
            }
            else if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(domain)) {
                selectUserSql = "SELECT Sid, Username, Domain, FullName, LastUpdated, PasswordLastSet FROM Users WHERE Username = @Username AND Domain = @Domain;";
            }
            else {
                throw new ArgumentException("Either userSid or both username and domain must be provided.");
            }
            using (var command = new SqliteCommand(selectUserSql, connection)) {
                command.Parameters.AddWithValue("@Sid", userSid);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Domain", domain);
                using (var reader = command.ExecuteReader()) {
                    if (reader.Read()) {
                        // if old entries are not allowed, check the LastUpdated timestamp
                        // and return null if the entry is too old
                        if (maxAge > 0) {
                            if (reader["LastUpdated"] == DBNull.Value) {
                                return null; // no timestamp; treat as too old
                            }
                            else {
                                if (DateTime.TryParseExact(
                                        reader["LastUpdated"].ToString(),
                                        "o",
                                        System.Globalization.CultureInfo.InvariantCulture,
                                        System.Globalization.DateTimeStyles.AdjustToUniversal,
                                        out var lastUpdated
                                    )) {
                                    if ((DateTime.UtcNow - lastUpdated).TotalSeconds > maxAge) {
                                        return null; // entry is too old
                                    }
                                }
                                else {
                                    return null; // invalid timestamp; treat as too old
                                }
                            }
                        }

                        userInfo = new UserInformation(
                            sid: reader["Sid"] as string ?? string.Empty,
                            username: reader["Username"] as string ?? string.Empty,
                            domain: reader["Domain"] as string ?? string.Empty,
                            fullName: reader["FullName"] as string ?? string.Empty,
                            groups: []
                        ) {
                            PasswordLastSet = DateTime.TryParseExact(
                                    reader["PasswordLastSet"] as string,
                                    "o",
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    System.Globalization.DateTimeStyles.AdjustToUniversal,
                                    out var passwordLastSet
                                )
                                ? DateTime.SpecifyKind(passwordLastSet, DateTimeKind.Utc)
                                : null
                        };
                    }
                }
            }

            // if the user was found, retrieve their cached group memberships
            if (userInfo != null) {
                var selectGroupsSql = @"
                    SELECT Groups.Sid, Groups.DisplayName
                    FROM Groups
                    INNER JOIN UserGroupMap ON Groups.Sid = UserGroupMap.GroupSid
                    WHERE UserGroupMap.UserSid = @UserSid;";
                using (var command = new SqliteCommand(selectGroupsSql, connection)) {
                    command.Parameters.AddWithValue("@UserSid", userInfo.Sid);

                    var groupsToAdd = new List<GroupInformation>();

                    using (var reader = command.ExecuteReader()) {
                        while (reader.Read()) {
                            groupsToAdd.Add(new GroupInformation(
                                sid: reader["Sid"] as string ?? string.Empty,
                                name: reader["DisplayName"].ToString()
                            ));
                        }
                    }

                    userInfo.Groups = groupsToAdd.ToArray();
                }
            }
        }
        return userInfo;
    }
}
