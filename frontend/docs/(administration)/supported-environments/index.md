---
title: Supported server environments
nav_title: Server environments
---

## Supported host machines

RAWeb can be hosted on any modern 64-bit Windows device. The primary requirement is the device must support Internet Information Services (IIS) 10 and .NET Framework 4.6.2 or newer. Windows Server 2016 and Windows 10 Version 1607 are the first versions to meet these requirements.

## Authenticiation scenarios

RAWeb can authenticate with local or domain credentials.

### Local credentials

Local credentials should work in all scenarios.

### Domain credentials

For domain credentials, the machine with the RAWeb installation must have permission to enumerate groups and their members. RAWeb uses group membership to restrict access to resources hosted on RAWeb. If RAWeb cannot search group memberships, resources will not load and authentication may fail. This is most likely an issue with complex domain forests that contain one-way trusts.

#### Application pool configuration

If necessary, you may change the credentials used by the RAWeb application in IIS so that it uses an account with permission to list groups and view group memberships. See the instructions below:

<details>
<summary>Instructions</summary>

1. Open **Internet Information Services (IIS) Manager**.
2. In the **Connections** pane, click on **Application Pools**.
3. In the list of application pools, right click on **raweb** and choose **Advanced Settings**.
4. In the **Process Model** group, click on **Identity**. Then, click the button with the ellipsis (**...**) to open the **Application Pool Identity** dialog.
5. Choose **Custom Account**, and then click **Set** to provide the credentials for the account.
6. Click **OK** on all three dialogs. The RAWeb application will now use the credentials you proivided for its process.

</details>

#### User cache

<InfoBar severity="caution" title="Security consideration">
  Group membership will not automatically update when the user cache is enabled.
</InfoBar>

If there are cases where the domain controller may be unavailable to RAWeb, you may also want to enable the user cache. The user cache stores details about a user every time the sign in, and RAWeb will fall back to the details in the user cache if the domain controller cannot be reached. If RAWeb is unable to load group memberships from the domain, the group membership cached in the user cache will be used instead. When the user cache is enabled and the domain controller cannot be accessed, the authentication mechanism can also sign in using the cached domain credentials stored by the Windows machine with RAWeb installed. Instructions for enabling are below:

<details>
<summary>Instructions</summary>

If you are able to sign in to RAWeb as an administrative user:

1. Open the RAWeb web app.
2. Navigate to the **Policies** page.
3. Set the **Enable the user cache** policy state to **Enabled**.
4. Click **OK** to apply the policy.

Otherwise, enable the policy via IIS Manager:

1. Open **Internet Information Services (IIS) Manager**.
2. In the **Connections** pane, find your installation of RAWeb and click it.
3. Open **Application Settings**.
4. In the **Actions** pane, click **Add...**.
5. For **Name**, specify _UserCache.Enabled_. For **Value**, specify _true_.
6. Click **OK** to apply the policy.

</details>

<script setup>
   import {InfoBar} from '$components';
</script>
