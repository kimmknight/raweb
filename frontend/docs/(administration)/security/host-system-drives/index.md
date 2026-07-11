---
title: Hide terminal server drives
---

When a user connects to a RemoteApp or desktop session, File Explorer and the [Windows Common Item Dialog](https://learn.microsoft.com/en-us/windows/win32/shell/common-file-dialog) may show the terminal server's local drives (e.g., `C:\`, `D:\`) in addition to the user's own local PC drives. This can be confusing for end-users and poses a moderate security risk by exposing the server's file system structure.

To provide a secure and seamless user experience, administrators should configure the terminal server to hide its local drives from users and configure RAWeb to generate RDP files that allow users to access only their own local PC drives. This can be achieved by combining the [Additional RemoteApp properties](/docs/policies/inject-rdp-properties) policy with the appropriate policies on the terminal server.

## Hiding the drives via Group Policy

To prevent users from accessing the terminal server's local drives when using the [Windows Common Item Dialog](https://learn.microsoft.com/en-us/windows/win32/shell/common-file-dialog) dialog, administrators should use the following steps to configure the terminal server:

<InfoBar severity="caution" title="Security note">
These policy combinations will not prevent users from accessing the server's drives via command line or other applications. The terminal server's drives will only be hidden in File Explorer and the Windows Common Item Dialog.
</InfoBar>

1. Open the Local Group Policy Editor (`gpedit.msc`).
2. Navigate to `User Configuration » Administrative Templates » Windows Components » File Explorer`.
3. Configure the **Hide these specified drives in My Computer** policy:
   1. Double click **Hide these specified drives in My Computer** to open the policy edit dialog.
   2. Set the policy to **Enabled**.
   3. In the **Options** section, set **Pick one of the following combinations** to **Restrict all drives**.
   4. Click **OK**.
4. Configure the **Prevent access to drives from My Computer** policy:
   1. Double click **Prevent access to drives from My Computer** to open the policy edit dialog.
   2. Set the policy to **Enabled**.
   3. In the **Options** section, set **Pick one of the following combinations** to **Restrict all drives**.
   4. Click **OK**.

<img src="./hide-drives.webp" alt="Windows Group Policy Editor showing File Explorer policies with a yellow title bar" class="screenshot" width="800">

## Redirecting local user drives

For users to still be able to save and open files seamlessly, their local PC drives must be mapped to the remote session.

On new installations, RAWeb automatically configures the [additional RemoteApp properties](/docs/policies/inject-rdp-properties) policy to inject the `drivestoredirect:s:*` property. Combined with the Group Policies above, users will only see their own local PC drives (e.g., `C on LOCAL-PC`) and will not be able to browse or modify the server's file system from File Explorer.

No additional configuration in RAWeb is required unless the default properties were manually overridden.
