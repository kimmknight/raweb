using RAWeb.Server.Management;

namespace RAWebServer {
  public static class SystemRemoteAppsClient {
    public static IManagementServiceHost Proxy => ManagementServiceClient.Proxy;
  }
}
