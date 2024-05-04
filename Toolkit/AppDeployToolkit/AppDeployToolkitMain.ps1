<#
.SYNOPSIS

PSAppDeployToolkit - This script contains the PSADT core runtime and functions using by a Deploy-Application.ps1 script.

.DESCRIPTION

The script can be called directly to dot-source the toolkit functions for testing, but it is usually called by the Deploy-Application.ps1 script.

The script can usually be updated to the latest version without impacting your per-application Deploy-Application scripts. Please check release notes before upgrading.

PSAppDeployToolkit is licensed under the GNU LGPLv3 License - (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham and Muhammad Mashwani).

This program is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the
Free Software Foundation, either version 3 of the License, or any later version. This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License
for more details. You should have received a copy of the GNU Lesser General Public License along with this program. If not, see <http://www.gnu.org/licenses/>.

.PARAMETER CleanupBlockedApps

Clean up the blocked applications.

This parameter is passed to the script when it is called externally, e.g. from a scheduled task or asynchronously.

.PARAMETER ShowBlockedAppDialog

Display a dialog box showing that the application execution is blocked.
This parameter is passed to the script when it is called externally, e.g. from a scheduled task or asynchronously.

.PARAMETER ReferredInstallName

Name of the referring application that invoked the script externally.
This parameter is passed to the script when it is called externally, e.g. from a scheduled task or asynchronously.

.PARAMETER ReferredInstallTitle

Title of the referring application that invoked the script externally.
This parameter is passed to the script when it is called externally, e.g. from a scheduled task or asynchronously.

.PARAMETER ReferredLogname

Logfile name of the referring application that invoked the script externally.
This parameter is passed to the script when it is called externally, e.g. from a scheduled task or asynchronously.

.PARAMETER AsyncToolkitLaunch

This parameter is passed to the script when it is being called externally, e.g. from a scheduled task or asynchronously.

.INPUTS

None

You cannot pipe objects to this script.

.OUTPUTS

None

This script does not generate any output.

.NOTES

The other parameters specified for this script that are not documented in this help section are for use only by functions in this script that call themselves by running this script again asynchronously.

.LINK

https://psappdeploytoolkit.com
#>


[CmdletBinding()]
Param (
    ## Script Parameters: These parameters are passed to the script when it is called externally from a scheduled task or because of an Image File Execution Options registry setting
    [Switch]$ShowInstallationPrompt = $false,
    [Switch]$ShowInstallationRestartPrompt = $false,
    [Switch]$CleanupBlockedApps = $false,
    [Switch]$ShowBlockedAppDialog = $false,
    [Switch]$DisableLogging = $false,
    [String]$ReferredInstallName = '',
    [String]$ReferredInstallTitle = '',
    [String]$ReferredLogName = '',
    [String]$Title = '',
    [String]$Message = '',
    [String]$MessageAlignment = '',
    [String]$ButtonRightText = '',
    [String]$ButtonLeftText = '',
    [String]$ButtonMiddleText = '',
    [String]$Icon = '',
    [String]$Timeout = '',
    [Switch]$ExitOnTimeout = $false,
    [Boolean]$MinimizeWindows = $false,
    [Switch]$PersistPrompt = $false,
    [Int32]$CountdownSeconds = 60,
    [Int32]$CountdownNoHideSeconds = 30,
    [Switch]$NoCountdown = $false,
    [Switch]$AsyncToolkitLaunch = $false,
    [Boolean]$TopMost = $true
)

##*=============================================
##* VARIABLE DECLARATION
##*=============================================
#region VariableDeclaration

## Add the custom types required for the toolkit
Add-Type -LiteralPath ($appDeployCustomTypesSourceCode = "$PSScriptRoot\AppDeployToolkitMain.cs") -ErrorAction Stop -ReferencedAssemblies $(
    'System.Drawing', 'System.Windows.Forms', 'System.DirectoryServices'
    if ($PSVersionTable.PSEdition.Equals('Core'))
    {
        'System.Collections', 'System.Text.RegularExpressions', 'System.Security.Principal.Windows', 'System.ComponentModel.Primitives', 'Microsoft.Win32.Primitives'
    }
)

. "$PSScriptRoot\PSAppDeployToolkit\Private\AppDeployToolkitPrivate.ps1"
. "$PSScriptRoot\PSAppDeployToolkit\Public\AppDeployToolkitPublic.ps1"

New-Variable -Name ADT -Option Constant -Value ([ordered]@{
    DotSourced = $MyInvocation.InvocationName.Equals('.') -or [System.String]::IsNullOrWhiteSpace($MyInvocation.Line)
    Environment = $null
    Sessions = $null
    Config = $null
    Strings = $null
    Progress = [ordered]@{
        Runspace = [runspacefactory]::CreateRunspace()
        SyncHash = [hashtable]::Synchronized(@{})
    }
})

Import-PsadtVariables -Cmdlet $PSCmdlet
Import-PsadtConfig

## Variables: Script Info
[Hashtable]$appDeployMainScriptParameters = $PSBoundParameters

## Variables: Datetime
[DateTime]$currentDateTime = Get-Date
[String]$currentTime = Get-Date -Date $currentDateTime -UFormat '%T'
[String]$currentDate = Get-Date -Date $currentDateTime -UFormat '%d-%m-%Y'
[Timespan]$currentTimeZoneBias = [TimeZone]::CurrentTimeZone.GetUtcOffset($currentDateTime)

