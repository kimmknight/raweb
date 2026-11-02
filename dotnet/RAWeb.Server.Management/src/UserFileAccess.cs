using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace RAWeb.Server.Management;

/// <summary>
/// Checks whether a user may access a file using the file's security descriptor
/// and the user's group memberships.
/// <br /><br />
/// Use this before a privileged process (e.g., the management service, which runs as
/// LocalSystem) reads a file on behalf of a user. Otherwise, the user could read files
/// that only the privileged process can read.
/// </summary>
internal static class UserFileAccess {
  private const uint FILE_READ_DATA = 0x0001;
  private const uint AUTHZ_RM_FLAG_NO_AUDIT = 0x1;

  /// <summary>
  /// Determines whether the user may read the data of the opened file.
  /// <br /><br />
  /// The check uses the security descriptor of the opened file instead of a path so that
  /// links (e.g., junctions or symbolic links) cannot point the check at a different file
  /// than the one that is read. Windows computes the user's groups (including domain groups)
  /// from the SID.
  /// <br /><br />
  /// Returns false if the access check cannot be completed (e.g., the groups for the
  /// user cannot be computed).
  /// </summary>
  /// <param name="file">The opened file. The handle must have read control access, which
  /// <see cref="FileAccess.Read"/> includes.</param>
  /// <param name="userSid">The user who requested the file.</param>
  public static bool CanRead(FileStream file, SecurityIdentifier userSid) {
    // read the security descriptor (owner, group, and access rules) from the opened file's handle
    byte[] securityDescriptor;
    try {
      securityDescriptor = file.GetAccessControl().GetSecurityDescriptorBinaryForm();
    }
    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PrivilegeNotHeldException) {
      return false;
    }

    var sidBytes = new byte[userSid.BinaryLength];
    userSid.GetBinaryForm(sidBytes, 0);

    // AuthzAccessCheck writes its results into these caller-allocated buffers (one entry each
    // because we check a single object)
    var resourceManager = IntPtr.Zero;
    var clientContext = IntPtr.Zero;
    var grantedAccessMask = Marshal.AllocHGlobal(sizeof(uint));
    var saclEvaluationResults = Marshal.AllocHGlobal(sizeof(uint));
    var error = Marshal.AllocHGlobal(sizeof(uint));
    try {
      // AuthZ requires a resource manager even though we only need it for this one check
      if (!AuthzInitializeResourceManager(AUTHZ_RM_FLAG_NO_AUDIT, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, null, out resourceManager)) {
        return false;
      }

      // create a client context for the user from only their SID, which makes Windows compute
      // the user's groups (including nested domain groups) like it would for a logon token
      if (!AuthzInitializeContextFromSid(0, sidBytes, resourceManager, IntPtr.Zero, default, IntPtr.Zero, out clientContext)) {
        return false;
      }

      // ask Windows whether that client context would be granted read access to the file
      var request = new AUTHZ_ACCESS_REQUEST { DesiredAccess = FILE_READ_DATA };
      var reply = new AUTHZ_ACCESS_REPLY {
        ResultListLength = 1,
        GrantedAccessMask = grantedAccessMask,
        SaclEvaluationResults = saclEvaluationResults,
        Error = error
      };
      if (!AuthzAccessCheck(0, clientContext, ref request, IntPtr.Zero, securityDescriptor, IntPtr.Zero, 0, ref reply, IntPtr.Zero)) {
        return false;
      }

      var accessCheckError = (uint)Marshal.ReadInt32(error);
      var grantedAccess = (uint)Marshal.ReadInt32(grantedAccessMask);
      return
        // a non-zero error (e.g., ERROR_ACCESS_DENIED) means read access was not granted
        accessCheckError == 0
        // the call can succeed while denying access, so we also check that read access is granted
        && (grantedAccess & FILE_READ_DATA) == FILE_READ_DATA;
    }
    finally {
      if (clientContext != IntPtr.Zero) {
        AuthzFreeContext(clientContext);
      }
      if (resourceManager != IntPtr.Zero) {
        AuthzFreeResourceManager(resourceManager);
      }
      Marshal.FreeHGlobal(grantedAccessMask);
      Marshal.FreeHGlobal(saclEvaluationResults);
      Marshal.FreeHGlobal(error);
    }
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct LUID {
    public uint LowPart;
    public int HighPart;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct AUTHZ_ACCESS_REQUEST {
    public uint DesiredAccess;
    public IntPtr PrincipalSelfSid;
    public IntPtr ObjectTypeList;
    public uint ObjectTypeListLength;
    public IntPtr OptionalArguments;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct AUTHZ_ACCESS_REPLY {
    public uint ResultListLength;
    public IntPtr GrantedAccessMask;
    public IntPtr SaclEvaluationResults;
    public IntPtr Error;
  }

  [DllImport("authz.dll", SetLastError = true, CharSet = CharSet.Unicode)]
  private static extern bool AuthzInitializeResourceManager(
    uint flags,
    IntPtr pfnDynamicAccessCheck,
    IntPtr pfnComputeDynamicGroups,
    IntPtr pfnFreeDynamicGroups,
    string? szResourceManagerName,
    out IntPtr phAuthzResourceManager
  );

  [DllImport("authz.dll", SetLastError = true)]
  private static extern bool AuthzInitializeContextFromSid(
    uint flags,
    byte[] userSid,
    IntPtr hAuthzResourceManager,
    IntPtr pExpirationTime,
    LUID identifier,
    IntPtr dynamicGroupArgs,
    out IntPtr phAuthzClientContext
  );

  [DllImport("authz.dll", SetLastError = true)]
  private static extern bool AuthzAccessCheck(
    uint flags,
    IntPtr hAuthzClientContext,
    ref AUTHZ_ACCESS_REQUEST pRequest,
    IntPtr hAuditEvent,
    byte[] pSecurityDescriptor,
    IntPtr optionalSecurityDescriptorArray,
    uint optionalSecurityDescriptorCount,
    ref AUTHZ_ACCESS_REPLY pReply,
    IntPtr phAccessCheckResults
  );

  [DllImport("authz.dll", SetLastError = true)]
  private static extern bool AuthzFreeContext(IntPtr hAuthzClientContext);

  [DllImport("authz.dll", SetLastError = true)]
  private static extern bool AuthzFreeResourceManager(IntPtr hAuthzResourceManager);
}
