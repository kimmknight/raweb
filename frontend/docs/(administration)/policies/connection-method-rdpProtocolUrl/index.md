---
title: $t{{ policies.App.ConnectionMethod.RdpProtocol.Enabled.title }}
nav_title: RDP URI connection method
redirects:
  - policies/App.ConnectionMethod.RdpProtocol.Enabled
---

This policy controls whether the option to launch a resources via its rdp:// URI is available to users when connecting to resources.

<img width="400" src="../connection-method-rdpFile/rdpProtocolUri-method.webp" />

When enabled, users will see a "Launch via rdp://" button in the connection dialog, allowing them to directly launch a resource without downloading it first. On supported systems, this will open the resource in the user's default RDP client application. See the table below for enabling support for rdp:// URIs on different platforms:

| Platform      | Required application |
|---------------|----------------------|
| Windows       | [Remote Desktop Protocol Handler](https://apps.microsoft.com/detail/9N1192WSCHV9?hl=en-us&gl=US&ocid=pdpshare) from the Microsoft Store or from [jackbuehner/rdp-protocol-handler](https://github.com/jackbuehner/rdp-protocol-handler/releases) |
| macOS         | [Windows App](https://apps.apple.com/us/app/windows-app/id1295203466) from the Mac App Store |
| iOS or iPadOS | [Windows App Mobile](https://apps.apple.com/us/app/windows-app-mobile/id714464092) from the App Store |
| Android       | Not supported |

When disabled, the "Launch via rdp://" option will not be shown.

If no connection methods are enabled, users will be unable to connect to resources via the web app. Instead, they will see this following dialog:

<img width="400" src="../connection-method-rdpFile/no-connection-method.webp" />

<PolicyDetails translationKeyPrefix="policies.App.ConnectionMethod.RdpProtocol.Enabled" />
