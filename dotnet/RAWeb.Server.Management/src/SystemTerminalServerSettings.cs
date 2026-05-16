using System;
using System.ServiceModel;

namespace RAWeb.Server.Management;

public class SystemTerminalServerSettings {
  /// <summary>
  /// Checks if the system Remote Desktop is enabled by reading the 'fDenyTSConnections' value from the registry.
  /// </summary>
  /// <returns></returns>
  /// <exception cref="Exception"></exception>
  public static bool AreConnectionsAllowed {
    get {
      using var regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server");
      if (regKey == null) {
        throw new Exception("Could not read registry key for Terminal Server settings.");
      }

      var fDenyTSConnectionsValue = regKey.GetValue("fDenyTSConnections");
      if (fDenyTSConnectionsValue == null) {
        throw new Exception("Could not read 'fDenyTSConnections' value from registry.");
      }

      return (int)fDenyTSConnectionsValue == 0;
    }
  }
}


[ServiceContract]
public interface IManagedSystemTerminalServerSettings {
  /// <summary>
  /// Service implementation of <c>SystemTerminalServerSettings.AreConnectionsAllowed</c>.
  /// </summary>
  [OperationContract]
  bool AreConnectionsAllowed();
}
