---
title: Configuring user‐based access to resources in the resources folder
nav_title: Resource folder permissions
---

**Summary:**

- RAWeb users (or groups) should *only* have **List folder contents** permissions on the **App_Data\resources** directory (disable inheritance).
- Any user or group requiring access to a RemoteApp or desktop must have **Read** permission for the RDP file for the app or desktop.
  - For icons to be visible, the user or group must also have **Read** permission for the icon(s) associated with the RDP file.

## Configure directory security permissions

By default, the **App_Data\resources** folder can be read by any user in the **Users** group. We need to change the permissions.

1. Open **File Explorer** and navigate to the RAWeb directory. The default installation directory is `C:\inetpub\RAWeb`.
2. Navigate to `App_Data`.
3. Right click the `resources` folder and choose **Properties** to open the properties window.
4. Switch to the **Security** tab and click **Advanced** to open the **Advanced Security Settings** dialog.\
   <img width="400" src="https://github.com/user-attachments/assets/c9c532ff-e8d5-4ad5-af84-2fe041d2a702" />
5. In the list of **Permissions entries**, select **Users**. Then, click **Edit**. A **Permission Entry** dialog will open.\
   <img width="768" src="https://github.com/user-attachments/assets/fe9547ee-db4a-4b2f-a69d-a8ea2ab61498" />
6. In the **Permission Entry** dialog, click **Show advanced permissions**. Then, in the **Advanced permissions** section, uncheck all permissions except _Traverse folder_ and _List folder_. Click **OK** to close the dialog.\
   <img width="918" src="https://github.com/user-attachments/assets/ff38c130-fae6-4302-a2bd-4f9420833368" />
7. In the **Advanced Security Settings** dialog, click **OK** to apply the changes and close the dialog.

## Grant access to resources to specific users or groups

Use the following steps to grant access to a single resource for a user or group. These steps need to be repeated for each RDP file or icon/wallpaper file. Changes to security permissions affect access to resources from the web app and all workspace clients (e.g., Windows App). If you only need to grant access to a collection of resources to a single user or group, consider using [multiuser-resources for folder-based permissions](/docs/publish-resources/#folder-based-permissions).

1. Navigate to the `resources` folder. In a standard installation, the path is `C:\inetpub\RAWeb\App_Data\resources`.
2. Right click a resource and choose **Properties** to open the properties window.
3. Switch to the **Security** tab and click **Edit** to open the **Permissions** dialog.\
   <img width="400" src="https://github.com/user-attachments/assets/df27a870-9830-42e9-b726-f0f413d5890e" />
4. Click **Add** to open the **Select Users or Groups** dialog.\
   <img width="360" src="https://github.com/user-attachments/assets/6dfffa48-ce9c-4f8d-8aed-ceb6f9753983" />
5. In the **Select Users or Groups** dialog, specify the users or groups you want to add. When you are ready, click **OK**.\
   <img width="458" src="https://github.com/user-attachments/assets/f74bbe8c-b84d-4c86-80bc-98a55283e226" />
6. In the **Permissions** dialog, confirm that only _Read_ or _Read_ and _Read and execute_ are allowed. Click **OK** to apply changes and close the dialog.\
   <img width="360" src="https://github.com/user-attachments/assets/c0ba7509-8dc4-41f0-9936-04a66a271a52" />
7. In the **Properties** window, click **OK**.
