using System.ServiceModel;
using System.ServiceModel.Channels;
using RAWeb.Server.Management;

namespace RAWebServer {
  public static class SystemRemoteAppsClient {
#if RELEASE
    const string EndpointName = "SystemRemoteApps";
#else
    const string EndpointName = "SystemRemoteApps-Dev";
#endif

#if RELEASE
    private static readonly Binding s_binding = ManagementServiceBinding.Create();
    private static readonly string s_address = "net.pipe://localhost/RAWeb/" + EndpointName;
#else
    private static readonly Binding s_binding = ManagementServiceBinding.CreateHttpForDevelopment();
    private static readonly string s_address = "http://localhost:8090/RAWeb/" + EndpointName;
#endif

    private static readonly ChannelFactory<IManagedResourceService> s_factory =
        new ChannelFactory<IManagedResourceService>(s_binding, new EndpointAddress(s_address));

    public static IManagedResourceService Proxy {
      get {
        return s_factory.CreateChannel();
      }
    }
  }
}
