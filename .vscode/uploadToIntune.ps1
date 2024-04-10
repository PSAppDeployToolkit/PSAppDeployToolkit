# Vars
. ".vscode\Global.ps1"

#Install-Module -Name "IntuneWin32App" -force
Get-installedModule -Name IntuneWin32App
# Retrieve auth token required for accessing Microsoft Graph
# Delegated authentication is currently supported only, app-based authentication is on the todo-list
Connect-MSIntuneGraph -TenantID "memtipsandtricks.tech" -Verbose


    $Publisher = "Application WP Ninja DEMO"
    $IntuneWinFile = "$Desktop\$Application\$Application.intunewin"
    $AppIconFile = "C:\- Stuff -\DEMO\notepad.jpg"

    # Create custom display name like 'Name' and 'Version'
    $DisplayName = "Notepad++"
    Write-Output -InputObject "Constructed display name for Win32 app: $($DisplayName)"

    # Create requirement rule for all platforms and Windows 10 20H2
    $RequirementRule = New-IntuneWin32AppRequirementRule -Architecture "All" -MinimumSupportedWindowsRelease "20H2"

    # Create PowerShell script detection rule
    $DetectionScriptFile = "C:\- Stuff -\DEMO\CustomDetection.ps1"
    $DetectionRule = New-IntuneWin32AppDetectionRuleScript -ScriptFile $DetectionScriptFile -EnforceSignatureCheck $false -RunAs32Bit $false

    # Convert image file to icon
    $Icon = New-IntuneWin32AppIcon -FilePath $AppIconFile
    
    # Add new EXE Win32 app
    $InstallCommandLine = "Deploy-Application.exe"
    $UninstallCommandLine = "Deploy-Application.exe Uninstall"
    Add-IntuneWin32App -FilePath $IntuneWinFile -DisplayName $DisplayName -Description "Notepad ++ is a nice editor" -Publisher $Publisher -InstallExperience "system" -RestartBehavior "suppress" -DetectionRule $DetectionRule -RequirementRule $RequirementRule -InstallCommandLine $InstallCommandLine -UninstallCommandLine $UninstallCommandLine -Icon $Icon -Verbose

    #Write-Output -InputObject "Starting to create Win32 app in Intune"
    #$Win32App = Add-IntuneWin32App @Win32AppArguments

    Write-Output -InputObject "Successfully created new Win32 app with name: $($Win32App.displayName)"
    