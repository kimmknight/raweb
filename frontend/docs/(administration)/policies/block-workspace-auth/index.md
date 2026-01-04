---
title: $t{{ policies.WorkspaceAuth.Block.title }}
nav_title: Block workspace authentication
---

Enable this policy to prevent workspace clients (such as Windows App) from authenticating to RAWeb. When enabled, users will be unable to add RAWeb's resources to workspace clients or refresh them if they have already been added.

This policy is useful if you want to require multi-factor authentication (MFA) for all users accessing RAWeb's resources. Workspace clients do not support MFA, so they must be blocked. For more information about using MFA with RAWeb, see [Enable multi-factor authentication (MFA) for the web app](/docs/security/mfa).

<PolicyDetails translationKeyPrefix="policies.WorkspaceAuth.Block" />

When this policy is enabled, the workspace URL section of the RAWeb settings page will be hidden.

When this policy is enabled, users will see an error message after attempting to authenticate via a workspace client. The error message will be a generic error message.

For example, on Windows, users may see the following error message:\
<img width="580" src="./windows-radc-blocked.webp" />
