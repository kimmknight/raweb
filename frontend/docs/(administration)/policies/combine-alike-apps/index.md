---
title: $t{{ policies.App.CombineTerminalServersModeEnabled.title }}
nav_title: Combine alike apps
redirects:
  - policies/App.CombineTerminalServersModeEnabled
---

<PolicyDetails translationKeyPrefix="policies.App.CombineTerminalServersModeEnabled" open />

# How apps are matched

Two RemoteApps are treated as the same app when they have the same name and run the same program with the same command-line arguments. Their other RDP settings can be different.

When comparing program locations, the drive letter is ignored. If the location contains a `Program Files` or `Program Files (x86)` folder, only the part after that folder is compared. For example, `C:\Program Files\Vendor\App.exe` and `D:\Program Files (x86)\Vendor\App.exe` are the same program.

The icon and file type associations of a combined app come from only one of the matching apps. Connecting to a server always uses that server's own settings. Desktops are never combined.
