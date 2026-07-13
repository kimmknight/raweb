---
title: $t{{ policies.RDP.StripSignatures.title }}
nav_title: Strip RDP signatures
redirects:
  - policies/RDP.StripSignatures
---

When enabled, this policy removes the cryptographic signature from `.rdp` files by removing the `signature:s` and `signscope:s` RDP file properties before the `.rdp` file is served by RAWeb.

This policy is useful in scenarios where signed `.rdp` files were provided to RAWeb. \
If an administrator needs to edit the RDP file properties for the RDP files served by RAWeb, and the property to edit is included in the `signscope:s` property, the signature must be stripped before the RDP file can be modified. Otherwise, users will see a security warning when connecting to a resource via the `.rdp` file.

Removing the `signature:s` and `signscope:s` properties allows users to download a resource as an RDP file and freely configure options such as Clipboard, Printers, and Local Drives in mstsc.exe before connecting.

<PolicyDetails translationKeyPrefix="policies.RDP.StripSignatures" />
