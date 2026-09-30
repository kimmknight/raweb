---
title: Signing generated executables and scripts
nav_title: Code signing
---

RAWeb's GitHub Actions workflows (`release.yaml` and `preview-backend.yaml`) automatically sign every `.exe` and `.ps1` file they produce or ship using [SignPath.io](https://about.signpath.io/), via a certificate from the [SignPath Foundation](https://signpath.org/). See [Code signing policy](/docs/code-signing/policy) for who can approve a signing request.

<InfoBar title="Requires a maintainer with repo admin access" severity="caution">
  Adding or changing signing credentials requires access to the repository's Actions secrets. Only repository owners and maintainers can do this.
</InfoBar>

## Required repository secret

| Secret               | Description                               |
| -------------------- | ----------------------------------------- |
| `SIGNPATH_API_TOKEN` | API token for the RAWeb SignPath project. |

In the repository, go to **Settings > Secrets and variables > Actions**, click **New repository secret**, name it `SIGNPATH_API_TOKEN`, and paste in the API token from the SignPath dashboard.

If `SIGNPATH_API_TOKEN` is not available (for example, pull requests from forks don't have access to repository secrets), signing is skipped and a notice is logged instead of failing the build.
