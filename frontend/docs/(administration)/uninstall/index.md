---
title: Uninstall RAWeb
---

## For v2026.05.x.x and later

1. Right click the Start button and select **Installed apps**. On some versions, it may be called **Apps and Features** or **Programs and Features**.
2. In the list of installed apps, find your RAWeb installation and click it. The name will be in the format _RAWeb (IIS web site name) (path in IIS web site)_. For standard installations, the name is _RAWeb (Default Web Site) (RAWeb)_.
3. Click the **Uninstall** button and follow the prompts to uninstall RAWeb.

## For v2026.03.29.0 and earlier

**Part 1. Remove from RAWeb virtual application**

1. Open Internet Information Services (IIS) Manager.
2. In the **Connections** pane, expand **_Your device name_ > Sites > Default Web Site**.
3. Right click **RAWeb** and choose **Remove**.
4. In the **Confirm Remove** dialog, choose **Yes**.
5. Run `sc stop RAWebManagementService` and `sc delete RAWebManagementService` in cmd.exe to remove RAWeb Management Service.

**Part 2. Remove installed files**

1. Open **File Explorer**.
2. Navigate to **C:\inetpub**.
3. Delete the **RAWeb** folder.

**Part 3. Remove Internet Information Services Manager**

_Only perform these steps if you do not have other IIS websites._

1. Open PowerShell as an administrator
   - Press the Windows key + X, then select PowerShell (Administrator) or Terminal (Administrator).
2. Copy and paste the code below, then press enter.

> For Windows Server:
>
> ```
> Uninstall-WindowsFeature -Name Web-Server, Web-Asp-Net45, Web-Windows-Auth, Web-Http-Redirect, Web-Mgmt-Console, Web-Basic-Auth
> ```

> For other versions of Windows:
>
> ```
> Disable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-CommonHttpFeatures,IIS-HttpErrors,IIS-HttpRedirect,IIS-ApplicationDevelopment,IIS-Security,IIS-RequestFiltering,IIS-NetFxExtensibility45,IIS-HealthAndDiagnostics,IIS-HttpLogging,IIS-Performance,IIS-WebServerManagementTools,IIS-StaticContent,IIS-DefaultDocument,IIS-DirectoryBrowsing,IIS-ASPNET45,IIS-ISAPIExtensions,IIS-ISAPIFilter,IIS-HttpCompressionStatic,IIS-ManagementConsole,IIS-WindowsAuthentication,NetFx4-AdvSrvs,NetFx4Extended-ASPNET45,IIS-BasicAuthentication
> ```
