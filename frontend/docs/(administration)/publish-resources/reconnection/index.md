---
title: Automatic and manual reconnection via RAWeb/RDWebService.asmx endpoint (MS-RDWR)
nav_title: Reconnection (MS-RDWR)
---

MS-RDWR allows Windows to ask the server for RDP files to use to reconnect after RemoteApps lose their connection.Windows expects the server to provide a response from the `RDWebService.asmx` endpoint, which RAWeb does not implement.

RAWeb cannot support this feature because RAWeb does not track the resources a user has launched. Therefore, RAWeb cannot provide the list reconnectable resources when Windows requests them.

RAWeb specifies the `SupportsReconnect="false"` attribute on the MS-TSWP-compilant workspace it provides to clients. Windows RemoteApp and Desktop Connections ignores this attribute, however, and will show an error in the system tray when a user attempts to reconnect to RAWeb via the system tray icon.
