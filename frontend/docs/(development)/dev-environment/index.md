---
title: Setting up a development environment
nav_title: Dev environment setup
---

RAWeb is a Windows-only application. Development requires a Windows machine because the backend is an ASP.NET Framework 4.6.2 web application that relies on Windows-specific APIs (Active Directory, WMI, Windows Authentication) that cannot run on other platforms.

## Architecture overview

RAWeb has two main parts:

- **Backend** (`dotnet/RAWebServer/`): An ASP.NET Framework 4.6.2 application hosted in IIS Express. Handles authentication, workspace feeds, resource management, and all API endpoints. Built with .NET SDK 9 targeting `net462`.
- **Frontend** (`frontend/`): A Vue + Vite application. In development, the Vite dev server runs on `https://localhost:5174` and proxies API calls to the backend. In production, the frontend is compiled directly into `dotnet/RAWebServer/`.

During development, you run both simultaneously: the Vite dev server delivers the UI with hot module replacement, and IIS Express hosts the .NET backend.

## Prerequisites

### Visual Studio Code

Download and install [Visual Studio Code](https://code.visualstudio.com/). The repository includes a `.code-workspace` file that configures VS Code for this project, including tasks that start the backend and frontend automatically.

Recommended extensions:

- **C# Dev Kit** (`ms-dotnettools.csdevkit`): IntelliSense, debugging, and project support for the .NET backend
- **Vue - Official** (`vue.volar`): Vue language features and TypeScript support for the frontend

### .NET SDK 9

Download and install the [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9). The SDK provides MSBuild, which is used to compile the `net462` ASP.NET Framework project.

To verify the installation, run `dotnet --list-sdks`. At least one SDK version beginning with `9.` should appear in the output.

### IIS Express

Download and install [IIS Express](https://www.microsoft.com/en-us/download/details.aspx?id=48264) from the Microsoft website. IIS Express is also included with Visual Studio. No additional configuration is required.

<InfoBar>

Full IIS can be used instead of IIS Express, and it more accurately mirrors a production environment. See the [manual installation instructions](/docs/installation/#manual-installation-in-iis) for details on enabling and configuring full IIS.

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

| Label                              | Path                                          | Purpose                                         |
| ---------------------------------- | --------------------------------------------- | ----------------------------------------------- |
| **Repository**                     | `.`                                           | The repo root (`RAWeb.slnx`, `setup.ps1`, etc.) |
| **Frontend**                       | `frontend/`                                   | The Vue + Vite application                      |
| **Workers » Install**              | `workers/raweb-install-worker/`               | The install worker service                      |
| **Backend**                        | `dotnet/RAWebServer/`                         | The ASP.NET web application root                |
| **Backend » Management**           | `dotnet/RAWeb.Server.Management/`             | The management API project                      |
| **Backend » Management » Service** | `dotnet/RAWeb.Server.Management.ServiceHost/` | The Windows service host                        |
| **Backend » Utilities**            | `dotnet/RAWeb.Server.Utilities/`              | Shared utility library                          |

## VS Code tasks

Each of the **Backend** and **Frontend** workspace folders contains a `tasks.json` with tasks that start automatically when the folder is opened. When you open the workspace for the first time, VS Code may ask whether you want to allow the tasks to run; click **Allow** to proceed.

### Backend tasks

The **Backend** folder (`dotnet/RAWebServer/`) defines two background tasks that run together on folder open:

- **Build**: runs `dotnet watch build` in debug configuration, recompiling the .NET solution whenever a C# source file changes
- **IIS**: starts IIS Express pointing at the build output folder on port 8080

### Frontend task

The **Frontend** folder (`frontend/`) defines a single background task that runs on folder open:

- **Web App**: installs frontend dependencies with `npm install` and then starts the Vite dev server

## Starting the development environment

Opening the workspace starts everything automatically:

1. VS Code opens all seven workspace folders.
2. The **Backend** folder triggers the **Build + Start IIS Express** task, which builds the .NET solution and starts IIS Express on `http://localhost:8080`.
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

The **Build** task uses `dotnet watch build`, so the .NET solution recompiles automatically when you save a C# file. IIS Express picks up the new assemblies from the build output folder without needing a restart.

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
    ├── RAWebServer/            # ASP.NET web application root
    │   ├── .vscode/tasks.json  # Backend tasks (Build, IIS, Build + Start IIS Express)
    │   ├── Web.config
    │   ├── Global.asax
    │   ├── App_Data/           # App settings, resources, policies (not committed)
    │   └── build/              # MSBuild output served by IIS Express (gitignored)
    ├── RAWeb.Server.Management/
    ├── RAWeb.Server.Management.ServiceHost/
    └── RAWeb.Server.Utilities/
```
