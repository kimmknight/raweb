---
title: Install RAWeb
---

## Understanding RAWeb's installation requirements

### Server

RAWeb is built using a combination of ASP.NET and Vue.js, and it runs on Internet Information Services (IIS). Therefore, to install and run RAWeb, your system must be a Windows machine capable of running IIS and ASP.NET web applications.

For more information about supported installation environments, including specific Windows versions, refer to our [supported environments documentation](/docs/supported-environments).

### Clients

Any client device can connect to RAWeb using a modern web browser, such as Microsoft Edge, Google Chrome, Mozilla Firefox, or Safari. Older versions of these browsers may not be fully supported.

Additionally, RAWeb exposes RemoteApps and desktops using the Terminal Server Workspace Provisioning specification, so any client that supports MS-TWSP workspaces can load RAWeb's resources. You can review the steps for using workspaces in our [Access RAWeb resources as a workspace documentation](/docs/workspaces). Microsoft provides clients for Windows, macOS, iOS/iPadOS, and Android.

## Installation {#installation}

RAWeb provides a few different installation methods. The easiest way to get started is to use our installation script, which automatically installs RAWeb and any required components.

Jump to an section:
- [Interactive installation script (recommended)](#interactive-installation-script)
- [Non-interactive installation](#non-interactive-installation)
- [Install unreleased features](#install-unreleased-features)
- [Manual installation in IIS](#manual-installation-in-iis)
- [Install development branches](#install-development-branches)

### Interactive installation script (recommended) {#interactive-installation-script}

1. **Open PowerShell as an administrator.** \
   Press the Windows key + X, then select PowerShell (Administrator) or Terminal (Administrator).

2. **Copy and paste the code below[^footnote-2016], and then press enter.**

    ```
    irm https://github.com/kimmknight/raweb/releases/latest/download/install.ps1 | iex
    ```

3. Follow the prompts.

4. **Install web client prerequisites.**\
   If you plan to use the web client connection method, follow the instructions in our [web client prerequisites documentation](/docs/web-client/prerequisites) to install and configure the required software.

<InfoBar severity="attention" title="Important">

  The installer will retrieve the pre-built version of RAWeb from the latest release and install it to `C:\inetpub\RAWeb`.

  Refer to [the release page](https://github.com/kimmknight/raweb/releases/latest) for more details.

</InfoBar>

<InfoBar title="Note">
  Internet Information Services (IIS) or other required components are not already installed, the RAWeb installer will retreive and install them.
</InfoBar>

To install other versions, visit the [the releases page](https://github.com/kimmknight/raweb/releases) on GitHub.

[^footnote-2016]: If you are attempting to install RAWeb on Windows Server 2016, you may need to enable TLS 1.2. In PowerShell, run `[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12`.

### Non-interactive installation {#non-interactive-installation}

To install the latest version without prompts, use the following command instead:

1. **Open PowerShell as an administrator.** \
   Press the Windows key + X, then select PowerShell (Administrator) or Terminal (Administrator).

2. **Copy and paste the code below[^footnote-2016], then press enter.**

    ```
    & ([scriptblock]::Create((irm https://github.com/kimmknight/raweb/releases/latest/download/install.ps1)) -AcceptAll
    ```

4. **Install web client prerequisites.**\
   If you plan to use the web client connection method, follow the instructions in our [web client prerequisites documentation](/docs/web-client/prerequisites) to install and configure the required software.

<InfoBar severity="caution" title="Caution">

  If RAWeb is already installed, installing with this option will replace the existing configuration and installed files. Resources, policies, and other data in `/App_Data` with be preserved.

</InfoBar>

### Install unreleased features {#install-unreleased-features}

To install the latest version of the RAWeb, including features that may not have been released, follow these steps:

1. Download the [latest RAWeb repository zip file](https://github.com/kimmknight/raweb/archive/master.zip).
2. Extract the zip file and run **Setup.ps1** in PowerShell as an administrator.

<InfoBar severity="caution" title="Unstable code">
  Unreleased versions may contain unstable or experimental code that has not been fully tested. Use these versions at your own risk.
</InfoBar>

<InfoBar title="Note">
  Unreleased versions are not pre-built. Therefore, they require the .NET SDK to build the application before installation.

  If you do not already have the .NET SDK installed, the setup script will download and install it for you.
</InfoBar>

### Manual installation in IIS {#manual-installation-in-iis}

_If you need to control user or group access to resources, want to configure RAWeb policies (application settings) via the web app, or plan to add RemoteApps and Desktops as a Workspace in the Windows App:_

1. Download and extract the latest pre-built RAWeb zip file from [the latest release](https://github.com/kimmknight/raweb/releases/latest).
2. Extract the contents of the zip file to a folder in your IIS website's directory (default is `C:\inetpub\wwwroot`)
3. In IIS Manager, create a new application pool with the name **raweb** (all lowercase). Use **.NET CLR Version v4.0.30319** with **Integrated** pipeline mode.
4. In IIS, convert the folder to an application. Use the **raweb** application pool.
5. At the application level, edit Anonymous Authentication to use the application pool identity (raweb) instead of IUSR.
6. At the application level, enable Windows Authentication.
7. Disable permissions enheritance on the `RAWeb` directory.
    1. In **IIS Manager**, right click the application and choose **Edit Permissions...**.
    1. Switch to the **Security** tab.
    1. Click **Advanced**.
    1. Click **Disable inheritance**.
8. Update the permissions to the following:

| Type  | Principal         | Access       | Applies to                        |
| ----- | ----------------- | ------------ | --------------------------------- |
| Allow | SYSTEM            | Full Control | This folder, subfolders and files |
| Allow | Administrators    | Full Control | This folder, subfolders and files |
| Allow | IIS AppPool\raweb | Read         | This folder, subfolders and files |

9. Grant modify access to the `App_Data` folder for **IIS AppPool\raweb**:
    1. Under the application in IIS Manager, right click **App_Data** and choose **Edit Permissions...**.
    1. Switch to the **Security** tab.
    1. Click **Edit**.
    1. Select **raweb** and the check **Modify** in the **Allow column**. Click **OK**.
10. Grant read access to `AppData\resources` for **Users**.
11. Grant read and execute access to `bin\SQLite.Interop.dll` for **IIS AppPool\raweb**
12. Install the management service:
    1. In Command Prompt or PowerShell, navigate to the `bin` folder. (for example: `cd C:\inetpub\wwwroot\RAWeb\bin`)
    1. Then, run `.\RAWeb.Server.Management.ServiceHost.exe install`.

_If you only plan to use the web interface without authentication (some features will be disabled):_

1. Download and extract the latest pre-built RAWeb zip file from [the latest release](https://github.com/kimmknight/raweb/releases/latest).
2. Extract the contents of the zip file to a folder in your IIS website's directory (default is `C:\inetpub\wwwroot`)
3. In IIS Manager, create a new application pool with the name **raweb** (all lowercase). Use **.NET CLR Version v4.0.30319** with **Integrated** pipeline mode.
4. In IIS, convert the folder to an application. Use the **raweb** application pool.
5. At the application level, edit Anonymous Authentication to use the application pool identity (raweb) instead of IUSR.
6. Ensure that the **Users** group has read and execute permissions for the application folder and its children.
7. Install the management service:
    1. In Command Prompt or PowerShell, navigate to the `bin` folder. (for example: `cd C:\inetpub\wwwroot\RAWeb\bin`)
    1. Then, run `.\RAWeb.Server.Management.ServiceHost.exe install`.

### Install development branches {#install-development-branches}

To install a specific development branch of RAWeb, follow these steps:

1. Determine the branch you want to install. You can view work-in-progress branches on the [pull requests page](https://github.com/kimmknight/raweb/pulls). Branches are in the format `<owner>/<branch>`. For example: `kimmknight/branch-name` or `jackbuehner/branch-name`.

2. **Open PowerShell as an administrator.** \
   Press the Windows key + X, then select PowerShell (Administrator) or Terminal (Administrator).

3. **Type the code below[^footnote-2016], replacing the branch name, and then press enter.**

    ```
    iwr install.raweb.app/preview/<owner>/<branch> | iex
    ```

<InfoBar severity="caution" title="Unstable code">
  Unreleased versions may contain unstable or experimental code that has not been fully tested. Use these versions at your own risk.
</InfoBar>

<InfoBar severity="caution" title="Caution">

  This will overwrite any existing RAWeb installation. Resources, policies, and other data in `/App_Data` with be preserved.

</InfoBar>

<InfoBar title="Note">
  Unreleased versions are not pre-built. Therefore, they require the .NET SDK to build the application before installation.

  If you do not already have the .NET SDK installed, the setup script will download and install it for you.
</InfoBar>

<script setup>
   import { InfoBar } from '$components';
</script>
