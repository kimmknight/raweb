using System;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.InteropServices;
using System.Security.Authentication;

namespace RAWeb.Server.Utilities;

public class ValidateCredentialsException(string message) : AuthenticationException(message) { }

public static class SignIn {
  /// <summary>
  /// Gets the current machine's domain. If the machine is not part of a domain, it returns the machine name.
  /// If the domain cannot be accessed, likely due to the machine either not being part of the domain
  /// or the network connection between the machine and the domain controller being unavailable, the machine
  /// name will be used instead.
  /// </summary>
  /// <returns>The domain name</returns>
  public static string GetDomainName() {
    try {
      return Domain.GetComputerDomain().Name;
    }
    catch (ActiveDirectoryObjectNotFoundException) {
      // if the domain cannot be found, attempt to get the domain from the registry
      var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters");
      if (regKey == null) {
        // if the registry key is not found, return the machine name
        return Environment.MachineName;
      }

      using (regKey) {
        // this either contains the machine's domain name or is empty if the machine is not part of a domain
        if (regKey.GetValue("Domain") is not string foundDomain || string.IsNullOrEmpty(foundDomain)) {
          // if the domain is not found, return the machine name
          return Environment.MachineName;
        }
        return foundDomain;
      }
    }
    catch (Exception) {
      return Environment.MachineName;
    }
  }

  [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
  public static extern bool LogonUser(
      string lpszUsername,
      string lpszDomain,
      string lpszPassword,
      int dwLogonType,
      int dwLogonProvider,
      out IntPtr phToken
  );

  public const int LOGON32_LOGON_INTERACTIVE = 2;
  public const int LOGON32_PROVIDER_DEFAULT = 0;

  // see https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--1300-1699-
  public const int ERROR_LOGON_FAILURE = 1326; // incorrect username or password
  public const int ERROR_ACCOUNT_RESTRICTION = 1327; // account restrictions, such as logon hours or workstation restrictions, are preventing this user from logging on
  public const int ERROR_INVALID_LOGON_HOURS = 1328; // the user is not allowed to log on at this time
  public const int ERROR_INVALID_WORKSTATION = 1329; // the user is not allowed to log on to this workstation
  public const int ERROR_PASSWORD_EXPIRED = 1330; // the user's password has expired
  public const int ERROR_ACCOUNT_DISABLED = 1331; // the user account is disabled
  public const int ERROR_PASSWORD_MUST_CHANGE = 1907; // the user account password must change before signing in

  /// <summary>
  /// A safe handle for a user token obtained from LogonUser.
  /// <br /><br />
  /// Close the handle by calling Dispose() or using a using statement.
  /// <br />
  /// This ensures that the handle is properly closed when no longer needed.
  /// </summary>
  public sealed class UserToken : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid {
    private UserToken() : base(true) { }

    internal UserToken(IntPtr handle) : base(true) {
      SetHandle(handle);
    }

    protected override bool ReleaseHandle() {
      return CloseHandle(handle);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);
  }

  /// <summary>
  /// Validates the user credentials against the local machine or domain.
  /// <br />
  /// If the domain is not specified or is the same as the machine name, it validates against the local machine.
  /// If the domain is specified, it validates against the domain.
  /// </summary>
  /// <param name="username"></param>
  /// <param name="password"></param>
  /// <param name="domain"></param>
  /// <returns>A pointer to the user from the credentials.</returns>
  public static UserToken ValidateCredentials(string username, string password, string domain) {
    if (string.IsNullOrEmpty(domain) || domain.Trim() == Environment.MachineName) {
      domain = "."; // for local machine
    }

    // if the user cache is not enabled, require the principal context to be accessible
    // because the GetUserInformation method will attempt to access the principal context
    // to get the user information, which will fail if the domain cannot be accessed
    // and the user cache is not enabled
    if (PoliciesManager.RawPolicies["UserCache.Enabled"] != "true") {
      try {
        // attempt to get the principal context for the domain or machine
        PrincipalContext principalContext;
        if (domain == ".") {
          principalContext = new PrincipalContext(ContextType.Machine);
        }
        else {
          principalContext = new PrincipalContext(ContextType.Domain, domain, null, ContextOptions.Negotiate | ContextOptions.Signing | ContextOptions.Sealing);
        }

        // dispose of the principal context once we have verified it can be accessed
        principalContext.Dispose();
      }
      catch (Exception) {
        throw new ValidateCredentialsException("login.server.unfoundDomain");
      }
    }

    if (LogonUser(username, domain, password, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, out var userToken)) {
      return new UserToken(userToken);
    }
    else {
      var errorCode = Marshal.GetLastWin32Error();
      switch (errorCode) {
        case ERROR_LOGON_FAILURE:

          // check if the domain can be resolved
          if (domain != ".") {
            try {
              Domain.GetDomain(new DirectoryContext(DirectoryContextType.Domain, domain));
            }
            catch (ActiveDirectoryObjectNotFoundException) {
              throw new ValidateCredentialsException("login.server.unfoundDomain");
            }
          }

          throw new ValidateCredentialsException("login.incorrectUsernameOrPassword");
        case ERROR_ACCOUNT_RESTRICTION:
          throw new ValidateCredentialsException("login.server.accountRestrictionError");
        case ERROR_INVALID_LOGON_HOURS:
          throw new ValidateCredentialsException("login.server.invalidLogonHoursError");
        case ERROR_INVALID_WORKSTATION:
          throw new ValidateCredentialsException("login.server.invalidWorkstationError");
        case ERROR_PASSWORD_EXPIRED:
          throw new ValidateCredentialsException("login.server.passwordExpiredError");
        case ERROR_ACCOUNT_DISABLED:
          throw new ValidateCredentialsException("login.server.accountDisabledError");
        case ERROR_PASSWORD_MUST_CHANGE:
          throw new ValidateCredentialsException("login.server.passwordMustChange");
        default:
          throw new ValidateCredentialsException("An unknown error occurred: " + errorCode);
      }
    }
  }
}
