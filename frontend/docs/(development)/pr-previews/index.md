---
title: Enabling deploy previews on your fork
nav_title: PR deploy previews
---

When you open a pull request against RAWeb from a fork, a deployment status table will be inserted at the bottom of your PR description. The table can link to a live preview of the web app. The preview must be hosted on your own fork's GitHub Pages.

If the deployment status table shows an error saying GitHub Pages is not enabled, follow these steps on your fork.

## 1. Allow Actions to run on your fork

GitHub disables Actions on forks by default.

1. Open your fork on GitHub.
2. Go to the **Actions** tab.
3. Click **I understand my workflows, go ahead and enable them**.

## 2. Enable GitHub Pages

1. Go to **Settings** > **Pages** on your fork.
2. Under **Build and deployment**, set **Source** to **Deploy from a branch**.
3. Push a commit to your branch or re-run the failed workflow so a `gh-pages` branch gets created.
4. Once `gh-pages` exists, set **Branch** to `gh-pages` and save.

## 3. Push again

Push a new commit to your PR's branch (or re-run the workflow). The next build will be published to `https://<your-username>.github.io/raweb/`. Your PR's deployment status table will link to it once it is ready.
