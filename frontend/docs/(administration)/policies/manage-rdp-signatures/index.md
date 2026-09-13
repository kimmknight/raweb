---
title: $t{{ policies.RDP.ManageSignatures.title }}
nav_title: Manage RDP signatures
redirects:
  - policies/RDP.StripSignatures
  - policies/RDP.ManageSignatures
  - policies/strip-rdp-signatures
---

This policy controls whether RAWeb strips or adds the `signature:s` and `signscope:s` RDP file properties before an `.rdp` file is served.

Four modes are available:

- **Do nothing**: RDP files are served as-is.
- **Strip all signatures**: the `signature:s` and `signscope:s` properties are removed from every RDP file. This is useful when a signed `.rdp` file was provided to RAWeb but an administrator needs to edit a property covered by `signscope:s` (which would otherwise cause a security warning when connecting). Stripping signatures also allows signed RDP files to be launched via `rdp://` URLs and lets users freely configure options such as Clipboard, Printers, and Local Drives in mstsc.exe before connecting.
- **Sign unsigned**: RDP files that do not already have a signature are signed with the configured certificate. Files that already carry a signature are left untouched.
- **Sign unsigned and re-sign signed**: RDP files are always (re-)signed with the configured certificate, replacing any existing signature.

Signing lets Remote Desktop clients detect tampering with security-relevant RDP file properties between RAWeb and the client. As of April 2026, Windows Remote Desktop clients also show a warning when attempting to use an unsigned and untrusted RDP file.

<PolicyDetails translationKeyPrefix="policies.RDP.ManageSignatures" />

## Certificate

Signing requires a certificate in the **LocalMachine\My** certificate store on the RAWeb server. The installer will configure RAWeb to use the certificate bound to the web site's HTTPS binding. The certificate must be trusted by the client machine for signing to be effective.

If the configured certificate cannot be found or used (it expired or was removed), RDP files are served unsigned.

If you change the web site's HTTPS binding to use a different certificate, you must edit the policy to have the correct certificate thumbprint. Additionally, you must make sure that the new certificate can be read by the application pool identity of the RAWeb installation. The easiest way to do this is to re-run the RAWeb installer and enable the option to sign RDP files.

## Signed properties {#signed-properties}

When signing is enabled, every signable RDP property is included in the signature if it is present in the file. There is no way to sign a file while excluding an individual property; the RDP signing format has no way to leave a recognized signable property present in the file but outside the signature, and the Windows Remote Desktop client rejects such a file with the message, "This RDP File is corrupted".

For the same reason, if an RDP file that was provided to RAWeb was already signed, RAWeb will not add or change a signable property on that file unless the policy is set to **Sign unsigned and re-sign signed**. This applies both to the values RAWeb itself would normally set for a resource (such as its address or RemoteApp command line) and to properties added via the [Additional RemoteApp properties](/docs/policies/inject-rdp-properties/) policy. In the RDP file properties editor, a signable property on an already-signed file is disabled and is indicated by a lock icon.

The following properties are signable and will be included in the signature if present:

- full address:s:
- alternate full address:s:
- pcb:s
- use redirection server name:i:
- server port:i:
- negotiate security layer:i:
- enablecredsspsupport:i
- disableconnectionsharing:i
- autoreconnection enabled:i
- gatewayhostname:s
- gatewayusagemethod:i
- gatewayprofileusagemethod:i
- gatewaycredentialssource:i
- support url:s:
- promptcredentialonce:i
- require pre-authentication:i:
- pre-authentication server address:s:
- alternate shell:s:
- shell working directory:s:
- remoteapplicationprogram:s
- remoteapplicationexpandworkingdir:s
- remoteapplicationmode:i
- remoteapplicationguid:s
- remoteapplicationname:s
- remoteapplicationicon:s
- remoteapplicationfile:s
- remoteapplicationfileextensions:s
- remoteapplicationcmdline:s
- remoteapplicationexpandcmdline:s
- prompt for credentials:i:
- authentication level:i:
- audiomode:i
- redirectdrives:i
- redirectprinters:i
- redirectcomports:i
- redirectsmartcards:i
- redirectposdevices:i
- redirectclipboard:i
- devicestoredirect:s
- drivestoredirect:s
- loadbalanceinfo:s
- redirectdirectx:i
- rdgiskdcproxy:i
- kdcproxyname:s
- eventloguploadaddress:s
- enablerdsaadauth:i
- redirectwebauthn:i
