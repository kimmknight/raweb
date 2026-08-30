# RAWeb Uninstaller
# Generated  : {{GENERATED}}
# Uninstalls : {{DISPLAY_NAME}}
#   Install directory : {{INSTALL_DIR}}
#   Web site          : {{WEB_SITE}}
#   Virtual path      : {{VIRTUAL_PATH}}
#   Application pool  : {{APP_POOL}}
#   Service name      : {{SERVICE_NAME}}
#   Registry key      : {{REG_KEY_NAME}}

[CmdletBinding()]
Param([switch]$FromTemp)

$is_admin = ([System.Security.Principal.WindowsPrincipal][System.Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([System.Security.Principal.WindowsBuiltInRole]"Administrator")
if (-not $is_admin) {
    Write-Host "This script must be run as Administrator." -ForegroundColor Red
    exit 1
}

if (-not $FromTemp) {
    $tmp = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "raweb_uninstall_{{REG_KEY_NAME}}.ps1")
    Copy-Item $PSCommandPath $tmp -Force
    Start-Process powershell -Verb RunAs -ArgumentList ("-ExecutionPolicy Bypass -File `"$tmp`" -FromTemp")
    exit 0
}

try {
    $host.UI.RawUI.BufferSize      = New-Object System.Management.Automation.Host.Size(80, 9999)
    $host.UI.RawUI.WindowSize      = New-Object System.Management.Automation.Host.Size(80, 20)
    $host.UI.RawUI.BackgroundColor = "Black"
    Clear-Host
} catch {}

Import-Module WebAdministration -Force -ErrorAction SilentlyContinue

$installDir  = "{{INSTALL_DIR_ESCAPED}}"
$webSite     = "{{WEB_SITE_ESCAPED}}"
$virtualPath = "{{VIRTUAL_PATH_ESCAPED}}"
$appPoolName = "{{APP_POOL}}"
$serviceName = "{{SERVICE_NAME}}"
$regKeyName  = "{{REG_KEY_NAME}}"
$displayName = "{{DISPLAY_NAME_ESCAPED}}"
$regPath     = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$regKeyName"
$webSitePort = "{{SITE_PORT}}"

Write-Host ""
Write-Host "+++ RAWeb Uninstaller +++" -BackgroundColor Black -ForegroundColor Red
Write-Host ""
Write-Host "This will uninstall : $displayName"
Write-Host "Install directory   : $installDir"
Write-Host "Local URL           : http://localhost:$webSitePort/$virtualPath"
Write-Host ""

$confirmed = $false
while (-not $confirmed) {
    $inp = Read-Host "(y/N) Are you sure?"
    if ([string]::IsNullOrEmpty($inp) -or $inp -eq "n") {
        Write-Host "Cancelled."
        Write-Host ""
        Read-Host "Press Enter to close"
        exit 0
    }
    if ($inp -eq "y") { $confirmed = $true }
    else { Write-Host "  Invalid input. Please enter Y or N." -ForegroundColor Yellow }
}
Write-Host ""

Write-Host "Stopping management service..." -ForegroundColor Cyan
$svc = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($svc) {
    $wmiSvc = Get-WmiObject Win32_Service -Filter "Name='$serviceName'" -ErrorAction SilentlyContinue
    $svcPid = if ($wmiSvc) { $wmiSvc.ProcessId } else { 0 }
    try { Stop-Service -Name $serviceName -Force -ErrorAction Stop } catch {}
    if ($svcPid -gt 0) {
        $proc = Get-Process -Id $svcPid -ErrorAction SilentlyContinue
        if ($proc) { $proc | Stop-Process -Force -ErrorAction SilentlyContinue }
    }
    Start-Sleep -Seconds 1
    sc.exe delete $serviceName | Out-Null
}

Write-Host "Stopping application pool..." -ForegroundColor Cyan
try { Stop-WebAppPool -Name $appPoolName -ErrorAction SilentlyContinue } catch {}
$poolElapsed = 0
while ($poolElapsed -lt 15) {
    $poolState = (Get-WebAppPoolState -Name $appPoolName -ErrorAction SilentlyContinue).Value
    if ($poolState -eq "Stopped" -or $null -eq $poolState) { break }
    Start-Sleep -Seconds 1
    $poolElapsed++
}

Write-Host "Removing IIS application..." -ForegroundColor Cyan
Remove-WebApplication -Site $webSite -Name $virtualPath -ErrorAction SilentlyContinue
try { Remove-Item "IIS:\Sites\$webSite\$virtualPath" -Recurse -Force -ErrorAction Stop } catch {}

Write-Host "Removing application pool..." -ForegroundColor Cyan
Remove-WebAppPool -Name $appPoolName -ErrorAction SilentlyContinue

Write-Host "Removing Add/Remove Programs entry..." -ForegroundColor Cyan
Remove-Item -Path $regPath -Force -ErrorAction SilentlyContinue

Write-Host "Removing install directory: $installDir" -ForegroundColor Cyan
$skippedFiles = @()
if (Test-Path $installDir) {
    # Delete files individually so a single locked file doesn't block the rest
    Get-ChildItem $installDir -Recurse -Force -File | Sort-Object FullName -Descending | ForEach-Object {
        try { Remove-Item $_.FullName -Force -ErrorAction Stop }
        catch { $skippedFiles += $_.FullName }
    }
    # Remove empty directories bottom-up
    Get-ChildItem $installDir -Recurse -Force -Directory | Sort-Object FullName -Descending | ForEach-Object {
        try { Remove-Item $_.FullName -Force -ErrorAction Stop } catch {}
    }
    try { Remove-Item $installDir -Force -ErrorAction Stop } catch {
        try { [System.IO.Directory]::Delete($installDir, $false) }
        catch { Write-Warning "Could not remove directory: $_" }
    }
}
$removedDir = -not (Test-Path $installDir)
if ($skippedFiles.Count -gt 0) {
    Write-Warning "The following files could not be removed (still in use):"
    $skippedFiles | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
}

if ($FromTemp) {
    Start-Sleep -Seconds 2
    Remove-Item $PSCommandPath -Force -ErrorAction SilentlyContinue
}

Write-Host ""
if ($removedDir) {
    Write-Host "RAWeb uninstalled successfully." -ForegroundColor Green
} else {
    Write-Host "RAWeb partially uninstalled. The install directory could not be removed." -ForegroundColor Yellow
    Write-Host "IIS application, app pool, service, and registry entry have been removed." -ForegroundColor Yellow
}
Write-Host ""
Read-Host "Press Enter to close"
