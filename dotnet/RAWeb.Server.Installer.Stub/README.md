# RAWeb.Server.Installer.Stub

A tiny exe with no installation logic of its own. It exists so a GitHub release or a preview build in `preview-backend.yaml` can offer a "just install this build" download without needing its own copy of the full [installer](../RAWeb.Server.Installer/README.md) rebuilt and re-signed each time.

## What it does

On startup, it:

1. Extracts an embedded, unmodified copy of `RAWeb.Server.Installer.exe` to a temp directory.
2. If it was also built with distributables embedded (see below), it extracts those too.
3. Launches the extracted installer with `--source` pointing at either a specific GitHub release tag or the locally-extracted distributables.

## Building

A release build:

```
dotnet publish dotnet/RAWeb.Server.Installer.Stub/RAWeb.Server.Installer.Stub.csproj --configuration Release -p:ReleaseTag=v2026.08.24.0
```

A preview build (see `preview-backend.yaml`'s `build-server` job), pinned to a specific branch's `raweb_dev.zip` instead of a GitHub release:

```
dotnet publish dotnet/RAWeb.Server.Installer.Stub/RAWeb.Server.Installer.Stub.csproj --configuration Release -p:EmbeddedDistributablesPath=C:\path\to\raweb_dev.zip
```
