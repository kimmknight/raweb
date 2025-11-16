using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

/// <summary>
/// A helper class that grants the current process the necessary privileges
/// for loading and unloading registry hives.
/// </summary>
public static class BackupAndRestorePrivilegesHelper {
    private const string SE_BACKUP_NAME = "SeBackupPrivilege";
    private const string SE_RESTORE_NAME = "SeRestorePrivilege";

    /// <summary>
    /// Gets the access token handle associated with the current process.
    /// </summary>
    /// <param name="ProcessHandle">The handle for the current process.</param>
    /// <param name="DesiredAccess">The amount of access to include in the token.</param>
    /// <param name="TokenHandle">The access token handle for the current process.</param>
    /// <returns>Whether the process succeeded.</returns>
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(
        IntPtr ProcessHandle,
        uint DesiredAccess,
        out IntPtr TokenHandle
    );

    /// <summary>
    /// Translates a privilege name to a locally-unique identifier (LUID).
    /// </summary>
    /// <param name="lpSystemName">The name of the system on which the privlage will be retrieved. If null, the local system will be used.</param>
    /// <param name="lpName">The name of the privlage to look up.</param>
    /// <param name="lpLuid">The LUID for the privlage.</param>
    /// <returns></returns>
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool LookupPrivilegeValue(
        string? lpSystemName,
        string lpName,
        out LUID lpLuid
    );

    /// <summary>
    /// Enables or disables privileges in the specified access token.
    /// </summary>
    /// <param name="TokenHandle">The handle for the access token to modify.</param>
    /// <param name="DisableAllPrivileges">If true, disable all privlages.</param>
    /// <param name="NewState">A TOKEN_PRIVLAGES structure containing the new privlages to apply to the access token.</param>
    /// <param name="PreviousStateBufferLength">Should be zero.</param>
    /// <param name="PreviousState">Should be null.</param>
    /// <param name="ReturnLength">Should be null.</param>
    /// <returns></returns>
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(
        IntPtr TokenHandle,
        bool DisableAllPrivileges,
        ref tokenPrivileges NewState, // optional
        int PreviousStateBufferLength,
        IntPtr PreviousState,
        IntPtr ReturnLength
    );
    private static bool AdjustTokenPrivileges(
    IntPtr token,
    bool disableAllPrivileges,
    ref tokenPrivileges newState
) {
        return AdjustTokenPrivileges(token, disableAllPrivileges, ref newState, 0, IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>
    /// Gets a handle for the current process.
    /// </summary>
    /// <returns></returns>
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    /// <summary>
    /// Closes an open object handle.
    /// </summary>
    /// <param name="hObject">The handle to close.</param>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint TOKEN_ADJUST_PRIVILEGES = 0x20;
    private const uint TOKEN_QUERY = 0x8;

    /// <summary>
    /// A locally-unique identifier (LUID).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct LUID {
        public uint LowPart;
        public int HighPart;
    }

    /// <summary>
    /// Contains a locally-unique identifier (LUID) and its attributes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct Privilege {
        public LUID Luid;
        public PrivilegeAttributes Attributes;
    }

    /// <summary>
    /// Attributes for a privilege.
    /// See https://learn.microsoft.com/en-us/windows/win32/api/securitybaseapi/nf-securitybaseapi-adjusttokenprivileges.
    /// </summary>
    enum PrivilegeAttributes : uint {
        Enabled = 0x00000002,
        Removed = 0x00000004,
    }

    /// <summary>
    /// Contains information about a set of privileges for an access token.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct tokenPrivileges {
        /// <summary>
        /// The number of privileges in the Privileges array.
        /// </summary>
        public uint PrivilegeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public Privilege[] Privileges;
    }

    /// <summary>
    /// Enables the SeBackupPrivilege and SeRestorePrivilege for the current process.
    /// </summary>
    /// <exception cref="Win32Exception"></exception>
    public static void EnableBackupAndRestorePrivileges() {
        // get the token for the current process
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out var processToken)) {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        // try to enable the privileges
        try {
            EnablePrivilege(processToken, SE_BACKUP_NAME);
            EnablePrivilege(processToken, SE_RESTORE_NAME);
        }
        finally {
            CloseHandle(processToken);
        }
    }

    /// <summary>
    /// Enables the specified privilege in the given access token.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="privilege"></param>
    /// <exception cref="Win32Exception"></exception>
    private static void EnablePrivilege(IntPtr token, string privilege) {
        // get the LUID for the privilege
        if (!LookupPrivilegeValue(null, privilege, out var luid)) {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        // enable the privilege in the token
        var newPrivileges = new tokenPrivileges {
            PrivilegeCount = 1,
            // note that this array must have exactly one element
            Privileges = [
                new Privilege {
                    Luid = luid,
                    Attributes = PrivilegeAttributes.Enabled
                }
            ]
        };
        if (!AdjustTokenPrivileges(token, false, ref newPrivileges)) {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}
