# RAWeb.Server.Installer

An installation wizard that installs RAWeb natively in C#, replacing `setup.ps1` as the primary install path. It targets `net472` and is published as a single self-contained `.exe`.

The installer is built with .NET Framework 4.7.2, Windows PResentation Framework (WPF), and [iNKORE.UI.WPF.Modern](https://github.com/iNKORE-NET/UI.WPF.Modern) for a Fluent 2 look and feel and maximum compatability with Windows 10, 11, and supporter Server versions.

## What it does

The installer is a linear wizard that walks through the pages in `Wizard/Pages/`, in this order:

| Page              | Purpose                                                                                                                                               |
| ----------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| `WelcomePage`     | Confirms the OS is supported and relaunches the process elevated if it isn't already.                                                                 |
| `VersionPage`     | Lets the user pick a GitHub release, an unreleased branch, or a local folder/zip to install.                                                          |
| `PreparePage`     | Downloads/extracts the chosen payload, reads `setup.json`, and runs the prerequisite checks.                                                          |
| `LegacySetupPage` | Hands off to a PowerShell window: `setup.ps1` for releases that predate `setup.json`, or install.raweb.app's preview script for an unreleased branch. |
| `IisPage`         | Lets the user pick the IIS web site and virtual path.                                                                                                 |
| `LocationPage`    | Lets the user pick the install directory.                                                                                                             |
| `OptionsPage`     | Renders one control per option declared in `setup.json` (see below).                                                                                  |
| `InstallPage`     | Runs `InstallEngine` and shows live progress/log output.                                                                                              |

## setup.json

Each new release archive carries a `setup.json` alongside the release binaries. `setup.json` describes the release's options, prerequisites, and other metadata. The installer reads it to render the options page and to check prerequisites before installing. If `setup.json` is missing, the installer falls back to `setup.ps1` (if present).

## Skipping the version picker: raweb-install-pin.json

A launcher that already knows what to install can create a `raweb-install-pin.json` file next to the installer `.exe` before running it. Some examples:

```json
{ "releaseTag": "v2026.08.24.0" }
```

or

```json
{ "sourcePath": "C:\\...\\extracted-release" }
```

`VersionPage` checks for this at `OnEnter` and, if present, resolves it and advances automatically instead of showing the picker. If the sourcePath or releaseTag are invalid, it falls back to the version picker.

## Building

Run the following command to build the installer and copy it into `dist/`:

```
dotnet publish dotnet/RAWeb.Server.Installer/RAWeb.Server.Installer.csproj --configuration Release
```

Every released executable follows the following naming convention:

```
Multi-version installer v<FileVersion> for RAWeb (<PlatformTarget>).exe
```

`FileVersion` is the installer's own version, which is independent of RAWeb's release version. The installer is generic and can install any release, so it does not need to change per release.
