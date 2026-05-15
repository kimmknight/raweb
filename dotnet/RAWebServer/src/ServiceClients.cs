using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace RAWebServer {
  public static class SystemRemoteAppsClient {
    /// <summary>
    /// The RAWeb Management Service includes the application pool name
    /// in the named pipe endpoint address to allow multiple instances of the
    /// service to run side by side for different RAWeb installations. However,
    /// this means that the client needs to know the application pool name in order
    /// contact the service.
    /// 
    /// Defaults to "raweb" to maintain compatability with RAWeb installations that
    /// were manually installed back when the application pool name was required
    /// to be raweb.
    /// </summary>
    private static string appPoolName => Environment.GetEnvironmentVariable("APP_POOL_ID") ?? "raweb";

#if RELEASE
    private static readonly Binding s_binding = ManagementServiceBinding.Create();
    private static readonly string s_address = $"net.pipe://localhost/RAWeb/SystemRemoteApps/{appPoolName}";
#else
    private static readonly Binding s_binding = ManagementServiceBinding.CreateHttpForDevelopment();
    private static readonly string s_address = "http://localhost:8090/RAWeb/SystemRemoteApps-Dev";
#endif

    private static readonly ChannelFactory<ISystemRemoteAppsServiceHost> s_factory =
        new ChannelFactory<ISystemRemoteAppsServiceHost>(s_binding, new EndpointAddress(s_address));

    public static ISystemRemoteAppsServiceHost Proxy {
      get {
        return s_factory.CreateChannel();
      }
    }
  }
}
