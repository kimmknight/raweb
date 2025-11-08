using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

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

  [DllImport("Netapi32.dll")]
  private static extern int NetApiBufferFree(IntPtr Buffer);

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
}