## Variables: Script Name and Script Paths
[String]$scriptPath = $MyInvocation.MyCommand.Definition
[String]$scriptName = [IO.Path]::GetFileNameWithoutExtension($scriptPath)
[String]$scriptFileName = Split-Path -Path $scriptPath -Leaf
[String]$scriptRoot = Split-Path -Path $scriptPath -Parent
[String]$invokingScript = (Get-Variable -Name 'MyInvocation').Value.ScriptName
#  Get the invoking script directory
If ($invokingScript) {
    #  If this script was invoked by another script
    [String]$scriptParentPath = Split-Path -Path $invokingScript -Parent
}
Else {
    #  If this script was not invoked by another script, fall back to the directory one level above this script
    [String]$scriptParentPath = (Get-Item -LiteralPath $scriptRoot).Parent.FullName
}

## Variables: App Deploy Script Dependency Files
[String]$appDeployRunHiddenVbsFile = Join-Path -Path $scriptRoot -ChildPath 'RunHidden.vbs'

#  App Deploy Optional Extensions File
[String]$appDeployToolkitDotSourceExtensions = 'AppDeployToolkitExtensions.ps1'

## Variables: Script Directories
[String]$dirFiles = Join-Path -Path $scriptParentPath -ChildPath 'Files'
[String]$dirSupportFiles = Join-Path -Path $scriptParentPath -ChildPath 'SupportFiles'
[String]$dirAppDeployTemp = Join-Path -Path $Script:ADT.Config.Toolkit_Options.Toolkit_TempPath -ChildPath $appDeployToolkitName

If (-not (Test-Path -LiteralPath $dirAppDeployTemp -PathType 'Container' -ErrorAction 'SilentlyContinue')) {
    $null = New-Item -Path $dirAppDeployTemp -ItemType 'Directory' -Force -ErrorAction 'SilentlyContinue'
}

## Set the deploy mode to "Interactive" if it has not been specified
If (!(Test-Path -LiteralPath 'variable:deployMode')) {
    [String]$deployMode = 'Interactive'
}

## Set the deployment type to "Install" if it has not been specified
If (!(Test-Path -LiteralPath 'variable:deploymentType')) {
    [String]$deploymentType = 'Install'
}

## Ensure the deployment type is always title-case for log aesthetics.
$deploymentType = $culture.TextInfo.ToTitleCase($deploymentType)

## COM Objects: Initialize
[__ComObject]$Shell = New-Object -ComObject 'WScript.Shell' -ErrorAction 'SilentlyContinue'
[__ComObject]$ShellApp = New-Object -ComObject 'Shell.Application' -ErrorAction 'SilentlyContinue'

## Variables: Reset/Remove Variables
[String]$installPhase = 'Initialization'
[String]$logName = [System.String]::Empty
[String]$defaultMsiExecutablesList = [System.String]::Empty
[String]$oldPSWindowTitle = $Host.UI.RawUI.WindowTitle
[Boolean]$instProgressRunning = $false
[Boolean]$useDefaultMsi = $false
[Boolean]$msiRebootDetected = $false
[Boolean]$BlockExecution = $false
[Boolean]$installationStarted = $false
[Boolean]$runningTaskSequence = $false
[Boolean]$LogFileInitialized = $AsyncToolkitLaunch -and $ReferredLogname
If (Test-Path -LiteralPath 'variable:welcomeTimer') {
    Remove-Variable -Name 'welcomeTimer' -Scope 'Script'
}
#  Reset the deferral history
If (Test-Path -LiteralPath 'variable:deferHistory') {
    Remove-Variable -Name 'deferHistory'
}
If (Test-Path -LiteralPath 'variable:deferTimes') {
    Remove-Variable -Name 'deferTimes'
}
If (Test-Path -LiteralPath 'variable:deferDays') {
    Remove-Variable -Name 'deferDays'
}

## Variables: Resolve Parameters. For use in a pipeline
filter Resolve-Parameters {
    <#
.SYNOPSIS

Resolve the parameters of a function call to a string.

.DESCRIPTION

Resolve the parameters of a function call to a string.

.PARAMETER Parameter

The name of the function this function is invoked from.

.INPUTS

System.Object

.OUTPUTS

System.Object

.EXAMPLE

Resolve-Parameters -Parameter $PSBoundParameters | Out-String

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    Param (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]$Parameter
    )

    Switch ($Parameter) {
        {$_.Value -is [System.Management.Automation.SwitchParameter]} {
            "-$($_.Key):`$$($_.Value.ToString().ToLower())"
            break
        }
        {$_.Value -is [System.Boolean]} {
            "-$($_.Key):`$$($_.Value.ToString().ToLower())"
            break
        }
        {$_.Value -is [System.Int16]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.Int32]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.Int64]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.UInt16]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.UInt32]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.UInt64]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.Single]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.Double]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.Decimal]} {
            "-$($_.Key):$($_.Value)"
            break
        }
        {$_.Value -is [System.Collections.IDictionary]} {
            "-$($_.Key):'$(($_.Value.GetEnumerator() | Resolve-Parameters).Replace("'",'"') -join "', '")'"
            break
        }
        {$_.Value -is [System.Collections.IEnumerable]} {
            "-$($_.Key):'$($_.Value -join "', '")'"
            break
        }
        default {
            "-$($_.Key):'$($_.Value)'"
            break
        }
    }
}
#endregion
##*=============================================
##* END VARIABLE DECLARATION
##*=============================================

##*=============================================
##* SCRIPT BODY
##*=============================================
#region ScriptBody

## If the script was invoked by the Help Console, exit the script now
If ($invokingScript) {
    If ((Split-Path -Path $invokingScript -Leaf) -eq 'AppDeployToolkitHelp.ps1') {
        Return
    }
}

