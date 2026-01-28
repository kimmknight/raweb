---
title: $t{{ policies.GuacdWebClient.Address.title }}
nav_title: Web client connection method
redirects:
  - policies/GuacdWebClient.Address
---

The web client allows users to access their desktops and applications through a web browser. This policy controls whether RAWeb will use a Guacamole daemon (guacd) as a remote desktop proxy for provide web client access.

<img width="400" src="../connection-method-rdpFile/webGuacd-method.webp" />

When enabled and properly configured, users will see a "Connect in browser" button in the connection dialog, allowing them to use the remote desktop connection proxy.

When disabled, the "Connect in browser" option will not be shown, preventing users from accessing the web client.

If no other connection methods are enabled, users will be unable to connect to resources via the web app. Instead, they will see this following dialog:

<img width="400" src="../connection-method-rdpFile/no-connection-method.webp" />

## Prerequisites

The web client requires the RAWeb server to have access to a [Guacamole](https://guacamole.apache.org/) daemon ([guacd](https://hub.docker.com/r/guacamole/guacd/)). There are two options for 
providing guacd to RAWeb:

- [Option 1. Allow RAWeb to start its own guacd instance](/docs/web-client/prerequisites#opt1) (recommended for most environments)
- [Option 2. Provide an address to an existing guacd server](/docs/web-client/prerequisites#opt2)

## Configuration

<PolicyDetails translationKeyPrefix="policies.GuacdWebClient.Address" open />
