---
title: Enable the test sign-in form
nav_title: Test sign-in form
redirects:
  - policies/App.Auth.TestLoginForm.Enabled
---

RAWeb includes a simple sign-in form for troubleshooting. It sends a username and password directly to RAWeb's sign-in endpoint and shows the response without the rest of the web app. This can help you find out whether a sign-in problem comes from the server or from the web app.

The form is disabled by default. When it is disabled, its address responds with `404 Not Found`.

<InfoBar severity="caution" title="Disable the form when you finish troubleshooting">

The test sign-in form is not intended for everyday use. Enable it only while you troubleshoot, and disable it again afterward.

</InfoBar>

This policy cannot be changed from the **Policies** tab in the web app. You must change it with IIS Manager on the server. This prevents someone who signs in to the web app as an administrator from enabling the form.

## Enable the form

1. Open **IIS Manager** and expand the tree in the **Connections pane** on the left side until you can see the **RAWeb** application. The default name is **RAWeb**, but it may have a different name if you performed a manual installation to a different folder. Click on the **RAWeb** application.
2. In the **Features View**, double click **Application Settings** \
   <img width="860" src="../iis-application-settings.png" />
3. In the **Actions pane**, click **Add** to open the **Add Application Setting** dialog. \
   <img width="860" src="../iis-application-settings-add.png" />
4. Specify the properties. For **Name**, use `App.Auth.TestLoginForm.Enabled`. For **Value**, use `true`.
5. When you are finished, click **OK**.

The form is now available at `/api/auth/login`. If RAWeb is installed in a folder of the web site, add that folder to the start of the path (for example, `https://example.raweb.app/RAWeb/api/auth/login`).

## Disable the form

1. Open **IIS Manager** and expand the tree in the **Connections pane** on the left side until you can see the **RAWeb** application. The default name is **RAWeb**, but it may have a different name if you performed a manual installation to a different folder. Click on the **RAWeb** application.
2. In the **Features View**, double click **Application Settings**.
3. Click `App.Auth.TestLoginForm.Enabled` in the list.
4. In the **Actions pane**, click **Remove**. When IIS Manager asks you to confirm, click **Yes**.
