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

RAWeb provides a few different installation methods. The easiest way to get started is to use our installation wizard, which automatically installs RAWeb and any required components.

Jump to a section:

- [Interactive installation script (recommended)](#interactive-installation-script)
- [Non-interactive installation](#non-interactive-installation)
- [Install unreleased features](#install-unreleased-features)
- [Manual installation in IIS](#manual-installation-in-iis)
- [Install development branches](#install-development-branches)

### Interactive installation wizard (recommended) {#interactive-installation-script}

1. **Download and run the installer for the latest release.**

<a href="https://install.raweb.app/latest" rel="nofollow">
<picture>
<source media="(prefers-color-scheme: dark)" srcset="https://install.raweb.app/buttons/download-and-install-raweb-dark.svg">
<source media="(prefers-color-scheme: light)" srcset="https://install.raweb.app/buttons/download-and-install-raweb-light.svg">
<img src="https://install.raweb.app/buttons/download-and-install-raweb-light.svg" alt="Download and install the latest release" height="32" style="padding-inline-start: 40px; margin-top: -0.5rem">
</picture>
</a>

2. **Follow the prompts.**

3. **Install web client prerequisites.**\
   If you plan to use the web client connection method, follow the instructions in our [web client prerequisites documentation](https://raweb.app/docs/web-client/prerequisites) to install and configure the required software.

<InfoBar title="Note">
  Internet Information Services (IIS) or other required components are not already installed, the RAWeb installer will retrieve and install them.
</InfoBar>

To install other versions, visit the [the releases page](https://github.com/kimmknight/raweb/releases) on GitHub.

### Non-interactive installation {#non-interactive-installation}

To install the latest version without prompts, use the following command in PowerShell:

```
Invoke-WebRequest "https://install.raweb.app/latest?" -OutFile "installer.exe"; Start-Process ".\installer.exe" -ArgumentList "--express","--overwrite" -Wait; Remove-Item "installer.exe"
```

<InfoBar severity="caution" title="Caution">

If RAWeb is already installed, installing with this option will replace the existing configuration and installed files. Resources, policies, and other data in `/App_Data` will be preserved.

</InfoBar>

#### Supported parameters

You can also specify additional parameters when running the installer. These parameters allow you to customize the installation process, which is especially useful for non-interactive installations.

| Option                                              | Description                                                                                                                         |
| --------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `--website <name>`, <br/>`-WebSite <name>`          | Specify the IIS web site that should serve RAWeb.                                                                                   |
| `--virtual-path <path>`, <br/>`-VirtualPath <path>` | Specify the virtual path within that IIS web site.                                                                                  |
| `--install-dir <path>`, <br/>`-InstallDir <path>`   | Specify where the software files will be stored. RAWeb's application data will be stored in this directory.                         |
| `--option <id>=<value>`                             | Specify one option declared in `setup.json`. Repeat for more than one.                                                              |
| `--express`, <br/>`-Express`, <br/>`-AcceptAll`     | Advance past a page automatically once it has a valid value.                                                                        |
| `--overwrite`, <br/>`-Overwrite`                    | Bypass warnings about replacing or losing existing data (`InstallPage`).                                                            |
| `--no-welcome`                                      | Skip the welcome page. The installer will automatically relaunch with administrator privileges.                                     |
| `--autoclose <always\|success\|never>`              | Close the window on its own once installation finishes (`InstallPage`). Defaults to `never`.                                        |
| `--no-gui`                                          | Run entirely in the console instead of showing the wizard window. You must specify all options that should not take default values. |

### Install unreleased features {#install-unreleased-features}

To install the latest version of the RAWeb, including features that may not have been released, follow these steps:

1. Download the [latest RAWeb repository zip file](https://github.com/kimmknight/raweb/archive/master.zip).
2. Download the multi-version installer `.exe` file from [the latest release on GitHub](https://github.com/kimmknight/raweb/releases/tag/v2026.07.14.4).
3. Run the multi-version installer to start the installer.
4. On the version selection page, provide the path to the RAWeb respository zip file.
5. Continue with the remaining steps of the installation wizard.

<InfoBar severity="caution" title="Unstable code">
  Unreleased versions may contain unstable or experimental code that has not been fully tested. Use these versions at your own risk.
</InfoBar>

<InfoBar title="Note">
  Unreleased versions are not pre-built. Therefore, they require the .NET SDK to build the application before installation.

If you do not already have the .NET SDK installed, the setup script will download a temporary copy of the correct .NET SDK version.
</InfoBar>

### Manual installation in IIS {#manual-installation-in-iis}

_If you need to control user or group access to resources, want to configure RAWeb policies (application settings) via the web app, or plan to add RemoteApps and Desktops as a Workspace in the Windows App:_

1. Download and extract the latest pre-built RAWeb zip file from [the latest release](https://github.com/kimmknight/raweb/releases/latest).
2. Extract the contents of the zip file to a folder in your IIS website's directory (default is `C:\inetpub\wwwroot`)
3. In IIS Manager, create a new application pool with the name **raweb** (all lowercase). Use **.NET CLR Version v4.0.30319** with **Integrated** pipeline mode.
4. In IIS, convert the folder to an application. Use the **raweb** application pool.
5. At the application level, edit Anonymous Authentication to use the application pool identity (raweb) instead of IUSR.
6. At the application level, enable Windows Authentication.
7. Disable permissions inheritance on the `RAWeb` directory.
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
10. Grant read access to `App_Data\resources` for **Users**. You may need to create the `resources` folder if it does not already exist.
11. Install the management service:
    1. Launch `rawebmgmtsvc.exe` from the extracted zip file.
    1. When asked if you want to install the service, type **Y** and press enter.
    1. For the name of the IIS application pool, type **raweb** and press enter.
    1. For the question about additional SIDs, leave it blank and press enter.
    1. When asked what you want to call the service, press enter to use the default name.
    1. You should see a message that the service was installed successfully. To uninstall the service, run `rawebmgmtsvc.exe` again.

### Install development branches {#install-development-branches}

To install a specific development branch of RAWeb, follow these steps:

1. Determine the branch you want to install. You can view work-in-progress branches on the [pull requests page](https://github.com/kimmknight/raweb/pulls). Branches are in the format `<owner>/<branch>`. For example: `kimmknight/branch-name` or `jackbuehner/branch-name`.
2. Download the multi-version installer `.exe` file from [the latest release on GitHub](https://github.com/kimmknight/raweb/releases/tag/v2026.07.14.4).
3. Run the multi-version installer as an administrator to start the installer.
4. Click **Continue** to go to the version selection page.
5. On the version selection page, enable the **Show unreleased versions** option.
6. Select the desired unreleased version from the list, and then click **Next**.
7. Continue with the remaining steps of the installation wizard.

<InfoBar severity="caution" title="Unstable code">
  Unreleased versions may contain unstable or experimental code that has not been fully tested. Use these versions at your own risk.
</InfoBar>

<InfoBar title="Note">
  Unreleased versions are not always pre-built. Therefore, they require the .NET SDK to build the application before installation.

If you do not already have the .NET SDK installed, the setup script will download a temporary copy of the correct .NET SDK version.
</InfoBar>

<script setup>
   import { InfoBar } from '$components';
</script>
