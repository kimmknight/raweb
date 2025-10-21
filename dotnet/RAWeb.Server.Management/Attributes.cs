using System;
using System.Security.Principal;

namespace RAWeb.Server.Management;

public class ElevatedPrivileges {
  public static bool Check() {
    var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    var isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
    return isElevated;
  }

  public static void Require() {
    var isElevated = Check();
    if (!isElevated) {
      throw new UnauthorizedAccessException("The current process does not have elevated/administrative privileges.");
    }
  }
}
