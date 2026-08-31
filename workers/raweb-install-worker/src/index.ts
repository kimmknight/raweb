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
			const isZip = url.searchParams.get('f') === 'zip';
			const isExe =
				url.searchParams.get('f') === 'exe' || (request.headers.get('Accept')?.includes('text/html') && !url.searchParams.has('f'));
			const pathParts = url.pathname.split('/').filter((part) => !!part);

			const allowedOwners = ['kimmknight', 'jackbuehner'];

			// /latest or /latest/[kimmknight|jackbuehner] redirect to the installer from the latest release
			if (pathParts[0] === 'latest') {
				if (pathParts.length > 2 || (pathParts[1] && !allowedOwners.includes(pathParts[1]))) {
					return new Response('Invalid URL format. Expected format: /latest or /latest/[kimmknight|jackbuehner]', {
						status: 400,
						headers: { 'Content-Type': 'text/plain' },
					});
				}

				const owner = pathParts[1] ?? 'kimmknight';
				const latestReleaseResponse = await getReleaseRedirect(owner, 'latest', env);
				ctx.waitUntil(cache.put(cacheKey, latestReleaseResponse.clone()));
				return latestReleaseResponse;
			}

			// /release/[kimmknight|jackbuehner]/[tag] redirect to the installer from a specific release
			if (pathParts[0] === 'release') {
				if (pathParts.length !== 3 || !allowedOwners.includes(pathParts[1]) || pathParts[2] === '') {
					return new Response('Invalid URL format. Expected format: /release/[kimmknight|jackbuehner]/[tag]', {
						status: 400,
						headers: { 'Content-Type': 'text/plain' },
					});
				}

				const releaseResponse = await getReleaseRedirect(pathParts[1], pathParts[2], env);
				ctx.waitUntil(cache.put(cacheKey, releaseResponse.clone()));
				return releaseResponse;
			}

			if (pathParts.length !== 3 || pathParts[0] !== 'preview' || !allowedOwners.includes(pathParts[1]) || pathParts[2] === '') {
				return new Response(
					'Invalid URL format. Expected format: /preview/[kimmknight|jackbuehner]/[branch], /release/[kimmknight|jackbuehner]/[tag], /latest/[kimmknight|jackbuehner], or /latest',
					{
						status: 400,
						headers: { 'Content-Type': 'text/plain' },
					},
				);
			}

			const owner = pathParts[1];
			const branch = pathParts[2];

			const shouldSkipArtifactCheck =
				['false', '0'].includes(url.searchParams.get('artifact')?.toLowerCase() ?? 'true') ||
				url.searchParams.get('source')?.toLowerCase() === 'force';

			const artifactsOrErrorResponse = shouldSkipArtifactCheck
				? { build: null, installer: null }
				: await getArtifactDownloadUrls(owner, branch, env).catch((error: Error) => {
						return new Response(`Error fetching artifact download URL: ${error.message}`, {
							status: 500,
							headers: { 'Content-Type': 'text/plain' },
						});
					});
			if (artifactsOrErrorResponse instanceof Response) {
				return artifactsOrErrorResponse;
			}
			const { build: buildArtifactUrl, installer: installerArtifactUrl } = artifactsOrErrorResponse;

			console.log(`Build artifact URL: ${buildArtifactUrl}`);
			console.log(`Installer artifact URL: ${installerArtifactUrl}`);

			const branchUrl = `https://github.com/${owner}/raweb/archive/refs/heads/${branch}.zip`;

			// if the zip is directly requested, we can just redirect directly to the download URL
			if (isZip) {
				const downloadUrl = buildArtifactUrl ?? branchUrl;
				return Response.redirect(downloadUrl, 302);
			}

			// if the exe is directly requested, redirect to the installer artifact
			if (isExe) {
				if (!installerArtifactUrl) {
					return new Response(
						`
<h1>
  No pre-built installer is available for this branch
</h1>
<p>
  Your options:
</p>
<ul>
  <li>Wait for a workflow run to finish and try again.</li>
  <li>Install directly from this branch's source.</li>
</ul>

<h2>
  Steps to install directly from source
</h2>
<ol>
  <li>
		Download the source code archive for this branch:
		<a href="${branchUrl}">${branchUrl}</a>
	</li>
  <li>
		Launch the <b>Multi-version installer for RAWeb</b>,
		usually available from <a href="https://github.com/${owner}/raweb/releases/latest">the latest release</a>.
	</li>
  <li>
		When prompted, choose <b>Use a local folder or .zip file</b> and select the source code archive you downloaded.
	</li>
</ol>
						`,
						{
							status: 404,
							headers: { 'Content-Type': 'text/html' },
						},
					);
				}
				return Response.redirect(installerArtifactUrl, 302);
			}

			const setupArgs: string[] = [];
			if (url.searchParams.get('express') === 'true') {
				setupArgs.push('-Express');
			}
			if (url.searchParams.get('overwrite') === 'true') {
				setupArgs.push('-Overwrite');
			}
			if (url.searchParams.get('skipHealthCheck') === 'true') {
				setupArgs.push('-SkipHealthCheck');
			}
			if (url.searchParams.get('installDir')) {
				setupArgs.push('-InstallDir', url.searchParams.get('installDir')!);
			}
			if (url.searchParams.get('webSite')) {
				setupArgs.push('-WebSite', url.searchParams.get('webSite')!);
			}
			if (url.searchParams.get('virtualPath')) {
				setupArgs.push('-VirtualPath', url.searchParams.get('virtualPath')!);
			}
			if (url.searchParams.get('anonymousAuthMode')) {
				setupArgs.push('-AnonymousAuthMode', url.searchParams.get('anonymousAuthMode')!);
			}
			if (url.searchParams.get('acceptAll') === 'true') {
				setupArgs.push('-AcceptAll');
			}
			const setupArgsString = setupArgs.map((arg) => (arg.includes(' ') ? `"${arg}"` : arg)).join(' ');

			const scriptContent = installerArtifactUrl
				? buildInstallerLaunchScript(owner, branch, installerArtifactUrl)
				: buildLegacyPreviewScript(owner, branch, buildArtifactUrl, branchUrl, setupArgsString);

			const encodedScriptContent = encodeScriptForPowerShell(scriptContent);

			const wrappedScriptContent = `# RAWeb Installer Script Launcher

# Ensure we are running as an administrator
$is_admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

if (-not $is_admin) {
		# Prefer to relaunch in Windows Terminal
    if (Get-Command wt.exe -ErrorAction SilentlyContinue) {
        Start-Process wt.exe -Verb RunAs -ArgumentList "powershell -NoExit -EncodedCommand \`"${encodedScriptContent}\`""
    } else {
        Start-Process powershell -Verb RunAs -ArgumentList "-NoExit -EncodedCommand \`"${encodedScriptContent}\`""
    }

    # Exit if fresh session
    if ((Get-History).Count -le 1) {
        exit
    }

		return;
}

# Check if running in Windows Terminal, and if not, relaunch in Windows Terminal (if available)
if ((-not $env:WT_SESSION) -and (Get-Command wt.exe -ErrorAction SilentlyContinue)) {
    # Launch in Windows Terminal
    wt.exe powershell -NoExit -EncodedCommand "${encodedScriptContent}"

    # Exit if fresh session
    if ((Get-History).Count -le 1) {
        exit
    }

		return;
}

${scriptContent}
`;

			response = new Response(wrappedScriptContent, {
				headers: {
					'Content-Type': isHtml ? 'text/plain' : 'application/x-powershell; charset=utf-8',
					'Content-Disposition': `filename="raweb-preview-${owner}-${branch}.ps1"`,
					'Cache-Control': 'max-age=45', // cache for 45 seconds (download links are only valid for 60 seconds, so we want to cache for slightly less than that)
				},
			});

			// store in cache
			if (buildArtifactUrl || installerArtifactUrl) {
				ctx.waitUntil(cache.put(cacheKey, response.clone()));
			}
		}

		return response;
	},
} satisfies ExportedHandler<Env>;

