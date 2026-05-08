---
title: 'Combine apps across servers: Show one icon per app'
nav_title: Combine apps across servers
---

When the same RemoteApp or desktop is published by more than one terminal server, RAWeb can either show a separate icon for each server or combine them into a single icon. The **Combine apps across servers** setting controls this behavior.

When combining is enabled, RAWeb shows only one icon for each app regardless of how many terminal servers it is published on. If multiple terminal servers are available when you launch the app, RAWeb will ask you to pick a terminal server. When only one terminal server is available for the app, it connects immediately without a prompt.

When combining is disabled, each terminal server shows its own icon for each app. This can result in duplicate icons when the same app is available on multiple servers.

Combining is enabled by default.

## Enabling or disabling combine mode

1. Open the RAWeb app and go to the **Settings** page.
2. Find the **Combine apps across servers** section.
3. Toggle **Enable combined apps** on or off.

<InfoBar>

This setting requires your browser to support the `requestClose()` method on `<dialog>` elements. If your browser does not support it, the toggle will be disabled. All modern browsers support this feature.

</InfoBar>

<InfoBar>

If your administrator has configured this setting as a policy, the toggle will be disabled and you will not be able to change it. Contact your administrator if you need to change this behavior.

</InfoBar>