## Set process as DPI-aware for better dialog rendering.
[System.Void][PSADT.UiAutomation]::SetProcessDPIAware()

## Define ScriptBlocks to disable/revert script logging
[ScriptBlock]$DisableScriptLogging = { $OldDisableLoggingValue = $DisableLogging ; $DisableLogging = $true }
[ScriptBlock]$RevertScriptLogging = { $DisableLogging = $OldDisableLoggingValue }

## Disable logging until log file details are available
. $DisableScriptLogging

## Assemblies: Load
Try {
    Add-Type -AssemblyName ('System.Drawing', 'System.Windows.Forms', 'PresentationFramework', 'Microsoft.VisualBasic', 'PresentationCore', 'WindowsBase') -ErrorAction 'Stop'
}
Catch {
    Write-Log -Message "Failed to load assembly. `r`n$(Resolve-Error)" -Severity 3 -Source $appDeployToolkitName
    If ($deployMode -eq 'Silent') {
        Write-Log -Message "Continue despite assembly load error since deployment mode is [$deployMode]." -Source $appDeployToolkitName
    }
    Else {
        Exit-Script -ExitCode 60004
    }
}

## Dot Source script extensions
If (Test-Path -LiteralPath "$scriptRoot\$appDeployToolkitDotSourceExtensions" -PathType 'Leaf') {
    . "$scriptRoot\$appDeployToolkitDotSourceExtensions"
}

## If the default Deploy-Application.ps1 hasn't been modified, and the main script was not called by a referring script, check for MSI / MST and modify the install accordingly
If ((-not $ReferredInstallName) -and (!(Test-Path -LiteralPath 'variable:appName') -or [System.String]::IsNullOrWhiteSpace($appName))) {
    # Build properly formatted Architecture String
    Switch ($Is64Bit) {
        $false {
            $formattedOSArch = 'x86'
        }
        $true {
            $formattedOSArch = 'x64'
        }
    }
    #  Find the first MSI file in the Files folder and use that as our install
    If ([String]$defaultMsiFile = (Get-ChildItem -LiteralPath $dirFiles -ErrorAction 'SilentlyContinue' | Where-Object { (-not $_.PsIsContainer) -and ([IO.Path]::GetExtension($_.Name) -eq '.msi') -and ($_.Name.EndsWith(".$formattedOSArch.msi")) } | Select-Object -ExpandProperty 'FullName' -First 1)) {
        Write-Log -Message "Discovered $formattedOSArch Zero-Config MSI under $defaultMSIFile" -Source $appDeployToolkitName
    }
    ElseIf ([String]$defaultMsiFile = (Get-ChildItem -LiteralPath $dirFiles -ErrorAction 'SilentlyContinue' | Where-Object { (-not $_.PsIsContainer) -and ([IO.Path]::GetExtension($_.Name) -eq '.msi') } | Select-Object -ExpandProperty 'FullName' -First 1)) {
        Write-Log -Message "Discovered Arch-Independent Zero-Config MSI under $defaultMSIFile" -Source $appDeployToolkitName
    }
    If ($defaultMsiFile) {
        Try {
            [Boolean]$useDefaultMsi = $true
            Write-Log -Message "Discovered Zero-Config MSI installation file [$defaultMsiFile]." -Source $appDeployToolkitName
            #  Discover if there is a zero-config MST file
            If ([System.String]::IsNullOrWhiteSpace($DefaultMstFile)) {$defaultMstFile = [IO.Path]::ChangeExtension($defaultMsiFile, 'mst')}
            If (Test-Path -LiteralPath $defaultMstFile -PathType 'Leaf') {
                Write-Log -Message "Discovered Zero-Config MST installation file [$defaultMstFile]." -Source $appDeployToolkitName
            }
            Else {
                [String]$defaultMstFile = ''
            }
            #  Discover if there are zero-config MSP files. Name multiple MSP files in alphabetical order to control order in which they are installed.
            If (!$defaultMspFiles -and ($mspFiles = Get-ChildItem -LiteralPath $dirFiles -ErrorAction 'SilentlyContinue' | Where-Object { (-not $_.PsIsContainer) -and ([IO.Path]::GetExtension($_.Name) -eq '.msp') } | Select-Object -ExpandProperty 'FullName')) {$defaultMspFiles = $mspFiles}
            If ($defaultMspFiles) {
                Write-Log -Message "Discovered Zero-Config MSP installation file(s) [$($defaultMspFiles -join ',')]." -Source $appDeployToolkitName
            }

            ## Read the MSI and get the installation details
            [Hashtable]$GetDefaultMsiTablePropertySplat = @{ Path = $defaultMsiFile; Table = 'Property'; ContinueOnError = $false; ErrorAction = 'Stop' }
            If ($defaultMstFile) {
                $GetDefaultMsiTablePropertySplat.Add('TransformPath', $defaultMstFile)
            }
            [PSObject]$defaultMsiPropertyList = Get-MsiTableProperty @GetDefaultMsiTablePropertySplat
            [String]$appVendor = $defaultMsiPropertyList.Manufacturer
            [String]$appName = $defaultMsiPropertyList.ProductName
            [String]$appVersion = $defaultMsiPropertyList.ProductVersion
            $GetDefaultMsiTablePropertySplat.Set_Item('Table', 'File')
            [PSObject]$defaultMsiFileList = Get-MsiTableProperty @GetDefaultMsiTablePropertySplat
            [String[]]$defaultMsiExecutables = Get-Member -InputObject $defaultMsiFileList -ErrorAction 'Stop' | Select-Object -ExpandProperty 'Name' -ErrorAction 'Stop' | Where-Object { [IO.Path]::GetExtension($_) -eq '.exe' } | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) }
            [String]$defaultMsiExecutablesList = $defaultMsiExecutables -join ','
            Write-Log -Message "App Vendor [$appVendor]." -Source $appDeployToolkitName
            Write-Log -Message "App Name [$appName]." -Source $appDeployToolkitName
            Write-Log -Message "App Version [$appVersion]." -Source $appDeployToolkitName
            If ($defaultMsiExecutablesList) {Write-Log -Message "MSI Executable List [$defaultMsiExecutablesList]." -Source $appDeployToolkitName}
        }
        Catch {
            Write-Log -Message "Failed to process Zero-Config MSI Deployment. `r`n$(Resolve-Error)" -Source $appDeployToolkitName
            $useDefaultMsi = $false ; $appVendor = '' ; $appName = '' ; $appVersion = ''
        }
    }
}

