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
$appPoolName = "raweb"

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
        Write-Host "Modifications to your existing RAWeb installation will be lost."
        Write-Host "Resources, policies, and other app data will be preserved."
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
        # Preserve the app data folders in the temp directory
        #  - App_Data: where the RAWeb data is stored
        #  - resources: where RAWeb used to read RDP files and icons (now in App_Data\resources)
        #  - multiuser-resources: where RAWeb used to read RDP files and icons and assigned permissions based on folder name (now in App_Data\multiuser-resources)
        $appdata = "$inetpub\RAWeb\App_Data"
        $resources = "$inetpub\RAWeb\resources"
        $multiuser_resources = "$inetpub\RAWeb\multiuser-resources"
        $tmp_resources_copy = [System.IO.Path]::GetTempPath() + "raweb_backup_resources"
        if (-not (Test-Path $tmp_resources_copy)) {
            New-Item -Path $tmp_resources_copy -ItemType Directory | Out-Null
        }
        if (Test-Path $appdata) {
            robocopy $appdata "$tmp_resources_copy/App_Data" /E /COPYALL /DCOPY:T | Out-Null
        }
        if (Test-Path $resources) {
            robocopy $resources "$tmp_resources_copy/resources" /E /COPYALL /DCOPY:T | Out-Null
        }
        if (Test-Path $multiuser_resources) {
            robocopy $multiuser_resources "$tmp_resources_copy/multiuser-resources" /E /COPYALL /DCOPY:T | Out-Null
        }

        # If the appSettings are in Web.config, we need to extract them and
        # move them to the App_Data/appSettings.config file. Old versions of RAWev
        # stored all appSettings in Web.config, but never versions store them
        # in App_Data/appSettings.config and specify configSource="App_Data\appSettings.config".
        $webConfigPath = "$inetpub\RAWeb\Web.config"
        if (Test-Path $webConfigPath) {
            $webConfig = [xml](Get-Content $webConfigPath)
            if ($webConfig.configuration.appSettings) {
                $appSettings = $webConfig.configuration.appSettings

                # only continue if there are children elements (settings to copy)
                if ($appSettings.ChildNodes.Count -gt 0)  {
                    Write-Host "Extracting appSettings from Web.config..."
                    $appSettingsFilePath = "$appdata\appSettings.config"
                    if (-not (Test-Path $appSettingsFilePath)) {
                        # write the appSettings to file
                        $appSettingsFileText = @"
<?xml version="1.0"?>
$($appSettings.OuterXml)
"@
                        $backupAppDataFolder = "$tmp_resources_copy\App_Data"
                        $backupAppSettingsFilePath = "$backupAppDataFolder\appSettings.config"
                        if (-not (Test-Path -Path $backupAppDataFolder -PathType Container)) {
                            New-Item -Path $backupAppDataFolder -ItemType Directory | Out-Null
                        }
                        New-Item -Path $backupAppSettingsFilePath -ItemType File | Out-Null
                        Set-Content -Path $backupAppSettingsFilePath -Value $appSettingsFileText -Encoding UTF8 | Out-Null
                    }
                    
                }

            }
        }

        Remove-Item -Path "$inetpub\RAWeb" -Force -Recurse | Out-Null
    }

    # Create the RAWeb folder
    New-Item -Path "$inetpub" -Name "RAWeb" -ItemType "directory" | Out-Null

    # Copy the folder structure
    Copy-Item -Path "$ScriptPath\$source_dir\*" -Destination "$inetpub\RAWeb" -Recurse -Force | Out-Null

    # Restore the app data folders
    if (Test-Path $tmp_resources_copy) {

        if (Test-Path "$tmp_resources_copy\App_Data") {
            robocopy "$tmp_resources_copy\App_Data" "$inetpub\RAWeb\App_Data" /E /COPYALL /DCOPY:T | Out-Null
        }
        if (Test-Path "$tmp_resources_copy\resources") { # migrate to App_Data\resources
            robocopy "$tmp_resources_copy\resources" "$inetpub\RAWeb\App_Data\resources" /E /COPYALL /DCOPY:T | Out-Null
        }
        if (Test-Path "$tmp_resources_copy\multiuser-resources") { # migrate to App_Data\multiuser-resources
            robocopy "$tmp_resources_copy\multiuser-resources" "$inetpub\RAWeb\App_Data\multiuser-resources" /E /COPYALL /DCOPY:T | Out-Null
        }
        Remove-Item -Path $tmp_resources_copy -Recurse -Force | Out-Null
    }
}

