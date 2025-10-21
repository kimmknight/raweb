using System.Net.Security;
using System.ServiceModel;
using RAWeb.Server.Management;

namespace RAWebServer {
  public static class SystemRemoteAppsClient {
#if RELEASE
    const string EndpointName = "SystemRemoteApps";
#else
    const string EndpointName = "SystemRemoteApps-Dev";
#endif

    private static readonly NetNamedPipeBinding s_binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.Transport) {
      Security = { Transport = { ProtectionLevel = ProtectionLevel.EncryptAndSign } }, // use authenticated transport

      // we need to increase the limit because the default is not enough for systems with many installed applications
      MaxReceivedMessageSize = 1024 * 1024, // 1 MB
      ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas {
        MaxStringContentLength = 1024 * 1024, // 1 MB
        MaxArrayLength = 1024 * 1024 // 1 MB
      }
    };

    private static readonly ChannelFactory<ISystemRemoteAppsService> s_factory =
        new ChannelFactory<ISystemRemoteAppsService>(s_binding, new EndpointAddress("net.pipe://localhost/RAWeb/" + EndpointName));

    public static ISystemRemoteAppsService Proxy {
      get {
        return s_factory.CreateChannel();
      }
    }
  }
}
