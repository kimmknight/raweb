# This script sets up RAWeb on a Windows host.
#
# Note: RAWeb will be installed into the default IIS website.
# If you are already using the default website for other purposes, this script may interfere with your existing configuration.
#
# A full installation involves the following steps:
#
# - Install IIS and required components (Web-Server, Web-Asp-Net45, Web-Windows-Auth, Web-Http-Redirect, Web-Mgmt-Console)
# - Copy the RAWeb directory to the inetpub directory
# - Create the RAWeb virtual directory
# - Create the RAWeb application
# - Enable Windows Authentication on RAWeb\auth
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






# VARIABLES

$debug = $false

$sitename = "Default Web Site"
$source_dir = "aspx\wwwroot"
$appname = "RAWeb"
$appname_webpath = "RAWeb"

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
$virtualdirectory = $null
$is_virtualdirectory_exists = $null
$virtualdirectorypath = $null
$is_virtualdirectoryrealfolder_exists = $null
$is_virtualdirectoryapplication = $null
$is_auth_enabled = $null
$is_httpsenabled = $null
$is_certificate = $null

$install_iis = $null
$install_copy_raweb = $null
$install_create_virtualdirectory = $null
$install_remove_virtualdirectory = $null
$install_create_application = $null
$install_remove_application = $null
$install_enable_auth = $null
$install_enable_https = $null
$install_create_certificate = $null





# WELCOME

Write-Host
Write-Host "+++ RAWeb Setup +++" -BackgroundColor Black -ForegroundColor Green
Write-Host
Write-Host "This script will enable IIS and install RAWeb on this computer."
Write-Host

