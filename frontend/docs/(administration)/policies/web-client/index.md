---
title: $t{{ policies.GuacdWebClient.Address.title }}
nav_title: Web client
redirects:
  - policies/GuacdWebClient.Address
---

The web client allows users to access their desktops and applications through a web browser. This policy controls whether RAWeb will use a Guacamole daemon (guacd) as a remote desktop proxy for provide web client access.

<img width="400" src="../connection-method-rdpFile/webGuacd-method.webp" />

When enabled and properly configured, users will see a "Connect in browser" button in the connection dialog, allowing them to use the remote desktop connection proxy.

When disabled, the "Connect in browser" option will not be shown, preventing users from accessing the web client.

If no other connection methods are enabled, users will be unable to connect to resources via the web app. Instead, they will see this following dialog:

<img width="400" src="../connection-method-rdpFile/no-connection-method.webp" />

## Prerequisites

The web client requires the RAWeb server to have access to a [Guacamole](https://guacamole.apache.org/) daemon ([guacd](https://hub.docker.com/r/guacamole/guacd/)). There are two options for 
providing guacd to RAWeb:

### Option 1. Allow RAWeb to start its own guacd instance

RAWeb can start its own guacd instance when a user first accesses the web client. This option requires a system-wide installation of Windows Subsystem for Linux 2 (WSL2) to be available on the RAWeb server.

The latest system-wide installer for Windows Subsystem for Linux can be obtained from https://github.com/microsoft/WSL/releases/latest. Chose the *msi* installer, not the *msixbundle*. 

WSL2 is only available on Windows 10 version 1903 (build 18362) or later. The first mainstream Windows Server version to support WSL2 is Windows Server 2022.

<InfoBar title="Installation sequence" severity="caution">
  If you install WSL2 after RAWeb, you will need to reinstall RAWeb. RAWeb's install script only builds the required guacd components when WSL2 is detected during installation.
</InfoBar>

<InfoBar title="Storage consideration">

  The `guacamole/guacd` image used by RAWeb consumes several hundred megabytes of disk space. If you choose this option, ensure that the RAWeb server has sufficient disk space to accommodate the image and its components.

</InfoBar>


### Option 2. Provide an address to existing guacd server

You can provide RAWeb the address of an existing guacd server. Be cautious when using this option; guacd does not have built-in authentication, so if the guacd server is accessible to unauthorized users, they could potentially access desktops and applications through it.

## Configuration

<PolicyDetails translationKeyPrefix="policies.GuacdWebClient.Address" open />

## Making connections

Every RDP file contains an address for the terminal server. Normally, users who launch an RDP file must ensure that the terminal server is accessible from their device. However, when using the web client, users connect to the RAWeb server instead of the terminal server. The RAWeb server then forwards the connection to the terminal server on behalf of the user. This means that users can connect to their desktops and applications through the web client even if their devices do not have direct access to the terminal server, as long as they can access the RAWeb server. **Therefore, you must ensure that the RAWeb server has access to the terminal servers that your RDP files reference**.
