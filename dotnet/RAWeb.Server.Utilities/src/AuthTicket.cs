using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace RAWeb.Server.Utilities;

public sealed class AuthTicket(int version, string name, DateTime issueDate, DateTime expiration, bool isPersistent, string userData) {
  public int Version { get; set; } = version;
  public string Name { get; set; } = name;
  public DateTime IssueDate { get; set; } = issueDate;
  public DateTime Expiration { get; set; } = expiration;
  public bool IsPersistent { get; set; } = isPersistent;
  public string UserData { get; set; } = userData;

  public override string ToString() {
    return Name;
  }


  /// <summary>
  /// Creates a string containing an encrypted representation of the authentication ticket
  /// suitable for use in an HTTP cookie.
  /// </summary>
  /// <returns></returns>
  public string ToEncryptedToken() {
#if NET462
    var tkt = new System.Web.Security.FormsAuthenticationTicket(Version, Name, IssueDate, Expiration, IsPersistent, UserData);
    var token = System.Web.Security.FormsAuthentication.Encrypt(tkt);
    return token;
#else
    throw new NotImplementedException();
#endif
  }

  /// <summary>
  /// Creates an authentication ticket from an encrypted token string.
  /// </summary>
  /// <param name="encryptedToken"></param>
  /// <returns></returns>
  public static AuthTicket FromEncryptedToken(string encryptedToken) {
#if NET462
    var formsAuthTicket = System.Web.Security.FormsAuthentication.Decrypt(encryptedToken);
    return new AuthTicket(formsAuthTicket.Version, formsAuthTicket.Name, formsAuthTicket.IssueDate, formsAuthTicket.Expiration, formsAuthTicket.IsPersistent, formsAuthTicket.UserData);
#else
    throw new NotImplementedException();
#endif
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
    return new AuthTicketCookie(cookieName, encryptedToken, path);
  }

#if NET462
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
  public static AuthTicket FromHttpRequestIdentity(System.Web.HttpRequest request) {
    if (request == null) {
      throw new ArgumentNullException("request", "HttpRequest cannot be null.");
    }

    return FromWindowsIdentity(request.LogonUserIdentity);
  }
#else
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
  public static AuthTicket FromHttpRequestIdentity(Microsoft.AspNetCore.Http.HttpRequest request) {
    if (request == null) {
      throw new ArgumentNullException(nameof(request), "HttpRequest cannot be null.");
    }

    // if Windows authentication is used, get the user from the windows identity
    if (request.HttpContext.User.Identity is WindowsIdentity windowsIdentity) {
      return FromWindowsIdentity(windowsIdentity);
    }

    throw new NotSupportedException("FromHttpRequestIdentity requires Windows authentication via IIS.");
  }
#endif

  /// <summary>
  /// Creates an encrypted forms authentication ticket for the specified user logon token.
  /// The user logon token should represent a logged-on user (e.g., from LogonUser).
  /// </summary>
  /// <param name="userLogonToken"></param>
  /// <returns></returns>
  public static AuthTicket FromLogonToken(IntPtr userLogonToken) {
    using (var logonUserIdentity = new WindowsIdentity(userLogonToken)) {
      return FromWindowsIdentity(logonUserIdentity);
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
  public static AuthTicket FromWindowsIdentity(WindowsIdentity logonUserIdentity) {
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
    if (NetUserInformation.IsUserLocalUser(userSid)) {
      groupInformation.Add(new GroupInformation("S-1-5-32-545"));
    }

    // check the local machine for whether the user is a local administrator
    // and add the local Administrators group if needed
    if (!groupInformation.Any(g => g.Sid == "S-1-5-32-544")) {
      if (NetUserInformation.IsUserLocalAdministrator(userSid)) {
        groupInformation.Add(new GroupInformation("S-1-5-32-544"));
      }
    }

    // exclude any excluded special identity groups
    foreach (var excludedGroup in UserInformation.ExcludedSpecialIdentityGroups) {
      groupInformation.RemoveAll(g => g.Sid == excludedGroup.Sid);
    }

    var userInfo = new UserInformation(userSid, username, domain, fullName, groupInformation.ToArray());
    return FromUserInformation(userInfo);
  }

  /// <summary>
  /// Creates an encrypted forms authentication ticket for the specified user.
  /// </summary>
  /// <param name="userInfo"></param>
  /// <returns></returns>
  public static AuthTicket FromUserInformation(UserInformation userInfo) {
    var version = 1;
    var issueDate = DateTime.Now;
    var expirationDate = DateTime.Now.AddMinutes(30);
    var isPersistent = false;
    var userData = "";

    if (PoliciesManager.RawPolicies["UserCache.Enabled"] == "true") {
      var dbHelper = new UserCacheDatabaseHelper();
      dbHelper.StoreUser(userInfo.Sid, userInfo.Username, userInfo.Domain, userInfo.FullName, [.. userInfo.Groups]);
    }

    return new AuthTicket(version, userInfo.Domain + "\\" + userInfo.Username, issueDate, expirationDate, isPersistent, userData);
  }

#if NET462
  /// <summary>
  /// Parses an authentication ticket from the specified HTTP request's cookies.
  /// </summary>
  /// <param name="request"></param>
  /// <param name="cookieName"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentNullException"></exception>
  public static AuthTicket? FromHttpRequestCookie(System.Web.HttpRequest request, string? cookieName = null) {
    // get the cookie value from the request
    if (request == null) {
      throw new ArgumentNullException("request", "HttpRequest cannot be null.");
    }
    if (request.Cookies == null) {
      throw new ArgumentNullException("request.Cookies", "Cookies collection cannot be null.");
    }

    if (request.Cookies[cookieName ?? Constants.DefaultAuthCookieName] == null) {
      // if the cookie does not exist, return null
      return null;
    }

    // if the cookie exists, get its value
    var cookieValue = request.Cookies[cookieName ?? Constants.DefaultAuthCookieName].Value;

    // decrypt the value and return it
    try {
      // decrypt may throw an exception if cookieValue is invalid
      var authTicket = FromEncryptedToken(cookieValue);
      return authTicket;
    }
    catch {
      return null;
    }
  }
#else
  /// <summary>
  /// Parses an authentication ticket from the specified HTTP request's cookies.
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

    // read the cookie value
    if (!request.Cookies.TryGetValue(cookieName ?? Constants.DefaultAuthCookieName, out var cookieValue)) {
      // if the cookie does not exist, return null
      return null;
    }

    // decrypt the value and return it
    try {
      // decrypt may throw an exception if cookieValue is invalid
      var authTicket = FromEncryptedToken(cookieValue);
      return authTicket;
    }
    catch {
      return null;
    }
  }
#endif
}

public sealed class AuthTicketCookie(string cookieName, string cookieValue, string cookiePath) {
  public string Name { get; private set; } = cookieName;
  public string Value { get; private set; } = cookieValue;
  public string Path { get; private set; } = cookiePath;
  public bool HttpOnly { get; set; } = true;
  public bool Secure { get; set; } = false;
  public DateTime Expires { get; set; } = DateTime.MinValue;
}
