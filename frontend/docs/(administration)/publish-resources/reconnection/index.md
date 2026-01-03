---
title: Automatic and manual reconnection via RAWeb/RDWebService.asmx endpoint (MS-RDWR)
nav_title: Reconnection (MS-RDWR)
---

[Remote Desktop Workspace Runtime Protocol](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-rdwr) (MS-RDWR) allows Windows to ask the server for RDP files to use to reconnect after RemoteApps lose their connection. When attempting to reconnect via the RemoteApp and Desktop Connections system tray icon, Windows will send post to RAWeb's `RDWebService.asmx` endpoint, requesting the list of reconnectable resources for the user.

RAWeb cannot support this feature because RAWeb does not track the resources a user has launched. Therefore, RAWeb cannot provide the list reconnectable resources when Windows requests them.

RAWeb will always respond to the `RDWebService.asmx` request with the following response, indicating that there are no reconnectable resources.

```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <GetRDPFilesResponse xmlns="http://schemas.microsoft.com/ts/2010/09/rdweb">
      <GetRDPFilesResult>
        <version>8.0</version>
        <wkspRC></wkspRC>
      </GetRDPFilesResult>
    </GetRDPFilesResponse>
  </soap:Body>
</soap:Envelope>
```

When attempting to reconnect via the system tray icon, Windows RemoteApp and Desktop Connections will show the following error. This error is expected behavior when using RAWeb, and there is no workaround or way to suppress it. Please do not file bug reports about this behavior.

<img width="440" alt="RemoteApp and Desktop Connections Reconnection Error" src="./reconnect-no-resources.webp">
