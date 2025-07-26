using AuthUtilities;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Web;

public class UserCacheDatabaseHelper
{
    private string dbPath;

    public UserCacheDatabaseHelper(string databaseName = "usercache")
    {
        string appDataPath = HttpContext.Current.Server.MapPath("~/App_Data");

        // ensure the App_Data directory exists
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        // construct the database file path
        string dbFilePath = Path.Combine(appDataPath, string.Format("{0}.sqlite", databaseName));

        // construct the connection string
        dbPath = string.Format("Data Source={0};Version=3;", dbFilePath);

        // only create the database file if it does not exist
        if (!File.Exists(dbFilePath))
        {
            SQLiteConnection.CreateFile(dbFilePath);
            CreateTable();
        }
    }

    private void CreateTable()
    {
        using (var connection = new SQLiteConnection(dbPath))
        {
            connection.Open();

            // create the users table to store basic user information for when the
            // domain cannot be reached
            string createUsersTableSql = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Sid TEXT PRIMARY KEY NOT NULL,
                    Username TEXT NOT NULL,
                    Domain TEXT NOT NULL,
                    FullName TEXT NOT NULL
                );";
            using (var command = new SQLiteCommand(createUsersTableSql, connection))
            {
                command.ExecuteNonQuery();
            }

            // create the groups table so we can store cached group memberships
            string createGroupsTableSql = @"
                CREATE TABLE IF NOT EXISTS Groups (
                    Sid TEXT PRIMARY KEY NOT NULL,
                    DisplayName TEXT NOT NULL
                );";
            using (var command = new SQLiteCommand(createGroupsTableSql, connection))
            {
                command.ExecuteNonQuery();
            }

            // Create Junction table for User-Group many-to-many relationship
            string createUserGroupMapTableSql = @"
                CREATE TABLE IF NOT EXISTS UserGroupMap (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserSid TEXT NOT NULL,
                    GroupSid TEXT NOT NULL,
                    FOREIGN KEY (UserSid) REFERENCES Users(Sid) ON DELETE CASCADE,
                    FOREIGN KEY (GroupSid) REFERENCES Groups(Sid) ON DELETE CASCADE,
                    UNIQUE(UserSid, GroupSid)
                );";
            using (var command = new SQLiteCommand(createUserGroupMapTableSql, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    /// <summary>
    /// Inserts a group into the cache if it does not exist or updates the group display name if it does.
    /// </summary>
    private void InsertOrUpdateGroup(SQLiteConnection connection, SQLiteTransaction transaction, GroupInformation group)
    {
        string upsertGroupSql = @"
            INSERT OR IGNORE INTO Groups (Sid, DisplayName) VALUES (@Sid, @DisplayName);
            UPDATE Groups SET DisplayName = @DisplayName WHERE Sid = @Sid;
        ";
        using (var command = new SQLiteCommand(upsertGroupSql, connection, transaction))
        {
            command.Parameters.AddWithValue("@Sid", group.Sid);
            command.Parameters.AddWithValue("@DisplayName", group.Name);
            command.ExecuteNonQuery();
        }
    }

    public void StoreUser(string userSid, string username, string domain, string fullName, List<GroupInformation> groups)
    {
        using (var connection = new SQLiteConnection(dbPath))
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                // insert the user if it does not exist or update the username, domain, and full name if it does
                string upsertUserSql = @"
                    INSERT OR IGNORE INTO Users (Sid, Username, Domain, FullName) VALUES (@Sid, @Username, @Domain, @FullName);
                    UPDATE Users SET Username = @Username, Domain = @Domain, FullName = @FullName WHERE Sid = @Sid;
                ";
                using (var command = new SQLiteCommand(upsertUserSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Sid", userSid);
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Domain", domain);
                    command.Parameters.AddWithValue("@FullName", fullName);
                    command.ExecuteNonQuery();
                }

                // insert/update cached groups and link them to the user
                if (groups != null && groups.Count > 0)
                {
                    string insertUserGroupMapSql = "INSERT OR IGNORE INTO UserGroupMap (UserSid, GroupSid) VALUES (@UserSid, @GroupSid);";

                    foreach (var group in groups)
                    {
                        // ensure the group exists in the Groups table
                        InsertOrUpdateGroup(connection, transaction, group);

                        // insert the user-group mapping
                        using (var command = new SQLiteCommand(insertUserGroupMapSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@UserSid", userSid);
                            command.Parameters.AddWithValue("@GroupSid", group.Sid);
                            command.ExecuteNonQuery();
                        }
                    }
                }

                // remove any group maps that are no longer associated with the user
                string deleteUserGroupMapSql = @"
                    DELETE FROM UserGroupMap
                    WHERE UserSid = @UserSid AND GroupSid NOT IN (
                        SELECT Sid FROM Groups WHERE Sid IN (" + string.Join(",", groups.Select(g => "\"" + g.Sid + "\"")) + @")
                    );
                ";
                using (var command = new SQLiteCommand(deleteUserGroupMapSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@UserSid", userSid);
                    command.ExecuteNonQuery();
                }

                // remove any groups that are no longer associated with any users
                string deleteUnusedGroupsSql = @"
                    DELETE FROM Groups
                    WHERE Sid NOT IN (
                        SELECT GroupSid FROM UserGroupMap
                    );
                ";
                using (var command = new SQLiteCommand(deleteUnusedGroupsSql, connection, transaction))
                {
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }
    }

    public void StoreUser(UserInformation userInfo)
    {
        if (userInfo == null)
        {
            throw new ArgumentNullException("User information cannot be null.");
        }

        var groupsList = new List<GroupInformation>();
        if (userInfo.Groups != null)
        {
            groupsList = userInfo.Groups.ToList();
        }

        // Store the user with their groups
        StoreUser(userInfo.Sid, userInfo.Username, userInfo.Domain, userInfo.FullName, groupsList);
    }

    public UserInformation GetUser(string userSid = null, string username = null, string domain = null)
    {
        UserInformation userInfo = null;
        using (var connection = new SQLiteConnection(dbPath))
        {
            connection.Open();

            // find the user's details in the database
            string selectUserSql;
            if (!string.IsNullOrEmpty(userSid))
            {
                selectUserSql = "SELECT Sid, Username, Domain, FullName FROM Users WHERE Sid = @Sid;";
            }
            else if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(domain))
            {
                selectUserSql = "SELECT Sid, Username, Domain, FullName FROM Users WHERE Username = @Username AND Domain = @Domain;";
            }
            else
            {
                throw new ArgumentException("Either userSid or both username and domain must be provided.");
            }
            using (var command = new SQLiteCommand(selectUserSql, connection))
            {
                command.Parameters.AddWithValue("@Sid", userSid);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Domain", domain);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        userInfo = new UserInformation(
                            sid: reader["Sid"].ToString(),
                            username: reader["Username"].ToString(),
                            domain: reader["Domain"].ToString(),
                            fullName: reader["FullName"].ToString(),
                            groups: new GroupInformation[0]
                        );
                    }
                }
            }

            // if the user was found, retrieve their cached group memberships
            if (userInfo != null)
            {
                string selectGroupsSql = @"
                    SELECT Groups.Sid, Groups.DisplayName
                    FROM Groups
                    INNER JOIN UserGroupMap ON Groups.Sid = UserGroupMap.GroupSid
                    WHERE UserGroupMap.UserSid = @UserSid;";
                using (var command = new SQLiteCommand(selectGroupsSql, connection))
                {
                    command.Parameters.AddWithValue("@UserSid", userInfo.Sid);

                    var groupsToAdd = new List<GroupInformation>();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            groupsToAdd.Add(new GroupInformation(
                                sid: reader["Sid"].ToString(),
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
