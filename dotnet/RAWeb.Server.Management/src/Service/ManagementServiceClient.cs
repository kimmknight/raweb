using System;
using System.ServiceModel;

namespace RAWeb.Server.Management;

/// <summary>
/// Factory for WCF client channels to the RAWeb Management Service.
/// </summary>
/// <remarks>
/// The RAWeb Management Service includes the application pool name
/// in the named pipe endpoint address to allow multiple instances of the
/// service to run side by side for different RAWeb installations. However,
/// this means that the client needs to know the application pool name in order
/// contact the service.
/// 
/// Defaults to "raweb" to maintain compatability with RAWeb installations that
/// were manually installed back when the application pool name was required
/// to be raweb.
/// </remarks>
public static class ManagementServiceClient {
  private static string AppPoolName => Environment.GetEnvironmentVariable("APP_POOL_ID") ?? "raweb";

#if RELEASE
  private static readonly System.ServiceModel.Channels.Binding s_binding = ManagementServiceBinding.Create();
  private static readonly string s_address = $"net.pipe://localhost/RAWeb/SystemRemoteApps/{AppPoolName}";
#else
  private static readonly System.ServiceModel.Channels.Binding s_binding = ManagementServiceBinding.CreateHttpForDevelopment();
  private static readonly string s_address = "http://localhost:8090/RAWeb/SystemRemoteApps-Dev";
#endif

  private static readonly ChannelFactory<IManagementServiceHost> s_factory =
      new(s_binding, new EndpointAddress(s_address));

  /// <summary>
  /// Creates a new WCF channel to the management service. Callers are responsible for
  /// closing the channel (cast to <see cref="IClientChannel"/> and call Close/Abort).
  /// </summary>
  public static IManagementServiceHost Proxy => s_factory.CreateChannel();
}