## Set up sample variables if Dot Sourcing the script, app details have not been specified
If (!(Test-Path -LiteralPath 'variable:appName') -or [System.String]::IsNullOrWhiteSpace($appName)) {
    [String]$appName = $appDeployMainScriptFriendlyName
    If (!(Test-Path -LiteralPath 'variable:appVendor') -or [System.String]::IsNullOrWhiteSpace($appVendor)) {
        [String]$appVendor = 'PS'
    }
    If (!(Test-Path -LiteralPath 'variable:appVersion') -or [System.String]::IsNullOrWhiteSpace($appVersion)) {
        [String]$appVersion = $appDeployMainScriptVersion
    }
    If (!(Test-Path -LiteralPath 'variable:appLang') -or [System.String]::IsNullOrWhiteSpace($appLang)) {
        [String]$appLang = $currentLanguage
    }
    If (!(Test-Path -LiteralPath 'variable:appRevision') -or [System.String]::IsNullOrWhiteSpace($appRevision)) {
        [String]$appRevision = '01'
    }
    If (!(Test-Path -LiteralPath 'variable:appArch') -or [System.String]::IsNullOrWhiteSpace($appArch)) {
        [String]$appArch = ''
    }
}
Else {
    If (!(Test-Path -LiteralPath 'variable:appVendor') -or [System.String]::IsNullOrWhiteSpace($appVendor)) {
        [String]$appVendor = ''
    }
    If (!(Test-Path -LiteralPath 'variable:appVersion') -or [System.String]::IsNullOrWhiteSpace($appVersion)) {
        [String]$appVersion = ''
    }
    If (!(Test-Path -LiteralPath 'variable:appLang') -or [System.String]::IsNullOrWhiteSpace($appLang)) {
        [String]$appLang = ''
    }
    If (!(Test-Path -LiteralPath 'variable:appRevision') -or [System.String]::IsNullOrWhiteSpace($appRevision)) {
        [String]$appRevision = ''
    }
    If (!(Test-Path -LiteralPath 'variable:appArch') -or [System.String]::IsNullOrWhiteSpace($appArch)) {
        [String]$appArch = ''
    }
}

## Sanitize the application details, as they can cause issues in the script
[String]$appVendor = (Remove-InvalidFileNameChars -Name ($appVendor.Trim()))
[String]$appName = (Remove-InvalidFileNameChars -Name ($appName.Trim()))
[String]$appVersion = (Remove-InvalidFileNameChars -Name ($appVersion.Trim()))
[String]$appArch = (Remove-InvalidFileNameChars -Name ($appArch.Trim()))
[String]$appLang = (Remove-InvalidFileNameChars -Name ($appLang.Trim()))
[String]$appRevision = (Remove-InvalidFileNameChars -Name ($appRevision.Trim()))

## Build the Installation Title
If ($ReferredInstallTitle) {
    [String]$installTitle = (Remove-InvalidFileNameChars -Name ($ReferredInstallTitle.Trim()))
}
If (-not $installTitle) {
    [String]$installTitle = "$appVendor $appName $appVersion".Trim()
}

## Set Powershell window title, in case the window is visible
$Host.UI.RawUI.WindowTitle = "$installTitle - $DeploymentType" -replace '\s{2,}',' '

## Build the Installation Name
If ($ReferredInstallName) {
    [String]$installName = (Remove-InvalidFileNameChars -Name $ReferredInstallName)
}
If (-not $installName) {
    If ($appArch) {
        [String]$installName = $appVendor + '_' + $appName + '_' + $appVersion + '_' + $appArch + '_' + $appLang + '_' + $appRevision
    }
    Else {
        [String]$installName = $appVendor + '_' + $appName + '_' + $appVersion + '_' + $appLang + '_' + $appRevision
    }
}
[String]$installName = (($installName -replace ' ', '').Trim('_') -replace '[_]+', '_')

## Set the Defer History registry path
[String]$regKeyDeferHistory = "$Script:ADT.Config.Toolkit_Options.Toolkit_RegPath\$appDeployToolkitName\DeferHistory\$installName"

