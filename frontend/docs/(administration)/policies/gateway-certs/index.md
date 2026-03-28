---
title: $t{{policies.GuacdWebClient.Security.AllowIgnoreGatewayCertErrors.title }}
nav_title: Gateway certificate errors
redirects:
  - policies/GuacdWebClient.Security.AllowIgnoreGatewayCertErrors
---

By default, RAWeb will not allow connections to a gateway server if the server's SSL certificate is untrusted. This policy controls whether users can ignore gateway certificate errors when connecting via the web client.

When users cannot ignore gateway certificate errors, they will see a message similar to the following when attempting to connect to a gateway server with an untrusted SSL certificate:

<img src="./strict-error.webp" width="400" alt="Gateway certificate error" style="border: 1px solid var(--wui-card-stroke-default); border-radius: var(--wui-control-corner-radius);" />

When users can ignore gateway certificate errors, they will see a message similar to the following when attempting to connect to a gateway server with an untrusted SSL certificate:

<img src="./skippable-error.webp" width="400" alt="Gateway certificate error" style="border: 1px solid var(--wui-card-stroke-default); border-radius: var(--wui-control-corner-radius);" />


<PolicyDetails translationKeyPrefix="policies.GuacdWebClient.Security.AllowIgnoreGatewayCertErrors" open />
