---
title: $t{{ policies.RDP.StripSignatures.title }}
nav_title: Strip RDP signatures
redirects:
  - policies/RDP.StripSignatures
---

When enabled, this policy removes the cryptographic signature from generated `.rdp` files.

By default, RAWeb generates signed RDP files to prevent security warnings when users launch the connection. However, signed RDP files cannot be modified by the user (for example, to change local resource redirection settings like Printers or Clipboard) without breaking the signature.

Enabling this policy strips the signature by removing the `signature:s` and `signscope:s` RDP file properties, allowing users to safely edit the downloaded `.rdp` file before connecting, at the cost of displaying an "Unknown Publisher" warning when connecting.

<PolicyDetails translationKeyPrefix="policies.RDP.StripSignatures" open />