## Variables: Log Files
If ($ReferredLogName) {
    [String]$logName = $ReferredLogName
}
If (-not $logName) {
    if ($IsAdmin) {
        [String]$logName = $installName + '_' + $appDeployToolkitName + '_' + $deploymentType + '.log'
    }
    else {
        #  Append the username to the log file name if the toolkit is not running as an administrator, since users do not have the rights to modify files in the ProgramData folder that belong to other users.
        [String]$logName = $installName + '_' + $appDeployToolkitName + '_' + $deploymentType + '_' + (Remove-InvalidFileNameChars -Name $EnvUserName) + '.log'
    }
}
#  If option to compress logs is selected, then log will be created in temp log folder ($logTempFolder) and then copied to actual log folder ($Script:ADT.Config.Toolkit_Options.Toolkit_LogPath) after being zipped.
[String]$logTempFolder = Join-Path -Path $envTemp -ChildPath "${installName}_$deploymentType"
If ($Script:ADT.Config.Toolkit_Options.Toolkit_CompressLogs) {
    #  If the temp log folder already exists from a previous ZIP operation, then delete all files in it to avoid issues
    If (Test-Path -LiteralPath $logTempFolder -PathType 'Container' -ErrorAction 'SilentlyContinue') {
        $null = Remove-Item -LiteralPath $logTempFolder -Recurse -Force -ErrorAction 'SilentlyContinue'
    }
}

## Revert script logging to original setting
. $RevertScriptLogging

## Initialize Logging
$scriptSeparator = '*' * 79
Write-Log -Message ($scriptSeparator, $scriptSeparator) -Source $appDeployToolkitName
Write-Log -Message "[$installName] setup started." -Source $appDeployToolkitName

# Calculate banner height
[Int32]$appDeployLogoBannerHeight = 0
Try {
    [System.Drawing.Bitmap]$appDeployLogoBannerObject = New-Object -TypeName 'System.Drawing.Bitmap' -ArgumentList ($appDeployLogoBanner)
    [Int32]$appDeployLogoBannerHeight = [System.Math]::Ceiling(450 * ($appDeployLogoBannerObject.Height/$appDeployLogoBannerObject.Width))
    If ($appDeployLogoBannerHeight -gt $Script:ADT.Config.BannerIcon_Options.Banner_MaxHeight) {
        $appDeployLogoBannerHeight = $Script:ADT.Config.BannerIcon_Options.Banner_MaxHeight
    }
    $appDeployLogoBannerObject.Dispose() # Must dispose() when installing from local cache or else AppDeployToolkitBanner.png is locked and cannot be removed
}
Catch {
}

## Get the default font to use in the user interface
[System.Drawing.Font]$defaultFont = [System.Drawing.SystemFonts]::MessageBoxFont

## Check how the script was invoked
If ($invokingScript) {
    Write-Log -Message "Script [$scriptPath] dot-source invoked by [$invokingScript]" -Source $appDeployToolkitName
}
Else {
    Write-Log -Message "Script [$scriptPath] invoked directly" -Source $appDeployToolkitName
}

## Evaluate non-default parameters passed to the scripts
If (Test-Path -LiteralPath 'variable:deployAppScriptParameters') {
    [String]$deployAppScriptParameters = ($deployAppScriptParameters.GetEnumerator() | Resolve-Parameters) -join ' '
}
#  Save main script parameters hashtable for async execution of the toolkit
[Hashtable]$appDeployMainScriptAsyncParameters = $appDeployMainScriptParameters
If ($appDeployMainScriptParameters) {
    [String]$appDeployMainScriptParameters = ($appDeployMainScriptParameters.GetEnumerator() | Resolve-Parameters) -join ' '
}
If ($appDeployExtScriptParameters) {
    [String]$appDeployExtScriptParameters = ($appDeployExtScriptParameters.GetEnumerator() | Resolve-Parameters) -join ' '
}

## Check the XML config file version
If ($Script:ADT.Config.Config_File.Config_Version -lt $appDeployMainScriptMinimumConfigVersion) {
    [String]$XMLConfigVersionErr = "The XML configuration file version [$($Script:ADT.Config.Config_File.Config_Version)] is lower than the supported version required by the Toolkit [$appDeployMainScriptMinimumConfigVersion]. Please upgrade the configuration file."
    Write-Log -Message $XMLConfigVersionErr -Severity 3 -Source $appDeployToolkitName
    Throw $XMLConfigVersionErr
}

