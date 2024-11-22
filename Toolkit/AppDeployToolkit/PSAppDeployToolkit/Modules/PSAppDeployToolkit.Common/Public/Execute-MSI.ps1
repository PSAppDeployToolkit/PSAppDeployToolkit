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

Specifies whether the function should call Close-ADTSession when the process returns an exit code that is considered an error/failure. Default: $true

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
        [ValidateScript({ ($_ -match (Get-ADTEnvironment).MSIProductCodeRegExPattern) -or ('.msi', '.msp' -contains [IO.Path]::GetExtension($_)) })]
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
        $adtEnv = Get-ADTEnvironment
        $adtConfig = Get-ADTConfig
        $adtSession = Get-ADTSession
        Write-ADTDebugHeader
    }
    Process {
        ## Initialize variable indicating whether $Path variable is a Product Code or not
        [Boolean]$PathIsProductCode = $false
        [String[]]$transforms = $null

        ## If the path matches a product code
        If ($Path -match $adtEnv.MSIProductCodeRegExPattern) {
            #  Set variable indicating that $Path variable is a Product Code
            [Boolean]$PathIsProductCode = $true

            #  Resolve the product code to a publisher, application name, and version
            Write-ADTLogEntry -Message 'Resolving product code to a publisher, application name, and version.'

            If ($IncludeUpdatesAndHotfixes) {
                [PSObject]$productCodeNameVersion = Get-ADTInstalledApplication -ProductCode $path -IncludeUpdatesAndHotfixes | Select-Object -Property 'Publisher', 'DisplayName', 'DisplayVersion' -First 1 -ErrorAction 'Ignore'
            }
            Else {
                [PSObject]$productCodeNameVersion = Get-ADTInstalledApplication -ProductCode $path | Select-Object -Property 'Publisher', 'DisplayName', 'DisplayVersion' -First 1 -ErrorAction 'Ignore'
            }

            #  Build the log file name
            If (-not $LogName) {
                If ($productCodeNameVersion) {
                    If ($productCodeNameVersion.Publisher) {
                        $LogName = (Remove-ADTInvalidFileNameChars -Name ($productCodeNameVersion.Publisher + '_' + $productCodeNameVersion.DisplayName + '_' + $productCodeNameVersion.DisplayVersion)) -replace ' ', ''
                    }
                    Else {
                        $LogName = (Remove-ADTInvalidFileNameChars -Name ($productCodeNameVersion.DisplayName + '_' + $productCodeNameVersion.DisplayVersion)) -replace ' ', ''
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

        If ($adtConfig.Toolkit.CompressLogs) {
            ## Build the log file path
            [String]$logPath = Join-Path -Path $adtSession.GetPropertyValue('LogTempFolder') -ChildPath $LogName
        }
        Else {
            ## Create the Log directory if it doesn't already exist
            If (-not (Test-Path -LiteralPath $adtConfig.MSI.LogPath -PathType 'Container' -ErrorAction 'Ignore')) {
                $null = New-Item -Path $adtConfig.MSI.LogPath -ItemType 'Directory' -ErrorAction 'Ignore'
            }
            ## Build the log file path
            [String]$logPath = Join-Path -Path $adtConfig.MSI.LogPath -ChildPath $LogName
        }

        ## Set the installation Parameters
        If ($adtSession.DeployModeSilent) {
            $msiInstallDefaultParams = $adtConfig.MSI.SilentParams
            $msiUninstallDefaultParams = $adtConfig.MSI.SilentParams
        }
        Else {
            $msiInstallDefaultParams = $adtConfig.MSI.InstallParams
            $msiUninstallDefaultParams = $adtConfig.MSI.UninstallParams
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
        If (Test-Path -LiteralPath (Join-Path -Path $adtSession.GetPropertyValue('dirFiles') -ChildPath $path -ErrorAction 'Ignore') -PathType 'Leaf' -ErrorAction 'Ignore') {
            [String]$msiFile = Join-Path -Path $adtSession.GetPropertyValue('dirFiles') -ChildPath $path
        }
        ElseIf (Test-Path -LiteralPath $Path -ErrorAction 'Ignore') {
            [String]$msiFile = (Get-Item -LiteralPath $Path).FullName
        }
        ElseIf ($PathIsProductCode) {
            [String]$msiFile = $Path
        }
        Else {
            Write-ADTLogEntry -Message "Failed to find MSI file [$path]." -Severity 3
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
                Write-ADTLogEntry -Message "Failed to get the ProductCode from the MSI file. Continue with requested action [$Action]..."
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
            $argsMSI = "$argsMSI $($adtConfig.MSI.LoggingOptions) $msiLogFile"
        }

        ## Check if the MSI is already installed. If no valid ProductCode to check or SkipMSIAlreadyInstalledCheck supplied, then continue with requested MSI action.
        [Boolean]$IsMsiInstalled = If ($MSIProductCode -and -not $SkipMSIAlreadyInstalledCheck) {
            If ($IncludeUpdatesAndHotfixes) {
                !!(Get-ADTInstalledApplication -ProductCode $MSIProductCode -IncludeUpdatesAndHotfixes)
            }
            Else {
                !!(Get-ADTInstalledApplication -ProductCode $MSIProductCode)
            }
        }
        Else {
            $Action -ne 'Install'
        }

        If (($IsMsiInstalled) -and ($Action -eq 'Install')) {
            Write-ADTLogEntry -Message "The MSI is already installed on this system. Skipping action [$Action]..."
            [PSObject]$ExecuteResults = @{ ExitCode = 1638; StdOut = 0; StdErr = '' }
        }
        ElseIf (((-not $IsMsiInstalled) -and ($Action -eq 'Install')) -or ($IsMsiInstalled)) {
            Write-ADTLogEntry -Message "Executing MSI action [$Action]..."
            #  Build the hashtable with the options that will be passed to Start-ADTProcess using splatting
            [Hashtable]$ExecuteProcessSplat = @{
                Path                 = $adtEnv.exeMsiexec
                Parameters           = $argsMSI
                WindowStyle          = 'Normal'
                NoExitOnProcessFailure = !$ExitOnProcessFailure
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

            #  Call the Start-ADTProcess function
            If ($PassThru) {
                [PSObject]$ExecuteResults = Start-ADTProcess @ExecuteProcessSplat
            }
            Else {
                Start-ADTProcess @ExecuteProcessSplat
            }
            #  Refresh environment variables for Windows Explorer process as Windows does not consistently update environment variables created by MSIs
            Update-ADTDesktop
        }
        Else {
            Write-ADTLogEntry -Message "The MSI is not installed on this system. Skipping action [$Action]..."
        }
    }
    End {
        If ($PassThru) {
            Write-Output -InputObject ($ExecuteResults)
        }
        Write-ADTDebugFooter
    }
}
