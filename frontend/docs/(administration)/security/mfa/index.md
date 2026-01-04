---
title: Enable multi-factor authentication (MFA) for the web app
nav_title: Multi-factor authentication (MFA)
---

RAWeb supports multi-factor authentication for the web app via external MFA providers. Visit the following policy documentation pages for instructions on configuring MFA with supported providers:

- [Duo Universal Prompt](/docs/policies/duo-mfa)

Windows RemoteApp and Desktop Connections and the Windows App provide no known mechanism for supporting MFA. Therefore, enabling MFA for the web app will not affect authentication for these clients. If you need to require MFA clients, consider disabling access to workspace clients with the [Block workspace client authentication](/docs/policies/block-workspace-auth) policy.
