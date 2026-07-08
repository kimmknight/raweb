---
title: $t{{ policies.RegistryApps.AdditionalProperties.title }}
nav_title: Additional RemoteApp properties
redirects:
  - policies/RegistryApps.AdditionalProperties
---

RAWeb has the ability to inject administrator-specified [RDP file properties](https://learn.microsoft.com/en-us/windows-server/remote/remote-desktop-services/clients/rdp-files) into all RDP files served by RAWeb.

Administrators can use this policy to define custom RDP properties that will be appended to the generated `.rdp` files. For example, you can use this policy to [hide the Session Host's local drives](/docs/security/host-system-drives).

<PolicyDetails translationKeyPrefix="policies.RegistryApps.AdditionalProperties" />

## Default values

On new installations, RAWeb preconfigures this policy with the following properties by default:
- `drivestoredirect:s:*` (Redirects all local client drives to the session)
- `redirectclipboard:i:1` (Enables clipboard redirection)

If an administrator changes the policy to "not configured", the preconfiguration is removed and RAWeb will no longer inject any additional properties.
