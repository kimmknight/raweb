# This script sets up RAWeb on a Windows host.
#
# Note: RAWeb will be installed into the default IIS website.
# If you are already using the default website for other purposes, this script may interfere with your existing configuration.
#
# A full installation involves the following steps:
#
# - Install IIS and required components (Web-Server, Web-Asp-Net45, Web-Windows-Auth, Web-Http-Redirect, Web-Mgmt-Console)
# - Build or copy the RAWeb frontend
# - Copy the RAWeb directory to the inetpub directory
# - Create the RAWeb application
# - Enable Authentication on RAWeb\auth
# - Enable HTTPS on the Default Web Site
# - Create and install an SSL certificate#
#
# The script performs the following actions:
#
# 1. Performs checks to to see which components are already installed, and ensures the environment is suitable for installation.
# 2. Prompts the user if any input is required.
# 3. Determines which components need to be installed or configured.
# 4. Confirms the installation steps with the user.
# 5. Installs or configures the necessary components.
#
# Note:
#
# - If RAWeb and IIS are already installed, the script will prompt to perform the necessary steps to repair/configure RAWeb.
# - The script includes no uninstallation or rollback functionality.
# - The script will prompt the user for confirmation before performing certain actions, such as overwriting existing directories or enabling HTTPS.
# - If IIS installation or configuration requires a system restart, the script will prompt the user to restart the computer.
# - The script is intended for use on Windows 10/11 and Windows Server editions.
[CmdletBinding()]
Param(
    [switch]$AcceptAll
)





# VARIABLES

$sitename = "Default Web Site"
$frontend_src_dir = "frontend"
$source_dir = "aspx\wwwroot"

$ScriptPath = Split-Path -Path $MyInvocation.MyCommand.Path

$is_admin = $null
$is_server = $null
$is_supportedwindows = $null
$is_home = $null
$is_iisinstalled = $null
$wwwroot = $null
$rawebinwwwroot = $null
$is_rawebinstallpath_exists = $null
$is_rawebrealfolder_exists = $null
$is_application = $null
$is_application_exists = $null
$applicationpath = $null
$is_applicationrealfolder_exists = $null
$is_auth_enabled = $null
$is_httpsenabled = $null
$is_certificate = $null

$install_iis = $null
$install_copy_raweb = $null
$install_create_application = $null
$install_remove_application = $null
$install_enable_auth = $null
$install_enable_https = $null
$install_create_certificate = $null





# CHECKS

# Is this script running as administrator?

$is_admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

# Is Windows 10/11 or Server?

$os = Get-WmiObject -Class Win32_OperatingSystem
$is_server = $os.Caption -like "*Server*"
$is_supportedwindows = $is_server -or $os.Version -like "10.*"

# Is Windows HOME edition?

$is_home = $os.Caption -like "*Home*"

# Does the source directory exist?

$is_sourceexist = Test-Path $ScriptPath\$source_dir
$is_frontendsourceexist = Test-Path $ScriptPath\$frontend_src_dir

# Is IIS installed?

if ($is_server) {
    $iis = Get-WindowsFeature -Name Web-Server
    $is_iisinstalled = $iis.Installed
} else {
    $iis = Get-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
    $is_iisinstalled = $iis.State -eq "Enabled"
}

# Does the RAWeb folder already exist in inetpub (the install location)?

$inetpub = "$env:SystemDrive\inetpub"
$rawebininetpub = "$inetpub\RAWeb"
$is_rawebinstallpath_exists = Test-Path $rawebininetpub

# Does a conflicting folder called RAWeb exist in wwwroot?

$wwwroot = "$env:SystemDrive\inetpub\wwwroot"
$rawebinwwwroot = "$wwwroot\RAWeb"
$is_rawebrealfolder_exists = Test-Path $rawebinwwwroot

# Some checks can't be completed if IIS is not installed yet.
# If IIS is already installed then perform the checks.

