using System.ServiceModel;
using RAWeb.Server.Management;

namespace RAWebServer {
  public static class SystemRemoteAppsClient {
#if RELEASE
    const string EndpointName = "SystemRemoteApps";
#else
    const string EndpointName = "SystemRemoteApps-Dev";
#endif

    private static readonly NetNamedPipeBinding s_binding = ManagementServiceBinding.Create();

    private static readonly ChannelFactory<IManagedResourceService> s_factory =
        new ChannelFactory<IManagedResourceService>(s_binding, new EndpointAddress("net.pipe://localhost/RAWeb/" + EndpointName));

    public static IManagedResourceService Proxy {
      get {
        return s_factory.CreateChannel();
      }
    }
  }
}