## Log system/script information
If ((Test-Path -LiteralPath 'variable:appScriptVersion') -and $appScriptVersion) {
    Write-Log -Message "[$installName] script version is [$appScriptVersion]" -Source $appDeployToolkitName
}
If ((Test-Path -LiteralPath 'variable:appScriptDate') -and $appScriptDate) {
    Write-Log -Message "[$installName] script date is [$appScriptDate]" -Source $appDeployToolkitName
}
If ((Test-Path -LiteralPath 'variable:appScriptAuthor') -and $appScriptAuthor) {
    Write-Log -Message "[$installName] script author is [$appScriptAuthor]" -Source $appDeployToolkitName
}
If (Test-Path -LiteralPath 'variable:deployAppScriptFriendlyName') {
    Write-Log -Message "[$deployAppScriptFriendlyName] script version is [$deployAppScriptVersion]" -Source $appDeployToolkitName
}
If (Test-Path -LiteralPath 'variable:deployAppScriptParameters') {
    Write-Log -Message "The following non-default parameters were passed to [$deployAppScriptFriendlyName]: [$deployAppScriptParameters]" -Source $appDeployToolkitName
}
If ($appDeployMainScriptFriendlyName) {
    Write-Log -Message "[$appDeployMainScriptFriendlyName] script version is [$appDeployMainScriptVersion]" -Source $appDeployToolkitName
}
If ($appDeployMainScriptParameters) {
    Write-Log -Message "The following non-default parameters were passed to [$appDeployMainScriptFriendlyName]: [$appDeployMainScriptParameters]" -Source $appDeployToolkitName
}
If ($appDeployExtScriptFriendlyName) {
    Write-Log -Message "[$appDeployExtScriptFriendlyName] version is [$appDeployExtScriptVersion]" -Source $appDeployToolkitName
}
If ($appDeployExtScriptParameters) {
    Write-Log -Message "The following non-default parameters were passed to [$appDeployExtScriptFriendlyName]: [$appDeployExtScriptParameters]" -Source $appDeployToolkitName
}
Write-Log -Message "Computer Name is [$envComputerNameFQDN]" -Source $appDeployToolkitName
Write-Log -Message "Current User is [$ProcessNTAccount]" -Source $appDeployToolkitName
If ($envOSServicePack) {
    Write-Log -Message "OS Version is [$envOSName $envOSServicePack $envOSArchitecture $envOSVersion]" -Source $appDeployToolkitName
}
Else {
    Write-Log -Message "OS Version is [$envOSName $envOSArchitecture $envOSVersion]" -Source $appDeployToolkitName
}
Write-Log -Message "OS Type is [$envOSProductTypeName]" -Source $appDeployToolkitName
Write-Log -Message "Current Culture is [$($culture.Name)], language is [$currentLanguage] and UI language is [$currentUILanguage]" -Source $appDeployToolkitName
Write-Log -Message "Hardware Platform is [$(. $DisableScriptLogging; Get-HardwarePlatform; . $RevertScriptLogging)]" -Source $appDeployToolkitName
Write-Log -Message "PowerShell Host is [$($envHost.Name)] with version [$($envHost.Version)]" -Source $appDeployToolkitName
Write-Log -Message "PowerShell Version is [$envPSVersion $psArchitecture]" -Source $appDeployToolkitName
If ($envPSVersionTable.ContainsKey('CLRVersion')) {
    Write-Log -Message "PowerShell CLR (.NET) version is [$envCLRVersion]" -Source $appDeployToolkitName
}
Write-Log -Message $scriptSeparator -Source $appDeployToolkitName

## Install required assemblies for toast notifications if conditions are right.
If (!$Script:ADT.Config.Toast_Options.Toast_Disable -and $PSVersionTable.PSEdition.Equals('Core') -and !(Get-Package -Name Microsoft.Windows.SDK.NET.Ref -ErrorAction Ignore)) {
    try {
        Write-Log -Message "Installing WinRT assemblies for PowerShell 7 toast notification support. This will take at least 5 minutes, please wait..." -Source $appDeployToolkitName
        Install-Package -Name Microsoft.Windows.SDK.NET.Ref -ProviderName NuGet -Force -Confirm:$false | Out-Null
    }
    catch {
        Write-Log -Message "An error occurred while preparing WinRT assemblies for usage. Toast notifications will not be available for this execution." -Severity 2 -Source $appDeployToolkitName
    }
}

## Set the install phase to asynchronous if the script was not dot sourced, i.e. called with parameters
If ($AsyncToolkitLaunch) {
    $installPhase = 'Asynchronous'
}

## If the ShowInstallationPrompt Parameter is specified, only call that function.
If ($showInstallationPrompt) {
    Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-ShowInstallationPrompt]." -Source $appDeployToolkitName
    $appDeployMainScriptAsyncParameters.Remove('ShowInstallationPrompt')
    $appDeployMainScriptAsyncParameters.Remove('AsyncToolkitLaunch')
    $appDeployMainScriptAsyncParameters.Remove('ReferredInstallName')
    $appDeployMainScriptAsyncParameters.Remove('ReferredInstallTitle')
    $appDeployMainScriptAsyncParameters.Remove('ReferredLogName')
    Show-InstallationPrompt @appDeployMainScriptAsyncParameters
    Exit 0
}

## If the ShowInstallationRestartPrompt Parameter is specified, only call that function.
If ($showInstallationRestartPrompt) {
    Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-ShowInstallationRestartPrompt]." -Source $appDeployToolkitName
    $appDeployMainScriptAsyncParameters.Remove('ShowInstallationRestartPrompt')
    $appDeployMainScriptAsyncParameters.Remove('AsyncToolkitLaunch')
    $appDeployMainScriptAsyncParameters.Remove('ReferredInstallName')
    $appDeployMainScriptAsyncParameters.Remove('ReferredInstallTitle')
    $appDeployMainScriptAsyncParameters.Remove('ReferredLogName')
    Show-InstallationRestartPrompt @appDeployMainScriptAsyncParameters
    Exit 0
}

## If the CleanupBlockedApps Parameter is specified, only call that function.
If ($cleanupBlockedApps) {
    $deployModeSilent = $true
    Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-CleanupBlockedApps]." -Source $appDeployToolkitName
    Unblock-AppExecution
    Exit 0
}

