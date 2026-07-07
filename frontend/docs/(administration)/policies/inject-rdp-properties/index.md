---
title: $t{{ policies.RegistryApps.AdditionalProperties.title }}
nav_title: Additional RemoteApp properties
redirects:
  - policies/RegistryApps.AdditionalProperties
---

By default, RAWeb automatically injects specific [RDP file properties](https://learn.microsoft.com/en-us/windows-server/remote/remote-desktop-services/clients/rdp-files) into every generated `.rdp` file to ensure a seamless experience. 

Administrators can use this policy to define custom RDP properties that will be appended to the generated `.rdp` files, overriding any default properties if they conflict. For example, you can use this policy to [hide the Session Host's local drives](/docs/security/host-system-drives).

## Default values

If this policy is not configured or left blank, the following properties are injected into all generated `.rdp` files by default:
- `drivestoredirect:s:*` (Redirects all local client drives to the session)
- `redirectclipboard:i:1` (Enables clipboard redirection)

<PolicyDetails translationKeyPrefix="policies.RegistryApps.AdditionalProperties" open />
