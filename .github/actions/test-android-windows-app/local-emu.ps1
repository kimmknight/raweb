# This script should be used when you are locally testing the emulator
# on a Windows test. This is faster than having to wait for nektos/act
# to go through the whole test-android-windows-app action

$localUserHomePath = [Environment]::GetFolderPath("UserProfile")
$localUserAndroidAvdPath = Join-Path $localUserHomePath ".android\avd"
$localUserAndroidSdkPath = Join-Path $localUserHomePath ".android\sdk"
$localUserAndroidEmulatorFolderPath = Join-Path $localUserAndroidSdkPath "emulator"
$emulatorExe = Join-Path $localUserAndroidEmulatorFolderPath "emulator.exe"
$avdmanagerBat = Join-Path $localUserAndroidSdkPath "cmdline-tools\20.0\bin\avdmanager.bat"

# create an android virtual device (AVD)
"no" | & $avdmanagerBat create avd --force --name test_avd_36 --package "system-images;android-36;google_apis;x86_64"

# edit config.ini to enable hardware keyboard
$configIniPath = Join-Path $localUserAndroidAvdPath "test_avd_36.avd\config.ini"
$configIniContent = Get-Content $configIniPath
$configIniContent | ForEach-Object {
  if ($_ -match "^hw.keyboard=") {
    "hw.keyboard=yes"
  } else {
    $_
  }
} | Set-Content $configIniPath

# start the emulator in a new process
Start-Process -NoNewWindow -FilePath $emulatorExe -ArgumentList "-avd test_avd_36 -gpu swiftshader_indirect -no-audio -no-snapshot-save -writable-system -memory 6144"

# run prepare-emulator.ps1
$env:ANDROID_HOME = $localUserAndroidSdkPath
./prepare-emulator.ps1

# launch appium server in a new process
Write-Host "[10/ ] Starting Appium..." -ForegroundColor Cyan
$env:ANDROID_HOME = $localUserAndroidSdkPath
$appiumDir = Join-Path $PSScriptRoot "appium"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$appiumDir'; fnm use 24; npm install; npm run appium"
