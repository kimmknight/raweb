---
title: $t{{ policies.RegistryApps.AdditionalProperties.title }}
nav_title: Additional RemoteApp properties
redirects:
  - policies/RegistryApps.AdditionalProperties
---

RAWeb has the ability to inject administrator-specified [RDP file properties](https://learn.microsoft.com/en-us/windows-server/remote/remote-desktop-services/clients/rdp-files) into all RDP files served by RAWeb. To edit the RDP file properties for a specific resource, see [Customize individual RDP file properties](/docs/publish-resources/#manage-rdp-file-properties).

The properties should be specified exactly as they would appear in the RDP file. Specify one property at a time. The properties will be added to the RDP file as-is, so ensure they are valid RDP properties.

If a RemoteApp's or desktop's RDP file is already signed, RAWeb will refus to inject any additional properties that are
in [the list of signable properties](/docs/policies/manage-rdp-signatures/#signed-properties) unless the [**Manage RDP signatures** policy](/docs/policies/manage-rdp-signatures/) is set to **Sign unsigned and re-sign signed**. Editing a signed RDP file to without re-signing it would invalidate the signature, and the Windows Remote Desktop client will reject the file with "This RDP File is corrupted".

<PolicyDetails translationKeyPrefix="policies.RegistryApps.AdditionalProperties" />

## Default values

On new installations, RAWeb preconfigures this policy with the following properties by default:

- `drivestoredirect:s:*` (Redirects all local client drives to the session)
- `redirectclipboard:i:1` (Enables clipboard redirection)

If an administrator changes the policy to "not configured", the preconfiguration is removed and RAWeb will no longer inject any additional properties.