## If the ShowBlockedAppDialog Parameter is specified, only call that function.
If ($showBlockedAppDialog) {
    Try {
        . $DisableScriptLogging
        Write-Log -Message "[$appDeployMainScriptFriendlyName] called with switch [-ShowBlockedAppDialog]." -Source $appDeployToolkitName
        #  Create a mutex and specify a name without acquiring a lock on the mutex
        [Boolean]$showBlockedAppDialogMutexLocked = $false
        [String]$showBlockedAppDialogMutexName = 'Global\PSADT_ShowBlockedAppDialog_Message'
        [Threading.Mutex]$showBlockedAppDialogMutex = New-Object -TypeName 'System.Threading.Mutex' -ArgumentList ($false, $showBlockedAppDialogMutexName)
        #  Attempt to acquire an exclusive lock on the mutex, attempt will fail after 1 millisecond if unable to acquire exclusive lock
        If ((Test-IsMutexAvailable -MutexName $showBlockedAppDialogMutexName -MutexWaitTimeInMilliseconds 1) -and ($showBlockedAppDialogMutex.WaitOne(1))) {
            [Boolean]$showBlockedAppDialogMutexLocked = $true
            Show-InstallationPrompt -Title $installTitle -Message $Script:ADT.Strings.BlockExecution_Message -Icon 'Warning' -ButtonRightText 'OK'
            Exit 0
        }
        Else {
            #  If attempt to acquire an exclusive lock on the mutex failed, then exit script as another blocked app dialog window is already open
            Write-Log -Message "Unable to acquire an exclusive lock on mutex [$showBlockedAppDialogMutexName] because another blocked application dialog window is already open. Exiting script..." -Severity 2 -Source $appDeployToolkitName
            Exit 0
        }
    }
    Catch {
        Write-Log -Message "There was an error in displaying the Installation Prompt. `r`n$(Resolve-Error)" -Severity 3 -Source $appDeployToolkitName
        Exit 60005
    }
    Finally {
        If ($showBlockedAppDialogMutexLocked) {
            $null = $showBlockedAppDialogMutex.ReleaseMutex()
        }
        If ($showBlockedAppDialogMutex) {
            $showBlockedAppDialogMutex.Close()
        }
    }
}

## Log details for all currently logged in users
Write-Log -Message "Display session information for all logged on users: `r`n$($LoggedOnUserSessions | Format-List | Out-String)" -Source $appDeployToolkitName
If ($usersLoggedOn) {
    Write-Log -Message "The following users are logged on to the system: [$($usersLoggedOn -join ', ')]." -Source $appDeployToolkitName

    #  Check if the current process is running in the context of one of the logged in users
    If ($CurrentLoggedOnUserSession) {
        Write-Log -Message "Current process is running with user account [$ProcessNTAccount] under logged in user session for [$($CurrentLoggedOnUserSession.NTAccount)]." -Source $appDeployToolkitName
    }
    Else {
        Write-Log -Message "Current process is running under a system account [$ProcessNTAccount]." -Source $appDeployToolkitName
    }

    # Guard Intune detection code behind a variable.
    If ($Script:ADT.Config.Toolkit_Options.Toolkit_OobeDetection -and ![PSADT.Utilities]::OobeCompleted()) {
        Write-Log -Message "Detected OOBE in progress, changing deployment mode to silent." -Source $appDeployToolkitExtName
        $deployMode = 'Silent'
    }

    #  Display account and session details for the account running as the console user (user with control of the physical monitor, keyboard, and mouse)
    If ($CurrentConsoleUserSession) {
        Write-Log -Message "The following user is the console user [$($CurrentConsoleUserSession.NTAccount)] (user with control of physical monitor, keyboard, and mouse)." -Source $appDeployToolkitName
    }
    Else {
        Write-Log -Message 'There is no console user logged in (user with control of physical monitor, keyboard, and mouse).' -Source $appDeployToolkitName
    }

    #  Display the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account
    If ($RunAsActiveUser) {
        Write-Log -Message "The active logged on user is [$($RunAsActiveUser.NTAccount)]." -Source $appDeployToolkitName
    }
}
Else {
    Write-Log -Message 'No users are logged on to the system.' -Source $appDeployToolkitName
}

## Log which language's UI messages are loaded from the config XML file
If ($HKUPrimaryLanguageShort) {
    Write-Log -Message "The active logged on user [$($RunAsActiveUser.NTAccount)] has a primary UI language of [$HKUPrimaryLanguageShort]." -Source $appDeployToolkitName
}
Else {
    Write-Log -Message "The current system account [$ProcessNTAccount] has a primary UI language of [$currentLanguage]." -Source $appDeployToolkitName
}
If ($Script:ADT.Config.UI_Options.InstallationUI_LanguageOverride) {
    Write-Log -Message "The config XML file was configured to override the detected primary UI language with the following UI language: [$($Script:ADT.Config.UI_Options.InstallationUI_LanguageOverride)]." -Source $appDeployToolkitName
}
Write-Log -Message "The following UI messages were imported from the config XML file: [$xmlUIMessageLanguage]." -Source $appDeployToolkitName

## Log system DPI scale factor of active logged on user
If ($UserDisplayScaleFactor) {
    Write-Log -Message "The active logged on user [$($RunAsActiveUser.NTAccount)] has a DPI scale factor of [$dpiScale] with DPI pixels [$dpiPixels]." -Source $appDeployToolkitName
}
Else {
    Write-Log -Message "The system has a DPI scale factor of [$dpiScale] with DPI pixels [$dpiPixels]." -Source $appDeployToolkitName
}

## Check if script is running from a SCCM Task Sequence
Try {
    [__ComObject]$SMSTSEnvironment = New-Object -ComObject 'Microsoft.SMS.TSEnvironment' -ErrorAction 'Stop'
    Write-Log -Message 'Successfully loaded COM Object [Microsoft.SMS.TSEnvironment]. Therefore, script is currently running from a SCCM Task Sequence.' -Source $appDeployToolkitName
    $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($SMSTSEnvironment)
    $runningTaskSequence = $true
}
Catch {
    Write-Log -Message 'Unable to load COM Object [Microsoft.SMS.TSEnvironment]. Therefore, script is not currently running from a SCCM Task Sequence.' -Source $appDeployToolkitName
    $runningTaskSequence = $false
}

