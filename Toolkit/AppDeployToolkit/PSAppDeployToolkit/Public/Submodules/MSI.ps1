#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Execute-MSI {
    <#
.SYNOPSIS

Executes msiexec.exe to perform the following actions for MSI & MSP files and MSI product codes: install, uninstall, patch, repair, active setup.

.DESCRIPTION

Executes msiexec.exe to perform the following actions for MSI & MSP files and MSI product codes: install, uninstall, patch, repair, active setup.

If the -Action parameter is set to "Install" and the MSI is already installed, the function will exit.

Sets default switches to be passed to msiexec based on the preferences in the XML configuration file.

Automatically generates a log file name and creates a verbose log file for all msiexec operations.

Expects the MSI or MSP file to be located in the "Files" sub directory of the App Deploy Toolkit. Expects transform files to be in the same directory as the MSI file.

.PARAMETER Action

The action to perform. Options: Install, Uninstall, Patch, Repair, ActiveSetup.

.PARAMETER Path

The path to the MSI/MSP file or the product code of the installed MSI.

.PARAMETER Transform

The name of the transform file(s) to be applied to the MSI. The transform file is expected to be in the same directory as the MSI file. Multiple transforms have to be separated by a semi-colon.

.PARAMETER Patch

The name of the patch (msp) file(s) to be applied to the MSI for use with the "Install" action. The patch file is expected to be in the same directory as the MSI file. Multiple patches have to be separated by a semi-colon.

.PARAMETER Parameters

Overrides the default parameters specified in the XML configuration file. Install default is: "REBOOT=ReallySuppress /QB!". Uninstall default is: "REBOOT=ReallySuppress /QN".

.PARAMETER AddParameters

Adds to the default parameters specified in the XML configuration file. Install default is: "REBOOT=ReallySuppress /QB!". Uninstall default is: "REBOOT=ReallySuppress /QN".

.PARAMETER SecureParameters

Hides all parameters passed to the MSI or MSP file from the toolkit Log file.

.PARAMETER LoggingOptions

Overrides the default logging options specified in the XML configuration file. Default options are: "/L*v".

.PARAMETER private:LogName

Overrides the default log file name. The default log file name is generated from the MSI file name. If LogName does not end in .log, it will be automatically appended.

For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

.PARAMETER LogName

Overrides the default log file name. The default log file name is generated from the MSI file name. If LogName does not end in .log, it will be automatically appended.

For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

.PARAMETER WorkingDirectory

Overrides the working directory. The working directory is set to the location of the MSI file.

.PARAMETER SkipMSIAlreadyInstalledCheck

Skips the check to determine if the MSI is already installed on the system. Default is: $false.

.PARAMETER IncludeUpdatesAndHotfixes

Include matches against updates and hotfixes in results.

.PARAMETER NoWait

Immediately continue after executing the process.

.PARAMETER PassThru

Returns ExitCode, STDOut, and STDErr output from the process.

.PARAMETER IgnoreExitCodes

List the exit codes to ignore or * to ignore all exit codes.

.PARAMETER PriorityClass

Specifies priority class for the process. Options: Idle, Normal, High, AboveNormal, BelowNormal, RealTime. Default: Normal

.PARAMETER ExitOnProcessFailure

Specifies whether the function should call Exit-Script when the process returns an exit code that is considered an error/failure. Default: $true

.PARAMETER RepairFromSource

Specifies whether we should repair from source. Also rewrites local cache. Default: $false

.PARAMETER ContinueOnError

Continue if an error occurred while trying to start the process. Default: $false.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

PSObject

Returns a PSObject with the results of the installation
- ExitCode
- STDOut
- STDErr

.EXAMPLE

Execute-MSI -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi'

Installs an MSI

.EXAMPLE

Execute-MSI -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -Transform 'Adobe_FlashPlayer_11.2.202.233_x64_EN_01.mst' -Parameters '/QN'

Installs an MSI, applying a transform and overriding the default MSI toolkit parameters

.EXAMPLE

[PSObject]$ExecuteMSIResult = Execute-MSI -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -PassThru

Installs an MSI and stores the result of the execution into a variable by using the -PassThru option

.EXAMPLE

Execute-MSI -Action 'Uninstall' -Path '{26923b43-4d38-484f-9b9e-de460746276c}'

Uninstalls an MSI using a product code

.EXAMPLE

Execute-MSI -Action 'Patch' -Path 'Adobe_Reader_11.0.3_EN.msp'

