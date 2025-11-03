using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using System.Text;

namespace RAWeb.Server.Utilities;

public class GroupInformation {
  public string? Name { get; set; }
  public string Sid { get; set; }
  public string? DN { get; set; }

  /// <summary>
  /// Escaped distinguished name for LDAP filters.
  /// </summary>
  public string? EscapedDN {
    get {
      if (DN is null || string.IsNullOrWhiteSpace(DN)) {
        return null;
      }

      // escape the distinguished name for LDAP filters
      var sb = new StringBuilder();
      foreach (var c in DN) {
        switch (c) {
          case '*': sb.Append(@"\2A"); break;
          case '(': sb.Append(@"\28"); break;
          case ')': sb.Append(@"\29"); break;
          case '\\': sb.Append(@"\5C"); break;
          default: sb.Append(c); break;
        }
      }
      return sb.ToString();
    }
  }

  public GroupInformation(string? name, string sid, string? dn = null) {
    Name = name;
    Sid = sid;
    DN = dn;
  }

  public GroupInformation(GroupPrincipal groupPrincipal) {
    if (groupPrincipal == null) {
      throw new ArgumentNullException(nameof(groupPrincipal), "GroupPrincipal cannot be null.");
    }

    Name = groupPrincipal.Name;
    Sid = groupPrincipal.Sid.ToString();
    DN = groupPrincipal.DistinguishedName;
  }

  public GroupInformation(string sid) {
    Name = ResolveLocalizedGroupName(sid);
    Sid = sid;
    DN = null;
  }

  public static string ResolveLocalizedGroupName(SecurityIdentifier sid) {
    try {
      var account = (NTAccount)sid.Translate(typeof(NTAccount));
      var groupName = account.Value.Split('\\')[1]; // remove the machine name
      return groupName;
    }
    catch (Exception) {
      return sid.ToString(); // return the SID string if the name cannot be resolved
    }
  }

  public static string ResolveLocalizedGroupName(string sidString) {
    var sid = new SecurityIdentifier(sidString);
    return ResolveLocalizedGroupName(sid);
  }
}