Write-Host "Choose the application name for the RAWeb virtual directory."
Write-Host "Leave blank to use the default name."
$continue = Read-Host -Prompt "(default: $appname)"
if ($continue -ne "" -and $continue -ne "/" -and $continue -ne "\") {
    # Translate forward slashes to backslashes
    $continue = $continue -replace '/', '\'

    # Collapse multiple backslashes to a single backslash
    $continue = $continue -replace '\s+\\', '\' # Remove spaces before backslashes
    $continue = $continue -replace '\\\s+', '\' # Remove spaces after backslashes
    $continue = $continue -replace '\\+', '\' 

    # Remove any leading or trailing slashes or spaces from the input
    $continue = $continue.Trim('./\')
    $continue = $continue.Trim('/\')
    $continue = $continue.Trim(' \') # Remove spaces from the start and end

    # Remove any invalid characters from the input
    $continue = $continue -replace '[<>:"|?*]', ''

    # Replace spaces with hyphens
    $continue = $continue -replace '\s+', '-'

    # Set the application name to the user input
    $appname = $continue
    $appname_webpath = $continue -replace '\\', '/'
}
Write-Host





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

# Is IIS installed?

if ($is_server) {
    $iis = Get-WindowsFeature -Name Web-Server
    $is_iisinstalled = $iis.Installed
} else {
    $iis = Get-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
    $is_iisinstalled = $iis.State -eq "Enabled"
}

# Does the application folder already exist in inetpub (the install location)?

$inetpub = "$env:SystemDrive\inetpub"
$rawebininetpub = "$inetpub\$appname"
$is_rawebinstallpath_exists = Test-Path $rawebininetpub

# Does a conflicting folder with the same name as the application name exist in wwwroot?

$wwwroot = "$env:SystemDrive\inetpub\wwwroot"
$rawebinwwwroot = "$wwwroot\$appname"
$is_rawebrealfolder_exists = Test-Path $rawebinwwwroot

# Some checks can't be completed if IIS is not installed yet.
# If IIS is already installed then perform the checks.

if ($is_iisinstalled) {
    # Does the application virtual directory already exist?

    $virtualdirectory = Get-WebVirtualDirectory -Site $sitename -Name $appname
    $is_virtualdirectory_exists = $null -ne $virtualdirectory

    # If so, does physical directory for the virtual directory actually exist in the filesystem?

    if ($is_virtualdirectory_exists) {
        $virtualdirectorypath = $virtualdirectory.PhysicalPath
        $is_virtualdirectoryrealfolder_exists = Test-Path $virtualdirectorypath
    }

    # Also, is the virtual directory converted to a be an IIS application?

    $is_virtualdirectoryapplication = $null -ne (Get-WebApplication -Site $sitename -Name $appname)

    # Is authentication enabled?

    if ($is_virtualdirectoryapplication) {
            $windows_auth = Get-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" -Location "$sitename/$appname/auth" -Name "enabled"
            $anonymous_auth = Get-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -Location "$sitename/$appname/auth" -Name "enabled"

            $is_auth_enabled = $windows_auth -eq "True" -and $anonymous_auth -eq "False"

            # Currently this only checks Windows and Anonymous auth. If any other kind of auth is enabled, this will be a problem.
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





# DEBUG

if ($debug) {
    Write-Host "Debugging information:"
    Write-Host
    Write-Host "OS: $($os.Caption)"
    Write-Host "Is admin: $is_admin"
    Write-Host "Is server: $is_server"
    Write-Host "Is supported Windows: $is_supportedwindows"
    Write-Host "Is home: $is_home"
    Write-Host "Is IIS installed: $is_iisinstalled"
    Write-Host "Install source directory exists: $is_sourceexist"
    Write-Host "RAWeb install path exists: $is_rawebinstallpath_exists"
    Write-Host "Conflicting $appname directory exists in wwwroot: $is_rawebrealfolder_exists"
    Write-Host "$appname virtual directory exists: $is_virtualdirectory_exists"
    Write-Host "$appname virtual directory real directory exists: $is_virtualdirectoryrealfolder_exists"
    Write-Host "$appname virtual directory is an application: $is_virtualdirectoryapplication"
    Write-Host "Authentication enabled: $is_auth_enabled"
    Write-Host "HTTPS enabled: $is_httpsenabled"
    Write-Host "Certificate bound to HTTPS binding: $is_certificate"
    Write-Host
}





# VERIFY

# Is running as administrator?

if (-not $is_admin) {
    Write-Host "This script must be run as an administrator."
    Write-Host "Please run this script as an administrator and try again."
    Write-Host
    Exit
}

# Is Windows 10/11 or Server?

if (-not $is_supportedwindows) {
    Write-Host "RAWeb is intended for use on Windows 10/11 and Windows Server."
    Write-Host "Running on other versions of Windows may not work as expected."
    Write-Host

    Write-Host "Do you want to continue anyway?"
    $continue = Read-Host -Prompt "(y/N)"
    Write-Host

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
    Write-Host "RAWeb can be manually installed; however, the Windows Authentication feature will not be available."
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

# Does a conflicting folder already exist in wwwroot?

if ($is_rawebrealfolder_exists) {
    Write-Host "A directory called $appname already exists in the root of the default web site."
    Write-Host "This will need to be removed before continuing."
    Write-Host
    Write-Host "Please remove $rawebinwwwroot and try again."
    Write-Host
}




# DETERMINE INSTALLATION STEPS

# IIS
$install_iis = $true

# app folder
if ($is_rawebinstallpath_exists) {
    Write-Host "$appname directory already exists in $inetpub."
    Write-Host "Would you like to overwrite it with a fresh copy?"
    Write-Host "Your existing configuration will be lost."
    Write-Host
    $continue = Read-Host -Prompt "(y/N)"
    Write-Host

    if ($continue -like "Y") {
        $install_copy_raweb = $true
    }
} else {
    $install_copy_raweb = $true
}

# app virtual directory

if ($is_virtualdirectory_exists) {
    Write-Host "$appname virtual directory already exists in IIS."
    Write-Host "Would you like to recreate it?"
    Write-Host
    $continue = Read-Host -Prompt "(y/N)"
    Write-Host

    if ($continue -notlike "Y") {
        if (-not $is_virtualdirectoryapplication) {
            $install_create_application = $true
        }
    } else {
        if ($is_virtualdirectoryapplication) {
            $install_remove_application = $true
        }
        $install_remove_virtualdirectory = $true
        $install_create_virtualdirectory = $true
        $install_create_application = $true
    }
} else {
    $install_create_virtualdirectory = $true
    $install_create_application = $true
}

# Enable authentication

if (-not $is_auth_enabled) {
    Write-Host "Authentication must be enabled to use the webfeed/workspace feature,"
    Write-Host "but it will also require users to authenticate to the web interface."
    Write-Host "Would you like to enable it?"
    Write-Host
    $continue = Read-Host -Prompt "(Y/n)"
    Write-Host

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
        Write-Host "HTTPS is not enabled on the Default Web Site."
        Write-Host "Would you like to enable HTTPS?"
        Write-Host
        $continue = Read-Host -Prompt "(Y/n)"
        Write-Host

        if ($continue -notlike "N") {
            $install_enable_https = $true
            $install_create_certificate = $true
        }
    } else {
        if (-not $is_certificate) {
            Write-Host "An SSL certificate is required use the webfeed/workspace feature."
            Write-Host "Would you like to create and bind a self-signed certificate?"
            Write-Host
            $continue = Read-Host -Prompt "(Y/n)"
            Write-Host
    
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
    Write-Host "-Install RAWeb to $inetpub\$appname"
}

if ($install_remove_application) {
    Write-Host "-Remove the existing $appname application"
}

if ($install_remove_virtualdirectory) {
    Write-Host "-Remove the existing $appname virtual directory"
} 

if ($install_create_virtualdirectory) {
    Write-Host "-Create the $appname virtual directory"
}

if ($install_create_application) {
    Write-Host "-Create the $appname application"
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

Write-Host
Write-Host "Do you want to proceed with the installation?"
$continue = Read-Host -Prompt "(Y/n)"
Write-Host

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
        $result = Install-WindowsFeature -Name Web-Server, Web-Asp-Net45, Web-Windows-Auth, Web-Http-Redirect, Web-Mgmt-Console
    } else {
        $result = Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-CommonHttpFeatures,IIS-HttpErrors,IIS-HttpRedirect,IIS-ApplicationDevelopment,IIS-Security,IIS-RequestFiltering,IIS-NetFxExtensibility45,IIS-HealthAndDiagnostics,IIS-HttpLogging,IIS-Performance,IIS-WebServerManagementTools,IIS-StaticContent,IIS-DefaultDocument,IIS-DirectoryBrowsing,IIS-ASPNET45,IIS-ISAPIExtensions,IIS-ISAPIFilter,IIS-HttpCompressionStatic,IIS-ManagementConsole,IIS-WindowsAuthentication,NetFx4-AdvSrvs,NetFx4Extended-ASPNET45
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

# Copy the app folder to the local inetpub/wwwroot directory

if ($install_copy_raweb) {
    Write-Host "Copying the $appname directory to $inetpub..."
    Write-Host

    # Delete the folder if it exists
    if (Test-Path "$inetpub\$appname") {
        Remove-Item -Path "$inetpub\$appname" -Force -Recurse | Out-Null
    }

    # Create the folder
    New-Item -Path "$inetpub" -Name $appname -ItemType "directory" | Out-Null

    # Copy the folder structure
    Copy-Item -Path "$ScriptPath\$source_dir\*" -Destination "$inetpub\$appname" -Recurse -Force | Out-Null
}

# Remove the application

if ($install_remove_application) {
    Write-Host "Removing the existing $appname application..."
    Write-Host
    Remove-WebApplication -Site $sitename -Name $appname | Out-Null
}

# Remove the virtual directory

if ($install_remove_virtualdirectory) {
    Write-Host "Removing the existing $appname virtual directory..."
    Write-Host
    #Remove-WebVirtualDirectory -Site $sitename -Name $appname
    Remove-Item -Path "IIS:\Sites\$($sitename)\$appname" -Recurse -Force | Out-Null
}

# Create the virtual directory

if ($install_create_virtualdirectory) {
    # Ensure the highest parent directory exists
    # (e.g. if the appname is "apps\remote-apps\RAWeb", then create the "apps" virtual directory)
    $parentdir = ($appname -split '\\')[0]
    if ($parentdir) {
        if (-not (Get-WebVirtualDirectory -Site $sitename -Name $parentdir)) {
            Write-Host "Creating the $parentdir virtual directory..."
            New-WebVirtualDirectory -Site $sitename -Name $parentdir -PhysicalPath "$inetpub\$parentdir" | Out-Null
        }
    }
}

# Create the application

if ($install_create_application) {
    Write-Host "Creating the $appname application..."
    Write-Host
    New-WebApplication -Site $sitename -Name $appname -PhysicalPath $rawebininetpub -force | Out-Null
}

if ($install_enable_auth) {
    Write-Host "Enabling Windows Authentication on $appname\auth..."
    Write-Host
    
    Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -Location "$sitename/$appname_webpath/auth" -Name "enabled" -Value "False" | Out-Null
    Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" -Location "$sitename/$appname_webpath/auth" -Name "enabled" -Value "True" | Out-Null
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
    Write-Host "https://$env:COMPUTERNAME/$appname_webpath"
    Write-Host
    if ($is_auth_enabled -or $install_enable_auth) {
        Write-Host "Webfeed/Workspace URL:"
        Write-Host
            Write-Host "https://$env:COMPUTERNAME/$appname_webpath/webfeed.aspx"
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
    Write-Host "http://$env:COMPUTERNAME/$appname_webpath"
    Write-Host
    Write-Host "The webfeed feature will not be available until HTTPS is enabled on the Default Web Site."
    Write-Host
}

# END