# Remove the RAWeb application

if ($install_remove_application) {
    Write-Host "Removing the existing RAWeb application..."
    Write-Host
    Remove-WebApplication -Site $sitename -Name "RAWeb" | Out-Null

    # old versions used to create a virtual directory, so
    # we need to remove it if it exists
    try {
        Remove-Item -Path "IIS:\Sites\$($sitename)\RAWeb" -Recurse -Force -ErrorAction Stop | Out-Null
    } catch {}
}

# Create the RAWeb application

if ($install_create_application) {
    # If it does not already exist, create the raweb app pools
    try {
        Get-WebAppPoolState -Name $appPoolName | Out-Null
    }
    catch {
        Write-Host "Creating the RAWeb application pool..."
        Write-Host
        New-WebAppPool -Name $appPoolName -Force | Out-Null
        Set-ItemProperty IIS:\AppPools\$appPoolName -Name processModel.identityType -Value ApplicationPoolIdentity | Out-Null # auth as ApplicationPoolIdentity (IIS AppPool\raweb)
    }

    Write-Host "Creating the RAWeb application..."
    Write-Host
    New-WebApplication -Site $sitename -Name "RAWeb" -PhysicalPath $rawebininetpub -ApplicationPool $appPoolName | Out-Null

    # disable permissions inheritance on the RAWeb directory
    $rawebAcl = Get-Acl $rawebininetpub
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
    
    # additionally grant write access to the App_Data folder, which is required for the policies web editor
    $appDataPath = Join-Path -Path $rawebininetpub -ChildPath "App_Data"
    $appDataAcl = Get-Acl $appDataPath
    $appDataAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($appPoolIdentity, "Write, Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
    $appDataAcl.SetAccessRule($appDataAccessRule)

    # allow read access for Everyone to the auth directory (login fails otherwise)
    $authPath = Join-Path -Path $rawebininetpub -ChildPath "auth"
    $authAcl = Get-Acl $authPath
    $everyoneSid = New-Object System.Security.Principal.SecurityIdentifier("S-1-1-0")
    $everyoneAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($everyoneSid, "Read", "ContainerInherit,ObjectInherit", "None", "Allow")
    $authAcl.SetAccessRule($everyoneAccessRule)

    # allow read access for the Users group for App_Data\resources since all users should have access to the resources by default
    $resourcesPath = Join-Path -Path $appDataPath -ChildPath "resources"
    $resourcesAcl = Get-Acl $resourcesPath
    $usersSid = New-Object System.Security.Principal.SecurityIdentifier("S-1-5-32-545")
    $usersAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($usersSid, "Read", "ContainerInherit,ObjectInherit", "None", "Allow")
    $resourcesAcl.SetAccessRule($usersAccessRule)

    # allow read and execute access to bin\SQLite.Interop.dll for the RAWeb application pool identity
    $sqliteInteropPath = Join-Path -Path $rawebininetpub -ChildPath "bin\SQLite.Interop.dll"
    if (Test-Path $sqliteInteropPath) {
        $sqliteInteropAcl = Get-Acl $sqliteInteropPath
        $sqliteInteropAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($appPoolIdentity, "ReadAndExecute", "None", "None", "Allow")
        $sqliteInteropAcl.SetAccessRule($sqliteInteropAccessRule)
        Set-Acl -Path $sqliteInteropPath -AclObject $sqliteInteropAcl
    }

    Set-Acl -Path $rawebininetpub -AclObject $rawebAcl
    Set-Acl -Path $appDataPath -AclObject $appDataAcl
    Set-Acl -Path $authPath -AclObject $authAcl
    Set-Acl -Path $resourcesPath -AclObject $resourcesAcl

    # run anonymous authentication to use the RAWeb application pool identity
    Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -Location "$sitename/RAWeb" -Name "enabled" -Value "True" | Out-Null
    Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -Location "$sitename/RAWeb" -Name "userName" -Value "" | Out-Null
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
