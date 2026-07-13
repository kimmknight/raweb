param(
    [string]$PfxPath = "$PSScriptRoot\.raweb\DevCert.pfx"
)

$env:PSModulePath = [Environment]::GetEnvironmentVariable('PSModulePath', 'Machine')
Import-Module Microsoft.PowerShell.Security

$subject = "CN=RAWeb"

Write-Host "Creating self-signed dev signing certificate..."
Write-Host "Writing self-signed certificate to: $PfxPath"

$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject $subject `
    -KeyUsage DigitalSignature `
    -FriendlyName "RAWeb Desktop (Dev)" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")


$outputDir = Split-Path $PfxPath
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Force $outputDir | Out-Null
}

Export-PfxCertificate -Cert $cert -FilePath $PfxPath -Password (New-Object System.Security.SecureString) | Out-Null

# Trust the cert so the installed MSIX is accepted by Windows
# Uses CurrentUser store to avoid requiring elevation
$trustedStore = [System.Security.Cryptography.X509Certificates.X509Store]::new("TrustedPeople", "CurrentUser")
$trustedStore.Open("ReadWrite")
$trustedStore.Add($cert)
$trustedStore.Close()

Write-Host "Certificate created and trusted."
Write-Host "  PFX:        $PfxPath"
Write-Host "  Thumbprint: $($cert.Thumbprint)"
Write-Host "  Subject:    $($cert.Subject)"
