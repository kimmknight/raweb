using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace RAWeb.Server.Utilities;

/// <summary>
/// Provides access to checks based on the elevation/group information
/// stored in a Windows access token. These checks use <c>GetTokenInformation</c>
/// instead of NetAPI/SAM calls, so they work in sandboxed (AppContainer) processes.
/// </summary>
public static class WindowsIdentityToken {
  private enum TOKEN_INFORMATION_CLASS {
    TokenElevationType = 18,
  }

  private enum TOKEN_ELEVATION_TYPE {
    /// <summary>
    /// Token does not have a linked token. This means either that the token is not
    /// associated with a user account that is a member of the local Administrators
    /// group or that the user account does not have User Account Control (UAC) enabled.
    /// In these cases, use <see cref="GetTokenInformation"/> to check for
    /// Administrators group membership directly.
    /// </summary>
    TokenElevationTypeDefault = 1,
    /// <summary>
    /// The token has full administrative privlages.
    /// </summary>
    TokenElevationTypeFull = 2,
    /// <summary>
    /// The token is a limited token that does not have administrative privileges, but
    /// it is linked to an administrator token. This means the user account is a member of the
    /// local Administrators group and has UAC enabled.
    /// </summary>
    TokenElevationTypeLimited = 3,
  }

  [DllImport("advapi32.dll", SetLastError = true)]
  private static extern bool GetTokenInformation(
    IntPtr tokenHandle,
    TOKEN_INFORMATION_CLASS tokenInformationClass,
    IntPtr tokenInformation,
    uint tokenInformationLength,
    out uint returnLength
  );

  /// <summary>
  /// Gets the elevation type of the specified token via <c>GetTokenInformation</c>.
  /// </summary>
  private static TOKEN_ELEVATION_TYPE GetTokenElevationType(IntPtr token) {
    // determine the required buffer size for the token elevation information and then create the buffer
    GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenElevationType, IntPtr.Zero, 0, out var returnLength);
    var tokenInformationBuffer = Marshal.AllocHGlobal((int)returnLength);

    // try to retreive the token elevation information
    try {
      if (!GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenElevationType, tokenInformationBuffer, returnLength, out _)) {
        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
      }
      return (TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(tokenInformationBuffer);
    }
    finally {
      Marshal.FreeHGlobal(tokenInformationBuffer);
    }
  }

  /// <summary>
  /// Determines whether the specified token belongs to a member of the local
  /// Administrators group, either because the token includes the Administrators
  /// group or because the token is the limited (standard user) version of an
  /// administrator user, indicated by <see cref="TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited"/>).
  /// </summary>
  public static bool IsLocalAdministrator(IntPtr token) {
    using var identity = new WindowsIdentity(token);
    if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)) {
      return true;
    }

    return GetTokenElevationType(token) == TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited;
  }
}

/// <summary>
/// Extension members for <see cref="WindowsIdentity"/> based on token information.
/// </summary>
public static class WindowsIdentityTokenExtensions {
  extension(WindowsIdentity identity) {
    /// <summary>
    /// Whether the Windows user is a member of the local Administrators group.
    /// See <see cref="WindowsIdentityToken.IsLocalAdministrator(IntPtr)"/>.
    /// </summary>
    public bool IsLocalAdministrator => WindowsIdentityToken.IsLocalAdministrator(identity.Token);
  }
}
