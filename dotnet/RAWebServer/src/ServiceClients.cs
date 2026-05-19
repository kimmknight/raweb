using RAWeb.Server.Management;

namespace RAWebServer {
  public static class SystemRemoteAppsClient {
    public static IManagementServiceDirectClient Proxy => ManagementServiceClient.Proxy;
  }
}
