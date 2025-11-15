using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Principal;
using Microsoft.Win32;

namespace RAWeb.Server.Management;

/// <summary>
/// Represents information about a user profile on the system.
/// </summary>
public class SystemUserProfile(string username, string profilePath, string displayName, SecurityIdentifier sid) {
  public string UserName { get; set; } = username;
  public string ProfilePath { get; set; } = profilePath;
  public string DisplayName { get; set; } = displayName;
  public SecurityIdentifier Sid { get; set; } = sid;

  public override string ToString() {
    return DisplayName + " (" + UserName + ")";
  }

  /// <summary>
  /// Gets a list of installed applications for this user profile based on the Start Menu entries.
  /// </summary>
  /// <returns></returns>
  public InstalledApps GetStartMenuApps() {
    ElevatedPrivileges.Require();
    return InstalledApps.FromStartMenu(Sid, this);
  }
}

/// <summary>
/// A collection of user profiles on the system.
/// </summary>
public class SystemUserProfiles : Collection<SystemUserProfile> {
  /// <summary>
  /// Initializes a new instance of the <see cref="SystemUserProfiles"/> class
  /// that is populated with all user profiles on the system.
  /// <br /><br />
  /// See <see cref="LoadFromRegistry"/> for details on how profiles are loaded.
  /// </summary>
  public SystemUserProfiles() {
    foreach (var profile in LoadFromRegistry())
      Add(profile);
  }

  public SystemUserProfiles(IEnumerable<SystemUserProfile> profiles) {
    foreach (var p in profiles)
      Add(p);
  }

  /// <summary>
  /// Gets a list of all user profiles on the system based on the registry.
  /// <br /><br />
  /// The profiles are read from the ProfileList registry key.
  /// If a profile folder does not exist on disk, the profile is skipped.
  /// This means that accounts that have never signed in to an interactive session
  /// (such as service accounts) will not be included in the list.
  /// </summary>
  /// <returns></returns>
  private static SystemUserProfiles LoadFromRegistry() {
    var collection = new SystemUserProfiles([]);

    var registryProfileListPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList";
    foreach (var sid in Registry.LocalMachine?.OpenSubKey(registryProfileListPath)?.GetSubKeyNames() ?? []) {
      using (var profileKey = Registry.LocalMachine?.OpenSubKey(registryProfileListPath + @"\" + sid)) {
        // get the path to the profile folder according to the registry
        var profilePath = (string?)profileKey?.GetValue("ProfileImagePath");

        // confirm that the profile path actualy exists
        // (service accounts, which don't have profile folders, may also be listed in the registry)
        var exists = profilePath is not null && System.IO.Directory.Exists(profilePath);
        if (!exists) {
          continue;
        }

        // resolve the sid into an account name
        var sidObj = new SecurityIdentifier(sid);
        string username;
        string domain;
        try {
          var ntAccount = (NTAccount)sidObj.Translate(typeof(NTAccount));
          var accountName = ntAccount.Value;
          var parts = accountName.Split('\\');
          if (parts.Length == 2) {
            domain = parts[0];
            username = parts[1];
          }
          else {
            domain = Environment.MachineName;
            username = accountName;
          }
        }
        catch {
          username = profilePath!.Split('\\').Last() ?? sid;
          domain = Environment.MachineName;
        }

        // resolve the sid into a display name
        // TODO: figure out why this times out on some machines, even with local accounts
        var displayName = domain + "\\" + username;
        // try {
        //   using (var context = new PrincipalContext(ContextType.Domain, domain)) {
        //     var principal = UserPrincipal.FindByIdentity(context, IdentityType.Sid, sid);

        //     if (principal != null && !string.IsNullOrEmpty(principal.DisplayName))
        //       displayName = principal.DisplayName;
        //   }
        // }
        // catch {
        //   if (domain == Environment.MachineName) {
        //     displayName = username;
        //   }
        // }

        // create the user profile object
        var userProfile = new SystemUserProfile(
          username: username,
          profilePath: profilePath!,
          displayName: displayName,
          sid: sidObj
        );
        collection.Add(userProfile);
      }
    }

    return collection;
  }
}
