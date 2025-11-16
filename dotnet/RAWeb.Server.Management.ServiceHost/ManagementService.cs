using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceProcess;
using RAWeb.Server.Management;
using static RAWeb.Server.Management.SystemRemoteApps;

/// <summary>
/// The RAWeb Management Service.
/// </summary>
public class ManagementService : ServiceBase {
  private ServiceHost? _host;

  protected override void OnStart(string[] args) {
    // close any existing host
    _host?.Close();

#if RELEASE
    const string endpointName = "SystemRemoteApps";
#else
    const string endpointName = "SystemRemoteApps-Dev";
#endif

    // create the service host
    _host = new ServiceHost(typeof(SystemRemoteAppsServiceHost));
    _host.AddServiceEndpoint(
        typeof(IManagedResourceService),
        ManagementServiceBinding.Create(),
        $"net.pipe://localhost/RAWeb/{endpointName}"
    );

    // require Windows authentication
    var authBehavior = _host.Description.Behaviors.Find<ServiceAuthorizationBehavior>();
    if (authBehavior == null) {
      authBehavior = new ServiceAuthorizationBehavior();
      _host.Description.Behaviors.Add(authBehavior);
    }

    // this will allow us to get the Windows groups of the authenticated user
    authBehavior.PrincipalPermissionMode = PrincipalPermissionMode.UseWindowsGroups;
    _host.Credentials.WindowsAuthentication.AllowAnonymousLogons = false;
    _host.Credentials.WindowsAuthentication.IncludeWindowsGroups = true;

    // open the service host
    _host.Open();
  }

  protected override void OnStop() {
    _host?.Close();
  }
}

public class ManagementServiceBinding {
  /// <summary>
  /// Creates the NetNamedPipeBinding used for the management service.
  /// This binding should be used on the service host and the client.
  /// This binding uses the streamed transfer mode so that image
  /// stream can be more easily transferred without hitting size limits.
  /// </summary>
  /// <returns></returns>
  public static NetNamedPipeBinding Create() {
    const int MiB = 1024 * 1024;

    return new NetNamedPipeBinding(NetNamedPipeSecurityMode.Transport) {
      Security = { Transport = { ProtectionLevel = ProtectionLevel.EncryptAndSign } }, // use authenticated transport

      // we need to increase the limit because the default is not enough for systems with many installed applications
      MaxReceivedMessageSize = MiB,
      ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas {
        MaxStringContentLength = MiB,
        MaxArrayLength = MiB,
      },
      TransferMode = TransferMode.Streamed
    };
  }
}

/// <summary>
/// Implements the IManagedResourceService interface for use in the management service.
/// </summary>
[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
public class SystemRemoteAppsServiceHost : IManagedResourceService {
  /// <summary>
  /// Ensures that the caller is authorized to perform management operations.
  /// <br /><br />
  /// Allowed identities: System, Local Administrators, IIS AppPool\raweb
  /// </summary>
  /// <exception cref="SecurityException"></exception>
  public void RequireAuthorization() {
    var identity = ServiceSecurityContext.Current?.WindowsIdentity;
    if (identity is null) {
      throw new SecurityException("Unauthenticated caller.");
    }

    string[] allowedIdentities = [
      @"IIS AppPool\raweb"
    ];

    // check if the caller is allowed: system, local admin, or in the allowed list
    var isAllowed = identity.IsSystem ||
                    NetUserGroupInformation.IsUserLocalAdministrator(identity.User.Value) ||
                    allowedIdentities.Contains(identity.Name, StringComparer.OrdinalIgnoreCase);
    if (!isAllowed) {
      throw new SecurityException($"Access denied for user {identity.Name}.");
    }
  }

  public void WriteRemoteAppToRegistry(SystemRemoteApp app) {
    RequireAuthorization();

    if (app is null) {
      throw new ArgumentNullException(nameof(app));
    }

    app.WriteToRegistry();
  }

  public void DeleteRemoteAppFromRegistry(SystemRemoteApp app) {
    RequireAuthorization();

    if (app is null) {
      throw new ArgumentNullException(nameof(app));
    }

    app.DeleteFromRegistry();
  }

  public InstalledApps ListInstalledApps(string? userSid) {
    RequireAuthorization();

    if (string.IsNullOrEmpty(userSid)) {
      var combined = InstalledApps.FromAppPackages().Concat(InstalledApps.FromStartMenu());
      return new InstalledApps([.. combined]);
    }
    else {
      var combined = InstalledApps.FromAppPackages().Concat(InstalledApps.FromStartMenu()).Concat(InstalledApps.FromStartMenu(new SecurityIdentifier(userSid)));
      return new InstalledApps([.. combined]);
    }
  }

  public void InitializeRegistryPaths(string? collectionName = null) {
    RequireAuthorization();
    new SystemRemoteApps(collectionName).EnsureRegistryPathExists();
  }

  public void InitializeDesktopRegistryPaths(string collectionName) {
    RequireAuthorization();
    var defaultDesktop = SystemDesktop.FromRegistry(collectionName, collectionName);
    if (defaultDesktop is null) {
      defaultDesktop = new SystemDesktop(collectionName, collectionName);
      defaultDesktop.WriteToRegistry();
    }
  }

  public void WriteDesktopToRegistry(SystemDesktop desktop) {
    RequireAuthorization();

    if (desktop is null) {
      throw new ArgumentNullException(nameof(desktop));
    }

    desktop.WriteToRegistry();
  }

  public void DeleteDesktopFromRegistry(SystemDesktop desktop) {
    RequireAuthorization();

    if (desktop is null) {
      throw new ArgumentNullException(nameof(desktop));
    }

    desktop.DeleteFromRegistry();
  }

  public Stream GetWallpaperStream(SystemDesktop desktop, ManagedFileResource.ImageTheme theme, string? userSid) {
    RequireAuthorization();

    if (desktop is null) {
      throw new ArgumentNullException(nameof(desktop));
    }

    try {
      var stream = desktop.GetWallpaperStream(theme, new SecurityIdentifier(userSid));
      return stream;
    }
    catch (Exception ex) {
      throw ManagedResourceFaultException.FromException(ex);
    }
  }
}

/// <summary>
/// Adapted subset of RAWebServer.Utilities.AuthUtilities.NetUserGroupInformation
/// </summary>
public class NetUserGroupInformation {

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

  public static bool IsUserLocalAdministrator(string userSid) {
    return IsUserMemberOfLocalGroup(new SecurityIdentifier(userSid), new SecurityIdentifier("S-1-5-32-544"));
  }

  public static string ResolveLocalizedGroupName(SecurityIdentifier sid) {
    try {
      var account = (NTAccount)sid.Translate(typeof(NTAccount));
      var groupName = account.Value.Split('\\')[1]; // remove the machine name
      return groupName;
    }
    catch (Exception) {
      return sid.ToString(); // return the SID string if the name cannot be resolved
    }
  }

  public static bool IsUserMemberOfLocalGroup(SecurityIdentifier userSid, SecurityIdentifier groupSid) {
    var level = 2;

    // resolve the SID to a localized name
    var groupName = ResolveLocalizedGroupName(groupSid);

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
