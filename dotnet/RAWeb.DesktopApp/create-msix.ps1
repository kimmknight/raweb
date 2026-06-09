param(
  [string]$AotDir = "$PSScriptRoot\.raweb\aot",
  [string]$OutputDir = "$PSScriptRoot\.raweb\",
  [string]$CertFile = "$PSScriptRoot\.raweb\DevCert.pfx",
  [string]$Arch = "x64"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

#-----------------------------------------------
# Locate Windows SDK tools
#-----------------------------------------------
$kitsRoot = (Get-ItemProperty "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows Kits\Installed Roots" -ErrorAction SilentlyContinue).KitsRoot10
if (-not $kitsRoot) {
  $kitsRoot = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots" ` -ErrorAction SilentlyContinue).KitsRoot10
}
if (-not $kitsRoot) { 
  throw "Windows SDK not found in registry."
}

$sdkVersion = Get-ChildItem "$kitsRoot\bin" -Directory |
  Where-Object { $_.Name -match "^10\." } |
  Sort-Object Name -Descending |
  Select-Object -First 1 -ExpandProperty Name

$makeAppx = "$kitsRoot\bin\$sdkVersion\x64\makeappx.exe"
$signTool  = "$kitsRoot\bin\$sdkVersion\x64\signtool.exe"

if (-not (Test-Path $makeAppx)) { 
  throw "makeappx.exe not found at: $makeAppx"
}
if (-not (Test-Path $signTool)) { 
  throw "signtool.exe not found at: $signTool" 
}

#-----------------------------------------------
# Validate inputs
#-----------------------------------------------
if (-not (Test-Path $AotDir)) { 
  throw "AOT publish dir not found. Run 'dotnet publish -r win-x64 -c Release' first."
}
if (-not (Test-Path $CertFile)) {
  throw "Signing cert not found. Run create-dev-signing-cert.ps1 first."
}

#-----------------------------------------------
# Generate AppxManifest.xml
#-----------------------------------------------
$sourceManifest = "$PSScriptRoot\Package.appxmanifest"
$destManifest = "$AotDir\AppxManifest.xml"

Write-Host "Generating AppxManifest.xml..."

[xml]$xml = Get-Content $sourceManifest -Encoding UTF8

$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace("m", "http://schemas.microsoft.com/appx/manifest/foundation/windows10")

# read the version from the published executable
$exePath = "$AotDir\rawebd.exe"
if (-not (Test-Path $exePath)) {
  throw "Published executable not found at: $exePath"
}
$version = (Get-Item $exePath).VersionInfo.FileVersion

# set ProcessorArchitecture and Version on Identity
$identity = $xml.SelectSingleNode("//m:Identity", $ns)
$identity.SetAttribute("ProcessorArchitecture", $Arch)
$identity.SetAttribute("Version", $version)

$xml.Save($destManifest)
Write-Host "  -> $destManifest"

#-----------------------------------------------
# Pack with MakeAppx
#-----------------------------------------------
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$msixFile = "$OutputDir\RAWeb.DesktopApp_${version}_${Arch}.msix"

Write-Host "Packing MSIX..."
& $makeAppx pack /d $AotDir /p $msixFile /o
if ($LASTEXITCODE -ne 0) {
  throw "makeappx failed (exit $LASTEXITCODE)"
}
Write-Host "  -> $msixFile"

#-----------------------------------------------
# Sign with DevCert.pfx
#-----------------------------------------------
Write-Host "Signing..."
& $signTool sign /fd SHA256 /f $CertFile $msixFile
if ($LASTEXITCODE -ne 0) {
  throw "signtool failed (exit $LASTEXITCODE)"
}

Write-Host "Done: $msixFile"

#-----------------------------------------------
# Stage embedded resources for installer
#-----------------------------------------------
$installerDir = "$PSScriptRoot\..\RAWeb.DesktopApp.Installer"
$embeddedCer  = "$installerDir\app.cer"
$embeddedMsix = "$installerDir\app.msix"

# Export the public certificate (no private key) for embedding in the installer
$pfxCert  = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CertFile, "")
$cerBytes = $pfxCert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
[System.IO.File]::WriteAllBytes($embeddedCer, $cerBytes)
Copy-Item $msixFile $embeddedMsix -Force
