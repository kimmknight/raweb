[CmdletBinding()]
Param(
    [string]$DefaultMode = ""
)

$mode = ""
if ($DefaultMode -eq "1" -or $DefaultMode -eq "2") {
    $mode = $DefaultMode
} else {
    Write-Host "=============================="
    Write-Host "RAWeb offers two different options for installing the frontend."
    Write-Host ""
    Write-Host "Option 1 is to use Vite. This option requires downloading"
    Write-Host "Node.js and NPM, which are not included in the repository."
    Write-Host "This option takes longer to install, but the frontend will be"
    Write-Host "bundled and minified, which will ensure fast page load times."
    Write-Host "Option 1 is the recommended option."
    Write-Host ""
    Write-Host "Option 2 is to use the unbundled version. This option does not"
    Write-Host "require downloading Node.js and NPM, but the frontend will not"
    Write-Host "be bundled and minified. This option is faster to install, but"
    Write-Host "the page load times will be slower."
    Write-Host ""
    Write-Host "Please choose an option:"
    Write-Host "[1] Vite (recommended) "
    Write-Host "[2] Unbundled (faster to install, but slower page load times)"
    Write-Host ""
    $mode = Read-Host -Prompt "(1/2)"
}
if ($mode -eq "1") {
    $vite = $true
} elseif ($mode -eq "2") {
    $vite = $false
} else {
    Write-Host "Invalid choice. Please run the script again and choose either 1 or 2."
    exit
}

# if the working directory is not the same as the script directory, change it to the script directory
# but store the original working directory in a variable
# so we can change it back later
$originalDir = Get-Location
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ($originalDir -ne $scriptDir) {
    Write-Host "Changing working directory to script directory..."
    Set-Location -Path $scriptDir
}

# in vite mode:

if ($vite) {
    Write-Host "Vite mode selected."

    # step 1. Download nodejs: https://nodejs.org/dist/v22.15.0/node-v22.15.0-win-x64.zip
    $nodeJsUrl = "https://nodejs.org/dist/v22.15.0/node-v22.15.0-win-x64.zip"
    $tmpDir = Join-Path -Path (Get-Location) -ChildPath "tmp"
    $nodeJsZipPath = Join-Path -Path $tmpDir -ChildPath "nodejs.zip"
    $nodeJsDir = Join-Path -Path (Get-Location) -ChildPath "nodejs"
    if (-not (Test-Path -Path $nodeJsDir)) {
        New-Item -ItemType Directory -Path $nodeJsDir | Out-Null
    }
    if (-not (Test-Path -Path $tmpDir)) {
        New-Item -ItemType Directory -Path $tmpDir | Out-Null
    }

    Write-Host "Downloading Node.js..."
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $nodeJsUrl -OutFile $nodeJsZipPath

    Write-Host "Unzipping Node.js..."
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $unzipPath = Join-Path -Path (Get-Location) -ChildPath "tmp\nodejs"
    [System.IO.Compression.ZipFile]::ExtractToDirectory($nodeJsZipPath, $unzipPath)

    # copy the contents of ./tmp/nodejs/node-v22.15.0-win-x64 to ./nodejs
    $sourcePath = Join-Path -Path $unzipPath -ChildPath "node-v22.15.0-win-x64"
    Copy-Item -Path $sourcePath\* -Destination $nodeJsDir -Recurse -Force

    # remove the tmp folder
    Write-Host "Cleaning up..."
    Remove-Item -Path $tmpDir -Recurse -Force

    # temporarily set the PATH environment variable to include the nodejs directory
    $env:PATH = "$nodeJsDir;$env:PATH"

    Write-Host ""
    Write-Host "Node.js temporarily installed. Version:" (node -v)
    Write-Host "NPM temporarily installed. Version:" (npm -v)
    Write-Host ""

    Write-Host "Installing node modules..."
    npm ci

    Write-Host "Building the frontend..."
    npm run build

    Write-Host "Deleting Node.js..."
    # remove the nodejs folder
    Remove-Item -Path $nodeJsDir -Recurse -Force
} else {
    Write-Host "Unbundled mode selected."
    
    # step 1. create the folder ../aspx/wwwroot/app if it doesn't exist
    Write-Host "Creating directory ../aspx/wwwroot/app if it doesn't exist..."
    $wwwrootAppPath = Join-Path -Path (Get-Location) -ChildPath "..\aspx\wwwroot\app"
    if (-not (Test-Path -Path $wwwrootAppPath)) {
        New-Item -ItemType Directory -Path $wwwrootAppPath | Out-Null
    }

    # step 2. copy files in ./public to ../aspx/wwwroot/app
    Write-Host "Copying files from ./public to ../aspx/wwwroot/app..."
    $publicPath = Join-Path -Path (Get-Location) -ChildPath "public"
    $publicFiles = Get-ChildItem -Path $publicPath -Recurse
    foreach ($file in $publicFiles) {
        $destinationPath = Join-Path -Path $wwwrootAppPath -ChildPath $file.FullName.Substring($publicPath.Length)
        if ($file.PSIsContainer) {
            New-Item -ItemType Directory -Path $destinationPath -Force | Out-Null
        } else {
            Copy-Item -Path $file.FullName -Destination $destinationPath -Force | Out-Null
        }
    }

    # step 3. ensure the lib folder exists in ../aspx/wwwroot/app/lib
    Write-Host "Creating directory ../aspx/wwwroot/app/lib if it doesn't exist..."
    $libPath = Join-Path -Path (Get-Location) -ChildPath "lib"
    $libDestinationPath = Join-Path -Path $wwwrootAppPath -ChildPath "lib"
    if (-not (Test-Path -Path $libDestinationPath)) {
        New-Item -ItemType Directory -Path $libDestinationPath | Out-Null
    }

    # step 4. copy files from ./lib to ../aspx/wwwroot/app/lib
    Write-Host "Copying files from ./lib to ../aspx/wwwroot/app/lib..."
    $libFiles = Get-ChildItem -Path $libPath -Recurse
    foreach ($file in $libFiles) {
        $destinationPath = Join-Path -Path $libDestinationPath -ChildPath $file.FullName.Substring($libPath.Length)
        if ($file.PSIsContainer) {
            New-Item -ItemType Directory -Path $destinationPath -Force | Out-Null
        } else {
            Copy-Item -Path $file.FullName -Destination $destinationPath -Force
        }
    }

}

# restore the original working directory
if ($originalDir -ne $scriptDir) {
    Write-Host "Restoring original working directory..."
    Set-Location -Path $originalDir
}
