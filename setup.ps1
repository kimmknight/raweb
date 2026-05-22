# RAWeb Installer
#
# Usage:
#   .\install.ps1                                              # interactive
#   .\install.ps1 -Express                                     # express mode, all defaults
#   .\install.ps1 -Express -Overwrite                          # upgrade without confirmation
#   .\install.ps1 -InstallDir "D:\RAWeb" -WebSite "My Site" -VirtualPath "RAWeb"

[CmdletBinding()]
Param(
    [switch]$Express,
    [switch]$Overwrite,              # skip confirmation when upgrading an existing installation
    [string]$InstallDir        = "",
    [string]$WebSite           = "",
    [string]$VirtualPath       = "",
    [string]$AnonymousAuthMode = "",
    [switch]$AcceptAll               # backward compat with old setup.ps1; same as -Express
)

if ($AcceptAll) { $Express = $true }

$ErrorActionPreference = "Stop"
$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$source_dir = "dotnet\RAWeb.Server"
$frontend_src_dir = "frontend"

$INSTALL_DIR_BASE = "C:\Program Files\RAWeb"
$DEFAULT_VIRTUAL_PATH = "RAWeb"
$DEFAULT_SITE_NAME = "Default Web Site"

# ── Parameter normalization ───────────────────────────────────────────────────

$InstallDir  = $InstallDir.Trim('"/\ ')
$VirtualPath = $VirtualPath.Trim('/\ ')
$WebSite = $WebSite.Trim()
$AnonymousAuthMode = $AnonymousAuthMode.Trim().ToLower()

