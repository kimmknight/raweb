using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace RAWeb.Server.Utilities;

public sealed class NetUserInformation {
  [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)]
  private static extern int NetUserGetInfo(
      string? servername,
      string username,
      int level,
      out IntPtr bufptr);

  [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
  private static extern int NetLocalGroupGetMembers(
      string? serverName,
      string localGroupName,
      int level,
      out IntPtr bufptr,
      int prefmaxlen,
      out int entriesRead,
      out int totalEntries,
      IntPtr resumeHandle
  );

  [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
  private static extern int NetLocalGroupEnum(
      string? servername,
      int level,
      out IntPtr bufptr,
      int prefmaxlen,
      out int entriesRead,
      out int totalEntries,
      ref IntPtr resumeHandle);

  [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
  private static extern bool LookupAccountName(
      string? lpSystemName,
      string lpAccountName,
      [Out] byte[]? sid,
      ref int cbSid,
      StringBuilder? referencedDomainName,
      ref int cchReferencedDomainName,
      out int peUse);

  [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
  private static extern int NetUserChangePassword(
        string? domainname,
        string username,
        string oldpassword,
        string newpassword
    );

  [DllImport("Netapi32.dll")]
  private static extern int NetApiBufferFree(IntPtr Buffer);

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  private struct LOCALGROUP_INFO_0 {
    [MarshalAs(UnmanagedType.LPWStr)]
    public string lgrpi0_name;
  }

  // USER_INFO_2 has the "usri2_full_name" field
  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  private struct USER_INFO_2 {
    public string usri2_name;
    public string usri2_password;
    public int usri2_password_age;
    public int usri2_priv;
    public string usri2_home_dir;
    public string usri2_comment;
    public int usri2_flags;
    public string usri2_script_path;
    public int usri2_auth_flags;
    public string usri2_full_name; // ← Display/Full name
    public string usri2_usr_comment;
    public string usri2_parms;
    public string usri2_workstations;
    public int usri2_last_logon;
    public int usri2_last_logoff;
    public int usri2_acct_expires;
    public int usri2_max_storage;
    public int usri2_units_per_week;
    public IntPtr usri2_logon_hours;
    public int usri2_bad_pw_count;
    public int usri2_num_logons;
    public string usri2_logon_server;
    public int usri2_country_code;
    public int usri2_code_page;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  struct LOCALGROUP_MEMBERS_INFO_2 {
    public IntPtr sid;
    public int sidUsage;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string domainAndName;
  }

#pragma warning disable IDE1006
  private static readonly int NERR_Success = 0;
  private static readonly int ERROR_MORE_DATA = 234;
#pragma warning restore IDE1006

  public static string GetFullName(string? domain, string username) {
    var level = 2;
    var server = string.IsNullOrEmpty(domain) ? null : @"\\" + domain;

    // attempt to get the user info
    var result = NetUserGetInfo(server, username, level, out var netUserInfoPointer);
    if (result != NERR_Success)
      throw new System.ComponentModel.Win32Exception(result);

    // if there was user info, marshall the pointer to a USER_INFO_2 structure
    // and return the full name from the structure
    try {
      var info = Marshal.PtrToStructure<USER_INFO_2>(netUserInfoPointer);
      return info.usri2_full_name;
    }
    finally {
      NetApiBufferFree(netUserInfoPointer);
    }
  }

  public static bool IsUserLocalAdministrator(string userSid) {
    return IsUserMemberOfLocalGroup(new SecurityIdentifier(userSid), new SecurityIdentifier("S-1-5-32-544"));
  }

  public static bool IsUserLocalUser(string userSid) {
    return IsUserMemberOfLocalGroup(new SecurityIdentifier(userSid), new SecurityIdentifier("S-1-5-32-545"));
  }

  public static bool IsUserMemberOfLocalGroup(SecurityIdentifier userSid, SecurityIdentifier groupSid) {
    var level = 2;

    // resolve the SID to a localized name
    var groupName = GroupInformation.ResolveLocalizedGroupName(groupSid);

    // attempt to get the members for the group
    var result = NetLocalGroupGetMembers(
            null, // local machine
            groupName,
            level, // level 2 will give us LOCALGROUP_MEMBERS_INFO_2
            out var netLocalGroupMembersPointer,
            -1,
            out var entriesRead,
            out _,
            IntPtr.Zero
        );
    if (result != NERR_Success) {
      throw new System.ComponentModel.Win32Exception(result);
    }

    // if there was a response, marshall the pointer to an
    // array of LOCALGROUP_MEMBERS_INFO_2 structures and
    // check if the specified user SID is in the list of members
    try {
      var iter = netLocalGroupMembersPointer;
      var memberStructSize = Marshal.SizeOf(typeof(LOCALGROUP_MEMBERS_INFO_2));

      for (var i = 0; i < entriesRead; i++) {
        var memberInfo = Marshal.PtrToStructure<LOCALGROUP_MEMBERS_INFO_2>(iter);
        var memberSid = new SecurityIdentifier(memberInfo.sid);

        // return early as soon as we find a match
        if (memberSid.Equals(userSid)) {
          return true;
        }

        iter = IntPtr.Add(iter, memberStructSize);
      }
    }
    finally {
      NetApiBufferFree(netLocalGroupMembersPointer);
    }

    return false;
  }

  /// <summary>
  /// Resolves a local account name to its SecurityIdentifier on this machine.
  /// </summary>
  public static SecurityIdentifier? GetSidFromAccountName(string accountName) {
    // check the size of a potential SID for this account
    // and the size of the domain name
    var sidBytesSize = 0;
    var domainNameLength = 0;
    LookupAccountName(null, accountName, null, ref sidBytesSize, null, ref domainNameLength, out _);
    if (sidBytesSize == 0) {
      // no SID was found for this account name
      return null;
    }

    var sidBytes = new byte[sidBytesSize];
    var domainName = new StringBuilder(domainNameLength);
    return LookupAccountName(null, accountName, sidBytes, ref sidBytesSize, domainName, ref domainNameLength, out _)
        ? new SecurityIdentifier(sidBytes, 0)
        : null;
  }

  /// <summary>
  /// Returns all local groups on this machine that contain the specified user (or any of
  /// <paramref name="additionalSids"/>) as a direct member.
  /// </summary>
  public static GroupInformation[] GetLocalGroupMemberships(SecurityIdentifier userSid, SecurityIdentifier[]? additionalSids = null) {
    var result = new List<GroupInformation>();

    foreach (var groupName in EnumerateLocalGroupNames()) {
      if (!IsUserMemberOfGroup(groupName, userSid, additionalSids)) {
        continue;
      }

      var groupSid = GetSidFromAccountName(groupName);
      if (groupSid is null) continue;

      var displayName = GroupInformation.ResolveLocalizedGroupName(groupSid);
      result.Add(new GroupInformation(displayName, groupSid.ToString()));
    }

    return [.. result];
  }

  /// <summary>
  /// Gets the names of all local groups on this machine.
  /// </summary>
  /// <returns></returns>
  private static List<string> EnumerateLocalGroupNames() {
    var names = new List<string>();

    // pointer to a handle that is used to resume enumeration
    var resumeHandle = IntPtr.Zero;
    int netResult;
    do {
      netResult = NetLocalGroupEnum(null, 0, out var bufptr, -1, out var entriesRead, out _, ref resumeHandle);

      // if there was an error other than the error that indicates
      // that we can keep enumerating to receive another result, 
      // break and return what we have so far
      if (netResult != NERR_Success && netResult != ERROR_MORE_DATA) {
        break;
      }

      // read each entry in the result buffer and clear
      // the buffer when we're done
      try {
        var iter = bufptr;
        var structSize = Marshal.SizeOf<LOCALGROUP_INFO_0>();
        for (var i = 0; i < entriesRead; i++) {
          names.Add(Marshal.PtrToStructure<LOCALGROUP_INFO_0>(iter).lgrpi0_name);
          iter = IntPtr.Add(iter, structSize);
        }
      }
      finally {
        _ = NetApiBufferFree(bufptr);
      }
    } while (netResult == ERROR_MORE_DATA);

    return names;
  }

  /// <summary>
  /// Checks if the specified user SID (or any of the <paramref name="additionalSids"/>)
  /// is a member of the specified local group.
  /// </summary>
  /// <param name="groupName">The name of the local group</param>
  /// <param name="userSid">The SID of the user</param>
  /// <param name="additionalSids">Additional SIDs that can stand in for the user during this check</param>
  /// <returns></returns>
  private static bool IsUserMemberOfGroup(string groupName, SecurityIdentifier userSid, SecurityIdentifier[]? additionalSids) {
    // read the group membership into a buffer
    var result = NetLocalGroupGetMembers(null, groupName, 2, out var bufptr, -1, out var entriesRead, out _, IntPtr.Zero);
    if (result != NERR_Success) {
      return false;
    }

    try {
      // resolve each structure in the buffer into a group membership entry
      // until we find a matching SID or run out of entries, then return whether we found a match
      var iter = bufptr; // a pointer to the buffer
      var structSize = Marshal.SizeOf<LOCALGROUP_MEMBERS_INFO_2>();
      for (var i = 0; i < entriesRead; i++) {
        // read the member SID
        var memberSid = new SecurityIdentifier(Marshal.PtrToStructure<LOCALGROUP_MEMBERS_INFO_2>(iter).sid);

        if (memberSid.Equals(userSid)) {
          // user is a direct member of the group
          return true;
        }
        if (additionalSids != null) {
          foreach (var extra in additionalSids) {
            if (extra.Equals(memberSid)) {
              // one of the stand-in SIDs is a direct member of the group
              return true;
            }
          }
        }

        // move the pointer to the next structure in the buffer and repeat
        iter = IntPtr.Add(iter, structSize);
      }
    }
    finally {
      _ = NetApiBufferFree(bufptr);
    }

    // no matches were found
    return false;
  }

  /// <summary>
  /// Attempts to change the password for the specified user. The domain can be null or empty to target the local machine.
  /// </summary>
  /// <param name="username"></param>
  /// <param name="oldPassword"></param>
  /// <param name="newPassword"></param>
  /// <param name="domain"></param>
  /// <returns></returns>
  public static (bool success, string? error) ChangeCredentials(string username, string oldPassword, string newPassword, string domain) {
    // pass null for the domain to target the local machine
    var resolvedDomain = domain.Trim().Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
        ? null
        : domain;

    var errorCode = NetUserChangePassword(resolvedDomain, username, oldPassword, newPassword);
    if (errorCode == 0) {
      return (true, null);
    }

    var message = new System.ComponentModel.Win32Exception(errorCode).Message;
    return (false, message);
  }
}
