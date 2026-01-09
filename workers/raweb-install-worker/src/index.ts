import { z } from 'zod';

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
		const cache = caches.default;
		const cacheKey = new Request(request.url, request);
		let response = await cache.match(cacheKey);

		if (!response) {
			const url = new URL(request.url);
			const isHtml = url.searchParams.get('f') === 'html';
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

			const artifactUrl = await getBuildArtifactDownloadUrl(owner, branch, env).catch((error) => {
				console.error('Error fetching artifact download URL:', error);
				throw new Response('Error fetching artifact download URL. Please try again later.', {
					status: 500,
					headers: { 'Content-Type': 'text/plain' },
				});
			});
			const branchUrl = `https://github.com/${owner}/raweb/archive/refs/heads/${branch}.zip`;

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

$branch = "${branch}";
$zipUrl = "${artifactUrl ?? branchUrl}";
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

${
	!branchUrl
		? ''
		: `
# for artifact downloads, the zip contains raweb_dev.zip that needs to be extracted again
$innerZipFile = Get-ChildItem -Path $tempDir -Filter "*.zip" | Select-Object -First 1
if ($innerZipFile) {
		Write-Host Extracting inner zip...
		Write-Verbose "Extracting $($innerZipFile.FullName) to $tempDir"
		Expand-Archive -Path $innerZipFile.FullName -DestinationPath $tempDir\\raweb-$branch -Force
		Write-Verbose "Removing $($innerZipFile.FullName)"
		Remove-Item -Path $innerZipFile.FullName
}
`
}

# run the setup script
Write-Host Starting...
Set-ExecutionPolicy Bypass -Scope Process -Force;
& "$tempDir\\raweb-$branch\\setup.ps1" -AcceptAll;

# remove the temporary directory
Write-Host "Cleaning up..."

$maxRetries = 5
$retryDelay = 2

Start-Sleep -Seconds $retryDelay

for ($i = 1; $i -le $maxRetries; $i++) {
    try {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction Stop
        Write-Host "Cleanup successful."
        break
    } catch {
				Write-Warning "Attempt \${i}: Cleanup failed. Retrying in $retryDelay seconds..."
        Start-Sleep -Seconds $retryDelay
    }
}

if (Test-Path $tempDir) {
    Write-Warning "Cleanup incomplete. Directory still exists: $tempDir"
}
`;
			response = new Response(scriptContent, {
				headers: {
					'Content-Type': isHtml ? 'text/plain' : 'application/x-powershell',
					'Content-Disposition': `filename="raweb-preview-${owner}-${branch}.ps1"`,
					'Cache-Control': 'max-age=45', // cache for 45 seconds (download links are only valid for 60 seconds, so we want to cache for slightly less than that)
				},
			});

			// store in cache
			if (artifactUrl) {
				ctx.waitUntil(cache.put(cacheKey, response.clone()));
			}
		}

		return response;
	},
} satisfies ExportedHandler<Env>;

async function getBuildArtifactDownloadUrl(owner: string, branch: string, env: Env) {
	const apiUrl = `https://api.github.com/repos/${owner}/raweb/actions/runs?branch=${branch}&event=push&per_page=1`;
	const apiData = await fetch(apiUrl, {
		headers: {
			Accept: 'application/vnd.github+json',
			'X-GitHub-Api-Version': '2022-11-28',
			'User-Agent': 'RAWeb-Installer-Script',
			Authorization: `Bearer ${env.GITHUB_TOKEN}`,
		},
	})
		.then((response) => {
			if (!response.ok) {
				throw new Error(`GitHub API responded with status ${response.status} (${apiUrl})`);
			}
			return response.json();
		})
		.then((data) => runsListSchema.parse(data));

	if (!apiData || !apiData.workflow_runs || apiData.workflow_runs.length === 0) {
		return null;
	}

	if (apiData.workflow_runs[0].status !== 'completed') {
		throw new Error('The most recent workflow run is not yet complete. Please try again later.');
	}

	// get the artifacts for the most recent workflow run
	const artifactsUrl = apiData.workflow_runs[0].artifacts_url;
	const artifactsData = await fetch(artifactsUrl, {
		headers: {
			Accept: 'application/vnd.github+json',
			'X-GitHub-Api-Version': '2022-11-28',
			'User-Agent': 'RAWeb-Installer-Script',
			Authorization: `Bearer ${env.GITHUB_TOKEN}`,
		},
	})
		.then((response) => response.json())
		.then((data) => artifactsListSchema.parse(data))
		.catch(() => null);

	if (!artifactsData || !artifactsData.artifacts || artifactsData.artifacts.length === 0) {
		return null;
	}

	// find the "build" artifact
	const buildArtifact = artifactsData.artifacts.find((artifact) => artifact.name === 'build');
	if (!buildArtifact) {
		return null;
	}

	// check that it is not expired
	if (buildArtifact.expired) {
		return null;
	}

	// get the 301 redirect URL for the artifact download
	const downloadUrl = buildArtifact.archive_download_url;
	const redirectResponse = await fetch(downloadUrl, {
		headers: {
			Accept: 'application/vnd.github+json',
			'X-GitHub-Api-Version': '2022-11-28',
			'User-Agent': 'RAWeb-Installer-Script',
			// TIP: update this with npx wrangler secret put GITHUB_TOKEN - it should be a GitHub access token with read only permissions for public repositories
			Authorization: `Bearer ${env.GITHUB_TOKEN}`,
		},
		redirect: 'manual',
	});
	const artifactUrl = redirectResponse.headers.get('Location');

	return artifactUrl;
}

const runsListSchema = z.object({
	total_count: z.number(),
	workflow_runs: z
		.object({
			id: z.number(),
			artifacts_url: z.string(),
			status: z.string(),
		})
		.array(),
});

const artifactsListSchema = z.object({
	total_count: z.number(),
	artifacts: z
		.object({
			id: z.number(),
			expired: z.boolean(),
			name: z.string(),
			archive_download_url: z.string(),
		})
		.array(),
});
