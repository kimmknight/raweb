# if the working directory is not the same as the script directory, change it to the script directory
# but store the original working directory in a variable
# so we can change it back later
$originalDir = Get-Location
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ($originalDir -ne $scriptDir) {
    Write-Host "Changing working directory to script directory..."
    Set-Location -Path $scriptDir
}
    
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



# restore the original working directory
if ($originalDir -ne $scriptDir) {
    Write-Host "Restoring original working directory..."
    Set-Location -Path $originalDir
}

