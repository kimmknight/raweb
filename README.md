# RAWeb

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="frontend/lib/assets/favorites_dark.png">
  <source media="(prefers-color-scheme: light)" srcset="frontend/lib/assets/favorites_light.png">
  <img src="frontend/lib/assets/favorites_light.png" alt="A screenshot of the favorites page in RAWeb">
</picture>

A web interface for your RemoteApps and Desktops hosted on Windows 10, 11 and Server.

To setup RemoteApps on your PC, try [RemoteApp Tool](https://github.com/kimmknight/remoteapptool).

## Features

- A web interface for viewing your RemoteApp and Desktop RDP connections
  - Search the list of apps and devices
  - Favorite your most-used apps and devices for easy access
  - Sort apps and desktops by name, date modifed, and terminal server
  - Stale-while-revalidate caching for fast load times
  - Progressive web app with [window controls overlay](https://github.com/WICG/window-controls-overlay/blob/main/explainer.md) support
  - Follows the style and layout of WinUI 3
- Fully-compliant Workspace (webfeed) feature to place your RemoteApps and desktop connections in:
  - The Start Menu of Windows clients
  - The Android/iOS/iPadOS/MacOS Windows app
- File type associations on webfeed clients
- Different RemoteApps for different users and groups
- A setup script for easy installation

## Installation

1. **Open PowerShell as an administrator**
   Press the Windows key + X, then select PowerShell (Administrator) or Terminal (Administrator).

2. **Copy and paste the code below, then press enter.**

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

<details>
<summary><h3>Other installation methods</h3></summary>

### Method 2. Non-interactive installation

To install the latest version without prompts, use the following command instead:

```
& ([scriptblock]::Create((irm https://github.com/kimmknight/raweb/releases/latest/download/install.ps1)) -AcceptAll
```

If RAWeb is already installed, installing with this option will replace the existing configuration and installed files. Resources in `/resources` and `/multiuser-resources` folders will be preserved.

### Method 3. Manual download and setup

1. Download the [latest RAWeb repository zip file](https://github.com/kimmknight/raweb/archive/master.zip).
2. Extract the zip file and run **Setup.ps1** in PowerShell as administrator.

### Method 4. Manual installation in IIS

Before you follow these steps, ensure you have installed Internet Information Services with the management console, ASP.NET 4.5, Windows authentication, and basic authentication.

1. Download and extract the latest pre-built RAWeb zip file from [the latest release](https://github.com/kimmknight/raweb/releases/latest).
2. Extract the contents of the zip file to the desired location within your IIS website.
3. In IIS, convert the folder to an application.
4. On the **auth** subfolder only, disable **Anonymous Authentication** and enabled **Basic Authentication** and **Windows Authentication**
   Copy the **aspx/wwwroot** folder to the desired location within your IIS website(s). In IIS, convert the folder to an application. To enable authentication, on the **auth** subfolder only, disable _Anonymous Authentication_ and enable _Windows Authentication_.

</details>

## Using RAWeb

By default, RAWeb is available at https://127.0.0.1/RAWeb. To access RAWeb from other computers on your local network, replace 127.0.0.1 with your host PC or server's name. To access RAWeb from outside your local network, expose port 443 and replace 127.0.0.1 with your public IP address.

The following resources from the RAWeb wiki are also helpful when getting started:

- [Publishing RemoteApps and Desktops](https://github.com/kimmknight/raweb/wiki/Publishing-RemoteApps-and-Desktops)
- [File type associations for RAWeb webfeed clients](https://github.com/kimmknight/raweb/wiki/File-type-associations-for-RAWeb-webfeed-clients)
- [Trusting the RAWeb server SSL certificate](<https://github.com/kimmknight/raweb/wiki/Trusting-the-RAWeb-server-(Fix-security-error-5003)>)
- [Configure hosting server and terminal server aliases](https://github.com/kimmknight/raweb/wiki/Configure-hosting-server-and-terminal-server-aliases)

## Translations

Please follow the instructions at [TRANSLATING.md](TRANSLATING.md) to add or update translations.

## Screenshots

A web interface for your RemoteApps:

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="frontend/lib/assets/apps_dark.png">
  <source media="(prefers-color-scheme: light)" srcset="frontend/lib/assets/apps_light.png">
  <img src="frontend/lib/assets/apps_light.png" alt="A screenshot of the apps page in RAWeb">
</picture>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="frontend/lib/assets/devices_dark.png">
  <source media="(prefers-color-scheme: light)" srcset="frontend/lib/assets/devices_light.png">
  <img src="frontend/lib/assets/devices_light.png" alt="A screenshot of the devices page in RAWeb">
</picture>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="frontend/lib/assets/settings_dark.png">
  <source media="(prefers-color-scheme: light)" srcset="frontend/lib/assets/settings_light.png">
  <img src="frontend/lib/assets/settings_light.png" alt="A screenshot of the settings page in RAWeb">
</picture>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="frontend/lib/assets/terminal-server-picker_dark.png">
  <source media="(prefers-color-scheme: light)" srcset="frontend/lib/assets/terminal-server-picker_light.png">
  <img src="frontend/lib/assets/terminal-server-picker_light.png" alt="A screenshot of the termninal server picker dialog in RAWeb, which appears when selecting an app that exists on multiple hosts">
</picture>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="frontend/lib/assets/app-properties_dark.png">
  <source media="(prefers-color-scheme: light)" srcset="frontend/lib/assets/app-properties_light.png">
  <img src="frontend/lib/assets/app-properties_light.png" alt="A screenshot of the propertiesr dialog in RAWeb, which shows the contents of the RDP file">
</picture>

Webfeed puts RemoteApps in Windows client Start Menu:

![](https://github.com/kimmknight/raweb/wiki/images/screenshots/windows-webfeed-sm.png)

Android RD Client app subscribed to the webfeed/workspace:

![](https://github.com/kimmknight/raweb/wiki/images/screenshots/android-workspace-sm.jpg)