Installs an MSP

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateSet('Install', 'Uninstall', 'Patch', 'Repair', 'ActiveSetup')]
        [String]$Action = 'Install',
        [Parameter(Mandatory = $true, HelpMessage = 'Please enter either the path to the MSI/MSP file or the ProductCode')]
        [ValidateScript({ ($_ -match $Script:ADT.Environment.MSIProductCodeRegExPattern) -or ('.msi', '.msp' -contains [IO.Path]::GetExtension($_)) })]
        [Alias('FilePath')]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Transform,
        [Parameter(Mandatory = $false)]
        [Alias('Arguments')]
        [ValidateNotNullorEmpty()]
        [String]$Parameters,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$AddParameters,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$SecureParameters = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Patch,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$LoggingOptions,
        [Parameter(Mandatory = $false)]
        [Alias('LogName')]
        [String]$private:LogName,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$WorkingDirectory,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$SkipMSIAlreadyInstalledCheck = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$IncludeUpdatesAndHotfixes = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$NoWait = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$PassThru = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$IgnoreExitCodes,
        [Parameter(Mandatory = $false)]
        [ValidateSet('Idle', 'Normal', 'High', 'AboveNormal', 'BelowNormal', 'RealTime')]
        [Diagnostics.ProcessPriorityClass]$PriorityClass = 'Normal',
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ExitOnProcessFailure = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$RepairFromSource = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## Initialize variable indicating whether $Path variable is a Product Code or not
        [Boolean]$PathIsProductCode = $false
        [String[]]$transforms = $null

        ## If the path matches a product code
        If ($Path -match $Script:ADT.Environment.MSIProductCodeRegExPattern) {
            #  Set variable indicating that $Path variable is a Product Code
            [Boolean]$PathIsProductCode = $true

            #  Resolve the product code to a publisher, application name, and version
            Write-Log -Message 'Resolving product code to a publisher, application name, and version.' -Source ${CmdletName}

            If ($IncludeUpdatesAndHotfixes) {
                [PSObject]$productCodeNameVersion = Get-InstalledApplication -ProductCode $path -IncludeUpdatesAndHotfixes | Select-Object -Property 'Publisher', 'DisplayName', 'DisplayVersion' -First 1 -ErrorAction 'SilentlyContinue'
            }
            Else {
                [PSObject]$productCodeNameVersion = Get-InstalledApplication -ProductCode $path | Select-Object -Property 'Publisher', 'DisplayName', 'DisplayVersion' -First 1 -ErrorAction 'SilentlyContinue'
            }

            #  Build the log file name
            If (-not $LogName) {
                If ($productCodeNameVersion) {
                    If ($productCodeNameVersion.Publisher) {
                        $LogName = (Remove-InvalidFileNameChars -Name ($productCodeNameVersion.Publisher + '_' + $productCodeNameVersion.DisplayName + '_' + $productCodeNameVersion.DisplayVersion)) -replace ' ', ''
                    }
                    Else {
                        $LogName = (Remove-InvalidFileNameChars -Name ($productCodeNameVersion.DisplayName + '_' + $productCodeNameVersion.DisplayVersion)) -replace ' ', ''
                    }
                }
                Else {
                    #  Out of other options, make the Product Code the name of the log file
                    $LogName = $Path
                }
            }
        }
        Else {
            #  Get the log file name without file extension
            If (-not $LogName) {
                $LogName = ([IO.FileInfo]$path).BaseName
            }
            ElseIf ('.log', '.txt' -contains [IO.Path]::GetExtension($LogName)) {
                While ('.log', '.txt' -contains [IO.Path]::GetExtension($LogName)) {
                    $LogName = [IO.Path]::GetFileNameWithoutExtension($LogName)
                }
            }
        }

        If ($Script:ADT.Config.Toolkit_Options.Toolkit_CompressLogs) {
            ## Build the log file path
            [String]$logPath = Join-Path -Path $logTempFolder -ChildPath $LogName
        }
        Else {
            ## Create the Log directory if it doesn't already exist
            If (-not (Test-Path -LiteralPath $Script:ADT.Config.MSI_Options.MSI_LogPath -PathType 'Container' -ErrorAction 'SilentlyContinue')) {
                $null = New-Item -Path $Script:ADT.Config.MSI_Options.MSI_LogPath -ItemType 'Directory' -ErrorAction 'SilentlyContinue'
            }
            ## Build the log file path
            [String]$logPath = Join-Path -Path $Script:ADT.Config.MSI_Options.MSI_LogPath -ChildPath $LogName
        }

        ## Set the installation Parameters
        If ($Script:ADT.CurrentSession.Session.State.DeployModeSilent) {
            $msiInstallDefaultParams = $Script:ADT.Config.MSI_Options.MSI_SilentParams
            $msiUninstallDefaultParams = $Script:ADT.Config.MSI_Options.MSI_SilentParams
        }
        Else {
            $msiInstallDefaultParams = $Script:ADT.Config.MSI_Options.MSI_InstallParams
            $msiUninstallDefaultParams = $Script:ADT.Config.MSI_Options.MSI_UninstallParams
        }

        ## Build the MSI Parameters
        Switch ($action) {
            'Install' {
                $option = '/i'; [String]$msiLogFile = "$logPath" + '_Install'; $msiDefaultParams = $msiInstallDefaultParams
            }
            'Uninstall' {
                $option = '/x'; [String]$msiLogFile = "$logPath" + '_Uninstall'; $msiDefaultParams = $msiUninstallDefaultParams
            }
            'Patch' {
                $option = '/update'; [String]$msiLogFile = "$logPath" + '_Patch'; $msiDefaultParams = $msiInstallDefaultParams
            }
            'Repair' {
                $option = '/f'; If ($RepairFromSource) {
                    $option += 'vomus'
                } [String]$msiLogFile = "$logPath" + '_Repair'; $msiDefaultParams = $msiInstallDefaultParams
            }
            'ActiveSetup' {
                $option = '/fups'; [String]$msiLogFile = "$logPath" + '_ActiveSetup'
            }
        }

        ## Append ".log" to the MSI logfile path and enclose in quotes
        If ([IO.Path]::GetExtension($msiLogFile) -ne '.log') {
            [String]$msiLogFile = $msiLogFile + '.log'
            [String]$msiLogFile = "`"$msiLogFile`""
        }

        ## If the MSI is in the Files directory, set the full path to the MSI
        If (Test-Path -LiteralPath (Join-Path -Path $Script:ADT.CurrentSession.GetPropertyValue('dirFiles') -ChildPath $path -ErrorAction 'SilentlyContinue') -PathType 'Leaf' -ErrorAction 'SilentlyContinue') {
            [String]$msiFile = Join-Path -Path $Script:ADT.CurrentSession.GetPropertyValue('dirFiles') -ChildPath $path
        }
        ElseIf (Test-Path -LiteralPath $Path -ErrorAction 'SilentlyContinue') {
            [String]$msiFile = (Get-Item -LiteralPath $Path).FullName
        }
        ElseIf ($PathIsProductCode) {
            [String]$msiFile = $Path
        }
        Else {
            Write-Log -Message "Failed to find MSI file [$path]." -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to find MSI file [$path]."
            }
            Continue
        }

        ## Set the working directory of the MSI
        If ((-not $PathIsProductCode) -and (-not $workingDirectory)) {
            [String]$workingDirectory = Split-Path -Path $msiFile -Parent
        }

        ## Enumerate all transforms specified, qualify the full path if possible and enclose in quotes
        If ($transform) {
            [String[]]$transforms = $transform -replace "`"", '' -split ';'
            For ($i = 0; $i -lt $transforms.Length; $i++) {
                [String]$FullPath = $null
                [String]$FullPath = Join-Path -Path (Split-Path -Path $msiFile -Parent) -ChildPath $transforms[$i].Replace('.\', '')
                If ($FullPath -and (Test-Path -LiteralPath $FullPath -PathType 'Leaf')) {
                    $transforms[$i] = $FullPath
                }
            }
            [String]$mstFile = "`"$($transforms -join ';')`""
        }

        ## Enumerate all patches specified, qualify the full path if possible and enclose in quotes
        If ($patch) {
            [String[]]$patches = $patch -replace "`"", '' -split ';'
            For ($i = 0; $i -lt $patches.Length; $i++) {
                [String]$FullPath = $null
                [String]$FullPath = Join-Path -Path (Split-Path -Path $msiFile -Parent) -ChildPath $patches[$i].Replace('.\', '')
                If ($FullPath -and (Test-Path -LiteralPath $FullPath -PathType 'Leaf')) {
                    $patches[$i] = $FullPath
                }
            }
            [String]$mspFile = "`"$($patches -join ';')`""
        }

        ## Get the ProductCode of the MSI
        [String]$MSIProductCode = If ($PathIsProductCode) {
            $path
        }
        ElseIf ([IO.Path]::GetExtension($msiFile) -eq '.msi') {
            Try {
                [Hashtable]$GetMsiTablePropertySplat = @{ Path = $msiFile; Table = 'Property'; ContinueOnError = $false }
                If ($transforms) {
                    $GetMsiTablePropertySplat.Add( 'TransformPath', $transforms )
                }
                Get-MsiTableProperty @GetMsiTablePropertySplat | Select-Object -ExpandProperty 'ProductCode' -ErrorAction 'Stop'
            }
            Catch {
                Write-Log -Message "Failed to get the ProductCode from the MSI file. Continue with requested action [$Action]..." -Source ${CmdletName}
            }
        }

        ## Enclose the MSI file in quotes to avoid issues with spaces when running msiexec
        [String]$msiFile = "`"$msiFile`""

        ## Start building the MsiExec command line starting with the base action and file
        [String]$argsMSI = "$option $msiFile"
        #  Add MST
        If ($transform) {
            $argsMSI = "$argsMSI TRANSFORMS=$mstFile TRANSFORMSSECURE=1"
        }
        #  Add MSP
        If ($patch) {
            $argsMSI = "$argsMSI PATCH=$mspFile"
        }
        #  Replace default parameters if specified.
        If ($Parameters) {
            $argsMSI = "$argsMSI $Parameters"
        }
        Else {
            $argsMSI = "$argsMSI $msiDefaultParams"
        }
        #  Add reinstallmode and reinstall variable for Patch
        If ($action -eq 'Patch') {
            $argsMSI += ' REINSTALLMODE=ecmus REINSTALL=ALL'
        }
        #  Append parameters to default parameters if specified.
        If ($AddParameters) {
            $argsMSI = "$argsMSI $AddParameters"
        }
        #  Add custom Logging Options if specified, otherwise, add default Logging Options from Config file
        If ($LoggingOptions) {
            $argsMSI = "$argsMSI $LoggingOptions $msiLogFile"
        }
        Else {
            $argsMSI = "$argsMSI $($Script:ADT.Config.MSI_Options.MSI_LoggingOptions) $msiLogFile"
        }

        ## Check if the MSI is already installed. If no valid ProductCode to check or SkipMSIAlreadyInstalledCheck supplied, then continue with requested MSI action.
        [Boolean]$IsMsiInstalled = If ($MSIProductCode -and -not $SkipMSIAlreadyInstalledCheck) {
            If ($IncludeUpdatesAndHotfixes) {
                !!(Get-InstalledApplication -ProductCode $MSIProductCode -IncludeUpdatesAndHotfixes)
            }
            Else {
                !!(Get-InstalledApplication -ProductCode $MSIProductCode)
            }
        }
        Else {
            $Action -ne 'Install'
        }

        If (($IsMsiInstalled) -and ($Action -eq 'Install')) {
            Write-Log -Message "The MSI is already installed on this system. Skipping action [$Action]..." -Source ${CmdletName}
            [PSObject]$ExecuteResults = @{ ExitCode = 1638; StdOut = 0; StdErr = '' }
        }
        ElseIf (((-not $IsMsiInstalled) -and ($Action -eq 'Install')) -or ($IsMsiInstalled)) {
            Write-Log -Message "Executing MSI action [$Action]..." -Source ${CmdletName}
            #  Build the hashtable with the options that will be passed to Execute-Process using splatting
            [Hashtable]$ExecuteProcessSplat = @{
                Path                 = $Script:ADT.Environment.exeMsiexec
                Parameters           = $argsMSI
                WindowStyle          = 'Normal'
                ExitOnProcessFailure = $ExitOnProcessFailure
                ContinueOnError      = $ContinueOnError
            }
            If ($WorkingDirectory) {
                $ExecuteProcessSplat.Add( 'WorkingDirectory', $WorkingDirectory)
            }
            If ($SecureParameters) {
                $ExecuteProcessSplat.Add( 'SecureParameters', $SecureParameters)
            }
            If ($PassThru) {
                $ExecuteProcessSplat.Add( 'PassThru', $PassThru)
            }
            If ($IgnoreExitCodes) {
                $ExecuteProcessSplat.Add( 'IgnoreExitCodes', $IgnoreExitCodes)
            }
            If ($PriorityClass) {
                $ExecuteProcessSplat.Add( 'PriorityClass', $PriorityClass)
            }
            If ($NoWait) {
                $ExecuteProcessSplat.Add( 'NoWait', $NoWait)
            }

            #  Call the Execute-Process function
            If ($PassThru) {
                [PSObject]$ExecuteResults = Execute-Process @ExecuteProcessSplat
            }
            Else {
                Execute-Process @ExecuteProcessSplat
            }
            #  Refresh environment variables for Windows Explorer process as Windows does not consistently update environment variables created by MSIs
            Update-Desktop
        }
        Else {
            Write-Log -Message "The MSI is not installed on this system. Skipping action [$Action]..." -Source ${CmdletName}
        }
    }
    End {
        If ($PassThru) {
            Write-Output -InputObject ($ExecuteResults)
        }
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Execute-MSP {
    <#
.SYNOPSIS

Executes an MSP file using the same logic as Execute-MSI.

.DESCRIPTION

Reads SummaryInfo targeted product codes in MSP file and determines if the MSP file applies to any installed products
If a valid installed product is found, triggers the Execute-MSI function to patch the installation.
Uses default config MSI parameters. You can use -AddParameters to add additional parameters.

.PARAMETER Path

Path to the msp file

.PARAMETER AddParameters

Additional parameters

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Execute-MSP -Path 'Adobe_Reader_11.0.3_EN.msp'

.EXAMPLE

Execute-MSP -Path 'AcroRdr2017Upd1701130143_MUI.msp' -AddParameters 'ALLUSERS=1'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, HelpMessage = 'Please enter the path to the MSP file')]
        [ValidateScript({ ('.msp' -contains [IO.Path]::GetExtension($_)) })]
        [Alias('FilePath')]
        [String]$Path,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$AddParameters
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## If the MSP is in the Files directory, set the full path to the MSP
        If (Test-Path -LiteralPath (Join-Path -Path $($Script:ADT.CurrentSession.GetPropertyValue('dirFiles')) -ChildPath $path -ErrorAction 'SilentlyContinue') -PathType 'Leaf' -ErrorAction 'SilentlyContinue') {
            [String]$mspFile = Join-Path -Path $($Script:ADT.CurrentSession.GetPropertyValue('dirFiles')) -ChildPath $path
        }
        ElseIf (Test-Path -LiteralPath $Path -ErrorAction 'SilentlyContinue') {
            [String]$mspFile = (Get-Item -LiteralPath $Path).FullName
        }
        Else {
            Write-Log -Message "Failed to find MSP file [$path]." -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to find MSP file [$path]."
            }
            Continue
        }
        Write-Log -Message 'Checking MSP file for valid product codes.' -Source ${CmdletName}

        [Boolean]$IsMSPNeeded = $false

        ## Create a Windows Installer object
        [__ComObject]$Installer = New-Object -ComObject 'WindowsInstaller.Installer' -ErrorAction 'Stop'

        ## Define properties for how the MSI database is opened
        [Int32]$msiOpenDatabaseModePatchFile = 32
        [Int32]$msiOpenDatabaseMode = $msiOpenDatabaseModePatchFile
        ## Open database in read only mode
        [__ComObject]$Database = Invoke-ObjectMethod -InputObject $Installer -MethodName 'OpenDatabase' -ArgumentList @($mspFile, $msiOpenDatabaseMode)
        ## Get the SummaryInformation from the windows installer database
        [__ComObject]$SummaryInformation = Get-ObjectProperty -InputObject $Database -PropertyName 'SummaryInformation'
        [Hashtable]$SummaryInfoProperty = @{}
        $AllTargetedProductCodes = (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(7)).Split(';')
        ForEach ($FormattedProductCode in $AllTargetedProductCodes) {
            [PSObject]$MSIInstalled = Get-InstalledApplication -ProductCode $FormattedProductCode
            If ($MSIInstalled) {
                [Boolean]$IsMSPNeeded = $true
            }
        }
        Try {
            $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($SummaryInformation)
        }
        Catch {
        }
        Try {
            $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Database)
        }
        Catch {
        }
        Try {
            $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Installer)
        }
        Catch {
        }
        If ($IsMSPNeeded) {
            If ($AddParameters) {
                Execute-MSI -Action 'Patch' -Path $Path -AddParameters $AddParameters
            }
            Else {
                Execute-MSI -Action 'Patch' -Path $Path
            }
        }
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Remove-MSIApplications {
    <#
.SYNOPSIS

Removes all MSI applications matching the specified application name.

.DESCRIPTION

Removes all MSI applications matching the specified application name.
Enumerates the registry for installed applications matching the specified application name and uninstalls that application using the product code, provided the uninstall string matches "msiexec".

.PARAMETER Name

The name of the application to uninstall. Performs a contains match on the application display name by default.

.PARAMETER Exact

Specifies that the named application must be matched using the exact name.

.PARAMETER WildCard

Specifies that the named application must be matched using a wildcard search.

.PARAMETER Parameters

Overrides the default parameters specified in the XML configuration file. Uninstall default is: "REBOOT=ReallySuppress /QN".

.PARAMETER AddParameters

Adds to the default parameters specified in the XML configuration file. Uninstall default is: "REBOOT=ReallySuppress /QN".

.PARAMETER FilterApplication

Two-dimensional array that contains one or more (property, value, match-type) sets that should be used to filter the list of results returned by Get-InstalledApplication to only those that should be uninstalled.
Properties that can be filtered upon: ProductCode, DisplayName, DisplayVersion, UninstallString, InstallSource, InstallLocation, InstallDate, Publisher, Is64BitApplication

.PARAMETER ExcludeFromUninstall

Two-dimensional array that contains one or more (property, value, match-type) sets that should be excluded from uninstall if found.
Properties that can be excluded: ProductCode, DisplayName, DisplayVersion, UninstallString, InstallSource, InstallLocation, InstallDate, Publisher, Is64BitApplication

.PARAMETER IncludeUpdatesAndHotfixes

Include matches against updates and hotfixes in results.

.PARAMETER LoggingOptions

Overrides the default logging options specified in the XML configuration file. Default options are: "/L*v".

.PARAMETER private:LogName

Overrides the default log file name. The default log file name is generated from the MSI file name. If LogName does not end in .log, it will be automatically appended.
For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

.PARAMETER LogName

Overrides the default log file name. The default log file name is generated from the MSI file name. If LogName does not end in .log, it will be automatically appended.
For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

.PARAMETER PassThru

Returns ExitCode, STDOut, and STDErr output from the process.

.PARAMETER ContinueOnError

Continue if an error occured while trying to start the processes. Default: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

PSObject

Returns an object with the following properties:
- ExitCode
- StdOut
- StdErr

.EXAMPLE

Remove-MSIApplications -Name 'Adobe Flash'

Removes all versions of software that match the name "Adobe Flash"

.EXAMPLE

Remove-MSIApplications -Name 'Adobe'

Removes all versions of software that match the name "Adobe"

.EXAMPLE

Remove-MSIApplications -Name 'Java 8 Update' -FilterApplication @(
        @('Is64BitApplication', $false, 'Exact'),
        @('Publisher', 'Oracle Corporation', 'Exact')
    )

Removes all versions of software that match the name "Java 8 Update" where the software is 32-bits and the publisher is "Oracle Corporation".

.EXAMPLE

Remove-MSIApplications -Name 'Java 8 Update' -FilterApplication @(, @('Publisher', 'Oracle Corporation', 'Exact')) -ExcludeFromUninstall @(, @('DisplayName', 'Java 8 Update 45', 'Contains'))

Removes all versions of software that match the name "Java 8 Update" and also have "Oracle Corporation" as the Publisher; however, it does not uninstall "Java 8 Update 45" of the software.
NOTE: If only specifying a single row in the two-dimensional arrays, the array must have the extra parentheses and leading comma as in this example.

.EXAMPLE

Remove-MSIApplications -Name 'Java 8 Update' -ExcludeFromUninstall @(, @('DisplayName', 'Java 8 Update 45', 'Contains'))

Removes all versions of software that match the name "Java 8 Update"; however, it does not uninstall "Java 8 Update 45" of the software.
NOTE: If only specifying a single row in the two-dimensional array, the array must have the extra parentheses and leading comma as in this example.

.EXAMPLE

Remove-MSIApplications -Name 'Java 8 Update' -ExcludeFromUninstall @(
    @('Is64BitApplication', $true, 'Exact'),
    @('DisplayName', 'Java 8 Update 45', 'Exact'),
    @('DisplayName', 'Java 8 Update 4*', 'WildCard'),
    @('DisplayName', 'Java \d Update \d{3}', 'RegEx'),
    @('DisplayName', 'Java 8 Update', 'Contains'))

Removes all versions of software that match the name "Java 8 Update"; however, it does not uninstall 64-bit versions of the software, Update 45 of the software, or any Update that starts with 4.

.NOTES

More reading on how to create arrays if having trouble with -FilterApplication or -ExcludeFromUninstall parameter: http://blogs.msdn.com/b/powershell/archive/2007/01/23/array-literals-in-powershell.aspx

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Name,
        [Parameter(Mandatory = $false)]
        [Switch]$Exact = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$WildCard = $false,
        [Parameter(Mandatory = $false)]
        [Alias('Arguments')]
        [ValidateNotNullorEmpty()]
        [String]$Parameters,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$AddParameters,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Array]$FilterApplication = @(@()),
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Array]$ExcludeFromUninstall = @(@()),
        [Parameter(Mandatory = $false)]
        [Switch]$IncludeUpdatesAndHotfixes = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$LoggingOptions,
        [Parameter(Mandatory = $false)]
        [Alias('LogName')]
        [String]$private:LogName,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$PassThru = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        ## Build the hashtable with the options that will be passed to Get-InstalledApplication using splatting
        [Hashtable]$GetInstalledApplicationSplat = @{ Name = $name }
        If ($Exact) {
            $GetInstalledApplicationSplat.Add( 'Exact', $Exact)
        }
        ElseIf ($WildCard) {
            $GetInstalledApplicationSplat.Add( 'WildCard', $WildCard)
        }
        If ($IncludeUpdatesAndHotfixes) {
            $GetInstalledApplicationSplat.Add( 'IncludeUpdatesAndHotfixes', $IncludeUpdatesAndHotfixes)
        }

        [PSObject[]]$installedApplications = Get-InstalledApplication @GetInstalledApplicationSplat

        Write-Log -Message "Found [$($installedApplications.Count)] application(s) that matched the specified criteria [$Name]." -Source ${CmdletName}

        ## Filter the results from Get-InstalledApplication
        [Collections.ArrayList]$removeMSIApplications = New-Object -TypeName 'System.Collections.ArrayList'
        If (($null -ne $installedApplications) -and ($installedApplications.Count)) {
            ForEach ($installedApplication in $installedApplications) {
                If ([String]::IsNullOrEmpty($installedApplication.ProductCode)) {
                    Write-Log -Message "Skipping removal of application [$($installedApplication.DisplayName)] because unable to discover MSI ProductCode from application's registry Uninstall subkey [$($installedApplication.UninstallSubkey)]." -Severity 2 -Source ${CmdletName}
                    Continue
                }

                #  Filter the results from Get-InstalledApplication to only those that should be uninstalled
                [Boolean]$addAppToRemoveList = $true
                If (($null -ne $FilterApplication) -and ($FilterApplication.Count)) {
                    Write-Log -Message 'Filter the results to only those that should be uninstalled as specified in parameter [-FilterApplication].' -Source ${CmdletName}
                    ForEach ($Filter in $FilterApplication) {
                        If ($Filter[2] -eq 'RegEx') {
                            If ($installedApplication.($Filter[0]) -match $Filter[1]) {
                                [Boolean]$addAppToRemoveList = $true
                                Write-Log -Message "Preserve removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of regex match against [-FilterApplication] criteria." -Source ${CmdletName}
                            }
                            Else {
                                [Boolean]$addAppToRemoveList = $false
                                Break
                            }
                        }
                        ElseIf ($Filter[2] -eq 'Contains') {
                            If ($installedApplication.($Filter[0]) -match [RegEx]::Escape($Filter[1])) {
                                [Boolean]$addAppToRemoveList = $true
                                Write-Log -Message "Preserve removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of contains match against [-FilterApplication] criteria." -Source ${CmdletName}
                            }
                            Else {
                                [Boolean]$addAppToRemoveList = $false
                                Break
                            }
                        }
                        ElseIf ($Filter[2] -eq 'WildCard') {
                            If ($installedApplication.($Filter[0]) -like $Filter[1]) {
                                [Boolean]$addAppToRemoveList = $true
                                Write-Log -Message "Preserve removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of wildcard match against [-FilterApplication] criteria." -Source ${CmdletName}
                            }
                            Else {
                                [Boolean]$addAppToRemoveList = $false
                                Break
                            }
                        }
                        ElseIf ($Filter[2] -eq 'Exact') {
                            If ($installedApplication.($Filter[0]) -eq $Filter[1]) {
                                [Boolean]$addAppToRemoveList = $true
                                Write-Log -Message "Preserve removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of exact match against [-FilterApplication] criteria." -Source ${CmdletName}
                            }
                            Else {
                                [Boolean]$addAppToRemoveList = $false
                                Break
                            }
                        }
                    }
                }

                #  Filter the results from Get-InstalledApplication to remove those that should never be uninstalled
                If (($null -ne $ExcludeFromUninstall) -and ($ExcludeFromUninstall.Count)) {
                    ForEach ($Exclude in $ExcludeFromUninstall) {
                        If ($Exclude[2] -eq 'RegEx') {
                            If ($installedApplication.($Exclude[0]) -match $Exclude[1]) {
                                [Boolean]$addAppToRemoveList = $false
                                Write-Log -Message "Skipping removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of regex match against [-ExcludeFromUninstall] criteria." -Source ${CmdletName}
                                Break
                            }
                        }
                        ElseIf ($Exclude[2] -eq 'Contains') {
                            If ($installedApplication.($Exclude[0]) -match [RegEx]::Escape($Exclude[1])) {
                                [Boolean]$addAppToRemoveList = $false
                                Write-Log -Message "Skipping removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of contains match against [-ExcludeFromUninstall] criteria." -Source ${CmdletName}
                                Break
                            }
                        }
                        ElseIf ($Exclude[2] -eq 'WildCard') {
                            If ($installedApplication.($Exclude[0]) -like $Exclude[1]) {
                                [Boolean]$addAppToRemoveList = $false
                                Write-Log -Message "Skipping removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of wildcard match against [-ExcludeFromUninstall] criteria." -Source ${CmdletName}
                                Break
                            }
                        }
                        ElseIf ($Exclude[2] -eq 'Exact') {
                            If ($installedApplication.($Exclude[0]) -eq $Exclude[1]) {
                                [Boolean]$addAppToRemoveList = $false
                                Write-Log -Message "Skipping removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of exact match against [-ExcludeFromUninstall] criteria." -Source ${CmdletName}
                                Break
                            }
                        }
                    }
                }

                If ($addAppToRemoveList) {
                    Write-Log -Message "Adding application to list for removal: [$($installedApplication.DisplayName) $($installedApplication.Version)]." -Source ${CmdletName}
                    $removeMSIApplications.Add($installedApplication)
                }
            }
        }

        ## Build the hashtable with the options that will be passed to Execute-MSI using splatting
        [Hashtable]$ExecuteMSISplat = @{
            Action          = 'Uninstall'
            Path            = ''
            ContinueOnError = $ContinueOnError
        }
        If ($Parameters) {
            $ExecuteMSISplat.Add( 'Parameters', $Parameters)
        }
        ElseIf ($AddParameters) {
            $ExecuteMSISplat.Add( 'AddParameters', $AddParameters)
        }
        If ($LoggingOptions) {
            $ExecuteMSISplat.Add( 'LoggingOptions', $LoggingOptions)
        }
        If ($LogName) {
            $ExecuteMSISplat.Add( 'LogName', $LogName)
        }
        If ($PassThru) {
            $ExecuteMSISplat.Add( 'PassThru', $PassThru)
        }
        If ($IncludeUpdatesAndHotfixes) {
            $ExecuteMSISplat.Add( 'IncludeUpdatesAndHotfixes', $IncludeUpdatesAndHotfixes)
        }

        If (($null -ne $removeMSIApplications) -and ($removeMSIApplications.Count)) {
            ForEach ($removeMSIApplication in $removeMSIApplications) {
                Write-Log -Message "Removing application [$($removeMSIApplication.DisplayName) $($removeMSIApplication.Version)]." -Source ${CmdletName}
                $ExecuteMSISplat.Path = $removeMSIApplication.ProductCode
                If ($PassThru) {
                    [PSObject[]]$ExecuteResults += Execute-MSI @ExecuteMSISplat
                }
                Else {
                    Execute-MSI @ExecuteMSISplat
                }
            }
        }
        Else {
            Write-Log -Message 'No applications found for removal. Continue...' -Source ${CmdletName}
        }
    }
    End {
        If ($PassThru) {
            Write-Output -InputObject ($ExecuteResults)
        }
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function New-MsiTransform {
    <#
.SYNOPSIS

Create a transform file for an MSI database.

.DESCRIPTION

Create a transform file for an MSI database and create/modify properties in the Properties table.

.PARAMETER MsiPath

Specify the path to an MSI file.

.PARAMETER ApplyTransformPath

Specify the path to a transform which should be applied to the MSI database before any new properties are created or modified.

.PARAMETER NewTransformPath

Specify the path where the new transform file with the desired properties will be created. If a transform file of the same name already exists, it will be deleted before a new one is created.

Default is: a) If -ApplyTransformPath was specified but not -NewTransformPath, then <ApplyTransformPath>.new.mst
                b) If only -MsiPath was specified, then <MsiPath>.mst

