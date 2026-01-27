---
title: Common web client errors
redirects:
  - wsl2
---

This section describes some common errors that may be encountered when using the RAWeb web client, along with their possible causes and solutions.

## The requested resource was not found {#code516}

This error indicates that the requested RemoteApp or desktop resource could not be found. This is most likely to occur if the resource was renamed or deleted after the user accessed the RAWeb web client. To resolve this issue, ensure that the resource exists and that the user has permission to access it.

## You are not authorized to access this resource {#code403}

This error indicates that the user does not have permission to access the requested RemoteApp or desktop resource. To resolve this issue, ensure that the user has been granted access to the resource in RAWeb.

## The RDP file is missing the full address property {#code10001}

This error indicates that RAWeb was unable to find a valid address for the requested RemoteApp or desktop resource. This error appears when the resource does not have the *full address* or *alternate full address* properties set in its RDP file. To resolve this issue, ensure that the resource's RDP file includes a valid *full address* or *alternate full address* property.

## The specified remote host could not be reached. {#code10010}

This error indicates that the RAWeb server was unable to connect to the remote host specified in the requested RemoteApp or desktop resource's RDP file. This may be due to network connectivity issues, firewall settings, or incorrect address information in the RDP file. To resolve this issue, verify that the RAWeb server can reach the remote host and that the address information in the RDP file is correct.

## The specified remote host refused the connection. {#code10011}