function buildInstallerLaunchScript(owner: string, branch: string, installerExeUrl: string) {
	return `# RAWeb Developer Preview Installer Script

try {
		$ProgressPreference = 'SilentlyContinue'
		$originalTitle = $Host.UI.RawUI.WindowTitle
		$Host.UI.RawUI.WindowTitle = "RAWeb Downloader"

		Write-Host ""
		Write-Host "┌─" -NoNewline
		Write-Host   " RAWeb Developer Preview Installer " -NoNewline -ForegroundColor Green
		Write-Host                                      "──────────────────────────────┐"
		Write-Host "│                                                                  │"
		Write-Host "│  Please wait while we download the developer preview installer.  │"
		Write-Host "│                                                                  │"
		Write-Host "└──────────────────────────────────────────────────────────────────┘"
		Write-Host "${owner}/${branch}" -ForegroundColor DarkGray
		Write-Host ""

		# show indeterminate progress
		Write-Host -NoNewline ([char]27 + "]9;4;3" + [char]7)

		# the installer artifact is a single .exe uploaded with archive:false, so it downloads raw -
		# there is no zip to extract here
		$exeUrl = "${installerExeUrl}";
		$tempDir = "$env:TEMP\\raweb-preview-$([Guid]::NewGuid().ToString('n'))";
		$installerPath = "$tempDir\\installer.exe";

		New-Item -ItemType Directory -Path $tempDir -Force | Out-Null;

		Write-Host "[1/2] Downloading installer..." -ForegroundColor Cyan
		Write-Verbose "  Downloading $exeUrl to $installerPath"
		Invoke-WebRequest -Uri $exeUrl -OutFile $installerPath

		# clear indeterminate progress
		Write-Host -NoNewline ([char]27 + "]9;4;0" + [char]7)

		# this installer already knows what to install (it was built pinned to this branch's
		# raweb_dev.zip - see RAWeb.Server.Installer.Stub), so it can run entirely unattended. It still
		# prompts for elevation itself when it needs to, so this script does not.
		Write-Host "[2/2] Launching installer..." -ForegroundColor Cyan
		Write-Host ""
		& $installerPath --no-gui --express

		Write-Host -NoNewline ([char]27 + "]9;4;3" + [char]7)
} finally {
		# the temporary directory is intentionally left behind: the installer commonly relaunches
		# itself elevated from inside it, so this script has no reliable way to know when the last of
		# those relaunches has actually finished running from it.
		$Host.UI.RawUI.WindowTitle = $originalTitle
}
`;
}

