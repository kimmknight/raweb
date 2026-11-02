using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json.Serialization;

namespace RAWeb.Server.Utilities;

public sealed class AuthTicket(int version, string name, DateTime issueDate, DateTime expiration, bool isPersistent, AuthTicketUserData? userData) {
  public int Version { get; set; } = version;
  public string Name { get; set; } = name;
  /// <summary>
  /// When the ticket was issued. Local times are converted to UTC.
  /// </summary>
  public DateTime IssueDate { get; set => field = ToUtc(value, nameof(IssueDate)); } = ToUtc(issueDate, nameof(issueDate));

  /// <summary>
  /// When the ticket expires. Local times are converted to UTC.
  /// </summary>
  public DateTime Expiration { get; set => field = ToUtc(value, nameof(Expiration)); } = ToUtc(expiration, nameof(expiration));
  public bool IsPersistent { get; set; } = isPersistent;
  public AuthTicketUserData UserData { get; init; } = userData ?? new AuthTicketUserData(AuthTicketLevel.ReadOnlyUser);

  public override string ToString() {
    return Name;
  }

  /// <summary>
  /// Whether the ticket's expiration time has passed. Expired tickets must not be accepted.
  /// </summary>
  public bool IsExpired => Expiration <= DateTime.UtcNow;

  /// <summary>
  /// Converts a date to UTC so that ticket dates can be compared with the current time
  /// regardless of the server's time zone.
  /// </summary>
  /// <exception cref="ArgumentException">
  /// When the date's kind is <see cref="DateTimeKind.Unspecified"/>, because it is unknown
  /// whether the date is in local time or UTC.
  /// </exception>
  private static DateTime ToUtc(DateTime value, string paramName) {
    return value.Kind switch {
      DateTimeKind.Utc => value,
      DateTimeKind.Local => value.ToUniversalTime(),
      _ => throw new ArgumentException("Auth ticket dates must be UTC or local times, not unspecified times.", paramName)
    };
  }

  /// <summary>
  /// How far apart two password-last-set values can be and still be treated as the same password.
  /// Local accounts only report the password's age in whole seconds, so their values drift slightly.
  /// </summary>
  private static readonly TimeSpan s_passwordLastSetTolerance = TimeSpan.FromSeconds(5);

  /// <summary>
  /// Whether the account's password changed after this ticket was issued, which means that
  /// the ticket must not be accepted. This ends existing sessions when a user changes their password.
  /// <br /><br />
  /// Tickets record the account's password-last-set value when they are issued.
  /// Tickets that do not have this value  are compared using their issue date.
  /// </summary>
  /// <param name="currentPasswordLastSet">
  /// The account's current <see cref="UserInformation.PasswordLastSet"/>.
  /// If null (e.g., the anonymous user), the ticket is not rejected.
  /// </param>
  public bool WasIssuedForOlderPassword(DateTime? currentPasswordLastSet) {
    if (currentPasswordLastSet is null) {
      return false;
    }

    var ticketPasswordLastSet = UserData.PasswordLastSet ?? IssueDate;
    return currentPasswordLastSet.Value - ticketPasswordLastSet > s_passwordLastSetTolerance;
  }