if ($InstallDir.TrimEnd('\') -like "C:\inetpub*") {
    Write-Host "ERROR: Installation inside C:\inetpub is not allowed." -ForegroundColor Red
    Write-Host "       Choose a different directory (e.g., C:\Program Files\RAWeb)." -ForegroundColor Yellow
    exit 1
}

# ensure that the InstallDir value is a rooted path
if (-not [string]::IsNullOrEmpty($InstallDir) -and ($InstallDir -ne "infer")) {
    try {
        $isRooted = [System.IO.Path]::IsPathRooted($InstallDir)
        if (-not $isRooted) {
            Write-Host "ERROR: InstallDir must be an absolute path or `"infer`"" -ForegroundColor Red
            exit 1
        }
    } catch {
        Write-Host "ERROR: InstallDir is not a valid path" -ForegroundColor Red
        exit 1
    }
}

# only allow specific values for AnonymousAuthMode
$validAuthModes = @("never", "allow", "always")
if ($AnonymousAuthMode -and -not $validAuthModes.Contains($AnonymousAuthMode)) {
    Write-Host "ERROR: Invalid value for AnonymousAuthMode: '$AnonymousAuthMode'" -ForegroundColor Red
    Write-Host "       Valid values are: $($validAuthModes -join ", ")" -ForegroundColor Yellow
    exit 1
}

# ── Powershell version ────────────────────────────────────────────────────────

if ($PSVersionTable.PSVersion.Major -ne 5) {
    if ($PSVersionTable.PSVersion.Major -gt 5) {
        $ps5 = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
        if (Test-Path $ps5) {
            Write-Host "Switching to powershell.exe..." -ForegroundColor Yellow
            & $ps5 -ExecutionPolicy Bypass -File $MyInvocation.MyCommand.Path @PSBoundParameters
            exit $LASTEXITCODE
        }
        Write-Host "This installer requires Windows PowerShell 5.x (5.0 or 5.1)." -ForegroundColor Red
        Write-Host "Your PowerShell version: $($PSVersionTable.PSVersion)"
        Write-Host "PowerShell 6+ is not supported and Windows PowerShell 5 was not found." -ForegroundColor Yellow
    } else {
        Write-Host "This installer requires Windows PowerShell 5.x (5.0 or 5.1)." -ForegroundColor Red
        Write-Host "Your PowerShell version: $($PSVersionTable.PSVersion)"
        Write-Host "Please upgrade to PowerShell 5.1 and try again." -ForegroundColor Yellow
    }
    Read-Host -Prompt "Press Enter to exit"
    exit 1
}

# ── Rollback stack ────────────────────────────────────────────────────────────

$script:_rollback = New-Object 'System.Collections.Generic.Stack[scriptblock]'

function Push-Rollback([scriptblock]$sb) {
    <#
    .SYNOPSIS
        Pushes a scriptblock onto the rollback stack to be executed on failure.
    #>
    $script:_rollback.Push($sb)
}

function Invoke-Rollback {
    <#
    .SYNOPSIS
        Pops and executes all rollback scriptblocks in LIFO order.
    .DESCRIPTION
        Runs each cleanup step registered via Push-Rollback, swallowing
        individual step errors so the remaining steps still execute.
    #>
    if ($script:_rollback.Count -eq 0) { return }
    Write-Host ""
    Write-Host "Rolling back changes..." -ForegroundColor Yellow
    while ($script:_rollback.Count -gt 0) {
        $rollbackStep = $script:_rollback.Pop()
        try   { & $rollbackStep }
        catch { Write-Warning "  Rollback step failed: $($_.Exception.Message)" }
    }
    Write-Host "Rollback complete." -ForegroundColor Yellow
    Write-Host ""
}

# ── Helpers ───────────────────────────────────────────────────────────────────

$script:CurrentProgress = @{
    State = 0
    Progress = 0
}
function Set-TerminalProgress {
    <#
    .SYNOPSIS
        Sets the Windows Terminal progress bar state.

    .DESCRIPTION
        Sends OSC 9;4 sequences to Windows Terminal to control the taskbar
        progress indicator. Supports different states (default, error, warning,
        indeterminate) and progress values from 0-100.

    .PARAMETER State
        The progress bar state:
        0 - Hidden (default, clears progress)
        1 - Default state with progress value
        2 - Error state with progress value
        3 - Indeterminate (spinner, ignores Progress value)
        4 - Warning state with progress value

    .PARAMETER Progress
        Progress value between 0 and 100. Ignored when State is 3.

    .EXAMPLE
        Set-TerminalProgress -State 1 -Progress 50
        Shows progress at 50% in default state.

    .EXAMPLE
        Set-TerminalProgress -State 2 -Progress 75
        Shows progress at 75% in error state.

    .EXAMPLE
        Set-TerminalProgress -State 3
        Shows indeterminate spinner.

    .EXAMPLE
        Set-TerminalProgress -State 0
        Clears/hides the progress bar.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [ValidateSet(0, 1, 2, 3, 4)]
        [int]$State = 0,

        [Parameter(Mandatory = $false)]
        [ValidateRange(0, 100)]
        [int]$Progress = 0
    )

    $ESC = [char]27
    $BEL = [char]7
    
    Write-Host "$ESC]9;4;$State;$Progress$BEL" -NoNewline

    $script:CurrentProgress.State = $State
    $script:CurrentProgress.Progress = $Progress
}

function Get-TerminalProgress {
    <#
    .SYNOPSIS
        Gets the last set terminal progress state.

    .DESCRIPTION
        Returns the progress state that was last set via Set-TerminalProgress.
        This does not query the terminal itself.

    .EXAMPLE
        Get-TerminalProgress
    #>
    [CmdletBinding()]
    param()

    return [PSCustomObject]$script:CurrentProgress
}

function Get-InstallHash([string]$installDirectoryPath) {
    <#
    .SYNOPSIS
        Returns the hex characters of the MD5 hash of the lowercased install directory path.
    .DESCRIPTION
        Produces a deterministic short identifier used to generate unique-per-installation
        names for the IIS app pool and Windows service.
    .PARAMETER dir
        Absolute path to the installation directory.
    #>

    # Remove trailing slashes so that we generate the same hash regardless of whether
    # the user includes a trailing slash in the install directory.
    $key = $installDirectoryPath.ToLower().TrimEnd('\/')

    # Generate the MD5 hash, but replace remove hyphens since
    # we will use hyphens to separate the hash from the prefix
    # in names like "raweb-<hash>".
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($key)
    $hash = [System.Security.Cryptography.MD5]::Create().ComputeHash($bytes)
    $hashString = ([System.BitConverter]::ToString($hash) -replace '-', '').ToLower()

    return $hashString
}

function Get-AppPoolName([string]$site, [string]$vpath) {
    <#
    .SYNOPSIS
        Returns the IIS application pool name for the given site and virtual path.
    .DESCRIPTION
        Derived from site+vpath (not the install directory) so the name stays stable
        across upgrades even if the install directory changes.
    #>
    $key = "$site|$($vpath.Trim('/\'))".ToLower()
    return "raweb-$(Get-InstallHash $key)"
}

function Get-ServiceName([string]$site, [string]$vpath) {
    <#
    .SYNOPSIS
        Returns the Windows service name for the given site and virtual path.
    .DESCRIPTION
        Derived from site+vpath (not the install directory) so the name stays stable
        across upgrades even if the install directory changes.
    #>
    $key = "$site|$($vpath.Trim('/\'))".ToLower()
    return "RAWebMgmt-$(Get-InstallHash $key)"
}

function Get-DisplayName([string]$site, [string]$vpath) {
    <#
    .SYNOPSIS
        Returns the human-readable display name shown in Add/Remove Programs.
    .EXAMPLE
        Get-DisplayName "Default Web Site" "RAWeb"  # -> "RAWeb (Default Web Site) (RAWeb)"
    #>
    $vpath = $vpath.Trim('/\')
    if ([string]::IsNullOrWhiteSpace($vpath)) { return "RAWeb ($site)" }
    return "RAWeb ($site) ($vpath)"
}

function Get-UninstallRegKeyName([string]$site, [string]$vpath) {
    <#
    .SYNOPSIS
        Returns the registry key name under Uninstall\ for this installation (e.g. RAWeb-14e2240d40a518a590cab1a7d5e001d6).
    .DESCRIPTION
        It is derived from site+vpath so the same logical installation always maps to the same key,
        even across upgrades that change the versioned directory.
    #>

    # Generate an MD5 hash so the key is deterministic but never exceeds the
    # registry's maximum value name length (255 chars)
    $key = "$site|$($vpath.Trim('/\'))".ToLower()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($key)
    $hash = [System.Security.Cryptography.MD5]::Create().ComputeHash($bytes)
    $hashString = ([System.BitConverter]::ToString($hash) -replace '-', '').ToLower()

    return "RAWeb-$hashString"
}

function Find-Wsl2 {
    <#
    .SYNOPSIS
        Returns $true if WSL2 is installed. WSL2 is required for the web client feature.
    #>
    return Test-Path "C:\Program Files\WSL\wsl.exe"
}

function Get-RaWebExePath([string]$srcDir) {
    <#
    .SYNOPSIS
        Returns the path to raweb.exe in the source directory, checking both .\ and dist\.
        Returns $null if not found.
    #>
    foreach ($potentialRelativeBinPath in @("raweb.exe", "dist\raweb.exe")) {
        $exePath = Join-Path $srcDir $potentialRelativeBinPath
        if (Test-Path $exePath) {
            return $exePath
        }
    }
    return $null
}

function Get-RaWebVersion([string]$srcDir) {
    <#
    .SYNOPSIS
        Returns the RAWeb version string from the file version of raweb.exe in the source directory.
    .DESCRIPTION
        Checks both .\ and dist\ for raweb.exe and returns the file version of the first one found.
        Returns "Unknown" if raweb.exe is not found or does not have a file version.
    #>
    $exePath = Get-RaWebExePath $srcDir
    if (-not $exePath) {
        return "0.0.0.0-missing"
    }
    try {
        $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath)
        return $versionInfo.FileVersion
    } catch {
        return "0.0.0.0-missing"
    }
}

function Get-VersionedDirName([string]$version) {
    <#
    .SYNOPSIS
        Returns a versioned directory name in the form "<version>__<yyyyMMdd-HHmmss>" (UTC).
    .DESCRIPTION
        Used to create a unique timestamped subdirectory for each installation so that
        multiple versions can coexist and upgrades can be rolled back.
    #>
    return "${version}__$([System.DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))"
}

function Get-DefaultInstallDir([string]$site, [string]$vpath) {
    <#
    .SYNOPSIS
        Returns the default installation base directory for the given site and virtual path.
    .DESCRIPTION
        Sanitizes the site name and virtual path for use as path segments under
        C:\Program Files\RAWeb\. Illegal path characters are replaced with underscores.
    #>
    $safeSiteName  = $site.Trim()  -replace '[<>:"/\\|?*]', '_'
    $safeVpath = ($vpath.Trim('/\') -replace '/', '\') -replace '[<>:"|?*]', '_'
    if ([string]::IsNullOrWhiteSpace($safeVpath)) {
        return "$INSTALL_DIR_BASE\$safeSiteName"
    }
    return "$INSTALL_DIR_BASE\$safeSiteName\$safeVpath"
}

function Remove-EmptyAncestorDirs([string]$startDir) {
    <#
    .SYNOPSIS
        Deletes $startDir and each empty ancestor, stopping before any top-level directory.
    .DESCRIPTION
        Walks up the directory tree from $startDir, deleting each directory that is empty.
        Stops as soon as a non-empty directory is encountered, or when the directory is a
        drive root (e.g. C:\).
    #>

    $current = $startDir
    while ($true) {
        # If the directory doesn't exist, keep climbing up until we find an existing ancestor or hit the root
        if (-not (Test-Path $current)) {
            $current = Split-Path $current -Parent
            continue
        }

        # If the directory exists but is not empty, stop. We only want to delete empty directories.
        if (@(Get-ChildItem $current -Force -ErrorAction SilentlyContinue).Count -gt 0) { break }

        
        # Stop climbing if parent is a drive root or blank
        # But still delete current if it's empty and under the root
        $parent = Split-Path $current -Parent
        if ([string]::IsNullOrEmpty($parent) -or $parent.Length -le 3) {
            try { Remove-Item $current -Force -ErrorAction Stop } catch {}
            break
        }

        # Delete the current empty directory and move up to the parent
        try { Remove-Item $current -Force -ErrorAction Stop } catch { break }
        $current = $parent
    }
}

function Get-ResolvedInstallDir([string]$site, [string]$vpath) {
    <#
    .SYNOPSIS
        Returns the install base directory to use for the given site and virtual path.
    .DESCRIPTION
        Checks IIS for an existing RAWeb application at site/vpath. If one exists and its
        base directory is not under C:\inetpub (the legacy location), that directory is
        returned so upgrades default to the same location. Otherwise falls back to
        Get-DefaultInstallDir.
    #>
    $physPath = if ($is_iisinstalled) { Get-IisAppPhysicalPath $site $vpath } else { $null }
    if ($physPath) {
        $baseDir = Split-Path $physPath -Parent
        if ($baseDir -notlike "C:\inetpub*") { return $baseDir }
    }
    return Get-DefaultInstallDir $site $vpath
}

function _WrapBoxLine([string]$text, [int]$width) {
    <#
    .SYNOPSIS
        Word-wraps a single line of text to fit within the specified character width.
    .DESCRIPTION
        Splits on spaces and greedily fills lines. Words longer than $width are emitted
        as-is on their own line rather than being broken mid-word.
    #>
    if ($text.Length -le $width) { 
        return @($text)
    }

    $result  = New-Object 'System.Collections.Generic.List[string]'
    $words   = $text -split ' '
    $current = ''
    foreach ($word in $words) {
        if ($current -eq '') {
            $current = $word
        } elseif (($current.Length + 1 + $word.Length) -le $width) {
            $current += ' ' + $word
        } else {
            $result.Add($current)
            $current = $word
        }
    }
    if ($current -ne '') { 
        $result.Add($current)
    }
    return @($result)
}

function Write-Box {
    <#
    .SYNOPSIS
        Renders $Content inside a Unicode box sized to the terminal width.
    .DESCRIPTION
        Lines containing ─── are treated as horizontal separators (├─...─┤) and
        auto-extend to fill the box. Long lines are word-wrapped before measuring
        so the box will not exceed the terminal width. -Title embeds colored text
        in the top border: ┌─ Title ───┐
    .PARAMETER Content
        Multiline string to render inside the box.
    .PARAMETER Title
        Optional title embedded in the top border. Defaults to "RAWeb Installer".
    .PARAMETER TitleColor
        Console color for the title text. Defaults to Green.
    #>
    param(
        [string]$Content,
        [string]$Title      = "RAWeb Installer",
        [string]$TitleColor = "Green"
    )

    # Max usable inner width = terminal width minus │·space·│ borders (4 chars)
    $termWidth = 80
    try { $termWidth = $host.UI.RawUI.WindowSize.Width } catch {}
    $maxInner = [Math]::Max(20, $termWidth - 4)

    # Expand and word-wrap all lines before measuring
    $rawLines = ($Content -split "`n") | ForEach-Object { $_.TrimEnd() }
    $lines = New-Object 'System.Collections.Generic.List[string]'
    foreach ($rawLine in $rawLines) {
        if ($rawLine -match '─{3}') {
            $lines.Add($rawLine)
        } else {
            foreach ($wrappedLine in @(_WrapBoxLine $rawLine $maxInner)) {
                $lines.Add($wrappedLine)
            }
        }
    }

    # Calculate the inner width of the box based on the longest line,
    # but it must not exceed $maxInner and must be wide enough to fit the title (if provided).
    $largestInnerLineWidth = $lines | Where-Object { $_ -notmatch '─{3}' } | ForEach-Object { $_.Length } | Measure-Object -Maximum
    $innerWidth = if ($largestInnerLineWidth.Count -gt 0) { $largestInnerLineWidth.Maximum } else { 0 }
    if ($Title) {
        $widthRequiredForTitle = $Title.Length + 2
        if ($widthRequiredForTitle -gt $maxInner) {
            $innerWidth = [Math]::Max($innerWidth, $maxInner)
        } else {
            $innerWidth = [Math]::Max($innerWidth, $widthRequiredForTitle)
        }
    }

    # Top border
    if ($Title) {
        $titleWithSpace   = " $Title "
        $titleFillLength = [Math]::Max(0, $innerWidth + 1 - $titleWithSpace.Length)
        Write-Host "┌─" -NoNewline
        Write-Host $titleWithSpace -NoNewline -ForegroundColor $TitleColor
        Write-Host "$('─' * $titleFillLength)┐"
    } else {
        Write-Host "┌$('─' * ($innerWidth + 2))┐"
    }

    foreach ($line in $lines) {
        # Lines with ─── are treated as full-width line separators and rendered as ├─...─┤
        if ($line -match '─{3}') {
            Write-Host "├$('─' * ($innerWidth + 2))┤"
        }
        
        # All other inner lines must be padded to the inner width and rendered with │...│ borders
        else {
            Write-Host "│ $($line.PadRight($innerWidth)) │"
        }
    }

    Write-Host "└$('─' * ($innerWidth + 2))┘"
    Write-Host ""
}

function Read-YesNo([string]$prompt, [bool]$defaultYes = $true) {
    <#
    .SYNOPSIS
        Prompts the user for a Y/N answer, re-prompting on invalid input.
    .PARAMETER prompt
        The question to display after the (Y/n)/(y/N) hint.
    .PARAMETER defaultYes
        When $true (default), an empty response returns $true and the hint shows (Y/n).
        When $false, an empty response returns $false and the hint shows (y/N).
    #>
    $hint = if ($defaultYes) { "(Y/n)" } else { "(y/N)" }
    while ($true) {
        $userInput = (Read-Host "$hint $prompt").Trim().ToLower()
        if ([string]::IsNullOrEmpty($userInput)) { return $defaultYes }
        if ($userInput -eq "y") { return $true }
        if ($userInput -eq "yes") { return $true }
        if ($userInput -eq "n") { return $false }
        if ($userInput -eq "no") { return $false }
        Write-Host "  Invalid input. Please enter Y or N." -ForegroundColor Yellow
    }
}

function Set-TextValueFromPrompt([string]$currentValue, [string]$prompt, [string]$label, [string]$defaultValue = "", [scriptblock]$validate = $null, [scriptblock]$normalize = $null) {
    <#
    .SYNOPSIS
        Prompts the user to enter a text value, re-prompting until the value is valid.
        When the prompt finishes, the prompt line is replaced with the label and the
        accepted value. If $currentValue is already non-empty the prompt is skipped.
    .PARAMETER currentValue
        Pre-existing value; if non-empty the prompt is skipped entirely.
    .PARAMETER prompt
        The question to display (without default hint — that is appended automatically).
    .PARAMETER label
        Short label shown in place of the prompt once a value is accepted.
    .PARAMETER defaultValue
        Value used when the user presses Enter without typing anything.
    .PARAMETER validate
        Optional scriptblock that receives the trimmed input and returns a non-empty
        error string when the value is invalid, or nothing/$null when it is valid.
    .PARAMETER normalize
        Optional scriptblock that receives the trimmed, validated input and returns
        the canonical form to store and display (e.g. ToLower).
    #>

    if (-not [string]::IsNullOrEmpty($currentValue)) {
        Write-Host "${label}: $currentValue"
        return $currentValue
    }

    $promptText = if ($defaultValue) { "$prompt (default: $defaultValue)" } else { $prompt }

    while ($true) {
        $userInput = (Read-Host $promptText).Trim()
        if ([string]::IsNullOrEmpty($userInput)) { 
            $userInput = $defaultValue
        }
        if ([string]::IsNullOrEmpty($userInput)) {
            Write-Host "  Please enter a non-empty value." -ForegroundColor Yellow
            continue
        }
        
        if ($validate) {
            $err = & $validate $userInput
            if (-not [string]::IsNullOrEmpty($err)) {
                Write-Host "  $err" -ForegroundColor Yellow
                continue
            }
        }

        if ($normalize) { 
            $userInput = & $normalize $userInput
        }

        Write-PreviousLine "${label}: $userInput"
        return $userInput
    }
}

function Stop-ServiceSafe([string]$name) {
    <#
    .SYNOPSIS
        Stops a Windows service by name without throwing if the service does not exist.
    .DESCRIPTION
        After stopping, it kills the specific service process by PID (not by name) so that
        other RAWeb service instances running on the same machine are not affected.
    #>

    # Ensure that a service with the given name actual exists before attempting to stop it
    $service = Get-Service -Name $name -ErrorAction SilentlyContinue
    if (-not $service) { 
        return
    }

    # Capture the PID before stopping the service. This allows us to kill it by PID
    # if the service does not stop gracefully with Stop-Service.
    $wmiSvc = Get-WmiObject Win32_Service -Filter "Name='$name'" -ErrorAction SilentlyContinue
    $serviceProcessID = if ($wmiSvc) { $wmiSvc.ProcessId } else { 0 }

    # Try to gracefully stop the service.
    try { Stop-Service -Name $name -Force -ErrorAction Stop } catch {}

    # Make sure the service process is not still running. If it is, kill it.
    if ($serviceProcessID -gt 0) {
        $process = Get-Process -Id $serviceProcessID -ErrorAction SilentlyContinue
        if ($process) { 
            Write-Host "  Service '$name' did not stop gracefully; killing service process $serviceProcessID..." -ForegroundColor Yellow
            $process | Stop-Process -Force -ErrorAction SilentlyContinue
        }
    }
    Start-Sleep -Seconds 1
}

function Unregister-ServiceSafe([string]$name) {
    <#
    .SYNOPSIS
        Stops and deletes a Windows service by name, doing nothing if it does not exist.
    #>

    # Ensure that a service with the given name actual exists before attempting to unregister it
    if (-not (Get-Service -Name $name -ErrorAction SilentlyContinue)) { 
        return
    }

    # Stop the service if it's running, then delete it.
    Stop-ServiceSafe $name
    sc.exe delete $name | Out-Null
    Start-Sleep -Milliseconds 500
}

function Register-MgmtService([string]$exePath, [string]$svcName, [string]$installDir, [string]$appPoolName, [string]$displayName = "RAWeb Management Service") {
    <#
    .SYNOPSIS
        Registers the RAWeb Management Service with the Windows Service Control Manager.
    .DESCRIPTION
        Creates the service via New-Service and configures a description and failure-restart policy.
    .PARAMETER exePath
        Full path to RAWeb.Server.Management.ServiceHost.exe.
    .PARAMETER svcName
        The SCM service name (e.g. RAWebMgmt-14e2240d).
    .PARAMETER installDir
        Install directory shown in the service description.
    .PARAMETER appPoolName
        IIS application pool name passed to the service via --app-pool at startup.
    .PARAMETER displayName
        Display name shown in Services. Defaults to "RAWeb Management Service".
    #>
    if (-not (Test-Path $exePath)) {
        Write-Warning "Service executable not found: $exePath"
        return
    }
    New-Service -Name $svcName `
        -BinaryPathName "`"$exePath`" --app-pool `"$appPoolName`"" `
        -StartupType Automatic `
        -DisplayName $displayName | Out-Null
    sc.exe description $svcName "This service performs privileged operations for RAWeb." | Out-Null
    sc.exe failure $svcName reset= 86400 actions= restart/5000/restart/10000/restart/30000 | Out-Null
}

function Get-IisAppPhysicalPath([string]$site, [string]$vpath) {
    <#
    .SYNOPSIS
        Returns the physical path of an IIS web application, or $null if it does not exist.
    #>
    $app = Get-WebApplication -Site $site -Name $vpath -ErrorAction SilentlyContinue
    if ($app) { return $app.PhysicalPath }
    return $null
}

function Invoke-HealthCheck([string]$url) {
    <#
    .SYNOPSIS
        Polls a URL up to 10 times (3-second intervals) and returns $true on a successful HTTP response.
    .DESCRIPTION
        Skips TLS certificate validation so self-signed certificates are accepted.
        Intended for verifying the application started successfully after installation.
    .PARAMETER url
        The URL to check (e.g. https://localhost/RAWeb/api/app-init-details).
    #>
    Add-Type -AssemblyName System.Net

    # Ignore TLS validation
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

    Write-Host "  Performing health check against $url..."

    for ($attempt = 1; $attempt -le 10; $attempt++) {
        try {
            $request = [System.Net.WebRequest]::Create($url)
            $request.Timeout = 5000
            $response = $request.GetResponse()
            $statusCode = [int]$([System.Net.HttpWebResponse]$response).StatusCode
            $statusDescription = $([System.Net.HttpWebResponse]$response).StatusDescription
            Write-Host "    Passed ($statusCode $statusDescription)." -ForegroundColor Green
            $response.Close()
            return $true
        } catch {
            $errorMessage = $_.Exception.InnerException
            if ($errorMessage) { $errorMessage = $errorMessage.Message } else { $errorMessage = $_.Exception.Message }
            Write-Host "    Error: $errorMessage" -ForegroundColor Red
        }
        Write-Host "  Attempt $attempt/10 - retrying in 3 s..."
        Start-Sleep -Seconds 3
    }

    Write-Host "  Health check failed after 10 attempts." -ForegroundColor Red
    return $false
}

function Clear-ScreenPreserveHistory {
    <#
    .SYNOPSIS
        Scrolls the terminal by one full window height so prior output is preserved in scroll-back history
        while effectively clearing the visible console area. The cursor is reset to the top-left corner.
    #>
    $height = [console]::WindowHeight
    1..$height | ForEach-Object { Write-Host "" }
    [Console]::SetCursorPosition(0, [Console]::WindowTop)
}

function Write-Divider {
    <#
    .SYNOPSIS
        Writes a horizontal divider line to the console in dark gray.
    #>
    Write-Host "───────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
}

function Write-PreviousLine([string]$text, [Nullable[ConsoleColor]]$ForegroundColor) {
    <#
    .SYNOPSIS
        Overwrites the previous console line with $text.
    .DESCRIPTION
        Used to replace a prompted line (e.g. "Select site number: 2") with a
        cleaner label (e.g. "Web site: Default Web Site") after the user answers.
    #>
    [Console]::SetCursorPosition(0, [Console]::CursorTop - 1)
    Write-Host (" " * ([Console]::WindowWidth - 1)) -NoNewline
    [Console]::SetCursorPosition(0, [Console]::CursorTop)
    
    if ($null -ne $ForegroundColor) {
        Write-Host $text -ForegroundColor $ForegroundColor
    } else {
        Write-Host $text
    }
}

# ── System checks ─────────────────────────────────────────────────────────────

$script:_originalWindowTitle = $host.UI.RawUI.WindowTitle
$Host.UI.RawUI.WindowTitle = "RAWeb Installer"

Write-Host "[0/13] Checking system prerequisites..." -ForegroundColor Cyan

$os              = Get-WmiObject -Class Win32_OperatingSystem
$is_admin        = ([System.Security.Principal.WindowsPrincipal][System.Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([System.Security.Principal.WindowsBuiltInRole]"Administrator")
$is_server       = $os.Caption -like "*Server*"
$is_supportedwin = $is_server -or $os.Version -like "10.*"
$is_home         = $os.Caption -like "*Home*"
$is_srcexist     = Test-Path "$ScriptPath\$source_dir"
$is_fesrcexist   = Test-Path "$ScriptPath\$frontend_src_dir"
$is_rawebexeist  = $null -ne (Get-RaWebExePath "$ScriptPath\$source_dir")

$serverFeatures = @("Web-Server","Web-Windows-Auth","Web-Mgmt-Console","Web-Basic-Auth","Web-WebSockets")
$clientFeatures = @("IIS-WebServerRole","IIS-WebServer","IIS-CommonHttpFeatures","IIS-ApplicationDevelopment","IIS-Security","IIS-RequestFiltering","IIS-ISAPIExtensions","IIS-ISAPIFilter","IIS-WebServerManagementTools","IIS-ManagementConsole","IIS-WindowsAuthentication","IIS-BasicAuthentication","IIS-WebSockets")

if ($is_server) {
    $is_iisinstalled         = (Get-WindowsFeature -Name "Web-Server").Installed
    $is_iisfeaturesinstalled = $is_iisinstalled -and (Get-WindowsFeature -Name $serverFeatures | Where-Object { -not $_.Installed }).Count -eq 0
} else {
    $dismOutput              = & dism.exe /online /get-features /format:table /english 2>$null
    $is_iisinstalled         = [bool]($dismOutput -match "^IIS-WebServerRole\s*\|[^|]*Enabled")
    $is_iisfeaturesinstalled = $is_iisinstalled
    if ($is_iisinstalled) {
        foreach ($iisFeature in $clientFeatures) {
            if (-not ($dismOutput -match "^$([regex]::Escape($iisFeature))\s*\|[^|]*Enabled")) {
                $is_iisfeaturesinstalled = $false
                break
            }
        }
    }
}

# Check installed Hosting Bundle versions in the registry,
# which contains the required ASP.NET Core Module (ANCM)
$minAncmVersion = [System.Version]"10.0.0"
$ancmInstalledVersion = $null
foreach ($regPath in @("HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall")) {
    if (-not (Test-Path $regPath)) { continue }
    $match = Get-ChildItem $regPath -ErrorAction SilentlyContinue |
        ForEach-Object { Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue } |
        Where-Object { $_.DisplayName -like "*Hosting Bundle*" -and $_.DisplayVersion -like "10.*" } |
        Sort-Object { [System.Version]($_.DisplayVersion) } -Descending |
        Select-Object -First 1
    if ($match -and $match.DisplayVersion) {
        $ancmInstalledVersion = [System.Version]$match.DisplayVersion;
        break
    }
}
$ancmInstalled = $ancmInstalledVersion -ge $minAncmVersion

# Clear the console for the main installation prompts (but keep debug info if in debug mode)
Clear-ScreenPreserveHistory

# ── Welcome ───────────────────────────────────────────────────────────────────

Write-Host ""

if ($DebugPreference -eq "Inquire") {
    $DebugPreference = "Continue"
    Write-Debug "OS                       : $($os.Caption)"
    Write-Debug "Is admin                 : $is_admin"
    Write-Debug "Is server                : $is_server"
    Write-Debug "Supported Win            : $is_supportedwin"
    Write-Debug "Is home                  : $is_home"
    Write-Debug "Source exists            : $is_srcexist"
    Write-Debug "Frontend src             : $is_fesrcexist"
    Write-Debug "IIS installed            : $is_iisinstalled"
    Write-Debug "WSL2 installed           : $(Find-Wsl2)"
    Write-Debug "Other features installed : $($is_iisfeaturesinstalled -and $is_iisinstalled)"
    Write-Debug "PowerShell version       : $($PSVersionTable.PSVersion)"
    Write-Host ""
    $DebugPreference = "Inquire"
}

# ── Prerequisites ─────────────────────────────────────────────────────────────

if (-not $is_admin) {
    Write-Host "This script must be run as Administrator." -ForegroundColor Red
    Read-Host -Prompt "Press Enter to exit"
    exit 1
}

if (-not $is_supportedwin) {
    Write-Host "WARNING: RAWeb is intended for Windows 10/11 and Windows Server."
    Write-Host ""
    if (-not $Express) {
        if (-not (Read-YesNo "Continue anyway?" $false)) { Write-Host "Exiting."; exit 1 }
        Write-Host ""
    }
}

if ($is_home) {
    Write-Host "RAWeb is not supported on Windows Home editions." -ForegroundColor Red
    Write-Host "Home editions do not support hosting Remote Desktop connections."
    Write-Host ""
    exit 1
}

if (-not $is_srcexist) {
    Write-Host "Source directory not found: $ScriptPath\$source_dir" -ForegroundColor Red
    Write-Host "Run install.ps1 from the RAWeb release directory."
    Write-Host ""
    exit 1
}

if (-not $is_fesrcexist -and -not $is_rawebexeist) {
    Write-Host "Frontend source not found and not pre-built." -ForegroundColor Red
    Write-Host "Expected: $ScriptPath\$frontend_src_dir"
    Write-Host ""
    exit 1
}

if (-not (Find-Wsl2)) {
    Write-Host "WARNING: WSL2 is not installed. The web client will be unavailable."
    Write-Host "         See: https://raweb.app/docs/wsl2-install"
    Write-Host ""
    if (-not $Express) {
        if (-not (Read-YesNo "Continue anyway?" $true)) { Write-Host "Exiting."; exit 1 }
        Write-Host ""
    }
}

# ── IIS module + early installation detection ─────────────────────────────────

if ($is_iisinstalled) { Import-Module WebAdministration -Force -ErrorAction SilentlyContinue }

# Detect any existing installation at the express defaults now, before showing the mode
# selection box, so the displayed directory reflects the actual upgrade target.
$expressWebSite = if ($WebSite)     { $WebSite }     else { $DEFAULT_SITE_NAME }
$expressVirtualPath = if ($VirtualPath) { $VirtualPath } else { $DEFAULT_VIRTUAL_PATH }
$expressInstallDir = if ($InstallDir)  { $InstallDir }  else { Get-ResolvedInstallDir $expressWebSite $expressVirtualPath }

# ── Mode selection ────────────────────────────────────────────────────────────

$expressMode   = $Express.IsPresent
$anyCustomFlag = (-not [string]::IsNullOrEmpty($InstallDir)) -or `
                 (-not [string]::IsNullOrEmpty($WebSite)) -or `
                 (-not [string]::IsNullOrEmpty($VirtualPath))

if (-not $expressMode -and -not $anyCustomFlag) {
    Write-Box -Title "RAWeb Installer" -Content @"

This script will enable IIS and install RAWeb on this computer.

───

Available modes:

  [1] Express  - install with defaults
        Directory : $expressInstallDir
        Web site  : $expressWebSite
        Path      : /$expressVirtualPath

  [2] Custom   - choose web site, path, and directory

"@
Write-PreviousLine "v$(Get-RaWebVersion "$ScriptPath\$source_dir")" -ForegroundColor DarkGray
Write-Host ""

    $modeChoice = ""
    while ($true) {
        $modeChoice = Read-Host "Select an installation mode (1 or 2, default: 1)"
        if ([string]::IsNullOrEmpty($modeChoice) -or $modeChoice -eq "1" -or $modeChoice -eq "2") { break }
        Write-Host "  Invalid input. Please enter 1 or 2." -ForegroundColor Yellow
    }
    $expressMode = ($modeChoice -ne "2")
    Write-PreviousLine "Installation mode: $(if ($expressMode) { "Express" } else { "Custom" })"
} else {
    Write-Box -Title "RAWeb Installer" -Content @"

This script will enable IIS and install RAWeb on this computer.

"@
Write-PreviousLine "RAWeb Version: $(Get-RaWebVersion "$ScriptPath\$source_dir")" -ForegroundColor DarkGray
Write-Host ""
}

# ── Gather configuration ──────────────────────────────────────────────────────

if ($expressMode) {
    if ([string]::IsNullOrEmpty($WebSite))     { $WebSite     = $expressWebSite }
    if ([string]::IsNullOrEmpty($VirtualPath)) { $VirtualPath = $expressVirtualPath }
    if ([string]::IsNullOrEmpty($InstallDir))  { $InstallDir  = $expressInstallDir }
} else {
    # WebSite
    if ($is_iisinstalled) {
        $sites = @(Get-Website | Select-Object -ExpandProperty Name)
    } else {
        $sites = @()
    }

    Write-Divider
    if ([string]::IsNullOrEmpty($WebSite)) {
        # If IIS is not installed, there are no available websites.
        # In this case, we need to force Default Web Site
        if ($sites.Count -eq 0) {
            Write-Host "IIS is not yet installed; the Default Web Site will be used after installation."
            $WebSite = $DEFAULT_SITE_NAME
        }
        
        # If there is only one website, auto-select it
        elseif ($sites.Count -eq 1) {
            $WebSite = $sites[0]
            Write-Host "Web site: $WebSite"
        }
        
        # If there are multiple websites, prompt the user to select one
        else {
            Write-Host "Available IIS web sites:"
            for ($i = 0; $i -lt $sites.Count; $i++) {
                $marker = if ($sites[$i] -eq $DEFAULT_SITE_NAME) { " (default)" } else { "" }
                Write-Host "  [$($i+1)] $($sites[$i])$marker"
            }
            Write-Host ""
            $selectedSiteIndex = 1
            while ($true) {
                $siteSelectionInput = Read-Host "Select site number (default: 1)"
                if ([string]::IsNullOrEmpty($siteSelectionInput)) { break }
                $parsedSiteIndex = 0
                if ([int]::TryParse($siteSelectionInput, [ref]$parsedSiteIndex) -and $parsedSiteIndex -ge 1 -and $parsedSiteIndex -le $sites.Count) {
                    $selectedSiteIndex = $parsedSiteIndex; break
                }
                Write-Host "  Invalid input. Please enter a number between 1 and $($sites.Count)." -ForegroundColor Yellow
            }
            $WebSite = $sites[$selectedSiteIndex - 1]

            # Replace the prompted number with the selected site name for clarity in the history
            Write-PreviousLine "Web site: $WebSite"
        }
    }
    
    # If the specified website (from the -WebSite parameter) does not exist, show an error with the available sites
    elseif ($sites.Count -gt 0 -and -not ($sites -contains $WebSite)) {
        Write-Host "Error: Web site '$WebSite' does not exist in IIS." -ForegroundColor Red
        Write-Host "Available sites: $($sites -join ', ')" -ForegroundColor Yellow
        exit 1
    }

    # If there are no found sites but the user specified one via -WebSite,
    # show an error that no sites were found and the specified one cannot be used
    elseif ($sites.Count -eq 0) {
        Write-Host "Error: No IIS web sites found. Cannot use specified web site '$WebSite'." -ForegroundColor Red
        Write-Host "IIS must be installed and a web site must be created before using the -WebSite parameter." -ForegroundColor Yellow
        exit 1
    }

    else {
        Write-Host "Web site: $WebSite"
    }

    # VirtualPath
    Write-Divider
    $VirtualPath = $VirtualPath.Trim('/\')
    $VirtualPath = Set-TextValueFromPrompt $VirtualPath "Virtual path within the web site (e.g., RAWeb, tools/apps)" "Virtual path" $DEFAULT_VIRTUAL_PATH

    # Detect any existing RAWeb installation at this site/path combination
    $existingPhysicalPath   = if ($is_iisinstalled) { Get-IisAppPhysicalPath $WebSite $VirtualPath } else { $null }
    $existingInstallBaseDir = Get-ResolvedInstallDir $WebSite $VirtualPath
    $existingAnonAuthMode   = $null
    if ($existingPhysicalPath) {
        $existingAppSettingsPath = Join-Path $existingPhysicalPath "App_Data\appSettings.config"
        if (Test-Path $existingAppSettingsPath) {
            try {
                $existingSettingsXml = [xml](Get-Content $existingAppSettingsPath)
                $existingAuthNode    = $existingSettingsXml.appSettings.add | Where-Object { $_.key -eq "App.Auth.Anonymous" }
                if ($existingAuthNode) { $existingAnonAuthMode = $existingAuthNode.value }
            } catch {}
        }
    }

    # InstallDir: default is the existing base dir if upgrading; otherwise, it is derived from site and path
    Write-Divider
    if ([string]::IsNullOrEmpty($InstallDir)) {
        $InstallDir = Set-TextValueFromPrompt "" "Installation directory" "Installation directory" $existingInstallBaseDir -validate {
            param($v)
            if ($v.TrimEnd('\') -like "C:\inetpub*") { "Installation inside C:\inetpub is not allowed. Choose a different directory (e.g., C:\Program Files\RAWeb)." }
        }
    } elseif ($InstallDir -eq "infer") {
        $InstallDir = Get-ResolvedInstallDir $WebSite $VirtualPath
        Write-Host "Installation directory: $InstallDir"
    }
}

$appPoolName = Get-AppPoolName $WebSite $VirtualPath
$serviceName = Get-ServiceName $WebSite $VirtualPath

# ── Anonymous auth (custom mode) ──────────────────────────────────────────────

# ── Express mode ──
if ($expressMode) {
    if ([string]::IsNullOrEmpty($AnonymousAuthMode)) {
        $AnonymousAuthMode = if ($existingAnonAuthMode) { $existingAnonAuthMode } else { "never" }
    }
}

# ── Custom mode ──
else {
    if ($existingAnonAuthMode) {
        $AnonymousAuthMode = $existingAnonAuthMode
    } elseif ([string]::IsNullOrEmpty($AnonymousAuthMode)) {
        Write-Divider
        Write-Host "Anonymous access to RAWeb:"
        Write-Host "  never  - require authentication (recommended)"
        Write-Host "  allow  - allow but do not require anonymous access"
        Write-Host "  always - always allow anonymous access (least secure)"
        Write-Host ""
        $AnonymousAuthMode = Set-TextValueFromPrompt "" "Anonymous access (never/allow/always)" "Anonymous access" "never" -validate {
            param($v)
            if ($v.Trim().ToLower() -notin @("never", "allow", "always")) { "Please enter never, allow, or always." }
        } -normalize { param($v) $v.Trim().ToLower() }
    }
}


# ── HTTPS config ──────────────────────────────────────────────────────────────

$siteHasHttps         = $false
$siteHasCert          = $false
$install_enable_https = $false
$install_create_cert  = $false

if ($is_iisinstalled) {
    $httpsBinding = Get-WebBinding -Name $WebSite -Protocol https -Port 443 -ErrorAction SilentlyContinue
    $siteHasHttps = $null -ne $httpsBinding
    if ($siteHasHttps) {
        $siteHasCert = -not [string]::IsNullOrEmpty($httpsBinding.certificateHash)
    }
}

if (-not $siteHasHttps) {
    $install_enable_https = $true
    $install_create_cert  = $true
    if (-not $expressMode -and $is_iisinstalled) {
        Write-Divider
        if (-not (Read-YesNo "HTTPS is not enabled on '$WebSite'. Enable HTTPS and create a self-signed certificate?" $true)) {
            $install_enable_https = $false
            $install_create_cert  = $false
        }
        Write-PreviousLine "Enable HTTPS and create a self-signed certificate: $(if ($install_enable_https) { "Yes" } else { "No" })"
    }
} elseif (-not $siteHasCert) {
    $install_create_cert = $true
}

# ── Existing installation detection ───────────────────────────────────────────

$existingPhysPath = $null
# When the IIS site and path already exist
$isUpgrade        = $false
# When the IIS site and path already exist but point to a different physical path
# than the installer-specified installation directory. This indicates that we need
# to check that the new physical path is empty.
$isUpgradeWithChangedPhysPath  = $false

if ($is_iisinstalled) {
    $existingPhysPath = Get-IisAppPhysicalPath $WebSite $VirtualPath
    $appSettingsPath   = if ($existingPhysPath) { Join-Path $existingPhysPath "App_Data\appSettings.config" } else { $null }
    
    # Only treat as an upgrade if the path contains App_Data\appSettings.config,
    # which the modern versions of RAWeb always have. This prevents an existing
    # folder at the same location from being mistaken as an RAWeb installation
    # and triggering upgrade warnings.
    $isUpgrade = $existingPhysPath -and (Test-Path $appSettingsPath)

    if ($isUpgrade) {
        Write-Divider
        Write-Host "WARNING: RAWeb is already installed at '$WebSite/$VirtualPath'." -ForegroundColor Yellow
        Write-Host "         Continuing will replace the existing application."
        Write-Host "         Resources, policies, and other app data will be preserved."
        Write-Host ""
        if (-not $Overwrite) {
            if (-not (Read-YesNo "Replace existing application?" $true)) { Write-Host "Exiting."; exit 1 }
            Write-PreviousLine "Replace existing application: Yes"
        }

        $resolvedCurrentInstallDir = Get-ResolvedInstallDir $WebSite $VirtualPath
        if ($InstallDir -and ($InstallDir -ne $resolvedCurrentInstallDir)) {
            $isUpgradeWithChangedPhysPath = $true
            Write-Divider
            Write-Host "WARNING: The existing application installation directory '$resolvedCurrentInstallDir' is different from the specified installation directory '$InstallDir'." -ForegroundColor Yellow
            Write-Host "         This will be treated as an upgrade with a changed physical"
            Write-Host "         path (installation directory), and the installer will"
            Write-Host "         check that the new physical path is empty to prevent"
            Write-Host "         accidental data loss."
            Write-Host ""
            if (-not (Read-YesNo "Continue with changed physical path?" $false)) { Write-Host "Exiting."; exit 1 }
            Write-PreviousLine "Continue with changed physical path: Yes"
        }
    }
}

# Warn if the chosen install directory is already serving another RAWeb IIS application
$isContinuingWithConflict = $false
if (-not $isUpgrade -and $is_iisinstalled -and (Test-Path $InstallDir)) {
    $normalizedDir   = $InstallDir.TrimEnd('\/')
    $conflictingApps = @()
    foreach ($site in @(Get-Website -ErrorAction SilentlyContinue)) {
        foreach ($app in @(Get-WebApplication -Site $site.Name -ErrorAction SilentlyContinue)) {
            $physNorm = $app.PhysicalPath.TrimEnd('\/')
            if ($physNorm -eq $normalizedDir -or $physNorm -like "$normalizedDir\*") {
                $conflictingApps += "$($site.Name)/$($app.Path.TrimStart('/'))"
            }
        }
    }
    if ($conflictingApps.Count -gt 0) {
        Write-Divider
        Write-Host "WARNING: '$InstallDir' is already in use by another RAWeb installation:" -ForegroundColor Yellow
        foreach ($conflictingApp in $conflictingApps) {
            Write-Host "           $conflictingApp" -ForegroundColor Yellow
        }
        Write-Host "         Consider choosing a different installation directory."
        Write-Host ""
        if (-not $expressMode) {
            if (-not (Read-YesNo "Continue anyway?" $false)) { Write-Host "Exiting."; exit 1 }
            Write-PreviousLine "Continue with RAWeb installation directory conflicts: Yes"
            $isContinuingWithConflict = $true
        }
    }
}

# Warn if the specified install directory is not empty (bypass this step if upgrading
# without changing the install path or conflicts were ignored)
$isUpgradeWithoutChangedPhysPath = $isUpgrade -and (-not $isUpgradeWithChangedPhysPath)
if (-not $isUpgradeWithoutChangedPhysPath -and -not $isContinuingWithConflict -and (Test-Path $InstallDir)) {
    $dirContents = Get-ChildItem -Path $InstallDir
    if ($dirContents.Count -gt 0) {
        Write-Divider
        Write-Host "WARNING: '$InstallDir' is not empty." -ForegroundColor Yellow
        Write-Host "         Existing files will be overwritten during installation."
        Write-Host "         Data may be permanently lost." -ForegroundColor Red
        Write-Host "         Your operating system may become irreversibly damaged." -ForegroundColor Red
        Write-Host ""
        if (-not $expressMode) {
            if (-not (Read-YesNo "Continue anyway?" $false)) { Write-Host "Exiting."; exit 1 }
            Write-PreviousLine "Dangerously continue with non-empty installation directory: Yes"
        }
    }
}

# Legacy inetpub migration (only for fresh installs)
$legacyPath    = "$env:SystemDrive\inetpub\RAWeb"
$hasLegacyData = (-not $isUpgrade) -and (Test-Path "$legacyPath\App_Data")

# Warn if a directory named $VirtualPath already exists under the site root —
# that physical folder would conflict with the IIS application we are about to create.
$targetSiteRoot = if ($is_iisinstalled) { (Get-Website -Name $WebSite -ErrorAction SilentlyContinue).PhysicalPath } else { $null }
if ($targetSiteRoot) {
    $targetSiteRoot   = [System.Environment]::ExpandEnvironmentVariables($targetSiteRoot)
    $siteRootConflict = Join-Path $targetSiteRoot $VirtualPath
    if (Test-Path $siteRootConflict) {
        Write-Divider
        Write-Host "WARNING: '$siteRootConflict' exists and may conflict with the installation." -ForegroundColor Yellow
        Write-Host "         Consider removing it before continuing."
    }
}

# ── Confirm ───────────────────────────────────────────────────────────────────

Write-Host ""
Write-Box -Title "Installation summary" -TitleColor "Blue" -Content @"

Install directory : $InstallDir
Web site          : $WebSite
Virtual path      : /$VirtualPath
Application pool  : $appPoolName
Management service: $serviceName
Anonymous access  : $AnonymousAuthMode
Enable HTTPS      : $(if ($siteHasHttps) { "Already enabled" } elseif ($install_enable_https) { "Yes" } else { "No" })
Create SSL cert   : $(if ($siteHasCert) { "Already exists" } elseif ($install_create_cert) { "Yes" } else { "No" })
Type              : $(if ($isUpgrade) { "Upgrade (previous: $existingPhysPath)" } else { "Fresh install" })
$(if ($hasLegacyData) { "Migrate legacy    : $legacyPath" })
"@

if (-not $expressMode) {
    if (-not (Read-YesNo "Proceed with installation?" $true)) { Write-Host "Exiting."; exit 1 }
    Write-PreviousLine "Proceed with installation: Yes"
    Write-Host ""
    Write-Divider
}

# ══════════════════════════════════════════════════════════════════════════════
# INSTALL
# ══════════════════════════════════════════════════════════════════════════════

$originalPath = Get-Location
Set-Location -Path $ScriptPath

$_installStart     = [System.DateTime]::UtcNow
$_installCompleted = $false
$_rollbackDone     = $false

try {

# [1] IIS features ────────────────────────────────────────────────────────────

Write-Host "[1/13] Installing IIS features..." -ForegroundColor Cyan
Set-TerminalProgress -State 3 -Progress 0

if ($is_iisfeaturesinstalled) {
    Write-Host "  All required IIS features are already installed; skipping."
} else {
    if ($is_iisinstalled) {
        Write-Host "  IIS is installed but some features are missing."
    }

    $restartNeeded = $false

    if ($is_server) {
        $featuresToInstall = @(Get-WindowsFeature -Name $serverFeatures | Where-Object { -not $_.Installed })
        $featureCount = $featuresToInstall.Count
        $featureIndex = 0
        foreach ($feature in $featuresToInstall) {
            $featureIndex++
            Write-Host "  Installing $($feature.Name) ($featureIndex/$featureCount)..."
            $installResult = Install-WindowsFeature -Name $feature.Name
            if ($installResult.RestartNeeded -ne "No") {
                $restartNeeded = $true
            }
        }
    } else {
        $featuresToInstall = @($clientFeatures | Where-Object { -not ($dismOutput -match "^$([regex]::Escape($_))\s*\|[^|]*Enabled") })
        $featureCount = $featuresToInstall.Count
        $featureIndex = 0
        foreach ($feature in $featuresToInstall) {
            $featureIndex++
            Write-Host "  Installing $feature ($featureIndex/$featureCount)..."
            $installResult = Enable-WindowsOptionalFeature -Online -FeatureName $feature -NoRestart
            if ($installResult.RestartNeeded) {
                $restartNeeded = $true
            }
        }
    }

    if ($restartNeeded) {
        Write-Host ""
        Write-Host "A restart is required to complete IIS installation." -ForegroundColor Yellow
        Write-Host "Re-run install.ps1 after the restart to continue."
        Read-Host "Press ENTER to restart now"
        Restart-Computer
        exit 0
    }
}


# [2] ASP.NET Core Module ─────────────────────────────────────────────────────

Write-Host "[2/13] Installing ASP.NET Core Module..." -ForegroundColor Cyan
Set-TerminalProgress -State 1 -Progress (1 / 13 * 100)

if ($ancmInstalled) {
    Write-Host "  ASP.NET Core Module $ancmInstalledVersion already installed; skipping."
} else {
    if ($ancmInstalledVersion) {
        Write-Host "  Older ASP.NET Core Hosting Bundle $ancmInstalledVersion detected. The required minimum version is $minAncmVersion."
        Write-Host "  A newer version will be installed to ensure compatibility with RAWeb."
    }

    $ancmUrl = $null
    try {
        $releaseMeta = Invoke-RestMethod -Uri "https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json" -ErrorAction Stop
        $ancmUrl     = ($releaseMeta.releases[0].'aspnetcore-runtime'.files | Where-Object { $_.name -eq "dotnet-hosting-win.exe" }).url
        $ancmLatestCompatableVersion = $releaseMeta.'latest-release'
    } catch {
        Write-Host "  WARNING: Could not fetch latest ASP.NET Core Module version from release metadata." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Installation cannot continue."
        Write-Host "Please install the .NET 10 Hosting Bundle manually and re-run this installer." -ForegroundColor Yellow
        Read-Host "Press ENTER to exit"
        exit 1
    }

    $ancmInstaller = Join-Path $env:TEMP "dotnet-hosting-$ancmLatestCompatableVersion.exe"
    Set-TerminalProgress -State 3 -Progress 0 # indeterminate
    Write-Host "  Downloading $ancmUrl..."
    $ProgressPreference = "SilentlyContinue"
    Invoke-RestMethod -Uri $ancmUrl -OutFile $ancmInstaller
    $ProgressPreference = "Continue"
    Write-Host "  Installing ASP.NET Core Module..."
    # ArgumentList: omit installing the .NET runtime since raweb.exe is self-contained and does not require the shared runtime
    $proc = Start-Process -FilePath $ancmInstaller `
        -ArgumentList "/install", "/quiet", "/norestart", "OPT_NO_RUNTIME=1", "OPT_NO_SHAREDFX=1", "OPT_NO_X86=1" `
        -Wait -PassThru
    Remove-Item $ancmInstaller -Force -ErrorAction SilentlyContinue
    if ($proc.ExitCode -notin @(0, 1641, 3010)) {
        throw "Hosting Bundle installer failed with exit code $($proc.ExitCode)."
    }
    Write-Host "  Restarting IIS to load the module..."
    & iisreset /noforce 2>&1 | Out-Null
    Write-Host "  ASP.NET Core Module installed successfully."
}

# [3] Build ───────────────────────────────────────────────────────────────────

Write-Host "[3/13] Building application..." -ForegroundColor Cyan
Set-TerminalProgress -State 1 -Progress (2 / 13 * 100)

$exe_workflow   = "$ScriptPath\$source_dir\raweb.exe"
$exe2_workflow  = "$ScriptPath\$source_dir\rawebmgmtsvc.exe"
$exe_local      = "$ScriptPath\$source_dir\dist\raweb.exe"
$exe2_local     = "$ScriptPath\$source_dir\dist\rawebmgmtsvc.exe"
$dev_marker     = "$ScriptPath\$source_dir\dist\DEVELOPMENT"
$built_workflow = (Test-Path $exe_workflow) -and (Test-Path $exe2_workflow) -and -not (Test-Path $dev_marker)
$built_local    = (Test-Path $exe_local) -and (Test-Path $exe2_local) -and -not (Test-Path $dev_marker)

if (-not $built_workflow -and -not $built_local) {
    $hasSdk10 = $false
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        $sdkList = dotnet --list-sdks 2>$null
        $hasSdk10 = $sdkList -match '^\s*10\.'
    }
    if (-not $hasSdk10) {
        Write-Host "  .NET SDK 10 not found - installing..."
        Set-TerminalProgress -State 3 -Progress 0 # indeterminate
        $dotnetScript = Join-Path $env:TEMP "dotnet-install.ps1"
        Invoke-WebRequest -Uri "https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1" -OutFile $dotnetScript
        & $dotnetScript -Version 10.0.300
    }

    # build frontend
    Set-TerminalProgress -State 3 -Progress 0 # indeterminate
    $feBuildScript = Join-Path $ScriptPath "$frontend_src_dir\build.ps1"
    if ($expressMode) { & $feBuildScript -DefaultMode 1 } else { & $feBuildScript }
    Write-Host ""

    # build backend
    $fileVer = [System.DateTime]::UtcNow.ToString("yyyy.MM.dd.HHmm")
    $cmd = "dotnet publish `"$ScriptPath\RAWeb.slnx`" --configuration Release -p:FileVersion=${fileVer}-unstable"
    Write-Host "  Running: $cmd"
    Invoke-Expression $cmd
    if ($LASTEXITCODE -ne 0) { throw "Backend build failed (exit code $LASTEXITCODE)." }
    $built_local = $true
    Write-Host ""
} else {
    Write-Host "  Pre-built binaries found; skipping build step."
}

# [4] Create versioned directory ───────────────────────────────────────────────

Write-Host "[4/13] Copying files..." -ForegroundColor Cyan
Set-TerminalProgress -State 1 -Progress (3 / 13 * 100)

$version    = Get-RaWebVersion "$ScriptPath\$source_dir"
$versionedDirName = Get-VersionedDirName $version
$versionedDir     = Join-Path $InstallDir $versionedDirName

New-Item -Path $versionedDir -ItemType Directory -Force | Out-Null
$script:_rb_versionedDir = $versionedDir
Push-Rollback {
    Write-Host "  Removing files..."
    Remove-Item $script:_rb_versionedDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-EmptyAncestorDirs (Split-Path $script:_rb_versionedDir -Parent)
}

Write-Host "  $versionedDir"

if ($built_local) {
    robocopy "$ScriptPath\$source_dir\dist" "$versionedDir" /E /COPYALL /DCOPY:T | Out-Null
} else {
    Copy-Item -Path "$ScriptPath\$source_dir\*" -Destination $versionedDir -Recurse -Force
}

# [5] Migrate App_Data ────────────────────────────────────────────────────────

Write-Host "[5/13] Migrating application data..." -ForegroundColor Cyan
Set-TerminalProgress -State 1 -Progress (4 / 13 * 100)

$appDataDest = Join-Path $versionedDir "App_Data"
if (-not (Test-Path $appDataDest)) { New-Item $appDataDest -ItemType Directory | Out-Null }

if ($isUpgrade -and (Test-Path $existingPhysPath)) {
    $prevAppData = Join-Path $existingPhysPath "App_Data"
    if (Test-Path $prevAppData) {
        Write-Host "  From previous version: $existingPhysPath"
        robocopy $prevAppData $appDataDest /E /COPYALL /DCOPY:T | Out-Null
    } else {
        Write-Host "  No App_Data found in previous version; skipping."
    }
} elseif ($hasLegacyData) {
    Write-Host "  From legacy installation: $legacyPath"
    robocopy "$legacyPath\App_Data" $appDataDest /E /COPYALL /DCOPY:T | Out-Null

    foreach ($legacySubdir in @("resources", "multiuser-resources")) {
        $legSrc = Join-Path $legacyPath $legacySubdir
        if (Test-Path $legSrc) {
            robocopy $legSrc (Join-Path $appDataDest $legacySubdir) /E /COPYALL /DCOPY:T | Out-Null
        }
    }

    $legacyWebCfg    = "$legacyPath\Web.config"
    $destAppSettings = Join-Path $appDataDest "appSettings.config"
    if ((Test-Path $legacyWebCfg) -and -not (Test-Path $destAppSettings)) {
        try {
            $cfg = [xml](Get-Content $legacyWebCfg)
            if ($cfg.configuration.appSettings -and $cfg.configuration.appSettings.ChildNodes.Count -gt 0) {
                $appSettingsText = "<?xml version=`"1.0`"?>`n" + $cfg.configuration.appSettings.OuterXml
                Set-Content -Path $destAppSettings -Value $appSettingsText -Encoding UTF8
            }
        } catch {
            Write-Warning "Could not extract appSettings from legacy Web.config: $($_.Exception.Message)"
        }
    }
} else {
    Write-Host "  No previous data to migrate."
}

# [6] Stop existing services ───────────────────────────────────────────────────

Write-Host "[6/13] Stopping existing services..." -ForegroundColor Cyan
Set-TerminalProgress -State 1 -Progress (5 / 13 * 100)

try { Stop-WebAppPool -Name $appPoolName -ErrorAction Stop } catch {}

if ($isUpgrade) {
    Stop-ServiceSafe $serviceName
    Stop-ServiceSafe "RAWebManagementService"
    Unregister-ServiceSafe "RAWebManagementService"

    # Capture the existing app pool name before changing it so rollback can restore it.
    $existingPoolOnApp = ""
    try {
        $existingPoolOnApp = (Get-ItemProperty "IIS:\Sites\$WebSite\$VirtualPath" -Name applicationPool).Value
    } catch {}

    $script:_rb_prevVerDir   = $existingPhysPath
    $script:_rb_serviceName  = $serviceName
    $script:_rb_installDir   = $InstallDir
    $script:_rb_webSite      = $WebSite
    $script:_rb_virtualPath  = $VirtualPath
    $script:_rb_originalPool = $existingPoolOnApp
    $script:_rb_svcDispName  = (Get-DisplayName $WebSite $VirtualPath) -replace '^RAWeb', 'RAWeb Management'

    Push-Rollback {
        Write-Host "  Reverting IIS physical path and app pool to previous version..."
        $rbIisPath = "IIS:\Sites\$($script:_rb_webSite)\$($script:_rb_virtualPath)"
        Set-ItemProperty $rbIisPath -Name physicalPath -Value $script:_rb_prevVerDir -ErrorAction SilentlyContinue
        if (-not [string]::IsNullOrEmpty($script:_rb_originalPool)) {
            Set-ItemProperty $rbIisPath -Name applicationPool -Value $script:_rb_originalPool -ErrorAction SilentlyContinue
        }
        if ($null -ne $script:_rb_prevVerDir -and (Test-Path $script:_rb_prevVerDir)) {
            Write-Host "  Re-registering previous management service..."
            Unregister-ServiceSafe $script:_rb_serviceName
            $oldExe = Join-Path $script:_rb_prevVerDir "bin\RAWeb.Server.Management.ServiceHost.exe"
            Register-MgmtService $oldExe $script:_rb_serviceName $script:_rb_installDir $script:_rb_originalPool $script:_rb_svcDispName
            Start-Service -Name $script:_rb_serviceName -ErrorAction SilentlyContinue
        }
        Write-Host "  Removing new versioned directory..."
        Remove-Item $script:_rb_versionedDir -Recurse -Force -ErrorAction SilentlyContinue
        Remove-EmptyAncestorDirs (Split-Path $script:_rb_versionedDir -Parent)
    }
}

# [7] Application pool ────────────────────────────────────────────────────────

Write-Host "[7/13] Configuring application pool..." -ForegroundColor Cyan
Set-TerminalProgress -State 1 -Progress (6 / 13 * 100)

$poolExists = Test-Path "IIS:\AppPools\$appPoolName"

if (-not $poolExists) {
    New-WebAppPool -Name $appPoolName -Force | Out-Null
    $script:_rb_appPoolName = $appPoolName
    Push-Rollback {
        Write-Host "  Removing application pool..."
        Remove-WebAppPool -Name $script:_rb_appPoolName -ErrorAction SilentlyContinue
    }
    Write-Host "  Created: $appPoolName"
} else {
    Write-Host "  Using existing: $appPoolName"
}

Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name processModel.identityType   -Value ApplicationPoolIdentity
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name processModel.loadUserProfile -Value $true

# [8] File system permissions ──────────────────────────────────────────────────

Write-Host "[8/13] Configuring permissions..." -ForegroundColor Cyan
Set-TerminalProgress -State 1 -Progress (7 / 13 * 100)

# disable permissions inheritance on the RAWeb directory
$rawebAcl = Get-Acl $versionedDir
$rawebAcl.SetAccessRuleProtection($true, $false)

# allow full control for SYSTEM and Administrators
$systemSid = New-Object System.Security.Principal.SecurityIdentifier("S-1-5-18")
$localAdminSid = New-Object System.Security.Principal.SecurityIdentifier("S-1-5-32-544")
$systemAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($systemSid, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$localAdminAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($localAdminSid, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$rawebAcl.SetAccessRule($systemAccessRule)
$rawebAcl.SetAccessRule($localAdminAccessRule)

# grant read access to the RAWeb application pool identity
$appPoolIdentity = "IIS AppPool\$appPoolName"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($appPoolIdentity, "Read", "ContainerInherit,ObjectInherit", "None", "Allow")
$rawebAcl.SetAccessRule($accessRule)

Set-Acl -Path $versionedDir -AclObject $rawebAcl

# additionally grant write access to the App_Data folder, which is required for the policies web editor
$appDataAcl = Get-Acl $appDataDest
$appDataAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($appPoolIdentity, "Write, Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
$appDataAcl.SetAccessRule($appDataAccessRule)
Set-Acl -Path $appDataDest -AclObject $appDataAcl

# allow read access for the Users group for App_Data\resources since all users should have access to the resources by default
$resourcesPath = Join-Path -Path $appDataDest -ChildPath "resources"
if (-not (Test-Path $resourcesPath)) { 
    New-Item -Path $resourcesPath -ItemType Directory | Out-Null
}
$resourcesAcl = Get-Acl $resourcesPath
$usersSid = New-Object System.Security.Principal.SecurityIdentifier("S-1-5-32-545")
$usersAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($usersSid, "Read", "ContainerInherit,ObjectInherit", "None", "Allow")
$resourcesAcl.SetAccessRule($usersAccessRule)
Set-Acl -Path $resourcesPath -AclObject $resourcesAcl

# allow read and execute access to raweb.exe for the RAWeb application pool identity
$exePath = Join-Path $versionedDir "raweb.exe"
$exeAcl = Get-Acl $exePath
$exeAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($appPoolIdentity, "ReadAndExecute", "None", "None", "Allow")
$exeAcl.SetAccessRule($exeAccessRule)
Set-Acl -Path $exePath -AclObject $exeAcl

# only allow the application pool identity and Administrators to read and modify the the DataProtection-Keys folder inside App_Data
$dataProtectionKeysPath = Join-Path $appDataDest "DataProtection-Keys"
if (-not (Test-Path $dataProtectionKeysPath)) { 
    New-Item -Path $dataProtectionKeysPath -ItemType Directory | Out-Null
}
$dataProtectionKeysAcl = Get-Acl $dataProtectionKeysPath
$dataProtectionKeysAcl.SetAccessRuleProtection($true, $false) # disable inheritance and remove inherited permissions
$dataProtectionKeysAcl.SetAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule($appPoolIdentity, "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")))
$dataProtectionKeysAcl.SetAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule($localAdminSid, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow"))) 
Set-Acl -Path $dataProtectionKeysPath -AclObject $dataProtectionKeysAcl
# apply the ACL to existing files in the DataProtection-Keys folder (if any exist)
Get-ChildItem -Path $dataProtectionKeysPath -Recurse | ForEach-Object {
    Set-Acl -Path $_.FullName -AclObject $dataProtectionKeysAcl
}

# [9] IIS application and authentication ──────────────────────────────────────

Write-Host "[9/13] Configuring IIS application..." -ForegroundColor Cyan
Set-TerminalProgress -State 1 -Progress (8 / 13 * 100)

Set-WebConfigurationProperty -Filter "system.webServer/webSocket" -Name "enabled" -Value "true" -PSPath "IIS:\Sites\$WebSite" -ErrorAction SilentlyContinue

$iisAppPath = "IIS:\Sites\$WebSite\$VirtualPath"

if ($isUpgrade) {
    Set-ItemProperty $iisAppPath -Name physicalPath    -Value $versionedDir
    Set-ItemProperty $iisAppPath -Name applicationPool -Value $appPoolName
    Write-Host "  Updated physical path -> $versionedDir"
} else {
    New-WebApplication -Site $WebSite -Name $VirtualPath -PhysicalPath $versionedDir -ApplicationPool $appPoolName | Out-Null
    Write-Host "  Created application '$WebSite/$VirtualPath' -> $versionedDir"

    $script:_rb_iisWebSite = $WebSite
    $script:_rb_iisVirtualPath = $VirtualPath
    Push-Rollback {
        Write-Host "  Removing IIS application..."
        Remove-WebApplication -Site $script:_rb_iisWebSite -Name $script:_rb_iisVirtualPath -ErrorAction SilentlyContinue
        try { Remove-Item "IIS:\Sites\$($script:_rb_iisWebSite)\$($script:_rb_iisVirtualPath)" -Recurse -Force -ErrorAction Stop } catch {}
    }
}

$appLocation = "$WebSite/$VirtualPath"

Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -Location $appLocation -Name "enabled" -Value "True" -ErrorAction SilentlyContinue
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -Location $appLocation -Name "userName" -Value "" -ErrorAction SilentlyContinue
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -Location "$appLocation/auth" -Name "enabled" -Value "True" -ErrorAction SilentlyContinue
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -Location "$appLocation/auth" -Name "userName" -Value "" -ErrorAction SilentlyContinue
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" -Location $appLocation -Name "enabled" -Value "True" -ErrorAction SilentlyContinue
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" -Location "$appLocation/auth" -Name "enabled" -Value "True" -ErrorAction SilentlyContinue

$appSettingsPath = Join-Path $versionedDir "App_Data\appSettings.config"
if (Test-Path $appSettingsPath) {
    $settingsXml = [xml](Get-Content $appSettingsPath)
} else {
    $settingsXml = New-Object System.Xml.XmlDocument
    $settingsXml.LoadXml('<?xml version="1.0"?><appSettings></appSettings>')
}
$appSettingsNode = $settingsXml.SelectSingleNode("//appSettings")
$authNode = $appSettingsNode.SelectSingleNode("add[@key='App.Auth.Anonymous']")
if ($authNode) {
    $authNode.value = $AnonymousAuthMode
} else {
    $newNode = $settingsXml.CreateElement("add")
    $newNode.SetAttribute("key",   "App.Auth.Anonymous")
    $newNode.SetAttribute("value", $AnonymousAuthMode)
    $appSettingsNode.AppendChild($newNode) | Out-Null
}
$settingsXml.Save($appSettingsPath)

# [10] Starting services ───────────────────────────────────────────────────────

Write-Host "[10/13] Starting services..." -ForegroundColor Cyan
Set-TerminalProgress -State 1 -Progress (9 / 13 * 100)

Unregister-ServiceSafe "RAWebManagementService"
Unregister-ServiceSafe $serviceName

$svcExe = Join-Path $versionedDir "rawebmgmtsvc.exe"
$svcDisplayName = (Get-DisplayName $WebSite $VirtualPath) -replace '^RAWeb', 'RAWeb Management'
Register-MgmtService $svcExe $serviceName $InstallDir $appPoolName $svcDisplayName

$script:_rb_newServiceName = $serviceName
Push-Rollback {
    Write-Host "  Stopping and removing management service..."
    Stop-ServiceSafe       $script:_rb_newServiceName
    Unregister-ServiceSafe $script:_rb_newServiceName
}

$svc = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($svc) {
    Write-Host "  Starting management service ($serviceName)..."
    Start-Service -Name $serviceName
} else {
    Write-Warning "Service '$serviceName' was not found after registration."
}

Start-WebAppPool -Name $appPoolName

if ($install_enable_https) {
    Write-Host "  Enabling HTTPS on '$WebSite'..."
    New-WebBinding -Name $WebSite -Protocol https -Port 443 | Out-Null
}

if ($install_create_cert) {
    Write-Host "  Creating self-signed SSL certificate..."
    $dnsDomain = if ([string]::IsNullOrWhiteSpace($env:USERDNSDOMAIN)) { "local" } else { $env:USERDNSDOMAIN }
    $certDnsNames = @(
        $env:COMPUTERNAME,
        $env:COMPUTERNAME.ToLower(),
        "$($env:COMPUTERNAME).$dnsDomain",
        "$($env:COMPUTERNAME).$dnsDomain".ToLower(),
        "localhost",
        "127.0.0.1"
    )
    $cert = New-SelfSignedCertificate `
        -DnsName $certDnsNames `
        -CertStoreLocation "Cert:\LocalMachine\My" `
        -FriendlyName "RAWeb Self-Signed Certificate"
    (Get-WebBinding -Name $WebSite -Port 443 -Protocol "https").AddSslCertificate($cert.Thumbprint, "my") | Out-Null
}

# [11] Verifying installation ─────────────────────────────────────────────────

Write-Host "[11/13] Verifying installation..." -ForegroundColor Cyan
Set-TerminalProgress -State 1 -Progress (10 / 13 * 100)

$useHttps = $siteHasHttps -or $install_enable_https
$urlProtocol = if ($useHttps) { "https" } else { "http" }

$_httpsBinding = Get-WebBinding -Name $WebSite -Protocol https -ErrorAction SilentlyContinue | Select-Object -First 1
$_activeBinding = if ($useHttps -and $_httpsBinding) { $_httpsBinding } else { Get-WebBinding -Name $WebSite -Protocol http -ErrorAction SilentlyContinue | Select-Object -First 1 }
$_activePort = if ($_activeBinding) { $_activeBinding.bindingInformation.Split(':')[1] } else { if ($useHttps) { '443' } else { '80' } }
$_defaultPort = if ($useHttps) { '443' } else { '80' }
$_portSuffix = if ($_activePort -ne $_defaultPort) { ":$_activePort" } else { '' }

$baseUrl = "${urlProtocol}://localhost${_portSuffix}/$VirtualPath/api/app-init-details"

if (-not (Invoke-HealthCheck $baseUrl)) {
    throw "Health check failed. The application did not start correctly."
}

# [12] Cleaning up ────────────────────────────────────────────────────────────

Write-Host "[12/13] Cleaning up..." -ForegroundColor Cyan
Set-TerminalProgress -State 1 -Progress (11 / 13 * 100)

if ($isUpgrade -and $null -ne $existingPhysPath -and (Test-Path $existingPhysPath)) {
    Write-Host "  Recycling app pool to release file locks..."
    try {
        Restart-WebAppPool -Name $appPoolName -ErrorAction Stop
        Start-Sleep -Seconds 2
    } catch {
        try { Stop-WebAppPool  -Name $appPoolName -ErrorAction Stop } catch {}
        Start-Sleep -Seconds 3
        try { Start-WebAppPool -Name $appPoolName -ErrorAction Stop } catch {}
    }

    # Remove the old version directory.
    $oldBaseDir = Split-Path $existingPhysPath -Parent
    Write-Host "  Removing previous version: $existingPhysPath"
    Remove-Item $existingPhysPath -Recurse -Force -ErrorAction SilentlyContinue
    # Since the uninstall script is being replaced later in this script, we can remove it immediately with no negative consequences.
    Remove-Item (Join-Path $oldBaseDir "uninstall.ps1") -Force -ErrorAction SilentlyContinue
    # When the $oldBaseDir is the same as the new installation path's parent, this does nothing.
    # When it is different, it ensures that we do not leave behind empty directories.
    Remove-EmptyAncestorDirs $oldBaseDir
}

if ($hasLegacyData) {
    Write-Host "  Removing legacy installation: $legacyPath"
    Unregister-ServiceSafe "RAWebManagementService"
    $legApp = Get-WebApplication -Site $DEFAULT_SITE_NAME -Name "RAWeb" -ErrorAction SilentlyContinue
    if ($null -ne $legApp -and $legApp.PhysicalPath -like "$legacyPath*") {
        Remove-WebApplication -Site $DEFAULT_SITE_NAME -Name "RAWeb" -ErrorAction SilentlyContinue
    }
    Remove-Item $legacyPath -Recurse -Force -ErrorAction SilentlyContinue
}

# [13] Registering installation ──────────────────────────────────────────────

Write-Host "[13/13] Registering installation..." -ForegroundColor Cyan
Set-TerminalProgress -State 1 -Progress (12 / 13 * 100)

$displayName   = Get-DisplayName $WebSite $VirtualPath
$regKeyName    = Get-UninstallRegKeyName $WebSite $VirtualPath
$uninstallPath = Join-Path $InstallDir "uninstall.ps1"

$_siteBinding = Get-WebBinding -Name $WebSite -Protocol http -ErrorAction SilentlyContinue | Select-Object -First 1
$_sitePort    = if ($_siteBinding) { $_siteBinding.bindingInformation.Split(':')[1] } else { '80' }

$uninstallContent = @"
# RAWeb Uninstaller
# Generated  : $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
# Uninstalls : $displayName
#   Install directory : $InstallDir
#   Web site          : $WebSite
#   Virtual path      : $VirtualPath
#   Application pool  : $appPoolName
#   Service name      : $serviceName
#   Registry key      : $regKeyName

[CmdletBinding()]
Param([switch]`$FromTemp)

`$is_admin = ([System.Security.Principal.WindowsPrincipal][System.Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([System.Security.Principal.WindowsBuiltInRole]"Administrator")
if (-not `$is_admin) {
    Write-Host "This script must be run as Administrator." -ForegroundColor Red
    exit 1
}

if (-not `$FromTemp) {
    `$tmp = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "raweb_uninstall_${regKeyName}.ps1")
    Copy-Item `$PSCommandPath `$tmp -Force
    Start-Process powershell -Verb RunAs -ArgumentList ("-ExecutionPolicy Bypass -File ``"`$tmp``" -FromTemp")
    exit 0
}

try {
    `$host.UI.RawUI.BufferSize    = New-Object System.Management.Automation.Host.Size(80, 9999)
    `$host.UI.RawUI.WindowSize    = New-Object System.Management.Automation.Host.Size(80, 20)
    `$host.UI.RawUI.BackgroundColor = "Black"
    Clear-Host
} catch {}

`$installDir  = "$($InstallDir  -replace '"', '`"')"
`$webSite     = "$($WebSite     -replace '"', '`"')"
`$virtualPath = "$($VirtualPath -replace '"', '`"')"
`$appPoolName = "$appPoolName"
`$serviceName = "$serviceName"
`$regKeyName  = "$regKeyName"
`$displayName = "$($displayName -replace '"', '`"')"
`$regPath     = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\`$regKeyName"
`$webSitePort = "$_sitePort"

Write-Host ""
Write-Host "+++ RAWeb Uninstaller +++" -BackgroundColor Black -ForegroundColor Red
Write-Host ""
Write-Host "This will uninstall : `$displayName"
Write-Host "Install directory   : `$installDir"
Write-Host "Local URL           : http://localhost:`$webSitePort/`$virtualPath"
Write-Host ""

`$confirmed = `$false
while (-not `$confirmed) {
    `$inp = Read-Host "(y/N) Are you sure?"
    if ([string]::IsNullOrEmpty(`$inp)) { 
        Write-Host "Cancelled.";
        Write-Host ""
        Read-Host "Press Enter to close"
        exit 0
    }
    if (`$inp -eq "y") { `$confirmed = `$true }
    elseif (`$inp -eq "n") { 
        Write-Host "Cancelled.";
        Write-Host ""
        Read-Host "Press Enter to close"
        exit 0
    }
    else { Write-Host "  Invalid input. Please enter Y or N." -ForegroundColor Yellow }
}
Write-Host ""

Write-Host "Stopping management service..." -ForegroundColor Cyan
`$svc = Get-Service -Name `$serviceName -ErrorAction SilentlyContinue
if (`$svc) {
    try { Stop-Service -Name `$serviceName -Force -ErrorAction Stop } catch {}
    `$proc = Get-Process -Name "RAWeb.Server.Management.ServiceHost" -ErrorAction SilentlyContinue
    if (`$proc) { `$proc | Stop-Process -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Seconds 1
    sc.exe delete `$serviceName | Out-Null
}

Write-Host "Stopping application pool..." -ForegroundColor Cyan
try { Stop-WebAppPool -Name `$appPoolName -ErrorAction SilentlyContinue } catch {}
`$poolTimeout = 15
`$poolElapsed = 0
while (`$poolElapsed -lt `$poolTimeout) {
    `$poolState = (Get-WebAppPoolState -Name `$appPoolName -ErrorAction SilentlyContinue).Value
    if (`$poolState -eq "Stopped" -or `$null -eq `$poolState) { break }
    Start-Sleep -Seconds 1
    `$poolElapsed++
}

Write-Host "Removing IIS application..." -ForegroundColor Cyan
Remove-WebApplication -Site `$webSite -Name `$virtualPath -ErrorAction SilentlyContinue
try { Remove-Item "IIS:\Sites\`$webSite\`$virtualPath" -Recurse -Force -ErrorAction Stop } catch {}

Write-Host "Removing application pool..." -ForegroundColor Cyan
Remove-WebAppPool -Name `$appPoolName -ErrorAction SilentlyContinue

Write-Host "Removing Add/Remove Programs entry..." -ForegroundColor Cyan
Remove-Item -Path `$regPath -Force -ErrorAction SilentlyContinue

Write-Host "Removing install directory: `$installDir" -ForegroundColor Cyan
`$skippedFiles = @()
if (Test-Path `$installDir) {
    # Delete files individually so a single locked file doesn't block the rest
    Get-ChildItem `$installDir -Recurse -Force -File | Sort-Object FullName -Descending | ForEach-Object {
        try { Remove-Item `$_.FullName -Force -ErrorAction Stop }
        catch { `$skippedFiles += `$_.FullName }
    }
    # Remove empty directories bottom-up
    Get-ChildItem `$installDir -Recurse -Force -Directory | Sort-Object FullName -Descending | ForEach-Object {
        try { Remove-Item `$_.FullName -Force -ErrorAction Stop } catch {}
    }
    try { Remove-Item `$installDir -Force -ErrorAction Stop } catch {
        # Fallback: use .NET directly
        try { [System.IO.Directory]::Delete(`$installDir, `$false) }
        catch { Write-Warning "Could not remove directory: `$_" }
    }
}
`$removedDir = -not (Test-Path `$installDir)
if (`$skippedFiles.Count -gt 0) {
    Write-Warning "The following files could not be removed (still in use):"
    `$skippedFiles | ForEach-Object { Write-Host "  `$_" -ForegroundColor Yellow }
}

if (`$FromTemp) {
    Start-Sleep -Seconds 2
    Remove-Item `$PSCommandPath -Force -ErrorAction SilentlyContinue
}

Write-Host ""
if (`$removedDir) {
    Write-Host "RAWeb uninstalled successfully." -ForegroundColor Green
} else {
    Write-Host "RAWeb partially uninstalled. The install directory could not be removed." -ForegroundColor Yellow
    Write-Host "IIS application, app pool, service, and registry entry have been removed." -ForegroundColor Yellow
}
Write-Host ""
Read-Host "Press Enter to close"
"@

Set-Content -Path $uninstallPath -Value $uninstallContent -Encoding UTF8

Write-Host "  Registering Add/Remove Programs entry..."

$regRoot = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$regKeyName"
if (-not (Test-Path $regRoot)) { New-Item -Path $regRoot -Force | Out-Null }

$sizeBytes = (Get-ChildItem $versionedDir -Recurse -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -notlike "$versionedDir\App_Data*" } |
                Measure-Object Length -Sum).Sum
$installSizeKb = [int][System.Math]::Round($sizeBytes / 1024)

Set-ItemProperty $regRoot -Name "DisplayName"    -Value $displayName
Set-ItemProperty $regRoot -Name "DisplayVersion"  -Value $version
Set-ItemProperty $regRoot -Name "Publisher"       -Value "RAWeb"
Set-ItemProperty $regRoot -Name "DisplayIcon"     -Value "$(Get-RaWebExePath "$ScriptPath\$source_dir"),0"
Set-ItemProperty $regRoot -Name "InstallDate"     -Value (Get-Date -Format "yyyyMMdd")
Set-ItemProperty $regRoot -Name "InstallLocation" -Value $InstallDir
Set-ItemProperty $regRoot -Name "UninstallString" -Value "powershell -ExecutionPolicy Bypass -File `"$uninstallPath`""
Set-ItemProperty $regRoot -Name "NoModify"        -Value 1 -Type DWord
Set-ItemProperty $regRoot -Name "NoRepair"        -Value 1 -Type DWord
Set-ItemProperty $regRoot -Name "EstimatedSize"   -Value $installSizeKb -Type DWord

$script:_rb_regRoot = $regRoot
Push-Rollback {
    Write-Host "  Removing Add/Remove Programs entry..."
    Remove-Item -Path $script:_rb_regRoot -Force -ErrorAction SilentlyContinue
}

# ── Done ──────────────────────────────────────────────────────────────────────

$_elapsed  = [System.DateTime]::UtcNow - $_installStart
$_duration = if ($_elapsed.TotalMinutes -ge 1) { "$([int]$_elapsed.TotalMinutes)m $($_elapsed.Seconds)s" } else { "$($_elapsed.Seconds)s" }

$_doneLines = @(
    "Installed     : $displayName  v$version",
    "Directory     : $versionedDir",
    "───",
    "Web interface : ${urlProtocol}://$env:COMPUTERNAME${_portSuffix}/$VirtualPath",
    "Workspace URL : ${urlProtocol}://$env:COMPUTERNAME${_portSuffix}/$VirtualPath/webfeed.aspx",
    "───",
    "Uninstall     : $uninstallPath"
)
if (-not $useHttps) {
    $_doneLines += "───"
    $_doneLines += "NOTE: HTTPS is not enabled. The webfeed feature requires HTTPS."
}
if (-not (Find-Wsl2)) {
    $_doneLines += "───"
    $_doneLines += "NOTE: WSL2 is not installed. The web client will be unavailable."
    $_doneLines += "      See: https://raweb.app/docs/wsl2-install"
}

Write-Host ""
Write-Divider
Write-Host ""
Write-Host "RAWeb installation " -NoNewline
Write-Host "succeeded" -NoNewline -ForegroundColor Green
Write-Host " in $($_duration)."
Write-Box -Content ($_doneLines -join "`n") -Title ""
$_installCompleted = $true
Set-TerminalProgress -State 0

} catch {
    Write-Host ""
    Write-Host "--------------------------------------------" -ForegroundColor Red
    Write-Host "INSTALLATION FAILED" -ForegroundColor Red
    Write-Host ""
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "At line $($_.InvocationInfo.ScriptLineNumber) in $($_.InvocationInfo.ScriptName)"
    Write-Host "--------------------------------------------" -ForegroundColor Red

    try { Set-TerminalProgress -State 2 -Progress (Get-TerminalProgress).Progress } catch {}
    Invoke-Rollback
    $_rollbackDone = $true

    exit 1
} finally {
    if (-not $_installCompleted -and -not $_rollbackDone) {
        Write-Host ""
        Write-Host "Installation interrupted. Rolling back..." -ForegroundColor Yellow
        try { Set-TerminalProgress -State 4 -Progress (Get-TerminalProgress).Progress } catch {}
        Invoke-Rollback
        Set-TerminalProgress -State 0 -Progress 0
    }
    Set-Location -Path $originalPath
    $Host.UI.RawUI.WindowTitle = $script:_originalWindowTitle
}
