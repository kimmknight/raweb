$adbExe = Join-Path $env:ANDROID_HOME "platform-tools\adb.exe"
function Wait-For-Emulator {
  param (
    [int]$TimeoutSeconds = 120
  )

  $elapsed = 0
  Write-Host "Waiting for emulator to come online in adb..."
  while ($elapsed -lt $TimeoutSeconds) {
    $online = & $adbExe devices | Select-String "emulator" | Where-Object { $_ -notmatch "offline" }
    if ($online) {
      Write-Host "Emulator online: $online"
      return
    }
    Start-Sleep -Seconds 2
    $elapsed += 2
  }
  Write-Error "Emulator did not come online within $TimeoutSeconds seconds."
  exit 1
}

function Wait-For-Emulator-Boot {
  param (
    [int]$TimeoutSeconds = 120
  )

  $elapsed = 0
  Write-Host "Waiting for emulator to finish booting..."
  while ($elapsed -lt $TimeoutSeconds) {
    $booted = (& $adbExe shell getprop sys.boot_completed 2>&1) | Where-Object { $_ -notmatch "error|offline" }
    if (($booted | Select-Object -Last 1).Trim() -eq "1") {
      Write-Host "Emulator boot completed."
      return
    }
    Start-Sleep -Seconds 2
    $elapsed += 2
  }
  Write-Error "Emulator did not finish booting within $TimeoutSeconds seconds."
  exit 1
}


# disable verity and reboot so that we can manipulate the system partition
Write-Host "[0/ ] Waiting for emulator to start..." -ForegroundColor Cyan
Wait-For-Emulator # wait for early (limited) adb connection
Write-Host "[1/ ] Disabling verity and verification..." -ForegroundColor Cyan
& $adbExe root
Wait-For-Emulator # wait for adb to reconnect after root
& $adbExe disable-verity # run time verification
& $adbExe shell avbctl disable-verification # boot time verification
Write-Host "[2/ ] Rebooting..." -ForegroundColor Cyan
& $adbExe reboot
Write-Host "[3/ ] Waiting for emulator to start..." -ForegroundColor Cyan
Wait-For-Emulator
Write-Host "[4/ ] Remounting partions in read-write mode..." -ForegroundColor Cyan
& $adbExe root
& $adbExe remount
& $adbExe unroot
Wait-For-Emulator # wait for adb to reconnect after root
Write-Host "[5/ ] Finishing boot..." -ForegroundColor Cyan
Wait-For-Emulator-Boot # wait for full boot

Write-Host "[6/ ] Reading TLS certificate from frontend certs directory..." -ForegroundColor Cyan
$frontendCertsDir = Join-Path $PSScriptRoot "..\..\..\frontend\certs"
$certCrtPath = Get-ChildItem -Path $frontendCertsDir -Filter "ca-cert.crt" -Recurse | Select-Object -First 1
if (-not $certCrtPath) {
  Write-Error "No ca-cert.crt found in $frontendCertsDir. Start the Vite dev server once to generate it."
  exit 1
}
Write-Host "  Using cert: $($certCrtPath.FullName)" -ForegroundColor Green


Write-Host "[7/ ] Waiting for storage to be ready..." -ForegroundColor Cyan
$elapsed = 0
while ($elapsed -lt 60) {
  & $adbExe shell "ls /sdcard/ 2>&1" | Out-Null
  if ($LASTEXITCODE -eq 0) { break }
  Start-Sleep -Seconds 2
  $elapsed += 2
}

Write-Host "[8/ ] Copying certificate to downloads folder..." -ForegroundColor Cyan
$pushSuccess = $false
for ($i = 0; $i -lt 10; $i++) {
  & $adbExe push $certCrtPath.FullName "/sdcard/Download/ca-cert.crt"
  if ($LASTEXITCODE -eq 0) { $pushSuccess = $true; break }
  Write-Host "  Push failed, retrying ($($i + 1)/10)..."
  Start-Sleep -Seconds 3
}
if (-not $pushSuccess) {
  Write-Error "Failed to push certificate to emulator after 10 attempts."
  exit 1
}

# install apk
Write-Host "[9/ ] Installing Windows App APK..." -ForegroundColor Cyan
& $adbExe install -r com.microsoft.rdc.androidx_11.0.0.106-10106.apk
if ($LASTEXITCODE -ne 0) {
  Write-Error "Failed to install APK"
  exit 1
}

Write-Host "Emulator is ready and APK installed." -ForegroundColor Green
