using System.Net.Security;
using System.ServiceModel;

namespace RAWeb.Server.Management;

/// <summary>
/// Combined WCF service contract implemented by the RAWeb Management Service.
/// Use <see cref="ManagementServiceClient.Proxy"/> to obtain a channel.
/// </summary>
[ServiceContract]
public interface IManagementServiceHost : IManagedResourceService, IManagedSystemTerminalServerSettings {
}

/// <summary>
/// WCF bindings shared by the management service host and all clients.
/// </summary>
public static class ManagementServiceBinding {
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
      Security = { Transport = { ProtectionLevel = ProtectionLevel.EncryptAndSign } },  // use authenticated transport

      // we need to increase the limit because the default is not enough for systems with many installed applications
      MaxReceivedMessageSize = MiB,
      ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas {
        MaxStringContentLength = MiB,
        MaxArrayLength = MiB,
      },
      TransferMode = TransferMode.Streamed
    };
  }

  /// <summary>
  /// Creates the BasicHttpBinding used for the management service in development
  /// builds, where we use HTTP instead of named pipes since using SSH into a
  /// development machine creates non-interactive sessions where named pipes won't work.
  /// </summary>
  /// <returns></returns>
  public static BasicHttpBinding CreateHttpForDevelopment() {
    const int MiB = 1024 * 1024;

    return new BasicHttpBinding {
      Security = {
        Mode = BasicHttpSecurityMode.TransportCredentialOnly,
        Transport = { ClientCredentialType = HttpClientCredentialType.Windows }
      },
      MaxReceivedMessageSize = MiB,
      ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas {
        MaxStringContentLength = MiB,
        MaxArrayLength = MiB,
      },
      TransferMode = TransferMode.Buffered
    };
  }
}
