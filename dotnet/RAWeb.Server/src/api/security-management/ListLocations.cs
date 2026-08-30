using System.DirectoryServices.Protocols;
using Microsoft.Win32;
using RAWeb.Server.Management;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class ListLocationsEndpoint {
  internal static void Map(IEndpointRouteBuilder app) {
    app.MapGet("/api/management/security/locations", Handle);
  }

  private static IResult Handle(HttpContext ctx) {
    var userInfo = UserInformation.FromHttpRequestSafe(ctx.Request);
    if (userInfo is null || !userInfo.AuthTicketLevel.IsAdmin) {
      return Results.Forbid();
    }

    var locations = new List<string>();

    // detect whether the local machine is a domain controller
    var isDomainController = IsDomainController();

    // include the local machine name unless it is a domain controller
    if (!isDomainController) {
      locations.Add(Environment.MachineName);
    }

    // get the current domain name via IPGlobalProperties
    var domainName = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
    if (string.IsNullOrEmpty(domainName)) {
      return Results.Ok(locations);
    }

    // if the local machine is part of a domain, list all domains in the forest
    List<string> forestDomains;
    try {
      forestDomains = GetForestDomains(domainName);
    }
    catch {
      // if forest enumeration fails, just include the local domain
      forestDomains = [domainName];
    }

    // add all forest domains and sort with the local domain first
    foreach (var domain in forestDomains) {
      if (!locations.Contains(domain, StringComparer.OrdinalIgnoreCase)) {
        locations.Add(domain);
      }
    }

    // if the local machine is part of a domain, make sure the default domain is first in the list
    locations = locations
      .OrderByDescending(loc => string.Equals(loc, domainName, StringComparison.OrdinalIgnoreCase))
      .ThenBy(loc => loc, StringComparer.OrdinalIgnoreCase)
      .ToList();

    return Results.Ok(locations);
  }

  /// <summary>
  /// Checks for indicators in the registry to determine if the local machine
  /// is likely a domain controller.
  /// </summary>
  /// <returns></returns>
  private static bool IsDomainController() {
    // NTDS service registry key usually only exists on domain controllers
    using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\NTDS");
    if (key is null) {
      return false;
    }

    // Confirm that the product type is also a domain controller
    using var key2 = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\ProductOptions");
    var productType = key2?.GetValue("ProductType") as string;
    return productType == "LanmanNT"; // Domain controller product type
  }

  /// <summary>
  /// Queries Active Directory to enumerate all domains in the current forest.
  /// </summary>
  /// <param name="domainName"></param>
  /// <returns></returns>
  private static List<string> GetForestDomains(string domainName) {
    using var connection = LdapHelpers.OpenLdapConnection(domainName);

    // Query rootDSE for the configuration naming context, which contains
    // forest-wide configuration data including the list of all domains
    var rootDSEReq = new SearchRequest("", "(objectClass=*)", SearchScope.Base, "configurationNamingContext");
    var rootDSEResp = (SearchResponse)connection.SendRequest(rootDSEReq);
    var configNC = (string)rootDSEResp.Entries[0].Attributes["configurationNamingContext"][0];

    // Search the Partitions container for all crossRef objects representing domain partitions.
    // The bitwise AND filter (systemFlags:1.2.840.113556.1.4.803:=2) matches objects where
    // bit 1 (CR_NTDS_DOMAIN) is set. This flag is only set on actual AD domain naming contexts
    // and excludes application partitions (DomainDnsZones, ForestDnsZones), the configuration
    // naming context, and the schema naming context which only have bit 0 (CR_NTDS_NC) set.
    var partitionsContainer = "CN=Partitions," + configNC;
    var req = new SearchRequest(
        partitionsContainer,
        "(&(objectClass=crossRef)(systemFlags:1.2.840.113556.1.4.803:=2))",
        SearchScope.OneLevel,
        "dnsRoot"
    );
    var resp = (SearchResponse)connection.SendRequest(req);

    // Extract the dnsRoot attribute from each crossRef, which contains the each domain's DNS name
    var domains = new List<string>();
    foreach (SearchResultEntry entry in resp.Entries) {
      if (entry.Attributes["dnsRoot"]?.Count > 0) {
        domains.Add((string)entry.Attributes["dnsRoot"][0]);
      }
    }
    return domains;
  }
}
