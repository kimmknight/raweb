# change the working directory to the script's directory
$oldWorkingDir = Get-Location
cd $PSScriptRoot

# temporarily add binaries to PATH
$binDir = "$PSScriptRoot/bin"
$env:PATH = "$binDir;$env:PATH"

# install dependencies
pnpm install --loglevel=warn

# build frontend
pnpm build

# revert working directory
cd $oldWorkingDir