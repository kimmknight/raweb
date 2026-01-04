---
title: $t{{ policies.RegistryApps.Enabled.title }}
nav_title: Centralized publishing (registry)
redirects:
  - policies/RegistryApps.Enabled
---

Enable this policy to store published RemoteApps and desktops in their own collection in the registry. If you have RAWeb and RDWeb running on the same server or multiple installations of RAWeb, enabling the policy ensures that visibility settings for RemoteApps and desktops do not conflict between the installations.

This policy defaults to enabled, but older versions of RAWeb may have it disabled by default. If you are upgrading from an older version of RAWeb, you must enable this policy manually.

This policy must be enabled for RDP file property customizations to be available.

The TSWebAccess option in RemoteApp Tool only works when this policy is disabled.

The option to show the system desktop in the web interface and in Workspace clients is only available when this policy is enabled.

For instructions on managing published RemoteApps and desktops, see [Publish RemoteApps and Desktops](/docs/publish-resources/).

<PolicyDetails translationKeyPrefix="policies.RegistryApps.Enabled" open />