See [The specified remote host could not be reached](#10010).

## Error checking server certificate {#code10009}

RAWeb encountered an error while attempting to validate the server certificate presented by the remote host. Review the latest log file in `C:\inetpub\RAWeb\logs` that starts with `guacd-tunnel-` for more details about the specific error encountered.

## Timeout while checking server certificate {#code10026}

See [Error checking server certificate](#10010).

## Failed to resolve hostname to an IPv4 address {#code10032}

This error indicates that RAWeb was unable to resolve the hostname specified in the requested RemoteApp or desktop resource's RDP file to an IPv4 address. This may be due to DNS configuration issues or an incorrect hostname in the RDP file. To resolve this issue, verify that the RAWeb server can resolve the hostname and that the hostname in the RDP file is correct.

A hostname must resolve to an IPv4 address when *full address* or *alternate full address* properties in the RDP file use a hostname rather than an IP address. RAWeb does not currently support using IPv6 addresses for these properties.

## Domain, username, and password must be provided. {#code10005}

This error indicates that the RAWeb server did not receive the necessary credentials to authenticate the user to the remote host. To resolve this issue, ensure that the user provides a valid domain, username, and password when connecting to the RemoteApp or desktop resource via the web client.

## Gateway username and password must be provided {#code10008}

When a resouce's RDP file includes the `gatewayhostname:s:` property, RAWeb will connect to the resource via the gateway rather than connecting directly. This error idicates that RAWeb was unable to obtain the necessary gateway credentials from the web client.

To resolve this issue, ensure that the user provides valid gateway credentials when connecting to the RemoteApp or desktop resource via the web client. The web client will prompy the user for their gateway credentials after they provide their credentials for the terminal server.

## Failed to install the remote desktop proxy service {#code10017}

This error occurs when RAWeb is configured to host its own instance of guacd using WSL2, but RAWeb is unable to install the remote desktop proxy service within WSL2. This may be due to issues with the WSL2 installation or configuration on the RAWeb server.

To resolve the error, ensure that WSL2 is properly installed and configured on the RAWeb server. Refer to the [Web client prerequisites documentation](/docs/web-client/prerequisites) for more information on setting up WSL2 for RAWeb.

For specific details about the error encountered, review the latest log file in `C:\inetpub\RAWeb\logs` that starts with `guacd-`.

## The remote desktop proxy service did not start in time {#code10014}

This error occurs if the remote desktop proxy service within WSL2 fails to start within 30 seconds. Your host system may be under heavy load, preventing WSL2 from starting the service in a timely manner.

## The remote desktop proxy service is not installed on the server {#code10015}

This error indicates that RAWeb is configured to use its own instance of guacd via WSL2, but the remote desktop proxy service has not been installed within WSL2. To resolve this issue, ensure that WSL2 is properly installed and configured on the RAWeb server. Refer to the [Web client prerequisites documentation](/docs/web-client/prerequisites) for more information on setting up WSL2 for RAWeb.

## The Windows Subsystem for Linux is not installed on the server {#code10016}

This error indicates that RAWeb is configured to use its own instance of guacd via WSL2, but WSL2 is not installed on the RAWeb server. To resolve this issue, install and configure WSL2 on the RAWeb server. Refer to the [Web client prerequisites documentation](/docs/web-client/prerequisites) for more information on setting up WSL2 for RAWeb.

## The remote desktop proxy service failed to install {#code10022}

This error indicates that RAWeb was unable to install the remote desktop proxy service within WSL2. This may be due to issues with the WSL2 installation or configuration on the RAWeb server.

For specific details about the error encountered, review the latest log file in `C:\inetpub\RAWeb\logs` that starts with `guacd-`.

## The Windows Subsystem for Linux optional component is not installed on the server {#code10023}

This error indicates that RAWeb is configured to use its own instance of guacd via WSL2, but the "Windows Subsystem for Linux" optional Windows component is not enabled on the RAWeb server.

To resolve this issue, run the following command in PowerShell as an administrator on the RAWeb server:

```powershell
wsl.exe --install --no-distribution
```

## The Virtual Machine Platform optional component is not installed on the server {#code10024}

This error indicates that RAWeb is configured to use its own instance of guacd via WSL2, but the "Virtual Machine Platform" optional Windows component is not enabled on the RAWeb server.

To resolve this issue, run the following command in PowerShell as an administrator on the RAWeb server:

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform -All
```

## The Virtual Machine Platform is unavailable {#code10028}

This error indicates that RAWeb is configured to use its own instance of guacd via WSL2, but the "Virtual Machine Platform" feature is unavailable. This may occur if the RAWeb server is running within a virtual machine that does not have nested virtualization enabled.

See the [Web client prerequisites documentation](/docs/web-client/prerequisites#wsl-hyperv) for more information on enabling nested virtualization for virtual machines.

## An error with the Windows Subsystem for Linux prevented the remote desktop proxy service from installing or starting {#code10025}

This error indicates that RAWeb encountered an error with WSL2 while attempting to install or start the remote desktop proxy service. This may be due to issues with the WSL2 installation or configuration on the RAWeb server.

Review the latest log file in `C:\inetpub\RAWeb\logs` that starts with `guacd-` for more details about the specific error encountered.

## The remote desktop proxy service failed to start {#code10013}

This error occurs when RAWeb is configured to host its own instance of guacd using WSL2, but RAWeb is unable to start the remote desktop proxy service within WSL2. This may be due to issues with the WSL2 installation or configuration on the RAWeb server.

Review the latest log file in `C:\inetpub\RAWeb\logs` that starts with `guacd-tunnel-` for more details about the specific error encountered.

## Guacd address is not properly configured {#code10011}

This error occurs when RAWeb is configured to use an external guacd server, but the address provided is invalid or unreachable.

To resolve this issue, edit the "Allow the web client connection method" policy in RAWeb to provide a valid and reachable guacd server address. Refer to the [Option 2. Provide an address to existing guacd server](/docs/web-client/prerequisites#opt2) for specific instructions on configuring RAWeb to use an external guacd server.

## The web client is using an unsupported Guacamole protocol version {#code10033}

The web client is using a version of the Guacamole protocol that is not supported by the remote desktop proxy service. This may occur if the web client is outdated.

To resolve this issue, ensure that the web client is up to date. The web client can be force updated by clearing the browser cache or performing a hard refresh (Ctrl + F5) multiple times while on the RAWeb web app.

## The specified connection file must not specify a file to open on the terminal server {#code10018}

This error indicates that the RDP file for the requested RemoteApp resource includes the `remoteapplicationmode:i:1` value and the `remoteapplicationfile:s:` property. The `remoteapplicationfile:s:` property is not supported for RemoteApp resources connected via the RAWeb web client.

To resolve this issue, remove the `remoteapplicationfile:s:` property from the resource's RDP file.

## The specified connection file must specify a program to open on the terminal server {#code10019}

This error indicates that the RDP file for the requested RemoteApp resource includes the `remoteapplicationmode:i:1` value but does not include the `remoteapplicationprogram:s:` property. The `remoteapplicationprogram:s:` property is required for RemoteApp resources.

To resolve this issue, ensure that the resource's RDP file includes the `remoteapplicationprogram:s:` property with a valid program path.

## The specified connection file must not expand the command line paramters on the terminal server {#code10020}

This error indicates that the RDP file for the requested RemoteApp resource includes the `remoteapplicationmode:i:1` value and the `remoteapplicationexpandcmdline:i:1` property. The `remoteapplicationexpandcmdline:i:1` property is not supported for RemoteApp resources connected via the RAWeb web client.

To resolve this issue, remove the `remoteapplicationexpandcmdline:i:1` property from the resource's RDP file.

## Connections to packaged applications must connect via C:\Windows\explorer.exe {#code10021}

This error indicates that the RDP file for the requested packaged application resource does not specify `C:\Windows\explorer.exe` as the program to launch on the terminal server. Packaged applications must launch via `C:\Windows\explorer.exe` to ensure proper functionality.

Packaged applications are identified by the presence of the `shell:AppsFolder` in the `remoteapplicationcmdline:s:` property value of the resource's RDP file.

To resolve this issue, ensure that the resource's RDP file specifies `C:\Windows\explorer.exe` as the program to launch on the terminal server.
RemoteApps added via the RAWeb management interface will automatically be configured to use `C:\Windows\explorer.exe` for packaged applications.

## The guacd server could not be reached {#code10029}

This error indicates that RAWeb is configured to use an external guacd server, but RAWeb was unable to connect to the specified guacd server. This may be due to network connectivity issues, firewall settings, or incorrect address information.

To resolve this issue, verify that the RAWeb server can reach the guacd server and that the address information is correct. Additionally, verify that the guacd server is running and accepting connections.

## The guacd server refused the connection {#code10030}

See [The guacd server could not be reached](#10029).

## An unexpected error occurred when attempting to connect to the guacd server {#code10031}

This error indicates that an unexpected error occurred while RAWeb was attempting to connect to the guacd server. This may be due to network connectivity issues, firewall settings, or other unforeseen problems.

Review the latest log file in `C:\inetpub\RAWeb\logs` that starts with `guacd-tunnel-`. for more details about the error.

## An unexpected error occurred during the remote desktop session {#code10034}

This error indicates that an unexpected error occurred during the remote desktop session between the web client and the remote host.

Review the latest log file in `C:\inetpub\RAWeb\logs` that starts with `guacd-tunnel-` for more details about the error.
