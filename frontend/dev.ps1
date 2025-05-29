# change the working directory to the script's directory
$oldWorkingDir = Get-Location
cd $PSScriptRoot

# temporarily add binaries to PATH
$binDir = "$PSScriptRoot/bin"
$env:PATH = "$binDir;$env:PATH"

# install dependencies if not already installed
if (-not (Test-Path "node_modules")) {
    pnpm install --loglevel=warn
}

# build frontend in watch mode
pnpm build --watch