if ($is_iisinstalled) {
    # Does RAWeb application already exist in IIS?
    
    $is_application = Get-WebApplication -Site $sitename -Name "RAWeb"
    $is_application_exists = $null -ne $is_application

    # If so, does physical directory for the application actually exist in the filesystem?

    if ($is_application_exists) {
        $applicationpath = $is_application.PhysicalPath
        $is_applicationrealfolder_exists = Test-Path $applicationpath
    }

    # Is authentication enabled?

    if ($is_application_exists) {
            $windows_auth = Get-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" -Location "$sitename/RAWeb/auth" -Name "enabled"
            $basic_auth = Get-WebConfigurationProperty -Filter "/system.webServer/security/authentication/basicAuthentication" -Location "$sitename/RAWeb/auth" -Name "enabled"
            $anonymous_auth = Get-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -Location "$sitename/RAWeb/auth" -Name "enabled"

            $is_auth_enabled = $windows_auth -eq "True" -and $basic_auth -eq "True" -and $anonymous_auth -eq "False"

            # Currently this only checks Windows, Basic, and Anonymous auth. If any other kind of auth is enabled, this will be a problem.
    }

    # Is HTTPS enabled?

    $binding = Get-WebBinding -Name $sitename -Protocol https -Port 443
    $is_httpsenabled = $null -ne $binding

    # If HTTPS is enabled, is a certificate bound to the HTTPS binding?

    if ($is_httpsenabled) {
        $cert = $binding.certificateHash
        $is_certificate = $null -ne $cert
    }
}






# WELCOME

Write-Host
Write-Host "+++ RAWeb Setup +++" -BackgroundColor Black -ForegroundColor Green
Write-Host
Write-Host "This script will enable IIS and install RAWeb on this computer."
Write-Host

if ($DebugPreference -eq "Inquire") {
    $DebugPreference = "Continue"
    Write-Debug "Debugging information:"
    Write-Host
    Write-Debug "OS: $($os.Caption)"
    Write-Debug "Is admin: $is_admin"
    Write-Debug "Is server: $is_server"
    Write-Debug "Is supported Windows: $is_supportedwindows"
    Write-Debug "Is home: $is_home"
    Write-Debug "Is IIS installed: $is_iisinstalled"
    Write-Debug "Install source directory exists: $is_sourceexist"
    Write-Debug "Frontend source directory exists: $is_sourceexist"
    Write-Debug "RAWeb install path exists: $is_rawebinstallpath_exists"
    Write-Debug "Conflicting RAWeb directory exists in wwwroot: $is_rawebrealfolder_exists"
    Write-Debug "RAWeb application exists: $is_application_exists"
    Write-Debug "RAWeb application source directory exists: $is_applicationrealfolder_exists"
    Write-Debug "Authentication enabled: $is_auth_enabled"
    Write-Debug "HTTPS enabled: $is_httpsenabled"
    Write-Debug "Certificate bound to HTTPS binding: $is_certificate"
    Write-Host
    $DebugPreference = "Inquire"
}





# VERIFY

# Is running as administrator?

if (-not $is_admin) {
    Write-Host "This script must be run as an administrator."
    Write-Host "Please run this script as an administrator and try again."
    Write-Host
    Read-Host -Prompt "Press enter to continue..."
    Exit
}

# Is Windows 10/11 or Server?

if (-not $is_supportedwindows) {
    Write-Host "RAWeb is intended for use on Windows 10/11 and Windows Server."
    Write-Host "Running on other versions of Windows may not work as expected."
    Write-Host

    if (-not $AcceptAll) {
        Write-Host "Do you want to continue anyway?"
        $continue = Read-Host -Prompt "(y/N)"
        Write-Host
    } else {
        $continue = "Y"
    }

    if ($continue -notlike "Y") {
        Write-Host "Exiting."
        Write-Host
        Exit
    }
}

# Is Windows a home edition?

if ($is_home) {
    Write-Host "RAWeb is not intended for Windows Home editions."
    Write-Host "Home editions do not support hosting Remote Desktop connections."
    Write-Host "RAWeb can be manually installed, however, the Windows Authentication feature will not be available."
    Write-Host "To enable Windows Authentication, install the ""Microsoft Windows IIS WebServer AddOn Package"" for your particular Windows version/update."
    Write-Host "Exiting."
    Write-Host
    Exit
}

if (-not $is_sourceexist) {
    Write-Host "The source directory cannot be found ($ScriptPath\$source_dir)."
    Write-Host "Exiting."
    Write-Host
    Exit
}

if (-not $is_frontendsourceexist) {
    Write-Host "The frontend source directory cannot be found ($ScriptPath\$frontend_src_dir)."
    Write-Host "Exiting."
    Write-Host
    Exit
}

# Does a conflicting folder called RAWeb already exist in wwwroot?

if ($is_rawebrealfolder_exists) {
    Write-Host "A directory called RAWeb already exists in the root of the default web site."
    Write-Host "This will need to be removed before continuing."
    Write-Host
    Write-Host "Please remove $rawebinwwwroot and try again."
    Write-Host
}




# DETERMINE INSTALLATION STEPS

# IIS
$install_iis = $true

