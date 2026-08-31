---
title: Signing generated executables and scripts
nav_title: Code signing
---

RAWeb's GitHub Actions workflows (`preview-backend.yaml` and `release.yaml`) automatically sign every `.exe` and `.ps1` file they produce or ship – `raweb.exe`, `rawebmgmtsvc.exe`, the DesktopApp installer, `setup.ps1`, and the generated `install.ps1` bootstrap script – using the [`sign-exe`](https://github.com/kimmknight/raweb/tree/master/.github/actions/sign-exe) composite action. Signing uses `signtool.exe` from the Windows SDK (already present on `windows-latest` runners) with a PFX certificate stored as a repository secret.

<InfoBar title="Requires a maintainer with repo admin access" severity="caution">
  Adding or changing the signing certificate requires access to the repository's Actions secrets. Only repository owners and constributors can do this.
</InfoBar>

## How it works

Each publish step is followed by a step that calls the `sign-exe` action, passing it a glob (or list) of the files to sign and the certificate secrets:

```yaml
- name: Sign server executables
  uses: ./.github/actions/sign-exe
  with:
    files: dotnet/RAWeb.Server/dist/*.exe
    certificate-base64: ${{ secrets.CODE_SIGNING_CERTIFICATE }}
    certificate-password: ${{ secrets.CODE_SIGNING_CERTIFICATE_PASSWORD }}
```

The action decodes the base64 certificate to a temporary `.pfx` file, locates `signtool.exe` under the Windows Kits installation, and signs each matching file with `signtool.exe`.

After signing, the action imports the certificate into the runner's local `Cert:\LocalMachine\Root` store (removed again in a `finally` block) and re-checks every signed file with `Get-AuthenticodeSignature`, failing the step if any file's signature status isn't `Valid`. This catches a bad or expired certificate, a `signtool` failure that didn't surface as a non-zero exit code, or a file type `signtool` silently failed to sign.

If `CODE_SIGNING_CERTIFICATE` is not available, the action skips signing and logs a notice instead of failing the build. Pull requests from forks do not have access to secrets, so signing is skipped for those runs.

## Required repository secrets

| Secret                              | Description                                            |
| ----------------------------------- | ------------------------------------------------------ |
| `CODE_SIGNING_CERTIFICATE`          | The signing certificate's `.pfx` file, base64-encoded. |
| `CODE_SIGNING_CERTIFICATE_PASSWORD` | The password used to export/protect the `.pfx` file.   |

## Generating a code signing certificate

Run the following script on a Windows machine with the Windows SDK installed (it ships with Visual Studio, or can be installed standalone). It uses `makecert.exe` and `pvk2pfx.exe` from the SDK to create a self-signed code signing certificate, and `certutil` to base64-encode the resulting `.pfx` for pasting into a GitHub secret.

```powershell
# Run this script on a Windows machine with the Windows SDK installed.
# It will create a code-signing certificate.
# This certificate is used in the GitHub Actions workflow to sign the Windows installer.

# remove existing cert files
Remove-Item -Path 'cert.pfx', 'cert.base64.txt' -ErrorAction SilentlyContinue

$password = Read-Host -AsSecureString -Prompt "Enter certificate password"

$cert = New-SelfSignedCertificate `
  -Type CodeSigningCert `
  -Subject "CN=Jack Buehner" `
  -KeyExportPolicy Exportable `
  -KeyLength 2048 `
  -NotAfter (Get-Date "12/31/2099") `
  -CertStoreLocation Cert:\CurrentUser\My

Export-PfxCertificate `
  -Cert $cert `
  -FilePath "cert.pfx" `
  -Password $password

& certutil -encode cert.pfx cert.base64.txt
```

Replace `CN=Jack Buehner` with the publisher name you want to appear on the signature. `makecert.exe` will prompt you to set (and re-enter) a password to protect the private key.

This produces four files: `cert.cer` (public certificate), `cert.pvk` (private key), `cert.pfx` (the combined certificate + key used for signing), and `cert.base64.txt` (the base64-encoded `.pfx`, ready to paste into `CODE_SIGNING_CERTIFICATE`).

<InfoBar title="Keep the .pfx and its password safe" severity="warning">
  Anyone with the .pfx file and its password can sign executables and scripts as RAWeb. Never commit `cert.pfx`, `cert.pvk`, or `cert.base64.txt` to the repository, and only share the certificate through the GitHub secrets UI described below. Delete these files once the secrets are saved.
</InfoBar>

## Adding the certificate to GitHub

1. Copy the base64-encoded certificate to your clipboard. The `sign-exe` action expects plain base64 with no headers, so use `Convert`'s output directly rather than `cert.base64.txt` (which `certutil -encode` wraps in `-----BEGIN CERTIFICATE-----` / `-----END CERTIFICATE-----` lines):
   ```powershell
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("cert.pfx")) | Set-Clipboard
   ```
2. In the repository, go to **Settings > Secrets and variables > Actions**.
3. Click **New repository secret**, name it `CODE_SIGNING_CERTIFICATE`, and paste the base64 value from your clipboard.
4. Click **New repository secret** again, name it `CODE_SIGNING_CERTIFICATE_PASSWORD`, and enter the password you set for `cert.pvk` when running `makecert.exe`.
5. Delete the local `cert.pvk`, `cert.pfx`, `cert.cer`, and `cert.base64.txt` files and clear your clipboard once both secrets are saved.

The next workflow run that publishes an `.exe` or `.ps1` file will sign it automatically.

## Replacing the certificate

Certificates expire or may need to be reissued. To rotate:

1. Obtain the new certificate and export it to a `.pfx` file as described above.
2. Update the `CODE_SIGNING_CERTIFICATE` and `CODE_SIGNING_CERTIFICATE_PASSWORD` secrets with the new values.
