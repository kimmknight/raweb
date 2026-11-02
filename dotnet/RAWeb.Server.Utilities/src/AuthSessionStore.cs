using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace RAWeb.Server.Utilities;

/// <summary>
/// Stores the sign-in sessions that are currently valid so that a session can be ended on the server
/// (e.g., when the user signs out) instead of only removing the cookie from one browser.
/// <br /><br />
/// Each authentication ticket has an auth session ID (<see cref="AuthTicketUserData.AuthSessionId"/>). A ticket is
/// only accepted while its sign-in session is in this store (see <see cref="AuthTicket.FromHttpRequestCookie"/>).
/// Sign-in sessions are added when a ticket is sent to a client, extended when a ticket is renewed, and removed when the user
/// signs out. Expired sessions records are deleted whenever a new session record is created.
/// <br /><br />
/// If the database is deleted or cannot be read, every session ends and users must sign in again.
/// </summary>
public sealed class AuthSessionStore {
  private readonly string _connectionString;

  /// <param name="databaseName">The name of the SQLite database file in App_Data (without the extension).</param>
  public AuthSessionStore(string databaseName = "sessions") {
    var dbFilePath = Path.Combine(Constants.AppDataFolderPath, databaseName + ".sqlite");
    _connectionString = $"Data Source={dbFilePath};";

    using var connection = Open();
    using var command = new SqliteCommand(@"
      CREATE TABLE IF NOT EXISTS AuthSessions (
        AuthSessionId TEXT PRIMARY KEY NOT NULL,
        Name TEXT NOT NULL,
        CreatedAt TEXT NOT NULL,
        ExpiresAt TEXT NOT NULL
      );
      CREATE INDEX IF NOT EXISTS IX_AuthSessions_ExpiresAt ON AuthSessions (ExpiresAt);
      CREATE INDEX IF NOT EXISTS IX_AuthSessions_Name ON AuthSessions (Name);", connection);
    command.ExecuteNonQuery();
  }

  /// <summary>
  /// Creates a new random auth session ID.
  /// </summary>
  public static string NewAuthSessionId() {
    return Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
  }

  private SqliteConnection Open() {
    var connection = new SqliteConnection(_connectionString);
    connection.Open();
    return connection;
  }

  /// <summary>
  /// Converts a DateTime to an ISO-8601 UTC timestamp string for storing in the database.
  /// </summary>
  /// <detail>
  /// The database stores timestamps as text in ISO 8601 format (e.g., "2023-08-15T12:34:56.789Z")
  /// so that they can be compared and sorted correctly in SQL queries.
  /// </detail>
  /// <param name="value">The DateTime value to convert.</param>
  /// <returns>The ISO-8601 UTC timestamp string representation of the DateTime.</returns
  private static string ToStoredTime(DateTime value) => value.ToUniversalTime().ToString("o");

  /// <summary>
  /// Adds the ticket's sign-in session. Does nothing for a ticket without an auth session ID.
  /// </summary>
  public void Add(AuthTicket ticket) {
    if (ticket.UserData.AuthSessionId is null) {
      return;
    }

    using var connection = Open();
    DeleteExpired(connection);

    using var command = new SqliteCommand(@"
      INSERT OR REPLACE INTO AuthSessions (AuthSessionId, Name, CreatedAt, ExpiresAt)
      VALUES (@AuthSessionId, @Name, @CreatedAt, @ExpiresAt);", connection);
    command.Parameters.AddWithValue("@AuthSessionId", ticket.UserData.AuthSessionId);
    command.Parameters.AddWithValue("@Name", ticket.Name);
    command.Parameters.AddWithValue("@CreatedAt", ToStoredTime(DateTime.UtcNow));
    command.Parameters.AddWithValue("@ExpiresAt", ToStoredTime(ticket.Expiration));
    command.ExecuteNonQuery();
  }

  /// <summary>
  /// Updates the expiration of the ticket's session after the ticket is renewed.
  /// <br /><br />
  /// Unlike <see cref="Add"/>, this never adds a session.
  /// A renewal that races with a sign-out cannot bring the session back.
  /// </summary>
  /// <returns>Whether the session still exists.</returns>
  public bool Extend(AuthTicket ticket) {
    if (ticket.UserData.AuthSessionId is null) {
      return false;
    }

    using var connection = Open();
    using var command = new SqliteCommand(@"
      UPDATE AuthSessions SET ExpiresAt = @ExpiresAt
      WHERE AuthSessionId = @AuthSessionId AND Name = @Name COLLATE NOCASE;", connection);
    command.Parameters.AddWithValue("@AuthSessionId", ticket.UserData.AuthSessionId);
    command.Parameters.AddWithValue("@Name", ticket.Name);
    command.Parameters.AddWithValue("@ExpiresAt", ToStoredTime(ticket.Expiration));
    return command.ExecuteNonQuery() > 0;
  }

  /// <summary>
  /// Whether the ticket's session exists and has not expired.
  /// Tickets without an auth session ID are not in the store, so this returns false for them.
  /// </summary>
  public bool IsActive(AuthTicket ticket) {
    if (ticket.UserData.AuthSessionId is null) {
      return false;
    }

    using var connection = Open();
    using var command = new SqliteCommand(@"
      SELECT 1 FROM AuthSessions
      WHERE AuthSessionId = @AuthSessionId AND Name = @Name COLLATE NOCASE AND ExpiresAt > @Now;", connection);
    command.Parameters.AddWithValue("@AuthSessionId", ticket.UserData.AuthSessionId);
    command.Parameters.AddWithValue("@Name", ticket.Name);
    command.Parameters.AddWithValue("@Now", ToStoredTime(DateTime.UtcNow));
    return command.ExecuteScalar() is not null;
  }

  /// <summary>
  /// Ends the ticket's session so that the ticket, and any copy of it, is no longer accepted.
  /// </summary>
  public void Remove(AuthTicket ticket) {
    if (ticket.UserData.AuthSessionId is null) {
      return;
    }

    using var connection = Open();
    using var command = new SqliteCommand("DELETE FROM AuthSessions WHERE AuthSessionId = @AuthSessionId;", connection);
    command.Parameters.AddWithValue("@AuthSessionId", ticket.UserData.AuthSessionId);
    command.ExecuteNonQuery();
  }

  private static void DeleteExpired(SqliteConnection connection) {
    using var command = new SqliteCommand("DELETE FROM AuthSessions WHERE ExpiresAt <= @Now;", connection);
    command.Parameters.AddWithValue("@Now", ToStoredTime(DateTime.UtcNow));
    command.ExecuteNonQuery();
  }
}
