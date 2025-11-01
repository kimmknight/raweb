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

RAWeb can publish RDP files from `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\CentralPublishedResources`. Only applications with the `ShowInPortal` DWORD set to `1` will be published.

To add a new RemoteApp, sign in the RAWeb's web interface with an administrator account and follow these steps:
1. Navigate to **Policies**.
2. At the top of the **Policies** page, click **Manage registry RemoteApps** to open the RemoteApps manager dialog. \
You will see a list of RemoteApps currently listed in `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\TSAppAllowList\Applications`. By default, if an app is not listed here, it will not be possible to remotely connect to it.\
   <img width="700" alt="" src="./apps manager.webp" />
4. To add a new RemoteApp, click the **Add new RemoteApp** button at the top left of the dialog to open the app discovery dialog.\
You will see a list of apps that RAWeb was able to discover on the server. RAWeb lists all packaged apps and any shortcut included in the system-wide Start Menu folder.\
   <img width="400" alt="" src="./app discovery.webp" />
5. Click the app you want to add. You will see a pre-populated **Add new RemoteApp** dialog.\
   <img width="500" alt="" src="./add new remoteapp.webp" />
6. Configure the properties as desired. Make sure that **Show in web interface and workspace feeds** is set to **Yes**. Click **OK** to save the RemoteApp details to the registry.

### Change the RemoteApp icon

To change the icon for a registry RemoteApp, you need to know the path to an icon file on the terminal server. You can use any `.exe`, `.dll`, `.ico`, `.png`, `.jpg`, `.jpeg`, `.bmp`, or `.gif` source on the server.

1. Navigate to **Policies**.
2. At the top of the **Policies** page, click **Manage registry RemoteApps** to open the RemoteApps manager dialog.
3. Click the RemoteApp for which you want to change the icon.
4. In the **Icon** group, click the **Select icon** button.\
   <img width="500" alt="" src="./select-icon-button.webp" />
5. In the **Select icon** dialog, enter the full path to the icon file on the server. Press Enter/Return on your keyboard to load icons at that path. If you specify an `exe`, `dll`, or `ico` file with multiple contained icons, you will see multiple icons. Click the icon you want to use.\
   <img width="600" alt="" src="./select-icon-dialog.webp" />
6. Click **OK** to save the RemoteApp details.

### Configure file type associations

Some workspace clients [support file type associations for RemoteApps](/docs/publish-resources/file-type-associations). RAWeb supports configuring file type associations for registry RemoteApps. RAWeb will automatically include the required metadata for file type associations in the workspace feed.

1. Navigate to **Policies**.
2. At the top of the **Policies** page, click **Manage registry RemoteApps** to open the RemoteApps manager dialog.
3. Click the RemoteApp for which you want to configure file type associations.
4. In the **Advanced** group, click the **Configure file type associations** button.\
   <img width="500" alt="" src="./file-type-associations-button.webp" />
5. You will see a dialog where you can add, remove, and edit file type associations.\
   Additionally, you can select specific icons for each file type association. \
   Click **Add association** to add a new file type association. All file type associations must start with a dot and must not include an asterisk.\
   <img width="500" alt="" src="./file-type-associations-dialog.webp" />
6. Click **OK** to confirm the specified file type associations.
7. Click **OK** to save the RemoteApp details.

### Configure visibility for specific users or groups

RAWeb supports restricting visibility of registry RemoteApps to specific users or groups. Note that this does not prevent users from launching the RemoteApp if they know the name of the RemoteApp and how to modify RDP files. It only controls whether the RemoteApp is visible in RAWeb's web interface and workspace feeds.

If you do not configure any user or group restrictions for a registry RemoteApp, it will be visible to all Remote Desktop users or Administrators on the server.

1. Navigate to **Policies**.
2. At the top of the **Policies** page, click **Manage registry RemoteApps** to open the RemoteApps manager dialog.
3. Click the RemoteApp for which you want to configure user or group restrictions.
4. In the **Advanced** group, click the **Manage user assignment** button.\
   <img width="500" alt="" src="./security-dialog-button.webp" />
5. Click **Add user or group** to open the **Select Users or Groups** dialog.
6. Enter the name of the user or group you want to add. Click **Check Names** to verify the name. Click **OK** to add the user or group.
7. If you want to explicitly deny access to a user or group, click the shield icon next to the user or group name.\
   <img width="460" alt="" src="./security-dialog-deny.webp" />
