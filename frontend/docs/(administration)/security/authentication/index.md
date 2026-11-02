---
title: Authentication modes
---

RAWeb offers several authentication modes to control access to the application. This document describes the available modes and how to configure them.

## Modes

### System authentication mode (recommended) (default)

This mode requires users to sign in with a username and password before accessing RAWeb. User credentials are managed by Windows; any user with a valid Windows account on the server hosting RAWeb or the same domain as the server can sign in.

When this mode is enabled, anonymous authentication is never allowed.

### Anonymous mode

In this mode, RAWeb does not perform any authentication, allowing anyone to access the application without signing in. It is not possible to sign in with credentials.

When this mode is enabled:

- The login page will automatically sign in as the anonymous user.
- The web app will hide the option to sign out or change credentials.
- The webfeed/workspace feature will work without authentication. A trusted certificate is still required.

### Hybrid mode

In hybrid mode, RAWeb allows both anonymous access and credential-based sign-in. Users can choose to sign in with their credentials or continue as an anonymous user.

When this mode is enabled, anonymous authentication is only allowed for the web interface; you must use credentials when accessing the RAWeb workspace/webfeed.

When this mode is enabled:

- The login page will show a **Skip** button, which signs in as the anonymous user.
- Users can still sign in with their Windows credentials.
- The webfeed/workspace feature still requires authentication with Windows credentials.
- Users who click **Skip** can only access the resources that are available to the anonymous user. See [The anonymous user](#anonymous-user).

_Note the skip button next to the continue button:_

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./sign-in-hybrid.dark.png">
  <source media="(prefers-color-scheme: light)" srcset="./sign-in-hybrid.png">
  <img width="400" src="./sign-in-hybrid.png" alt="A screenshot of the RAWeb sign in page when anonymous authentication is allowed" border="1">
</picture>

## The anonymous user {#anonymous-user}

In anonymous mode and hybrid mode, RAWeb treats users who do not provide credentials as the anonymous user. The anonymous user is not a Windows account. RAWeb defines it with these details:

| Property                  | Value         |
| ------------------------- | ------------- |
| Domain                    | `RAWEB`       |
| Username                  | `anonymous`   |
| Security identifier (SID) | `S-1-4-447-1` |

RAWeb treats the anonymous user as a member of these groups:

| Group    | SID            |
| -------- | -------------- |
| Everyone | `S-1-1-0`      |
| Users    | `S-1-5-32-545` |

The anonymous user is not a member of **Authenticated Users** (`S-1-5-11`) because it never signs in. Resources that you allow only for **Authenticated Users** are not available to the anonymous user.

The anonymous user can access:

- **Files in `App_Data/resources`** when the **Everyone** or **Users** group can read them. By default, RAWeb allows the **Users** group to read this folder.
- **Resources in `App_Data/multiuser-resources`** in the `user/anonymous` folder, the `group/Everyone`, or the `group/Users` folder. These group folders can also be named with the group's SID.
- **Managed resources** that do not have a security descriptor or whose security descriptor allows **Everyone** or **Users**.
- **Registry resources** that do not have a security descriptor or whose security descriptor allows **Everyone** or **Users**.
- **Files in `App_Data/inject/filestore`** that the **Everyone** or **Users** group can read. By default, RAWeb allows the **Users** group to read this folder. Files in the `public` folder are always available to everyone, including users who have not signed in.

To make a resource unavailable to the anonymous user, remove access for **Everyone** and **Users** and allow only the specific users or groups that need it. If you want to allow access to the resource for all users who sign in, you can allow access for **Authenticated Users** instead of **Users**.

## Configuration

1. Once RAWeb is installed, open **IIS Manager** and expand the tree in the **Connections pane** on the left side until you can see the **RAWeb** application. The default name is **RAWeb**, but it may have a different name if you performed a manual installation to a different folder. Click on the **RAWeb** application.
2. In the **Features View**, double click **Application Settings** \
   <img width="860" src="../../policies/iis-application-settings.png" />
3. In the **Actions pane**, click **Add** to open the **Add Application Setting** dialog. \
   <img width="860" src="../../policies/iis-application-settings-add.png" />
4. Specify the properties. For **Name**, use `App.Auth.Anonymous`. For **Value**, specify the value that corresponds to the desired authentication mode:
   - System authentication mode: `never`
   - Anonymous mode: `always`
   - Hybrid mode: `allow`
5. When you are finished, click **OK**.
