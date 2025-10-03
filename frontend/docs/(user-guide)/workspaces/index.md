---
title: Access RAWeb resources as a workspace
nav_title: Access via Windows App
---

In addition to accessing resources from the RAWeb web interface, you can also access resources via:

- RemoteApp and Desktop Connections (RADC) on Windows
- Workspaces in Windows App (formerly Microsoft Remote Desktop) on macOS, Android, iOS, and iPadOS

> [!IMPORTANT]
> This feature will only work if RAWeb is using an SSL certificate that is trusted on every device that attempts to access the resources in RAWeb.\
> Refer to [Trusting the RAWeb server](<https://github.com/kimmknight/raweb/wiki/Trusting-the-RAWeb-server-(Fix-security-error-5003)>) for more details and instructions for using a trusted SSL certificate.\
> We recommend [using a certificate from a globally trusted certificate authority](<https://github.com/kimmknight/raweb/wiki/Trusting-the-RAWeb-server-(Fix-security-error-5003)#option-2-use-a-certificate-from-a-trusted-certificate-authority>).

## Identify your workspace URL or email address

Before you can add RAWeb's resources to RADC or Windows App, you need to know the URL for the workspace. Follow these instructions for finding your workspace URL.

1. Navigate to your RAWeb installation's web interface from the device with RADC or Windows App. _This step is important; If you cannot access the web interface from the device with RADC or Windows App, your workspace URL will not work._
2. Sign in to RAWeb.
3. Navigate to RAWeb settings.
   - For most users, access settings by clicking or tapping **Settings** in the bottom-left corner of the screen.
   - If you or your administrator have configured RAWeb to use _simple mode_, click or tap the settings icon next to you username in the top-right area of the titlebar.
4. In the **Workspace URL** section, click or tap **Copy workspace URL** or **Copy workspace email**. Use this URL or email address when adding a workspace. <br /> _For information about email-based workspace discovery, refer to [the documentaiton on Microsoft Learn](https://learn.microsoft.com/en-us/windows-server/remote/remote-desktop-services/rds-email-discovery) and [PR#129](https://github.com/kimmknight/raweb/pull/129)._

Now, jump to one of the follow sections based on which device you are using:

- [Identify your workspace URL or email address](#identify-your-workspace-url-or-email-address)
- [Windows via RemoteApp and Desktop Connections](#windows-via-remoteapp-and-desktop-connections)
- [macOS via Windows App](#macos-via-windows-app)
- [Android via Windows App](#android-via-windows-app)
- [iOS and iPadOS via Windows App](#ios-and-ipados-via-windows-app)

> [!NOTE]
> Windows App on Windows does not support adding workspaces via URL or email address.\
> Instead, use RemoteApp and Desktop Connections.

## Windows via RemoteApp and Desktop Connections

1. Right click the Start menu (or press the Windows key + X) and choose **Run**.
2. In the **Run** dialog, type _control.exe_. Click **OK**. **Control Panel** will open.<br/><img width="380" src="https://github.com/user-attachments/assets/c5501233-6ef0-48b4-b10d-026139d90c0f" />
3. If needed, change the view from **Category** to **Small icons** or **Large icons**.<br/><img width="830" src="https://github.com/user-attachments/assets/f4551b21-bea4-42bd-9bf0-be728d3d2d39" />
4. Click **RemoteApp and Desktop Connections** in the list.<br/><img width="830" src="https://github.com/user-attachments/assets/6d3eccd5-eb60-4573-9d09-ec178aa95dbc" />
5. On the left side, click **Access RemoteApp and desktops**.<br/><img width="830" src="https://github.com/user-attachments/assets/f3f997d2-3bbe-4965-b3ec-675a5565111c" />
6. In the **Access RemoteApp and desktops** window, enter the [workspace URL or email address](#identify-your-workspace-url-or-email-address). Click **Next** to continue to the next step.<br/><img width="586" src="https://github.com/user-attachments/assets/bde8567d-dbff-47d7-81f1-695de517d01e" />
7. Review the information. Then, click **Next** to connect.
8. You will see an **Adding connection resources** message. During this step, resources and icons are downloaded from RAWeb. Depending on the number of resources, this may take a while.
   - If you see a **Windows Security** dialog with the message _Your credentials did not work_, enter the credentials you use to sign in to the RAWeb web interface.<br/><img width="586" src="https://github.com/user-attachments/assets/d69d8074-78a2-4774-8ba0-9e1a08d083f0" /><br/><img width="456" src="https://github.com/user-attachments/assets/e84cb7e8-48ff-4d82-bfee-d49c682a2fd6" />
9. If the connection succeeded, you will see a message indicating the connection name and URL and the programs and desktops that have been added to the Start menu.<br/>Windows will periodically update the connection. You may also manually force the connection to update via the control panel.<br/><img width="586" src="https://github.com/user-attachments/assets/12967053-a025-4ec8-ac56-ebd4d5da109c" />

## macOS via Windows App

1. Install [Windows App from the App Store](https://apps.apple.com/us/app/windows-app/id1295203466).
2. Open **Windows App**.
3. In the menu bar, choose **Connections > Add Workspace...**.<br/><img width="477" src="https://github.com/user-attachments/assets/3b838293-83f5-4016-b828-761beefb3179" />
4. In the **Add Workspace** sheet, enter the [workspace URL or email address](#identify-your-workspace-url-or-email-address). Change **Credentials** to the credentials you use when you sign in to the RAWeb web interface. Click **Add** to add the workspace.<br/><img width="500" src="https://github.com/user-attachments/assets/e3584a4e-ebdf-4707-a299-5604538af954" />
5. You will see a **Setting up workspace...** message. During this step, resources and icons are downloaded from RAWeb. Depending on the number of resources, this may take a while.<br/><img width="500" src="https://github.com/user-attachments/assets/3594cd9d-d108-46fc-bade-f535449746cc" />

If the connection succeeeded, you will see your apps and devices included in Windows App.

## Android via Windows App

1. Install [Windows App from the Play Store](https://play.google.com/store/apps/details?id=com.microsoft.rdc.androidx).
2. Open **Windows App**.
3. Tap the **+** button in the top-right corner of the app.
4. Choose **Workspace**.
5. In the **Add Workspace** dialog, enter the [workspace URL or email address](#identify-your-workspace-url-or-email-address). Change **User account** to the credentials you use when you sign in to the RAWeb web interface. Tap **Next** to add the workspace.
6. You will see a **Setting up workspace...** message. During this step, resources and icons are downloaded from RAWeb. Depending on the number of resources, this may take a while.

## iOS and iPadOS via Windows App

1. Install [Windows App Mobile from the App Store](https://apps.apple.com/my/app/windows-app-mobile/id714464092).
2. Open **Windows App**.
3. Tap the **+** button in the top-right corner of the app.
4. Choose **Workspace**.
5. In the **Add Workspace** sheet, enter the [workspace URL or email address](#identify-your-workspace-url-or-email-address). Change **Credentials** to the credentials you use when you sign in to the RAWeb web interface. Tap **Next** to add the workspace.
6. You will see a **Setting up workspace...** message. During this step, resources and icons are downloaded from RAWeb. Depending on the number of resources, this may take a while.
