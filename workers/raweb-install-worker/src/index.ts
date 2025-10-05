/**
 * Welcome to Cloudflare Workers! This is your first worker.
 *
 * - Run `npm run dev` in your terminal to start a development server
 * - Open a browser tab at http://localhost:8787/ to see your worker in action
 * - Run `npm run deploy` to publish your worker
 *
 * Bind resources to your worker in `wrangler.jsonc`. After adding bindings, a type definition for the
 * `Env` object can be regenerated with `npm run cf-typegen`.
 *
 * Learn more at https://developers.cloudflare.com/workers/
 */

export default {
	async fetch(request, env, ctx): Promise<Response> {
		const url = new URL(request.url);
		const pathParts = url.pathname.split('/').filter((part) => !!part);

		const allowedOwners = ['kimmknight', 'jackbuehner'];

		if (pathParts.length !== 3 || pathParts[0] !== 'preview' || !allowedOwners.includes(pathParts[1]) || pathParts[2] === '') {
			return new Response('Invalid URL format. Expected format: /preview/[kimmknight|jackbuehner]/[branch]', {
				status: 400,
				headers: { 'Content-Type': 'text/plain' },
			});
		}

		const owner = pathParts[1];
		const branch = pathParts[2];
		const scriptContent = `$ProgressPreference = 'SilentlyContinue'

Write-Host "================================================"
Write-Host "RAWeb Developer Preview Edition Installer Script"
Write-Host "================================================"
Write-Host

$is_admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $is_admin) {
    Write-Host "This script must be run as an administrator."
    Write-Host "Please run this script as an administrator and try again."
    Write-Host
    Read-Host -Prompt "Press any key to exit..."
    Exit
}

$owner = "${owner}"
$branch = "${branch}";
$zipUrl = "https://github.com/$owner/raweb/archive/refs/heads/$branch.zip";
$tempDir = "$env:TEMP\\raweb";
$zipFile = "$tempDir\\master.zip";

# create a temporary working directory
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null;

# download and extract the zip file
Write-Host Downloading...
Write-Verbose "Downloading $zipUrl to $zipFile"
Invoke-WebRequest -Uri $zipUrl -OutFile $zipFile
Write-Host Extracting...
Write-Verbose "Extracting $zipFile to $tempDir"
Expand-Archive -Path $zipFile -DestinationPath $tempDir -Force
Write-Verbose "Removing $zipFile"
Remove-Item -Path $zipFile

# run the setup script
Write-Host Starting...
Set-ExecutionPolicy Bypass -Scope Process -Force;
& "$tempDir\\raweb-$branch\\setup.ps1" -AcceptAll;

# remove the temporary directory
Write-Host Cleaning up...
Remove-Item -Path $tempDir -Recurse -Force;
`;

		return new Response(scriptContent, {
			headers: { 'Content-Type': 'text/plain' },
		});
	},
} satisfies ExportedHandler<Env>;
