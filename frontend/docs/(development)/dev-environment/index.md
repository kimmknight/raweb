---
title: Setting up a development environment
nav_title: Dev environment setup
---

RAWeb is a Windows-only application. Development requires a Windows machine because the backend is an ASP.NET Core web application that relies on Windows-specific APIs that cannot run on other platforms.

## Architecture overview

RAWeb has two main parts:

- **Backend** (`dotnet/RAWeb.Server/`): An ASP.NET Core web application that runs a Kestrel web server. Handles authentication, workspace feeds, resource management, and all API endpoints. Built with .NET SDK 10 targeting `net10.0-windows`.
- **Frontend** (`frontend/`): A Vue + Vite application. In development, the Vite dev server runs on `https://localhost:5174` and proxies API calls to the backend. In production, the frontend is compiled directly into `dotnet/RAWeb.Server/.raweb/client` and is then embedded directly into `raweb.exe`.

During development, you run both simultaneously: the Vite dev server delivers the UI with hot module replacement, and IIS Express hosts the .NET backend.

## Prerequisites

### Visual Studio Code

Download and install [Visual Studio Code](https://code.visualstudio.com/). The repository includes a `.code-workspace` file that configures VS Code for this project, including tasks that start the backend and frontend automatically.

Recommended extensions:

- **C# Dev Kit** (`ms-dotnettools.csdevkit`): IntelliSense, debugging, and project support for the .NET backend
- **Vue - Official** (`vue.volar`): Vue language features and TypeScript support for the frontend

### .NET SDK 10

Download and install the [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0). The SDK provides MSBuild, which is used to compile the .NET projects.

To verify the installation, run `dotnet --list-sdks`. At least one SDK version beginning with `10.` should appear in the output.

<InfoBar>

RAWeb's Kestrel server can run behind Full IIS to more accurately mirror a production environment. See the [manual installation instructions](/docs/installation/#manual-installation-in-iis) for details on enabling and configuring full IIS.

</InfoBar>

## Cloning and opening the repository

```
git clone https://github.com/kimmknight/raweb.git
```

Open the repository in VS Code using the included workspace file:

```
code raweb.code-workspace
```

<InfoBar>

Always open the **workspace file** (`raweb.code-workspace`) rather than the folder directly. The workspace file configures multiple root folders, sets up the terminal PATH for the bundled Node.js and pnpm binaries, and applies VS Code settings for this project.

</InfoBar>

## Workspace folders

The workspace defines seven root folders displayed in the VS Code Explorer:

| Label                              | Path                                          | Purpose                                                            |
| ---------------------------------- | --------------------------------------------- | ------------------------------------------------------------------ |
| **Repository**                     | `.`                                           | The repo root (`RAWeb.slnx`, `setup.ps1`, etc.)                    |
| **Frontend**                       | `frontend/`                                   | The Vue + Vite application                                         |
| **Workers » Install**              | `workers/raweb-install-worker/`               | The install worker service                                         |
| **E2W » Android**                  | `.github/actions/test-android-windows-app/`   | The workflow that confirms RAWeb works with Windows App on Android |
| **Backend**                        | `dotnet/RAWeb.Server/`                        | The ASP.NET Core web application root                              |
| **Backend » Management**           | `dotnet/RAWeb.Server.Management/`             | The management API project                                         |
| **Backend » Management » Service** | `dotnet/RAWeb.Server.Management.ServiceHost/` | The Windows service host                                           |
| **Backend » Utilities**            | `dotnet/RAWeb.Server.Utilities/`              | Shared utility library                                             |

## VS Code tasks

Each of the **Backend** and **Frontend** workspace folders contains a `tasks.json` with tasks that start automatically when the folder is opened. When you open the workspace for the first time, VS Code may ask whether you want to allow the tasks to run; click **Allow** to proceed.

### Backend tasks

The **Backend** folder (`dotnet/RAWeb.Server/`) defines a single background task that runs on folder open:

- **Build**: runs `dotnet watch build` in debug configuration, recompiling the .NET solution whenever a C# source file changes

- **Server**: runs `dotnet watch run`, which re-builds the .NET project every time a change is made, and it hosts a Kestrel server on port 5135 that the frontend development server will proxy.

### Frontend task

The **Frontend** folder (`frontend/`) defines a single background task that runs on folder open:

- **Web App**: installs frontend dependencies with `pnpm install` and then starts the Vite dev server

## Starting the development environment

Opening the workspace starts everything automatically:

1. VS Code opens all seven workspace folders.
2. The **Backend** folder triggers the **Server** task, which builds the .NET solution and starts Kestrel on `http://localhost:5135`.
3. The **Frontend** folder triggers the **Web App** task, which installs dependencies and starts the Vite dev server on `https://localhost:5174`.
4. Open `https://localhost:5174/` in your browser.

On the first visit, you will see a certificate warning. Vite generates a self-signed certificate automatically on first run and caches it under `frontend/certs/`. Proceed past the warning.

If a task did not start, you can run it manually from the Command Palette (Ctrl+Shift+P) by choosing **Tasks: Run Task** and selecting the task by name.

## What the dev server does

- Serves all Vue and TypeScript source files with hot module replacement. Changes appear in the browser without a full page reload.
- Proxies the following paths to the backend: `/api/*`, `/webfeed.aspx`, `/RDWebService.asmx`, `/auth/login.aspx`, `/inject/*`, and `/guacd-tunnel`.
- Generates the Pagefind documentation search index on startup and regenerates it whenever a `.md` file changes.

## Typical development workflow

### Frontend-only changes

Changes to Vue components, TypeScript, CSS, and Markdown docs do not require any manual steps. The `dotnet watch build` task and the Vite dev server handle reloading automatically – edit a file and the browser updates instantly.

### Backend changes

The **Server** task uses `dotnet watch run`, so the .NET solution recompiles automatically when you save a C# file. The `run` portion of the task restarts the Kestrel server automatically once the build finishes.

### Test Windows App on Android

To test that RAWeb's workspace feeds work with the Windows App on Android, run:

```
gh act -j test-android-windows-app
```

<InfoBar>

You may need to install the [GitHub CLI](https://cli.github.com/) and [nektos/act](https://nektosact.com/installation/gh.html) to run this command.

</InfoBar>

This will download the Android SDK, build raweb.exe, start raweb.exe, start an android emulator,
and use Appium to simulate actions on the Android device.

Simulated actions include:

- installing a certificate authority that matches the one used by
  the raweb.exe server
- installing the Windows App
- adding a workspace in the Windows App
- confirming that the expected resources are loaded into the app

## Project structure reference

```
raweb/
├── raweb.code-workspace       # VS Code multi-root workspace file
├── RAWeb.slnx                 # .NET solution (all four projects)
├── setup.ps1                  # Production installation script
├── frontend/
│   ├── .vscode/tasks.json     # Frontend tasks (Web App)
│   ├── bin/                   # Bundled node.exe and pnpm.exe
│   ├── build.ps1              # Installs deps and builds the frontend
│   ├── build.proj             # MSBuild wrapper that calls build.ps1
│   ├── dev.mjs                # Custom Vite dev server entry point
│   ├── vite.config.ts         # Vite config (proxy, plugins, build)
│   ├── lib/                   # Vue source code
│   │   ├── components/        # Shared UI components
│   │   ├── pages/             # Route-level page components
│   │   ├── utils/             # Utility functions and composables
│   │   ├── stores/            # Pinia state stores
│   │   └── assets/            # Icons, images, CSS
│   ├── docs/                  # Documentation Markdown pages
│   └── certs/                 # Auto-generated dev TLS certificates (gitignored)
└── dotnet/
    ├── RAWeb.Server/           # ASP.NET web application root
    │   ├── .vscode/tasks.json  # Backend tasks (Build, IIS, Build + Start IIS Express)
    │   ├── web.config
    │   ├── Global.asax
    │   ├── App_Data/           # App settings, resources, policies (not committed)
    │   └── .raweb/             # Build output (gitignored)
    │       ├── client/         # Frontend build output (production only)
    │       └── server/         # MSBuild output served by Kestrel
    │           └── App_Data/   # App settings and resources used by RAWeb when developing
    ├── RAWeb.Server.Management/
    ├── RAWeb.Server.Management.ServiceHost/
    └── RAWeb.Server.Utilities/
```
