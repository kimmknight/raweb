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

## Skipping the version picker: --source

A launcher that already knows what to install can pass `--source <value>` on the command line instead of showing the picker. `<value>` can be:

- A local folder or `.zip` file already on disk (e.g. `--source C:\extracted-release`).
- A GitHub release tag (e.g. `--source v2026.08.24.0`), resolved from `kimmknight/raweb`.
- `<owner>::<tag>` (e.g. `--source jackbuehner::v2026.08.24.0`) to resolve a tag from a trusted fork instead. `kimmknight` and `jackbuehner` are the only allowed owners (`ReleaseSource.TrustedOwners`).

`VersionPage` checks for this at `OnEnter` and, if present, resolves it and advances automatically instead of showing the picker. If `--source` is invalid or fails to resolve, the installer falls back to the version picker with an error message.

`--release-label <text>` overrides the version label shown in the installer interface.

## Command-line options

A launcher can also pre-answer some or all of the wizard on the command line. A switch that fills in a page's value still lets the user review and change it before continuing, unless combined with `--express`.

| Option                                              | Description                                                                                                                         |
| --------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `--source <value>`                                  | Skip the version picker (see above).                                                                                                |
| `--release-label <text>`                            | Override the version label shown in the wizard/console.                                                                             |
| `--website <name>`, <br/>`-WebSite <name>`          | Specify the IIS web site that should serve RAWeb.                                                                                   |
| `--virtual-path <path>`, <br/>`-VirtualPath <path>` | Specify the virtual path within that IIS web site.                                                                                  |
| `--install-dir <path>`, <br/>`-InstallDir <path>`   | Specify where the software files will be stored. RAWeb's application data will be stored in this directory.                         |
| `--option <id>=<value>`                             | Specify one option declared in `setup.json`. Repeat for more than one.                                                              |
| `--express`, <br/>`-Express`, <br/>`-AcceptAll`     | Advance past a page automatically once it has a valid value.                                                                        |
| `--overwrite`, <br/>`-Overwrite`                    | Bypass warnings about replacing or losing existing data (`InstallPage`).                                                            |
| `--no-welcome`                                      | Skip the welcome page. The installer will automatically relaunch with administrator privileges.                                     |
| `--autoclose <always\|success\|never>`              | Close the window on its own once installation finishes (`InstallPage`). Defaults to `never`.                                        |
| `--no-gui`                                          | Run entirely in the console instead of showing the wizard window. You must specify all options that should not take default values. |

Combine `--express` and `--overwrite` for a wizard that runs start to finish without prompting. `--express` implies `--no-welcome`, and does not answer the warnings covered by `--overwrite` on its own. `--overwrite` does not bypass unrelated warnings.

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