.PARAMETER TransformProperties

Hashtable which contains calls to Set-MsiProperty for configuring the desired properties which should be included in new transform file.

Example hashtable: [Hashtable]$TransformProperties = @{ 'ALLUSERS' = '1' }

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE
    [Hashtable]$TransformProperties = {
        'ALLUSERS' = '1'
        'AgreeToLicense' = 'Yes'
        'REBOOT' = 'ReallySuppress'
        'RebootYesNo' = 'No'
        'ROOTDRIVE' = 'C:'
    }
    New-MsiTransform -MsiPath 'C:\Temp\PSADTInstall.msi' -TransformProperties $TransformProperties

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
        [String]$MsiPath,
        [Parameter(Mandatory = $false)]
        [ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
        [String]$ApplyTransformPath,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$NewTransformPath,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [Hashtable]$TransformProperties,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        ## Define properties for how the MSI database is opened
        [Int32]$msiOpenDatabaseModeReadOnly = 0
        [Int32]$msiOpenDatabaseModeTransact = 1
        [Int32]$msiViewModifyUpdate = 2
        [Int32]$msiViewModifyReplace = 4
        [Int32]$msiViewModifyDelete = 6
        [Int32]$msiTransformErrorNone = 0
        [Int32]$msiTransformValidationNone = 0
        [Int32]$msiSuppressApplyTransformErrors = 63
    }
    Process {
        Try {
            Write-Log -Message "Creating a transform file for MSI [$MsiPath]." -Source ${CmdletName}

            ## Discover the parent folder that the MSI file resides in
            [String]$MsiParentFolder = Split-Path -Path $MsiPath -Parent -ErrorAction 'Stop'

            ## Create a temporary file name for storing a second copy of the MSI database
            [String]$TempMsiPath = Join-Path -Path $MsiParentFolder -ChildPath ([IO.Path]::GetFileName(([IO.Path]::GetTempFileName()))) -ErrorAction 'Stop'

            ## Create a second copy of the MSI database
            Write-Log -Message "Copying MSI database in path [$MsiPath] to destination [$TempMsiPath]." -Source ${CmdletName}
            $null = Copy-Item -LiteralPath $MsiPath -Destination $TempMsiPath -Force -ErrorAction 'Stop'

            ## Create a Windows Installer object
            [__ComObject]$Installer = New-Object -ComObject 'WindowsInstaller.Installer' -ErrorAction 'Stop'

            ## Open both copies of the MSI database
            #  Open the original MSI database in read only mode
            Write-Log -Message "Opening the MSI database [$MsiPath] in read only mode." -Source ${CmdletName}
            [__ComObject]$MsiPathDatabase = Invoke-ObjectMethod -InputObject $Installer -MethodName 'OpenDatabase' -ArgumentList @($MsiPath, $msiOpenDatabaseModeReadOnly)
            #  Open the temporary copy of the MSI database in view/modify/update mode
            Write-Log -Message "Opening the MSI database [$TempMsiPath] in view/modify/update mode." -Source ${CmdletName}
            [__ComObject]$TempMsiPathDatabase = Invoke-ObjectMethod -InputObject $Installer -MethodName 'OpenDatabase' -ArgumentList @($TempMsiPath, $msiViewModifyUpdate)

            ## If a MSI transform file was specified, then apply it to the temporary copy of the MSI database
            If ($ApplyTransformPath) {
                Write-Log -Message "Applying transform file [$ApplyTransformPath] to MSI database [$TempMsiPath]." -Source ${CmdletName}
                $null = Invoke-ObjectMethod -InputObject $TempMsiPathDatabase -MethodName 'ApplyTransform' -ArgumentList @($ApplyTransformPath, $msiSuppressApplyTransformErrors)
            }

            ## Determine the path for the new transform file that will be generated
            If (-not $NewTransformPath) {
                If ($ApplyTransformPath) {
                    [String]$NewTransformFileName = [IO.Path]::GetFileNameWithoutExtension($ApplyTransformPath) + '.new' + [IO.Path]::GetExtension($ApplyTransformPath)
                }
                Else {
                    [String]$NewTransformFileName = [IO.Path]::GetFileNameWithoutExtension($MsiPath) + '.mst'
                }
                [String]$NewTransformPath = Join-Path -Path $MsiParentFolder -ChildPath $NewTransformFileName -ErrorAction 'Stop'
            }

            ## Set the MSI properties in the temporary copy of the MSI database
            $TransformProperties.GetEnumerator() | ForEach-Object { Set-MsiProperty -DataBase $TempMsiPathDatabase -PropertyName $_.Key -PropertyValue $_.Value }

            ## Commit the new properties to the temporary copy of the MSI database
            $null = Invoke-ObjectMethod -InputObject $TempMsiPathDatabase -MethodName 'Commit'

            ## Reopen the temporary copy of the MSI database in read only mode
            #  Release the database object for the temporary copy of the MSI database
            $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($TempMsiPathDatabase)
            #  Open the temporary copy of the MSI database in read only mode
            Write-Log -Message "Re-opening the MSI database [$TempMsiPath] in read only mode." -Source ${CmdletName}
            [__ComObject]$TempMsiPathDatabase = Invoke-ObjectMethod -InputObject $Installer -MethodName 'OpenDatabase' -ArgumentList @($TempMsiPath, $msiOpenDatabaseModeReadOnly)

            ## Delete the new transform file path if it already exists
            If (Test-Path -LiteralPath $NewTransformPath -PathType 'Leaf' -ErrorAction 'Stop') {
                Write-Log -Message "A transform file of the same name already exists. Deleting transform file [$NewTransformPath]." -Source ${CmdletName}
                $null = Remove-Item -LiteralPath $NewTransformPath -Force -ErrorAction 'Stop'
            }

            ## Generate the new transform file by taking the difference between the temporary copy of the MSI database and the original MSI database
            Write-Log -Message "Generating new transform file [$NewTransformPath]." -Source ${CmdletName}
            $null = Invoke-ObjectMethod -InputObject $TempMsiPathDatabase -MethodName 'GenerateTransform' -ArgumentList @($MsiPathDatabase, $NewTransformPath)
            $null = Invoke-ObjectMethod -InputObject $TempMsiPathDatabase -MethodName 'CreateTransformSummaryInfo' -ArgumentList @($MsiPathDatabase, $NewTransformPath, $msiTransformErrorNone, $msiTransformValidationNone)

            If (Test-Path -LiteralPath $NewTransformPath -PathType 'Leaf' -ErrorAction 'Stop') {
                Write-Log -Message "Successfully created new transform file in path [$NewTransformPath]." -Source ${CmdletName}
            }
            Else {
                Throw "Failed to generate transform file in path [$NewTransformPath]."
            }
        }
        Catch {
            Write-Log -Message "Failed to create new transform file in path [$NewTransformPath]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to create new transform file in path [$NewTransformPath]: $($_.Exception.Message)"
            }
        }
        Finally {
            Try {
                $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($TempMsiPathDatabase)
            }
            Catch {
            }
            Try {
                $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($MsiPathDatabase)
            }
            Catch {
            }
            Try {
                $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Installer)
            }
            Catch {
            }
            Try {
                ## Delete the temporary copy of the MSI database
                If (Test-Path -LiteralPath $TempMsiPath -PathType 'Leaf' -ErrorAction 'Stop') {
                    $null = Remove-Item -LiteralPath $TempMsiPath -Force -ErrorAction 'Stop'
                }
            }
            Catch {
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
