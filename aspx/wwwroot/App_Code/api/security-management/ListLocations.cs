using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Management;
using System.Web.Http;

namespace RAWebServer.Api {
  public partial class SecurityManagementController : ApiController {

    [HttpGet]
    [Route("locations")]
    [RequireLocalAdministrator]
    public IHttpActionResult ListLocations() {
      var locations = new List<string>();

      // detect whether the local machine is a domain controller
      var isDomainController = false;
      try {
        using (var searcher = new ManagementObjectSearcher("SELECT DomainRole FROM Win32_ComputerSystem")) {

          foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>()) {
            var role = Convert.ToInt32(obj["DomainRole"]);
            // Domain role values 4 or 5 → DC or backup DC
            if (role == 4 || role == 5) {
              isDomainController = true;
            }
          }
        }
      }
      catch {
        // ignore management lookup failures
      }

      // include the local machine name unless it is a domain controller
      if (!isDomainController) {
        locations.Add(Environment.MachineName);
      }

      // check if the local machine is part of a domain
      Domain defaultDomain = null;
      try {
        defaultDomain = Domain.GetComputerDomain();
      }
      catch (ActiveDirectoryObjectNotFoundException) {
      }

      // if the local machine is part of a domain, list all domains in the forest
      if (defaultDomain != null) {
        var forest = Forest.GetCurrentForest();

        foreach (Domain domain in forest.Domains) {
          locations.Add(domain.Name);
        }
      }

      return Ok(locations);
    }
  }
}