function buildLegacyPreviewScript(
	owner: string,
	branch: string,
	buildArtifactUrl: string | null,
	branchUrl: string,
	setupArgsString: string,
) {
	return `# RAWeb Developer Preview Installer Script

function Expand-ArchiveQuiet {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$DestinationPath,

        [switch]$Force
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $resolvedPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
    $resolvedDest = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($DestinationPath)

    if (-not (Test-Path $resolvedPath)) {
        throw "Cannot find path '$resolvedPath' because it does not exist."
    }

    if (-not (Test-Path $resolvedDest)) {
        New-Item -Path $resolvedDest -ItemType Directory -Force | Out-Null
    }

    if ($Force) {
        $zip = [System.IO.Compression.ZipFile]::OpenRead($resolvedPath)
        try {
            foreach ($entry in $zip.Entries) {
                $targetPath = Join-Path $resolvedDest $entry.FullName
                if (Test-Path $targetPath) {
                    Remove-Item -Path $targetPath -Force -Recurse
                }
            }
        }
        finally {
            $zip.Dispose()
        }
    }

    [System.IO.Compression.ZipFile]::ExtractToDirectory($resolvedPath, $resolvedDest)
}

try {
		$ProgressPreference = 'SilentlyContinue'
		$originalTitle = $Host.UI.RawUI.WindowTitle
		$Host.UI.RawUI.WindowTitle = "RAWeb Downloader"

		Write-Host ""
		Write-Host "┌─" -NoNewline
		Write-Host   " RAWeb Developer Preview Installer " -NoNewline -ForegroundColor Green
		Write-Host                                      "──────────────────────────────┐"
		Write-Host "│                                                                  │"
		Write-Host "│  Please wait while we download the developer preview installer.  │"
		Write-Host "│                                                                  │"
		Write-Host "└──────────────────────────────────────────────────────────────────┘"
		Write-Host "${owner}/${branch}" -ForegroundColor DarkGray
		Write-Host ""

		$is_admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
		if (-not $is_admin) {
				Write-Host "This script must be run as an administrator."
				Write-Host "Please run this script as an administrator and try again."
				Write-Host
				Read-Host -Prompt "Press any key to exit..."
				$Host.UI.RawUI.WindowTitle = $originalTitle
				Exit
		}

		# show indeterminate progress
		Write-Host -NoNewline ([char]27 + "]9;4;3" + [char]7)

		$branch = "${branch}";
		$zipUrl = "${buildArtifactUrl ?? branchUrl}";
		$tempDir = "$env:TEMP\\raweb";
		$zipFile = "$tempDir\\master.zip";

		# create a temporary working directory
		New-Item -ItemType Directory -Path $tempDir -Force | Out-Null;

		# download and extract the zip file
		Write-Host "[1/${branchUrl ? 4 : 3}] Downloading..." -ForegroundColor Cyan
		Write-Verbose "  Downloading $zipUrl to $zipPath"
		Invoke-WebRequest -Uri $zipUrl -OutFile $zipFile
		Write-Host "[2/${branchUrl ? 4 : 3}] Extracting..." -ForegroundColor Cyan
		Write-Verbose "  Extracting $zipFile to $tempDir"
		Expand-ArchiveQuiet -Path $zipFile -DestinationPath $tempDir -Force
		Write-Verbose "  Removing $zipFile"
		Remove-Item -Path $zipFile

		${
			!branchUrl
				? ''
				: `
		# for artifact downloads, the zip contains raweb_dev.zip that needs to be extracted again
		$innerZipFile = Get-ChildItem -Path $tempDir -Filter "*.zip" | Select-Object -First 1
		if ($innerZipFile) {
				Write-Host "[3/4] Extracting inner zip..." -ForegroundColor Cyan
				Write-Verbose "  Extracting $($innerZipFile.FullName) to $tempDir"
				Expand-ArchiveQuiet -Path $innerZipFile.FullName -DestinationPath $tempDir\\raweb-$branch -Force
				Write-Verbose "  Removing $($innerZipFile.FullName)"
				Remove-Item -Path $innerZipFile.FullName
		}
		`
		}

		# clear indeterminate progress
		Write-Host -NoNewline ([char]27 + "]9;4;0" + [char]7)

		# run the setup script
		Write-Host "[${branchUrl ? '4/4' : '3/3'}] Starting..." -ForegroundColor Cyan
		Write-Verbose "  Running setup.ps1 with args: $setupArgs"
		Write-Host ""
		powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$tempDir\\raweb-$branch\\setup.ps1" ${setupArgsString}

		Write-Host -NoNewline ([char]27 + "]9;4;3" + [char]7)
} finally {
		# remove the temporary directory
		Write-Host "Cleaning up downloaded files..." -ForegroundColor Cyan

		$maxRetries = 5
		$retryDelay = 2

		Start-Sleep -Seconds $retryDelay

		for ($i = 1; $i -le $maxRetries; $i++) {
				try {
						Remove-Item -Path $tempDir -Recurse -Force -ErrorAction Stop
						Write-Host "Cleanup complete." -ForegroundColor Green
						break
				} catch {
						Write-Warning "  Attempt \${i}: Cleanup failed. Retrying in $retryDelay seconds..."
						Start-Sleep -Seconds $retryDelay
				}
		}

		if (Test-Path $tempDir) {
				Write-Warning "Cleanup incomplete. Directory still exists: $tempDir"
		}

		Write-Host -NoNewline ([char]27 + "]9;4;0" + [char]7)
		$Host.UI.RawUI.WindowTitle = $originalTitle
}
`;
}

