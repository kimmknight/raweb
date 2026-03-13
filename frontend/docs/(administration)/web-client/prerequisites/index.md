---
title: Install web client software prerequisites
nav_title: Required software
redirects:
  - wsl2-install
---

The web client requires the RAWeb server to have access to a [Guacamole](https://guacamole.apache.org/) daemon ([guacd](https://hub.docker.com/r/guacamole/guacd/)). There are two options for 
providing guacd to RAWeb:

- [Option 1. Allow RAWeb to start its own guacd instance](#opt1) (recommended for most environments)
- [Option 2. Provide an address to an existing guacd server](#opt2)

<InfoBar severity="attention" title="You only need to follow these instructions once.">
  When you upgrade RAWeb, you do not need to repeat these steps unless you are switching to a different option for providing guacd.
</InfoBar>

<InfoBar>
  These prerequisites are only necessary if you plan to use the web client connection method.
</InfoBar>

# Option 1. Allow RAWeb to start its own guacd instance {#opt1}

RAWeb can start its own guacd instance when a user first accesses the web client. This option requires a system-wide installation of Windows Subsystem for Linux 2 (WSL2) to be available on the RAWeb server. Use the following steps to install WSL2 and ensure it is ready for RAWeb to use.

<InfoBar>
  On Windows 11 or Windows Server 2022 Desktop Experience or newer, you may be able to skip the first two steps.
</InfoBar>

1. **Download the latest system-wide installer for Windows Subsystem for Linux from https://github.com/microsoft/WSL/releases/latest.** \
   Be sure to choose the *msi* installer, not the *msixbundle*.

2. **Run the installer.** \
   When it completes, it will automatically close the installation window.

3. **Open PowerShell as an administrator.** \
   Press the Windows key + X, then select PowerShell (Administrator) or Terminal (Administrator).

4. **Copy and paste the following command, and then press enter.** \
   This command will enable the "Windows Subsystem for Linux" and "Virtual Machine Platform" optional Windows components if they are not already enabled.
    ```powershell
    wsl.exe --install --no-distribution
    ```

5. **Restart the server or PC if prompted.** \
   Enabling the virtual machine platform requires a restart to actually enable the feature.

<InfoBar title="Storage consideration">

  The `guacd` image used by RAWeb consumes 30-40 megabytes of disk space. If you choose this option, ensure that the RAWeb server has sufficient disk space to accommodate the image.

</InfoBar>

<InfoBar severity="caution" title="Virtual Machine Platform requirement">
  If the "Windows Subsystem for Linux" optional Windows component was already enabled before step 4, you must manually enable the "Virtual Machine Platform" optional Windows component. To do this, run the following command in PowerShell as an administrator:

  ```powershell
  Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform -All
  ```

  After running the command, restart the server or PC to apply the changes.
</InfoBar>

Review the [capabilities and considerations](/docs/web-client/about/) page for additional information about the web client and guacd.

## If RAWeb is running within a virtual machine {#wsl-hyperv}

If you are running RAWeb within a Hyper-V virtual machine, you must also enable nested virtualization for the VM. Without nested virtualization, WSL2 will not be able to start, preventing RAWeb from starting guacd. To enable nested virtualization, shut down the VM and run the following command in PowerShell as an administrator on the Hyper-V host:

```
Set-VMProcessor -VMName <VMName> -ExposeVirtualizationExtensions $true
```

<InfoBar severity="caution" title="AMD processor limitation">
  Windows versions prior to Windows 11 and Windows Server 2025 do not support nested virtualization on AMD processors.
</InfoBar>

If you are using a different hypervisor, refer to its documentation for instructions on enabling nested virtualization.

# Option 2. Provide an address to an existing guacd server {#opt2}

You can provide RAWeb the address of an existing guacd server. Be cautious when using this option; guacd does not have built-in authentication, so if the guacd server is accessible to unauthorized users, they could potentially access desktops and applications through it.

To provide RAWeb with the address of an existing guacd server, follow these steps:
1. Open the RAWeb web interface.
2. Sign in to RAWeb as an administrator.
3. Navigate to **Policies**.
4. Select the **Allow the web client connection method** policy.
5. If you see a choice between "Use an RAWeb-managed guacd container" and "Use an externally-managed guacd instance", select **Use an externally-managed guacd instance** option.
6. Enter the hostname or IP address and port of the guacd server in the **External address** fields.
7. Click **OK** to apply the policy changes.

Review the [capabilities and considerations](/docs/web-client/about/) page for additional information about the web client and guacd.
