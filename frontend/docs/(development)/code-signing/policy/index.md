---
title: Code signing policy
nav_title: Code signing policy
---

Free code signing is provided by [SignPath.io](https://about.signpath.io/). The certificate is provided by [SignPath Foundation](https://signpath.org/).

<InfoBar title="Privacy policy" severity="attention">
  See our <a href="/docs/code-signing/privacy-policy">privacy policy</a> for details about what data is processed while building, signing, and distributing RAWeb releases, including links to the privacy policies of third-party services involved.
</InfoBar>

## Who can commit to this repository

Anyone can propose changes by opening a pull request, but changes are only merged into the repository by collaborators with write access. Currently, collaborators are:

- **Kim Knight** ([@kimmknight](https://github.com/kimmknight))
- **Jack Buehner** ([@jackbuehner](https://github.com/jackbuehner))
- **MrBrianGale** ([@MrBrianGale](https://github.com/MrBrianGale))

## Who can approve a release for signing

Releases of RAWeb, and the signing requests submitted to SignPath as part of building a release, are approved by one of the following individuals:

- **Jack Buehner** ([@jackbuehner](https://github.com/jackbuehner))
- **Kim Knight** ([@kimmknight](https://github.com/kimmknight))

## How signing works

RAWeb's release workflow (`.github/workflows/release.yaml`) builds the project's executables and submits a signing request to SignPath using their official GitHub Action. SignPath verifies that the request originates from this repository's trusted build system before signing and then returns the signed files to the workflow for inclusion in the release. See [Code signing](/docs/code-signing) for implementation details.
