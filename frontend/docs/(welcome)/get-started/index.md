---
title: Get started with RAWeb
nav_title: Get started
---

The easiest way to get started with RAWeb is to install it with our installation script. Before you install RAWeb, review our [supported environments documentation](/docs/supported-environments). Follow the steps below:

1. **Open PowerShell as an administrator**\
   Press the Windows key + X, then select PowerShell (Administrator) or Terminal (Administrator).

2. **Copy and paste the code below[^footnote-2016], then press enter.**

   ```
   irm https://github.com/kimmknight/raweb/releases/latest/download/install.ps1 | iex
   ```

3. **Follow the prompts.**

   <InfoBar severity="caution" title="Caution">
      The installer will retrieve the pre-built version of RAWeb from the latest release and install it to 
      <code>C:\inetpub\RAWeb</code>.
      <br />
      Refer to <a href="https://github.com/kimmknight/raweb/releases/latest" target="_blank" rel="noopener noreferrer">the release page</a> for more details.
   </InfoBar>
   <InfoBar severity="attention" title="Note">
      If Internet Information Services (IIS) or other required components are not already installed, the RAWeb installer will retreive and install them.
   </InfoBar>

4. **Install web client prerequisites.**\
   If you plan to use the web client connection method, follow the instructions in our [web client prerequisites documentation](/docs/web-client/prerequisites) to install and configure the required software.

To install other versions, visit the [the releases page](https://github.com/kimmknight/raweb/releases) on GitHub.

## Using RAWeb

By default, RAWeb is available at https://127.0.0.1/RAWeb. To access RAWeb from other computers on your local network, replace 127.0.0.1 with your host PC or server's name. To access RAWeb from outside your local network, expose port 443 and replace 127.0.0.1 with your public IP address.

To add resources to the RAWeb interface, [refer to Publishing RemoteApps and Desktops](/docs/publish-resources).

Refer to the guides in this wiki's sidebar for more information about using RAWeb.

[^footnote-2016]: If you are attempting to install RAWeb on Windows Server 2016, you may need to enable TLS 1.2. In PowerShell, run `[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12`.

<script setup>
   import {InfoBar} from '$components';
</script>
