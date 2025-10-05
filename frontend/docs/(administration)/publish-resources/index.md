---
title: Publishing RemoteApps and Desktops
nav_title: Publish RemoteApps and Desktops
---

By default, RAWeb will install to **C:\\inetpub\RAWeb**. This guide assumes that RAWeb is installed to the default location.

RAWeb can publish RDP files from any device. RAWeb can also publish RemoteApps soecified in the registry.

## Standard RDP files

RDP files should be placed in **C:\\inetpub\RAWeb\App_Data\resources**. Any RDP file in this folder will be automatically published.

You can create subfolders to sort your RemoteApps and desktops into groups. RemoteApps and desktops are organized into sections on the RAWeb web interface based on subfolder name.

To add icons, specify a **.ico** or **.png** file in with the same name as the **.rdp** file.

- .ico and .png icons are the only file types supported.
- For RemoteApps, RAWeb will not serve an icon unless the width and height is the same.
- For desktops, if the icon width and height are not the same, RAWeb will assume that the icon file represents the destkop wallpaper. When an icon is needed for the desktop, RAWeb will place the wallpaper into the blue rectangle section of Windows 11's This PC icon. RAWeb will directly use the wallpaper on the devices tab of the web interface when the display mode is set to card.
- RAWeb's interface can use dark mode icons and wallpapers. Add "-dark" to the end of the icon name to specify a dark-mode icon or wallpaper.

<img width="600" alt="" src="./28276875-8592-48f5-8db6-975d23136cff.png" />

<br />
<br />
You can also configure RAWeb to restrict which users see certain RDP files.

### Configure security permissions

By default, the **App_Data\resources** folder can be read by any user in the **Users** group.

RAWeb uses standard Windows security descriptors when determining user access to files in the **App_Data\resources** folder. Configure security permissions via the security tab in the folder or files properties. For more information, see [Configuring user‐based access to resources in the resources folder](/docs/publish-resources/resource-folder-permissions).

### Use folder-based permissions {#folder-based-permissions}

You can optionally provide different RemoteApps and desktops to different users based on their username or group membership.

Inside the RAWeb folder, you will find a folder called **App_Data\multiuser-resources**.

It contains the folders:

**/user** - Create folders in here for each user you wish to target (folder name = username). Drop rdp/image files into a user folder to publish them to the user.

**/group** - Create folders in here for each group you wish to target (folder name = group name). Drop rdp/image files into a group folder to publish them to all users in the group.

Note: Subfolders within user and group folders are supported. For clients that show folders, each subfolder will appear as a distinct section in the list of apps.

## Registry RemoteApps

RAWeb can publish RDP files from `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\TSAppAllowList\Applications`. Only applications with the `ShowInTSWA` DWORD set to `1` will be published.

Use [RemoteApp Tool](https://github.com/kimmknight/remoteapptool) to add, remove, and configure RemoteApps in the registry.

1. Open **RemoteApp Tool**.
2. Click the green plus icon in the bottom-left corner to **Add a new RemoteApp**. Find the executable for the application you want to add.\
   <img width="400" alt="" src="./97a0db8c-768d-4f8c-89c6-5f597d1276ea.png" />
3. The application you added should now appear in the list of applications. **Double click** it in the list to configure the properties.
4. Set **TSWebAccess** to **Yes**. You may configure other options as well. Remember to click **Save** when you are finished.\
   <img width="400" alt="image" src="./89e0db48-c585-4b08-8cd1-ab18fe0343f1.png" />

The application should now appear in RAWeb.