async function getReleaseRedirect(owner: string, tag: string, env: Env): Promise<Response> {
	const apiUrl =
		tag === 'latest'
			? `https://api.github.com/repos/${owner}/raweb/releases/latest`
			: `https://api.github.com/repos/${owner}/raweb/releases/tags/${encodeURIComponent(tag)}`;

	const release = await fetch(apiUrl, {
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
		.then((data) => releaseSchema.parse(data))
		.catch((error: Error) => error);

	if (release instanceof Error) {
		const releaseLabel = tag === 'latest' ? 'the latest release' : `release "${tag}"`;
		return new Response(`Error fetching ${releaseLabel}: ${release.message}`, {
			status: 500,
			headers: { 'Content-Type': 'text/plain' },
		});
	}

	// prefer the installer exe so users get the full guided setup experience; fall back to the
	// PowerShell install script for releases that did not publish an installer exe
	const installerAsset = release.assets.find((asset) => asset.name.startsWith('Installer_for_RAWeb') && asset.name.endsWith('(x64).exe'));
	const installScriptAsset = release.assets.find((asset) => asset.name === 'install.ps1');
	const downloadUrl = installerAsset?.browser_download_url ?? installScriptAsset?.browser_download_url;

	if (!downloadUrl) {
		const releaseLabel = tag === 'latest' ? 'the latest release' : `release "${tag}"`;
		return new Response(`No installer or install script is available for ${releaseLabel}.`, {
			status: 404,
			headers: { 'Content-Type': 'text/plain' },
		});
	}

	// a specific tag's assets never change, so it can be cached far longer than the "latest" alias,
	// whose target moves every time a new release is published
	const cacheSeconds = tag === 'latest' ? 300 : 86400;

	return new Response(null, {
		status: 302,
		headers: {
			Location: downloadUrl,
			'Cache-Control': `max-age=${cacheSeconds}`,
		},
	});
}

async function getArtifactDownloadUrls(
	owner: string,
	branch: string,
	env: Env,
): Promise<{ build: string | null; installer: string | null }> {
	const apiUrl = `https://api.github.com/repos/${owner}/raweb/actions/runs?branch=${branch}&event=push&per_page=10`;
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
		return { build: null, installer: null };
	}

	const previewRuns = apiData.workflow_runs.filter((run) => run.path === '.github/workflows/preview-backend.yaml');

	if (previewRuns.length === 0) {
		return { build: null, installer: null };
	}

	// get the artifacts for the most recent workflow run
	const artifactsUrl = previewRuns[0].artifacts_url;
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
		if (previewRuns[0].status !== 'completed') {
			throw new Error('The most recent workflow run is not yet complete. Please try again later.');
		}

		return { build: null, installer: null };
	}

	// find the "build" artifact
	const buildArtifact = artifactsData.artifacts.find((artifact) => artifact.name === 'build');
	if (!buildArtifact) {
		if (previewRuns[0].status !== 'completed') {
			throw new Error('The most recent workflow run is not yet complete. Please try again later.');
		}

		if (branch === 'guac') {
			// TODO: Make this apply to all branches once the guacd branch is merged into main.
			throw new Error('The build artifact for the "guac" branch is not yet available. Please try again later.');
		}

		return { build: null, installer: null };
	}

	// check that it is not expired
	if (buildArtifact.expired) {
		throw new Error('The build artifact for this version has expired.');
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

	// find the specific installer version
	const installerArtifact = artifactsData.artifacts.find(
		(artifact) => artifact.name.startsWith('Installer for RAWeb') && artifact.name.endsWith('-unstable (x64).exe'),
	);
	if (!installerArtifact) {
		if (previewRuns[0].status !== 'completed') {
			throw new Error('The most recent workflow run is not yet complete. Please try again later.');
		}

		return { build: artifactUrl, installer: null };
	}

	if (installerArtifact?.expired) {
		throw new Error('The installer artifact for this version has expired.');
	}

	// get the 301 redirect URL for the artifact download
	const installerDownloadUrl = installerArtifact.archive_download_url;
	const installerRedirectResponse = await fetch(installerDownloadUrl, {
		headers: {
			Accept: 'application/vnd.github+json',
			'X-GitHub-Api-Version': '2022-11-28',
			'User-Agent': 'RAWeb-Installer-Script',
			Authorization: `Bearer ${env.GITHUB_TOKEN}`,
		},
		redirect: 'manual',
	});
	const installerUrl = installerRedirectResponse.headers.get('Location');

	return { build: artifactUrl, installer: installerUrl };
}

function encodeScriptForPowerShell(script: string) {
	// Convert to UTF-16LE bytes
	const utf16Bytes = script.split('').flatMap((c) => {
		const code = c.charCodeAt(0);
		return [code & 0xff, (code >> 8) & 0xff];
	});

	const uint8Array = new Uint8Array(utf16Bytes);

	// Convert Uint8Array to base64
	let binary = '';
	for (let i = 0; i < uint8Array.length; i++) {
		binary += String.fromCharCode(uint8Array[i]);
	}

	return btoa(binary);
}

const releaseSchema = z.object({
	assets: z
		.object({
			name: z.string(),
			browser_download_url: z.string(),
		})
		.array(),
});

const runsListSchema = z.object({
	total_count: z.number(),
	workflow_runs: z
		.object({
			id: z.number(),
			artifacts_url: z.string(),
			status: z.string(),
			path: z.string(),
			workflow_id: z.number(),
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