## Check to see if the Task Scheduler service is in a healthy state by checking its services to see if they exist, are currently running, and have a start mode of 'Automatic'.
## The task scheduler service and the services it is dependent on can/should only be started/stopped/modified when running in the SYSTEM context.
[Boolean]$IsTaskSchedulerHealthy = $true
If ($IsLocalSystemAccount) {
    #  Check the health of the 'Task Scheduler' service
    Try {
        If (Test-ServiceExists -Name 'Schedule' -ContinueOnError $false) {
            If ((Get-ServiceStartMode -Name 'Schedule' -ContinueOnError $false) -ne 'Automatic') {
                Set-ServiceStartMode -Name 'Schedule' -StartMode 'Automatic' -ContinueOnError $false
            }
            Start-ServiceAndDependencies -Name 'Schedule' -SkipServiceExistsTest -ContinueOnError $false
        }
        Else {
            [Boolean]$IsTaskSchedulerHealthy = $false
        }
    }
    Catch {
        [Boolean]$IsTaskSchedulerHealthy = $false
    }
    #  Log the health of the 'Task Scheduler' service
    Write-Log -Message "The task scheduler service is in a healthy state: $IsTaskSchedulerHealthy." -Source $appDeployToolkitName
}
Else {
    Write-Log -Message "Skipping attempt to check for and make the task scheduler services healthy, because the App Deployment Toolkit is not running under the [$LocalSystemNTAccount] account." -Source $appDeployToolkitName
}

## If script is running in session zero
If ($SessionZero) {
    ##  If the script was launched with deployment mode set to NonInteractive, then continue
    If ($deployMode -eq 'NonInteractive') {
        Write-Log -Message "Session 0 detected but deployment mode was manually set to [$deployMode]." -Source $appDeployToolkitName
    }
    ElseIf ($Script:ADT.Config.Toolkit_Options.Toolkit_SessionDetection) {
        ##  If the process is not able to display a UI, enable NonInteractive mode
        If (-not $IsProcessUserInteractive) {
            $deployMode = 'NonInteractive'
            Write-Log -Message "Session 0 detected, process not running in user interactive mode; deployment mode set to [$deployMode]." -Source $appDeployToolkitName
        }
        Else {
            If (-not $usersLoggedOn) {
                $deployMode = 'NonInteractive'
                Write-Log -Message "Session 0 detected, process running in user interactive mode, no users logged in; deployment mode set to [$deployMode]." -Source $appDeployToolkitName
            }
            Else {
                Write-Log -Message 'Session 0 detected, process running in user interactive mode, user(s) logged in.' -Source $appDeployToolkitName
            }
        }
    }
    Else {
        Write-Log -Message "Session 0 detected but toolkit configured to not adjust deployment mode." -Source $appDeployToolkitName
    }
}
Else {
    Write-Log -Message 'Session 0 not detected.' -Source $appDeployToolkitName
}

## Set Deploy Mode switches
If ($deployMode) {
    Write-Log -Message "Installation is running in [$deployMode] mode." -Source $appDeployToolkitName
}
Switch ($deployMode) {
    'Silent' {
        $deployModeNonInteractive = $true; $deployModeSilent = $true
    }
    'NonInteractive' {
        $deployModeNonInteractive = $true; $deployModeSilent = $true
    }
    Default {
        $deployModeNonInteractive = $false; $deployModeSilent = $false
    }
}

## Check deployment type (install/uninstall)
Switch ($deploymentType) {
    'Install' {
        $deploymentTypeName = $Script:ADT.Strings.DeploymentType_Install
    }
    'Uninstall' {
        $deploymentTypeName = $Script:ADT.Strings.DeploymentType_UnInstall
    }
    'Repair' {
        $deploymentTypeName = $Script:ADT.Strings.DeploymentType_Repair
    }
    Default {
        $deploymentTypeName = $Script:ADT.Strings.DeploymentType_Install
    }
}
If ($deploymentTypeName) {
    Write-Log -Message "Deployment type is [$deploymentTypeName]." -Source $appDeployToolkitName
}

If ($useDefaultMsi) {
    Write-Log -Message "Discovered Zero-Config MSI installation file [$defaultMsiFile]." -Source $appDeployToolkitName
}

## Check current permissions and exit if not running with Administrator rights
If ($Script:ADT.Config.Toolkit_Options.Toolkit_RequireAdmin) {
    #  Check if the current process is running with elevated administrator permissions
    If ((-not $IsAdmin) -and (-not $ShowBlockedAppDialog)) {
        [String]$AdminPermissionErr = "[$appDeployToolkitName] has an XML config file option [Toolkit_RequireAdmin] set to [True] so as to require Administrator rights for the toolkit to function. Please re-run the deployment script as an Administrator or change the option in the XML config file to not require Administrator rights."
        Write-Log -Message $AdminPermissionErr -Severity 3 -Source $appDeployToolkitName
        Show-DialogBox -Text $AdminPermissionErr -Icon 'Stop'
        Throw $AdminPermissionErr
    }
}

## If terminal server mode was specified, change the installation mode to support it
If ($terminalServerMode) {
    Enable-TerminalServerInstallMode
}

## If not in install phase Asynchronous, change the install phase so we dont have Initialization phase when we are done initializing
## This should get overwritten shortly, unless this is not dot sourced by Deploy-Application.ps1
If (-not $AsyncToolkitLaunch) {
    $installPhase = 'Execution'
}

#endregion
##*=============================================
##* END SCRIPT BODY
##*=============================================