# RAWeb folder
if ($is_rawebinstallpath_exists) {
    if (-not $AcceptAll) {
        Write-Host "RAWeb directory already exists in inetpub."
        Write-Host "Would you like to overwrite it with a fresh copy?"
        Write-Host "Your existing RAWeb configuration will be lost."
        Write-Host
        $continue = Read-Host -Prompt "(y/N)"
        Write-Host
    } else {
        $continue = "Y"
    }

    if ($continue -like "Y") {
        $install_copy_raweb = $true
    }
} else {
    $install_copy_raweb = $true
}

# RAWeb application (in IIS)

if ($is_application_exists) {
    if (-not $AcceptAll) {
        Write-Host "RAWeb application already exists in IIS."
        Write-Host "Would you like to recreate it?"
        Write-Host
        $continue = Read-Host -Prompt "(y/N)"
        Write-Host
    } else {
        $continue = "Y"
    }

    if ($continue -like "Y") {
        $install_remove_application = $true
        $install_create_application = $true
    }
} else {
    $install_create_application = $true
}

# Enable authentication

if (-not $is_auth_enabled) {
    if (-not $AcceptAll) {
        Write-Host "Authentication must be enabled to use the webfeed/workspace feature,"
        Write-Host "but it will also require users to authenticate to the web interface."
        Write-Host "Would you like to enable it?"
        Write-Host
        $continue = Read-Host -Prompt "(Y/n)"
        Write-Host
    } else {
        $continue = "Y"
    }

    if ($continue -notlike "N") {
        $install_enable_auth = $true
    }
}

# Enable HTTPS

if (-not $is_iisinstalled) {
    $install_enable_https = $true
    $install_create_certificate = $true
} else {
    if (-not $is_httpsenabled) {
        if (-not $AcceptAll) {
            Write-Host "HTTPS is not enabled on the Default Web Site."
            Write-Host "Would you like to enable HTTPS?"
            Write-Host
            $continue = Read-Host -Prompt "(Y/n)"
            Write-Host
        } else {
            $continue = "Y"
        }

        if ($continue -notlike "N") {
            $install_enable_https = $true
            $install_create_certificate = $true
        }
    } else {
        if (-not $is_certificate) {
            if (-not $AcceptAll) {
                Write-Host "An SSL certificate is required use the webfeed/workspace feature."
                Write-Host "Would you like to create and bind a self-signed certificate?"
                Write-Host
                $continue = Read-Host -Prompt "(Y/n)"
                Write-Host
            } else {
                $continue = "Y"
            }
    
            if ($continue -notlike "N") {
                $install_create_certificate = $true
            }
        }
    }
}




# CONFIRM

Write-Host "The following installation steps will be performed:"
Write-Host

if ($install_iis) {
    Write-Host "-Verify all required IIS features are installed"
}

if ($install_copy_raweb) {
    Write-Host "-Build and copy the RAWeb directory to the inetpub directory"
}

if ($install_remove_application) {
    Write-Host "-Remove the existing RAWeb application"
}

if ($install_create_application) {
    Write-Host "-Create the RAWeb application"
}

if ($install_enable_auth) {
    Write-Host "-Enable Authentication"
}

if ($install_enable_https) {
    Write-Host "-Enable HTTPS on the Default Web Site"
}

if ($install_create_certificate) {
    Write-Host "-Create and install an SSL certificate"
}

if (-not $AcceptAll) {
    Write-Host
    Write-Host "Do you want to proceed with the installation?"
    $continue = Read-Host -Prompt "(Y/n)"
    Write-Host
} else {
    $continue = "Y"
}

if ($continue -like "N") {
    Write-Host "Exiting."
    Write-Host
    Exit
}




# INSTALL

# Install IIS and required components

if ($install_iis) {
    Write-Host "Installing IIS and required components..."
    Write-Host
    if ($is_server) {
        $result = Install-WindowsFeature -Name Web-Server, Web-Asp-Net45, Web-Windows-Auth, Web-Http-Redirect, Web-Mgmt-Console, Web-Basic-Auth
    } else {
        $result = Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-CommonHttpFeatures,IIS-HttpErrors,IIS-HttpRedirect,IIS-ApplicationDevelopment,IIS-Security,IIS-RequestFiltering,IIS-NetFxExtensibility45,IIS-HealthAndDiagnostics,IIS-HttpLogging,IIS-Performance,IIS-WebServerManagementTools,IIS-StaticContent,IIS-DefaultDocument,IIS-DirectoryBrowsing,IIS-ASPNET45,IIS-ISAPIExtensions,IIS-ISAPIFilter,IIS-HttpCompressionStatic,IIS-ManagementConsole,IIS-WindowsAuthentication,NetFx4-AdvSrvs,NetFx4Extended-ASPNET45,IIS-BasicAuthentication
    }

    if (
            ((-not $is_server) -and $result.RestartNeeded) -or 
            ($is_server -and $result.RestartNeeded -ne "No")
        ) {
        Write-Host "A restart is required to complete the installation."
        Write-Host
        Write-Host "Press ENTER to restart now."
        Read-Host
        Restart-Computer
    }
}