8. Click **OK** to confirm the specified user and group restrictions.
9. Click **OK** to save the RemoteApp details.

### Customize individual RDP file properties

RAWeb allows you to customize most RDP file properties for registry RemoteApps. This allows you to optimize the experience for individual RemoteApps.

<InfoBar severity="caution">
    Properties will be ignored and possibly overwritten for any properties specified in the policy: <b>Add additional RDP file properties to RemoteApps listed in the registry</b>.
</InfoBar>

1. Navigate to **Policies**.
2. At the top of the **Policies** page, click **Manage registry RemoteApps** to open the RemoteApps manager dialog.
3. Click the RemoteApp for which you want to configure RDP file properties.
4. In the **Advanced** group, click the **Edit RDP file** button.
   <InfoBar severity="attention">
      If you do not see the <b>Edit RDP file</b> button, make sure the <b>Use a dedicated collection for RemoteApps in the registry instead of the global list </b> policy is set to <b>Disabled</b> or <b>Not configured</b>.
   </InfoBar>
5. You will see a dialog where you can edit supported RDP file properties. Properties related to settings that are available in the main RemoteApp porperties dialog are disabled in this dialog. If you want to test the properties before you save them, click the **Download** button to download a test RDP file.\
   <img width="580" alt="" src="./rdp-file-properties-editor.webp" />
   <InfoBar severity="information" title="Tip">
      Place your mouse cursor over each property label to view a description and possible values.
   </InfoBar>
6. After making your changes, click **OK** to confirm the specified RDP file properties.
6. Click **OK** to save the RemoteApp details.

### Add via RDP file (add external RemoteApp)

1. Navigate to **Policies**.
2. At the top of the **Policies** page, click **Manage registry RemoteApps** to open the RemoteApps manager dialog.
3. Click the dropdown arrow next to the **Add new RemoteApp** button at the top left of the dialog. Select **Add from RDP file** to open the RDP file upload dialog.
   <InfoBar severity="attention">
      If you do not see the dropdown arrow button, make sure the <b>Use a dedicated collection for RemoteApps in the registry instead of the global list </b> policy is set to <b>Disabled</b> or <b>Not configured</b>.
   </InfoBar>
4. Select an RDP file from your computer. The RDP file must contain at least the following properties:
   - `remoteapplicationprogram:s:`
   - `full address:s:`\

### Remove a RemoteApp from the registry

1. Navigate to **Policies**.
2. At the top of the **Policies** page, click **Manage registry RemoteApps** to open the RemoteApps manager dialog.
3. Select the RemoteApp you want to delete.
4. In the **Danger zone** group, click the **Delete RemoteApp** button.\
   <img width="500" alt="" src="./delete-remoteapp-danger.webp" />

## Registry RemoteApps via RemoteApp Tool (deprecated)

RAWeb can publish RDP files from `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\TSAppAllowList\Applications`. Only applications with the `ShowInTSWA` DWORD set to `1` will be published. This behavior is not the preferred method of adding registry RemoteApps, and it may be removed in a future release. Use the RemoteApps manager in RAWeb's web interface instead.

<InfoBar severity="attention" title="Policy configuration required">
   You must set the <b>Use a dedicated collection for RemoteApps in the registry instead of the global list </b> policy to <b>Disabled</b> in order for RAWeb to publish RemoteApps from the registry path <code>HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\TSAppAllowList\Applications</code>.
</InfoBar>

Use [RemoteApp Tool](https://github.com/kimmknight/remoteapptool) to add, remove, and configure RemoteApps in the registry.

1. Open **RemoteApp Tool**.
2. Click the green plus icon in the bottom-left corner to **Add a new RemoteApp**. Find the executable for the application you want to add.\
   <img width="400" alt="" src="./97a0db8c-768d-4f8c-89c6-5f597d1276ea.png" />
3. The application you added should now appear in the list of applications. **Double click** it in the list to configure the properties.
4. Set **TSWebAccess** to **Yes**. You may configure other options as well. Remember to click **Save** when you are finished.\
   <img width="400" alt="image" src="./89e0db48-c585-4b08-8cd1-ab18fe0343f1.png" />

The application should now appear in RAWeb.

<script setup>
   import {InfoBar} from '$components';
</script>
