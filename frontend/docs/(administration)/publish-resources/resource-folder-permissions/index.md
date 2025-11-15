---
title: Configuring user-based and group‐based access to resources
nav_title: Security permissions
---

RAWeb supports restricting visibility of managed resources to specific users or groups. Note that this does not prevent users from launching the RemoteApp or desktop if they know the name of the RemoteApp or desktop and how to modify RDP files. It only controls whether the RemoteApp or desktop is visible in RAWeb's web interface and workspace feeds.

RAWeb offers different ways to configure access to RemoteApps and desktops for users and groups based on how the resources have been provided to RAWeb. There are four different locations where resources can be stored for RAWeb:
- `App_Data\managed_resources` (managed resources) - RDP files that have been provided to RAWeb via the RemoteApps and desktops manager.
- Registry (managed resources) - RemoteApps and desktops on the RAWeb host that have been configured via the RemoteApps and desktops manager.
- `App_Data\multiuser-resources` (multiuser resources) - RDP files that have been placed in subfolders to configure folder-based permissions.
- `App_Data\resources` (resources) - RDP files that have been placed directly in the resources folder.

## Managed resources {#managed-resources}

If you do not configure any user or group restrictions for a managed resource, it will be visible to all Remote Desktop users or Administrators on the server.

1. Navigate to **Policies**.
2. At the top of the **Policies** page, click **Manage resources** to open the RemoteApps and desktops manager dialog.
3. Click the RemoteApp for which you want to configure user or group restrictions.
4. In the **Advanced** group, click the **Manage user assignment** button.\
   <img width="500" alt="" src="./security-dialog-button.webp" />
5. Click **Add user or group** to open the **Select Users or Groups** dialog.
6. Enter the name of the user or group you want to add. Click **Check Names** to verify the name. Click **OK** to add the user or group.
7. If you want to explicitly deny access to a user or group, click the shield icon next to the user or group name.\
   <img width="460" alt="" src="./security-dialog-deny.webp" />
8. Click **OK** to confirm the specified user and group restrictions.
9. Click **OK** to save the RemoteApp details.

## Resources in `App_Data\multiuser-resources` {#multiuser-resources}

Inside the RAWeb folder, you will find a folder called **App_Data\multiuser-resources**. If it does not exist, create it. This folder is used to store resources that are published to specific users or groups based on folder structure.

It contains the folders:

- **/user** - Create folders in here for each user you wish to target (folder name = username). Drop rdp/image files into a user folder to publish them to the user.

- **/group** - Create folders in here for each group you wish to target (folder name = group name). Drop rdp/image files into a group folder to publish them to all users in the group.

<InfoBar title="Note">
   Subfolders within user and group folders are supported. For clients that show folders, each subfolder will appear as a distinct section in the list of apps.
</InfoBar>

## Resources in `App_Data\resources` {#resource-folder-permissions}

RAWeb uses standard Windows security descriptors when determining user access to files in the **App_Data\resources** folder. Configure security permissions via the security tab in the folder or files properties. The following subsections describe how to configure security permissions for the **App_Data\resources** folder and its contents.

**Section summary:**

- RAWeb users (or groups) should *only* have **List folder contents** permissions on the **App_Data\resources** directory (disable inheritance).
- Any user or group requiring access to a RemoteApp or desktop must have **Read** permission for the RDP file for the app or desktop.
  - For icons to be visible, the user or group must also have **Read** permission for the icon(s) associated with the RDP file.

### Configure directory security permissions

By default, the **App_Data\resources** folder can be read by any user in the **Users** group. We need to change the permissions.

1. Open **File Explorer** and navigate to the RAWeb directory. The default installation directory is `C:\inetpub\RAWeb`.
2. Navigate to `App_Data`.
3. Right click the `resources` folder and choose **Properties** to open the properties window.
4. Switch to the **Security** tab and click **Advanced** to open the **Advanced Security Settings** dialog.\
   <img width="400" src="./c9c532ff-e8d5-4ad5-af84-2fe041d2a702.png" />
5. In the list of **Permissions entries**, select **Users**. Then, click **Edit**. A **Permission Entry** dialog will open.\
   <img width="768" src="./fe9547ee-db4a-4b2f-a69d-a8ea2ab61498.png" />
6. In the **Permission Entry** dialog, click **Show advanced permissions**. Then, in the **Advanced permissions** section, uncheck all permissions except _Traverse folder_ and _List folder_. Click **OK** to close the dialog.\
   <img width="918" src="./ff38c130-fae6-4302-a2bd-4f9420833368.png" />
7. In the **Advanced Security Settings** dialog, click **OK** to apply the changes and close the dialog.

### Grant access to resources to specific users or groups

Use the following steps to grant access to a single resource for a user or group. These steps need to be repeated for each RDP file or icon/wallpaper file. Changes to security permissions affect access to resources from the web app and all workspace clients (e.g., Windows App). If you only need to grant access to a collection of resources to a single user or group, consider using [multiuser-resources for folder-based permissions](#folder-based-permissions).

1. Navigate to the `resources` folder. In a standard installation, the path is `C:\inetpub\RAWeb\App_Data\resources`.
2. Right click a resource and choose **Properties** to open the properties window.
3. Switch to the **Security** tab and click **Edit** to open the **Permissions** dialog.\
   <img width="400" src="./df27a870-9830-42e9-b726-f0f413d5890e.png" />
4. Click **Add** to open the **Select Users or Groups** dialog.\
   <img width="360" src="./6dfffa48-ce9c-4f8d-8aed-ceb6f9753983.png" />
5. In the **Select Users or Groups** dialog, specify the users or groups you want to add. When you are ready, click **OK**.\
   <img width="458" src="./f74bbe8c-b84d-4c86-80bc-98a55283e226.png" />
6. In the **Permissions** dialog, confirm that only _Read_ or _Read_ and _Read and execute_ are allowed. Click **OK** to apply changes and close the dialog.\
   <img width="360" src="./c0ba7509-8dc4-41f0-9936-04a66a271a52.png" />
7. In the **Properties** window, click **OK**.

<script setup>
   import { InfoBar } from '$components';
</script>