# Copy the RAWeb folder to the local inetpub/wwwroot directory

if ($install_copy_raweb) {
    # Build the frontend if it is missing
    $lib_timestamp_file = "$ScriptPath\$source_dir\lib\build.timestamp"
    $already_built = Test-Path $lib_timestamp_file
    if (-not $already_built) {
         Write-Host "Building the frontend..."
        $ScriptPath = Split-Path -Path $MyInvocation.MyCommand.Path
        $FrontEndBuildScriptPath = Join-Path -Path $ScriptPath -ChildPath "$frontend_src_dir\build.ps1"
        if ($AcceptAll) {
            & $FrontEndBuildScriptPath -DefaultMode 1
        } else {
            & $FrontEndBuildScriptPath
        }
        Write-Host
    }


    Write-Host "Copying the RAWeb directory to the inetpub directory..."
    Write-Host

    # Delete the RAWeb folder if it exists
    if (Test-Path "$inetpub\RAWeb") {
        Remove-Item -Path "$inetpub\RAWeb" -Force -Recurse | Out-Null
    }

    # Create the RAWeb folder
    New-Item -Path "$inetpub" -Name "RAWeb" -ItemType "directory" | Out-Null

    # Copy the folder structure
    Copy-Item -Path "$ScriptPath\$source_dir\*" -Destination "$inetpub\RAWeb" -Recurse -Force | Out-Null
}

# Remove the RAWeb application

if ($install_remove_application) {
    Write-Host "Removing the existing RAWeb application..."
    Write-Host
    Remove-WebApplication -Site $sitename -Name "RAWeb" | Out-Null
}

# Create the RAWeb application

if ($install_create_application) {
    Write-Host "Creating the RAWeb application..."
    Write-Host
    New-WebApplication -Site $sitename -Name "RAWeb" -PhysicalPath $rawebininetpub | Out-Null
}

if ($install_enable_auth) {
    Write-Host "Enabling Authentication on RAWeb\auth..."
    Write-Host
    
    Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -Location "$sitename/RAWeb/auth" -Name "enabled" -Value "False" | Out-Null
    Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/basicAuthentication" -Location "$sitename/RAWeb/auth" -Name "enabled" -Value "True" | Out-Null
    Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" -Location "$sitename/RAWeb/auth" -Name "enabled" -Value "True" | Out-Null
}

# Enable HTTPS

if ($install_enable_https) {
    Write-Host "Enabling HTTPS on the Default Web Site..."
    Write-Host
    New-WebBinding -Name $sitename -Protocol https -Port 443 | Out-Null
}

# Create and install an SSL certificate

if ($install_create_certificate) {
    Write-Host "Creating a self-signed SSL certificate..."
    Write-Host
    $cert = New-SelfSignedCertificate -DnsName $env:COMPUTERNAME -CertStoreLocation "Cert:\LocalMachine\My"
    $thumbprint = $cert.Thumbprint

    Write-Host "Binding the SSL certificate to the Default Web Site..."
    Write-Host
    
    (Get-WebBinding -Name $sitename -Port 443 -Protocol "https").AddSslCertificate($thumbprint, "my") | Out-Null
}

Write-Host "RAWeb setup is complete." -BackgroundColor Black -ForegroundColor Green
Write-Host

if ($binding -or $install_enable_https) {
    Write-Host "Web interface:"
    Write-Host
    Write-Host "https://$env:COMPUTERNAME/RAWeb"
    Write-Host
    if ($is_auth_enabled -or $install_enable_auth) {
        Write-Host "Webfeed/Workspace URL:"
        Write-Host
            Write-Host "https://$env:COMPUTERNAME/RAWeb/webfeed.aspx"
            Write-Host
            Write-Host "If you wish to access via a different URL/domain, you will need to configure the appropriate DNS records and SSL certificate in IIS."
            Write-Host
    } else {
        Write-Host "The webfeed feature will not be available until authentication is enabled."
        Write-Host
    }
} else {
    Write-Host "Web interface:"
    Write-Host
    Write-Host "http://$env:COMPUTERNAME/RAWeb"
    Write-Host
    Write-Host "The webfeed feature will not be available until HTTPS is enabled on the Default Web Site."
    Write-Host
}

# END
