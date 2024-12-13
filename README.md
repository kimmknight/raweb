# RAWeb

![](https://github.com/kimmknight/raweb/wiki/images/screenshots/webui-sm.png)

A simple web interface for your RemoteApps hosted on Windows 10, 11 and Server.

To setup RemoteApps on your PC, try [RemoteApp Tool](https://github.com/kimmknight/remoteapptool).

## Features

* A web interface for your RemoteApps full-desktop RDP connections
* Webfeed (workspace) feature to place your RemoteApps and desktop connections in:
  *  The Start Menu of Windows clients
  *  The Android/IOS/MacOS RD Client app
* File type associations on webfeed clients
* Different RemoteApps for different users/groups
* A setup script for easy installation

## Installation

There are three different methods for installing RAWeb:

### 1. Easy Setup (online)

1. Run PowerShell or Terminal as administrator
2. Paste the following line and press enter:

```
$zipUrl = "https://github.com/kimmknight/raweb/archive/refs/heads/master.zip"; $tempDir = "$env:TEMP\raweb"; $zipFile = "$tempDir\master.zip"; New-Item -ItemType Directory -Path $tempDir -Force | Out-Null; Invoke-WebRequest -Uri $zipUrl -OutFile $zipFile; Expand-Archive -Path $zipFile -DestinationPath $tempDir -Force; Set-ExecutionPolicy Bypass -Scope Process -Force; & "$tempDir\raweb-master\Setup.ps1"; Remove-Item -Path $zipFile -Force; Remove-Item -Path $tempDir -Recurse -Force;
```

### 2. Manual Download and Setup

Download the [latest RAWeb zip file](https://github.com/kimmknight/raweb/archive/master.zip).

Extract the zip file and run **Setup.ps1** in PowerShell as administrator.

### 3. Manual Installation in IIS

Download and extract the [latest RAWeb zip file](https://github.com/kimmknight/raweb/archive/master.zip). Copy the **aspx/wwwroot** folder to the desired location within your IIS website(s). In IIS, convert the folder to an application. To enable authentication, on the **auth** subfolder only, disable *Anonymous Authentication* and enable *Windows Authentication*. 

## Publishing RemoteApps and Desktops

By default, RAWeb will typically install to **c:\inetpub\RAWeb**.

Drop a **.rdp** file into the **resources** folder to publish it.

To add images, you can drop a **.ico** or **.png** file in with the same name.

You can also create subfolders in here to sort your RemoteApps/desktops into groups.

### Multiuser

You can optionally provide different RemoteApps/desktops to different users based on their username or group membership.

Inside the RAWeb folder, you will find a folder called **multiuser-resources**.

It contains the folders:

**/user** - Create folders in here for each user you wish to target (folder name = username). Drop rdp/image files into a user folder to publish them to the user.

**/group** - Create folders in here for each group you wish to target (folder name = group name). Drop rdp/image files into a group folder to publish them to all users in the group.

Note: Subfolders within user and group folders will not work.

## Screenshots

A web interface for your RemoteApps:

![](https://github.com/kimmknight/raweb/wiki/images/screenshots/webui-sm.png)

Webfeed puts RemoteApps in Windows client Start Menu:

![](https://github.com/kimmknight/raweb/wiki/images/screenshots/windows-webfeed-sm.png)

Android RD Client app subscribed to the webfeed/workspace:

![](https://github.com/kimmknight/raweb/wiki/images/screenshots/android-workspace-sm.jpg)