  /// <summary>
  /// Determines whether this ticket is a valid sudo ticket for the specified auth ticket.
  /// <br /><br />
  /// A sudo ticket only elevates the session of the user who created it, so the ticket must have
  /// the <see cref="AuthTicketLevel.ReadAndWriteAdmin"/> level and belong to the same user as the
  /// main auth ticket. Otherwise, any administrator's sudo ticket could elevate another user's session.
  /// </summary>
  /// <param name="mainAuthTicket">The ticket from the main authentication cookie.</param>
  public bool IsSudoTicketFor(AuthTicket mainAuthTicket) {
    return UserData.Level == AuthTicketLevel.ReadAndWriteAdmin && string.Equals(Name, mainAuthTicket.Name, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Whether the sudo policy is enabled. When it is enabled, write access requires a sudo ticket
  /// (from the .ASPXAUTH_SUDO cookie), and the main auth ticket is limited to <see cref="AuthTicketLevel.ReadOnlyAdmin"/>.
  /// </summary>
  public static bool IsSudoPolicyEnabled => PoliciesManager.RawPolicies["App.Auth.Sudo.Enabled"] != "false";

  /// <summary>
  /// The level that this ticket grants when it is used as the main auth ticket.
  /// <br /><br />
  /// When the sudo policy is enabled, the main auth ticket is capped at <see cref="AuthTicketLevel.ReadOnlyAdmin"/>
  /// because write access must come from a sudo ticket. Without this cap, a sudo ticket placed in the main auth cookie
  /// would grant write access without the sudo cookie and would be renewed as a long-lived session.
  /// This also downgrades main auth tickets that were issued before the sudo policy was enabled.
  /// </summary>
  public AuthTicketLevel MainAuthTicketLevel =>
    IsSudoPolicyEnabled && UserData.Level == AuthTicketLevel.ReadAndWriteAdmin
      ? AuthTicketLevel.ReadOnlyAdmin
      : UserData.Level;


  /// <summary>
  /// Creates a string containing an encrypted representation of the authentication ticket
  /// suitable for use in an HTTP cookie.
  /// </summary>
  /// <returns></returns>
  public string ToEncryptedToken() {
    var tkt = CreateFakeFormsAuthenticationTicket(Version, Name, IssueDate, Expiration, IsPersistent, UserData);
    var token = Protect(tkt);
    return token;
  }

  /// <summary>
  /// Creates an authentication ticket from an encrypted token string.
  /// </summary>
  /// <param name="encryptedToken"></param>
  /// <returns></returns>
  public static AuthTicket FromEncryptedToken(string encryptedToken) {
    try {
      var fakeFormsAuthTicket = Unprotect(encryptedToken);
      return ParseFakeFormsAuthenticationTicket(fakeFormsAuthTicket);
    }
    catch (CryptographicException ex) {
      throw new InvalidTicketException("Failed to decrypt the authentication ticket.", ex);
    }
    catch (FormatException ex) {
      throw new InvalidTicketException("Failed to parse the authentication ticket.", ex);
    }
    catch (ArgumentException ex) {
      throw new InvalidTicketException("Invalid argument provided while processing the authentication ticket.", ex);
    }
    catch (Exception ex) {
      throw new InvalidTicketException("An unexpected error occurred while processing the authentication ticket.", ex);
    }
  }

  private static Func<string, string>? s_protect;
  private static Func<string, string>? s_unprotect;

  /// <summary>
  /// Registers the protect/unprotect functions used to encrypt auth ticket cookies.
  /// This method must be called during application startup before any auth operations.
  /// </summary>
  /// <example>
  /// <code>
  /// AuthTicket.InitializeProtection(
  ///     protect: plaintext => {
  ///       var cipherBytes = dataProtector.Protect(Encoding.UTF8.GetBytes(plaintext));
  ///       return Convert.ToBase64String(cipherBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
  ///    },
  ///   unprotect: token => {
  ///      var base64 = token.Replace('-', '+').Replace('_', '/');
  ///      base64 += (base64.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
  ///      var plainBytes = dataProtector.Unprotect(Convert.FromBase64String(base64));
  ///      return Encoding.UTF8.GetString(plainBytes);
  ///   }
  /// );
  /// </code>
  /// </example>
  public static void InitializeProtection(Func<string, string> protect, Func<string, string> unprotect) {
    s_protect = protect;
    s_unprotect = unprotect;
  }

  /// <summary>
  /// Encrypts the given plaintext string using the registered protect
  /// function and returns the encrypted token.
  /// </summary>
  /// <param name="plaintext"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException">
  /// If the unprotect function has not been registered via InitializeProtection.
  /// </exception>
  private static string Protect(string plaintext) {
    if (s_protect is null) {
      throw new InvalidOperationException(
        "AuthTicket.InitializeProtection() must be called during application startup."
      );
    }
    return s_protect(plaintext);
  }

  /// <summary>
  /// Decrypts the given encrypted token string using the registered unprotect
  /// function and returns the original plaintext.
  /// </summary>
  /// <param name="token"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException">
  /// If the unprotect function has not been registered via InitializeProtection.
  /// </exception>
  private static string Unprotect(string token) {
    if (s_unprotect is null) {
      throw new InvalidOperationException(
        "AuthTicket.InitializeProtection() must be called during application startup."
      );
    }
    return s_unprotect(token);
  }

  /// <summary>
  /// Creates a string representation of the authentication ticket with the specified properties.
  /// This method is an alternative to ASP.NET's FormsAuthenticationTicket constructor.
  /// </summary>
  /// <param name="version"></param>
  /// <param name="name"></param>
  /// <param name="issueDate"></param>
  /// <param name="expiration"></param>
  /// <param name="isPersistent"></param>
  /// <param name="userData"></param>
  /// <returns></returns>
  private static string CreateFakeFormsAuthenticationTicket(
    int version,
    string name,
    DateTime issueDate,
    DateTime expiration,
    bool isPersistent,
    AuthTicketUserData userData
  ) {
    // dates are always UTC (see the IssueDate and Expiration properties), so the ticks are UTC ticks
    return string.Join("\n", [
        version.ToString(),
        name,
        issueDate.Ticks.ToString(),
        expiration.Ticks.ToString(),
        isPersistent.ToString(),
        System.Text.Json.JsonSerializer.Serialize(userData, AuthTicketJsonContext.Default.AuthTicketUserData)
    ]);
  }

  /// <summary>
  /// Parses a string representation of an authentication ticket created
  /// by CreateFakeFormsAuthenticationTicket.
  /// </summary>
  /// <param name="payload"></param>
  /// <returns></returns>
  /// <exception cref="FormatException"></exception>
  private static AuthTicket ParseFakeFormsAuthenticationTicket(string payload) {
    var parts = payload.Split('\n');
    if (parts.Length != 6) throw new FormatException("Invalid auth ticket format.");
    return new AuthTicket(
        version: int.Parse(parts[0]),
        name: parts[1],
        issueDate: new DateTime(long.Parse(parts[2]), DateTimeKind.Utc),
        expiration: new DateTime(long.Parse(parts[3]), DateTimeKind.Utc),
        isPersistent: bool.Parse(parts[4]),
        userData: System.Text.Json.JsonSerializer.Deserialize(parts[5], AuthTicketJsonContext.Default.AuthTicketUserData)
    );
  }

  /// <summary>
  /// Creates cookie information containing the encrypted authentication ticket.
  /// </summary>
  /// <param name="cookieName"></param>
  /// <returns></returns>
  public AuthTicketCookie ToCookie(string path, string? cookieName = null) {
    var encryptedToken = ToEncryptedToken();

    // the cookie name+value length must be less than 4096 bytes
    var combinedCookieNameAndValue = cookieName + "=" + encryptedToken;
    if (combinedCookieNameAndValue.Length >= 4096) {
      throw new Exception("Cookie name and value length exceeds 4096 bytes.");
    }

    cookieName ??= Constants.DefaultAuthCookieName;
    return new AuthTicketCookie(cookieName, encryptedToken, path, Expiration) {
      // The sudo cookie is only needed by requests from RAWeb's own pages, so it can be strict.
      // The main auth cookie must be lax so that it is sent when the MFA provider redirects
      // back to RAWeb and when the user follows a link to RAWeb from another site.
      SameSite = cookieName == Constants.SudoAuthCookieName
        ? Microsoft.AspNetCore.Http.SameSiteMode.Strict
        : Microsoft.AspNetCore.Http.SameSiteMode.Lax
    };
  }

  /// <summary>
  /// Creates an encrypted forms authentication ticket for the user included in the
  /// request info. This user is populated by IIS when authentication is used.
  /// <br /><br />
  /// If override the user, use the <see cref="FromUserInformation(UserInformation)">,
  /// <see cref="FromLogonToken(IntPtr)">, or <see cref="FromWindowsIdentity(WindowsIdentity)">
  /// instead.
  /// </summary>
  /// <param name="request"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static AuthTicket FromHttpRequestIdentity(Microsoft.AspNetCore.Http.HttpRequest request, AuthTicketLevel maxLevel) {
    if (request == null) {
      throw new ArgumentNullException(nameof(request), "HttpRequest cannot be null.");
    }

    // if Windows authentication is used, get the user from the windows identity
    if (request.HttpContext.User.Identity is WindowsIdentity windowsIdentity) {
      return FromWindowsIdentity(windowsIdentity, maxLevel);
    }

    throw new NotSupportedException("FromHttpRequestIdentity requires Windows authentication via IIS.");
  }

  /// <summary>
  /// Creates an encrypted forms authentication ticket for the specified user logon token.
  /// The user logon token should represent a logged-on user (e.g., from LogonUser).
  /// </summary>
  /// <param name="userLogonToken"></param>
  /// <returns></returns>
  public static AuthTicket FromLogonToken(IntPtr userLogonToken, AuthTicketLevel maxLevel) {
    using (var logonUserIdentity = new WindowsIdentity(userLogonToken)) {
      return FromWindowsIdentity(logonUserIdentity, maxLevel);
    }
  }

  /// <summary>
  /// Creates an encrypted forms authentication ticket for the specified Windows identity.
  /// The Windows identity should reprent a logged-on user.
  /// <br /><br />
  /// If you have a user logon token (e.g., from LogonUser), use <see cref="FromLogonToken(IntPtr)"> instead.
  /// </summary>
  /// <param name="logonUserIdentity"></param>
  /// <returns></returns>
  public static AuthTicket FromWindowsIdentity(WindowsIdentity logonUserIdentity, AuthTicketLevel maxLevel) {
    var userSid = logonUserIdentity.User?.Value;
    if (userSid is null) {
      throw new Exception("Failed to retrieve user SID from Windows identity.");
    }

    var username = logonUserIdentity.Name.Split('\\').Last(); // get the username from the LogonUserIdentity, which is in DOMAIN\username format
    var domain = logonUserIdentity.Name.Contains("\\") ? logonUserIdentity.Name.Split('\\')[0] : Environment.MachineName; // get the domain from the username, or use machine name if no domain

    // attempt to get the user's full/display name
    string? fullName = null;
    try {
      fullName = NetUserInformation.GetFullName(domain == Environment.MachineName ? null : domain, username);
    }
    catch (Exception) { }

    // parse the groups from the user identity
    var groupInformation = new List<GroupInformation>();
    foreach (var group in logonUserIdentity.Groups ?? []) {
      var groupSid = group.Value;
      var displayName = groupSid;

      // attempt to translate the SID to an NTAccount (e.g., DOMAIN\GroupName)
      try {
        var ntAccount = (NTAccount)group.Translate(typeof(NTAccount));
        displayName = ntAccount.Value.Split('\\').Last(); // get the group name from the NTAccount
      }
      catch (IdentityNotMappedException) {
        // identity cannot be mapped - use SID as display name
      }
      catch (SystemException) {
        // cannot communicate with the domain controller - use SID as display name
      }

      groupInformation.Add(new GroupInformation(displayName, groupSid));
    }

    // LogonUser always adds the User group, so we need to exclude it
    // and then check for the membership separately
    groupInformation.RemoveAll(g => g.Sid == "S-1-5-32-545");

    // check the local machine for whether the user is a member
    // of the Users group and add it if needed
    if (logonUserIdentity.IsLocalUser) {
      groupInformation.Add(new GroupInformation("S-1-5-32-545"));
    }

    // check the local machine for whether the user is a local administrator
    // and add the local Administrators group if needed
    if (!groupInformation.Any(g => g.Sid == "S-1-5-32-544")) {
      if (logonUserIdentity.IsLocalAdministrator) {
        groupInformation.Add(new GroupInformation("S-1-5-32-544"));
      }
    }

    // exclude any excluded special identity groups
    foreach (var excludedGroup in UserInformation.ExcludedSpecialIdentityGroups) {
      groupInformation.RemoveAll(g => g.Sid == excludedGroup.Sid);
    }

    var authTicketLevel = maxLevel == AuthTicketLevel.ReadOnlyWorkspaceUser ? AuthTicketLevel.ReadOnlyWorkspaceUser : AuthTicketLevel.ReadOnlyUser;
    if (logonUserIdentity.IsLocalAdministrator && maxLevel == AuthTicketLevel.ReadAndWriteAdmin) {
      authTicketLevel = AuthTicketLevel.ReadAndWriteAdmin;
    }
    if (logonUserIdentity.IsLocalAdministrator && maxLevel == AuthTicketLevel.ReadOnlyAdmin) {
      authTicketLevel = AuthTicketLevel.ReadOnlyAdmin;
    }

    var userInfo = new UserInformation(userSid, username, domain, fullName, groupInformation.ToArray());
    return FromUserInformation(userInfo, authTicketLevel);
  }

  /// <summary>
  /// Creates an encrypted forms authentication ticket for the specified user.
  /// <br /><br />
  /// The ticket records the user's current <see cref="UserInformation.PasswordLastSet"/>.
  /// To extend an existing session, use <see cref="Renew"/>.
  /// </summary>
  /// <param name="userInfo"></param>
  /// <returns></returns>
  public static AuthTicket FromUserInformation(UserInformation userInfo, AuthTicketLevel level) {
    return Create(userInfo, level, userInfo.PasswordLastSet);
  }

  /// <summary>
  /// Creates a new ticket for the same sign-in session with a later expiration.
  /// <br /><br />
  /// The new ticket keeps this ticket's password-last-set value instead of reading the account's current
  /// value. Otherwise, a session renewed while the current value is unknown (e.g., the domain controller
  /// cannot be reached) would lose track of which password it was signed in with and would survive a
  /// password change. Tickets without the value (e.g., it was unknown when they were issued) get the current one.
  /// <br /><br />
  /// Only renew a ticket after it has been accepted for the request (e.g., by <see cref="UserInformation.FromHttpRequest"/>).
  /// </summary>
  /// <param name="userInfo">The user for which this ticket was accepted.</param>
  /// <param name="expiration">When the renewed ticket expires.</param>
  /// <exception cref="ArgumentException">If <paramref name="userInfo"/> is not the user to which this ticket belongs.</exception>
  /// <exception cref="InvalidOperationException">If this ticket does not have an auth session ID.</exception>
  public AuthTicket Renew(UserInformation userInfo, DateTime expiration) {
    if (!string.Equals(Name, userInfo.Domain + "\\" + userInfo.Username, StringComparison.OrdinalIgnoreCase)) {
      throw new ArgumentException("The user information does not belong to this ticket.", nameof(userInfo));
    }

    if (UserData.AuthSessionId is null) {
      throw new InvalidOperationException("A ticket without an auth session ID cannot be renewed.");
    }

    var renewed = Create(userInfo, userInfo.AuthTicketLevel, UserData.PasswordLastSet ?? userInfo.PasswordLastSet, UserData.AuthSessionId);
    renewed.Expiration = expiration;
    return renewed;
  }

  private static AuthTicket Create(UserInformation userInfo, AuthTicketLevel level, DateTime? passwordLastSet, string? authSessionId = null) {
    var version = 1;
    var issueDate = DateTime.UtcNow;
    var expirationDate = issueDate.AddMinutes(30);
    var isPersistent = false;

    var userData = new AuthTicketUserData(level, passwordLastSet, authSessionId ?? AuthSessionStore.NewAuthSessionId());

    if (PoliciesManager.RawPolicies["UserCache.Enabled"] == "true") {
      var dbHelper = new UserCacheDatabaseHelper();
      dbHelper.StoreUser(userInfo.Sid, userInfo.Username, userInfo.Domain, userInfo.FullName, [.. userInfo.Groups]);
    }

    return new AuthTicket(version, userInfo.Domain + "\\" + userInfo.Username, issueDate, expirationDate, isPersistent, userData);
  }

  /// <summary>
  /// The <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/> key that marks a request
  /// as allowed to use a workspace authentication ticket (<see cref="AuthTicketLevel.ReadOnlyWorkspaceUser"/>).
  /// </summary>
  public const string AllowWorkspaceAuthTicketContextKey = "RAWeb.AllowWorkspaceAuthTicket";

  /// <summary>
  /// Marks the request as allowed to use a workspace authentication ticket.
  /// This must only be called for the endpoints that Remote Desktop clients use.
  /// </summary>
  public static void AllowWorkspaceAuthTicket(Microsoft.AspNetCore.Http.HttpContext context) {
    context.Items[AllowWorkspaceAuthTicketContextKey] = true;
  }

  private static bool IsWorkspaceAuthTicketAllowed(Microsoft.AspNetCore.Http.HttpContext context) {
    return context.Items[AllowWorkspaceAuthTicketContextKey] is true;
  }

  /// <summary>
  /// Parses an authentication ticket from the specified HTTP request's cookies.
  /// <br /><br />
  /// Expired tickets are treated as if the cookie does not exist. Returns null for workspace user
  /// tickets (<see cref="AuthTicketLevel.ReadOnlyWorkspaceUser"/>) unless the request was marked
  /// with <see cref="AllowWorkspaceAuthTicket"/>.
  /// </summary>
  /// <param name="request"></param>
  /// <param name="cookieName"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentNullException"></exception>
  /// <exception cref="NullReferenceException"></exception>
  public static AuthTicket? FromHttpRequestCookie(Microsoft.AspNetCore.Http.HttpRequest request, string? cookieName = null) {
    if (request == null) {
      throw new ArgumentNullException(nameof(request), "HttpRequest cannot be null.");
    }
    if (request.Cookies == null) {
      throw new NullReferenceException("Cookies collection cannot be null.");
    }

    static AuthTicket? NoValidTicket() {
      // if the cookie does not exist, but anonymous mode is set to always,
      // then we can return an auth ticket for the anonymous user instead of returning null
      var anonSetting = PoliciesManager.RawPolicies["App.Auth.Anonymous"];
      if (anonSetting == "always") {
        return FromUserInformation(UserInformation.AnonymousUser, AuthTicketLevel.ReadOnlyUser);
      }

      return null;
    }

    // read the cookie value
    if (!request.Cookies.TryGetValue(cookieName ?? Constants.DefaultAuthCookieName, out var cookieValue)) {
      return NoValidTicket();
    }

    // decrypt the value and return it
    try {
      // decrypt may throw an exception if cookieValue is invalid
      var authTicket = FromEncryptedToken(cookieValue);

      // the browser removes expired cookies, but the server must also reject them
      // so that a copied cookie value cannot be used after it expires
      if (authTicket.IsExpired) {
        return NoValidTicket();
      }

      // workspace user tickets are only accepted by endpoints that explicitly
      // allow them so that they cannot be used as a web app session to bypass MFA
      if (authTicket.UserData.Level == AuthTicketLevel.ReadOnlyWorkspaceUser && !IsWorkspaceAuthTicketAllowed(request.HttpContext)) {
        return null;
      }

      // the sign-in session must still be valid on the server (e.g., the user has not signed out);
      // tickets without an auth session ID (from older versions of RAWeb) are never accepted
      if (authTicket.UserData.AuthSessionId is null || !new AuthSessionStore().IsActive(authTicket)) {
        return NoValidTicket();
      }

      return authTicket;
    }
    catch {
      return null;
    }
  }
}

public sealed class AuthTicketCookie(string cookieName, string cookieValue, string cookiePath, DateTime? expires = null) {
  public string Name { get; private set; } = cookieName;
  public string Value { get; private set; } = cookieValue;
  public string Path { get; private set; } = cookiePath;
  public bool HttpOnly { get; set; } = true;
  public bool Secure { get; set; } = false;
  /// Browsers do not all default to Lax, so it must always be set explicitly.
  /// It prevents browsers from sending the cookie with requests that other sites start.
  public Microsoft.AspNetCore.Http.SameSiteMode SameSite { get; set; } = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
  public DateTime Expires { get; set; } = expires ?? DateTime.MinValue;
}

/// <param name="Level">The access level that the ticket grants.</param>
/// <param name="PasswordLastSet">
/// When the account's password was last changed at the time the ticket was issued.
/// See <see cref="AuthTicket.WasIssuedForOlderPassword"/>.
/// </param>
/// <param name="AuthSessionId">
/// The sign-in session to which the ticket belongs. Renewed tickets keep the
/// same auth session ID.
/// A ticket is only accepted while its sign-in session is in the
/// <see cref="AuthSessionStore"/>, so tickets without an auth session ID are
/// never accepted.
/// </param>
public record AuthTicketUserData(
  [property: JsonPropertyName("level")] AuthTicketLevel Level,
  [property: JsonPropertyName("pwdLastSet"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? PasswordLastSet = null,
  [property: JsonPropertyName("authSessionId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AuthSessionId = null
);

[JsonSerializable(typeof(AuthTicketUserData))]
internal partial class AuthTicketJsonContext : JsonSerializerContext { }

public enum AuthTicketLevel {
  /// <summary>
  /// Can only access the workspace feed endpoints that Remote Desktop clients use (the feed, the
  /// resource files and icons referenced by the feed, and the reconnect service). All other endpoints
  /// treat the user as unauthenticated.
  /// <br /><br />
  /// This level is used for workspace authentication (loginfeed.aspx), which uses Windows authentication
  /// without MFA because Remote Desktop clients cannot complete MFA prompts. Limiting these tickets ensures
  /// that they cannot be used as a web app session to bypass MFA.
  /// </summary>
  ReadOnlyWorkspaceUser = -1,
  ReadOnlyUser = 0,
  ReadOnlyAdmin = 1,
  ReadAndWriteAdmin = 2
}
public static class AuthTicketLevelExtensions {
  extension(AuthTicketLevel level) {
    /// <summary>
    /// Whether the user has any level of admin access to RAWeb.
    /// <br /><br />
    /// This is different from being a local administrator on the machine where RAWeb is running.
    /// </summary>
    public bool IsAdmin => level == AuthTicketLevel.ReadOnlyAdmin || level == AuthTicketLevel.ReadAndWriteAdmin;
  }
}

public sealed class InvalidTicketException(string message, Exception? innerException = null) : Exception(message, innerException) {
}
