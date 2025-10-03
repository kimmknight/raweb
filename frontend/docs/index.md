## Get started

The easiest way to get started with RAWeb is to install it with our installation script. Before you install RAWeb, review our [supported environments documentation](https://github.com/kimmknight/raweb/wiki/Supported-environments). Follow the steps below:

1. **Open PowerShell as an administrator**
   Press the Windows key + X, then select PowerShell (Administrator) or Terminal (Administrator).

2. **Copy and paste the code below<sup><a href="#footnote-2016">[1]</a></sup>, then press enter.**

```
irm https://github.com/kimmknight/raweb/releases/latest/download/install.ps1 | iex
```

3. Follow the prompts.

> [!IMPORTANT]
> The installer will retrieve the pre-built version of RAWeb from the latest release and install it to `C:\inetpub\RAWeb`.
> Refer to [the release page](https://github.com/kimmknight/raweb/releases/latest) for more details.

> [!NOTE]
> If Internet Information Services (IIS) or other required components are not already installed, the RAWeb installer will retreive and install them.

To install other versions, visit the [the releases page](https://github.com/kimmknight/raweb/releases) on GitHub.

## Using RAWeb

By default, RAWeb is available at https://127.0.0.1/RAWeb. To access RAWeb from other computers on your local network, replace 127.0.0.1 with your host PC or server's name. To access RAWeb from outside your local network, expose port 443 and replace 127.0.0.1 with your public IP address.

To add resources to the RAWeb interface, [refer to Publishing RemoteApps and Desktops](https://github.com/kimmknight/raweb/wiki/Publishing-RemoteApps-and-Desktops).

Refer to the guides in this wiki's sidebar for more information about using RAWeb.

---

**Footnotes**

<a name="footnote-2016">1</a>: If you are attempting to install RAWeb on Windows Server 2016, you may need to enable TLS 1.2. In PowerShell, run `[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12`.
