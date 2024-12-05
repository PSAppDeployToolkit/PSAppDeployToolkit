#-----------------------------------------------------------------------------
#
# MARK: Start-ADTMsiProcess
#
#-----------------------------------------------------------------------------

function Start-ADTMsiProcess
{
    <#
    .SYNOPSIS
        Executes msiexec.exe to perform actions such as install, uninstall, patch, repair, or active setup for MSI and MSP files or MSI product codes.

    .DESCRIPTION
        This function utilizes msiexec.exe to handle various operations on MSI and MSP files, as well as MSI product codes.
        The operations include installation, uninstallation, patching, repair, and setting up active configurations.

        If the -Action parameter is set to "Install" and the MSI is already installed, the function will terminate without performing any actions.

        The function automatically sets default switches for msiexec based on preferences defined in the XML configuration file.
        Additionally, it generates a log file name and creates a verbose log for all msiexec operations, ensuring detailed tracking.

        The MSI or MSP file is expected to reside in the "Files" subdirectory of the App Deploy Toolkit, with transform files expected to be in the same directory as the MSI file.

    .PARAMETER Action
        Specifies the action to be performed. Available options: Install, Uninstall, Patch, Repair, ActiveSetup.

    .PARAMETER FilePath
        The file path to the MSI/MSP file.

    .PARAMETER ProductCode
        The product code of the installed MSI.

    .PARAMETER InstalledApplication
        The InstalledApplication object of the installed MSI.

    .PARAMETER Transforms
        The name(s) of the transform file(s) to be applied to the MSI. The transform files should be in the same directory as the MSI file.

    .PARAMETER Patches
        The name(s) of the patch (MSP) file(s) to be applied to the MSI for the "Install" action. The patch files should be in the same directory as the MSI file.

    .PARAMETER ArgumentList
        Overrides the default parameters specified in the XML configuration file. The install default is: "REBOOT=ReallySuppress /QB!". The uninstall default is: "REBOOT=ReallySuppress /QN".

    .PARAMETER AdditionalArgumentList
        Adds additional parameters to the default set specified in the XML configuration file. The install default is: "REBOOT=ReallySuppress /QB!". The uninstall default is: "REBOOT=ReallySuppress /QN".

    .PARAMETER SecureArgumentList
        Hides all parameters passed to the MSI or MSP file from the toolkit log file.

    .PARAMETER LoggingOptions
        Overrides the default logging options specified in the XML configuration file.

    .PARAMETER LogFileName
        Overrides the default log file name. The default log file name is generated from the MSI file name. If LogFileName does not end in .log, it will be automatically appended.

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

    .PARAMETER SuccessExitCodes
        List of exit codes to be considered successful. Defaults to values set during ADTSession initialization, otherwise: 0

    .PARAMETER RebootExitCodes
        List of exit codes to indicate a reboot is required. Defaults to values set during ADTSession initialization, otherwise: 1641, 3010

    .PARAMETER IgnoreExitCodes
        List the exit codes to ignore or * to ignore all exit codes.

    .PARAMETER PriorityClass
        Specifies priority class for the process. Options: Idle, Normal, High, AboveNormal, BelowNormal, RealTime. Default: Normal

    .PARAMETER RepairFromSource
        Specifies whether we should repair from source. Also rewrites local cache. Default: $false

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Types.ProcessResult

        Returns an object with the results of the installation if -PassThru is specified.
        - ExitCode
        - StdOut
        - StdErr

    .EXAMPLE
        Start-ADTMsiProcess -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi'

        Install an MSI.

    .EXAMPLE
        Start-ADTMsiProcess -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -Transform 'Adobe_FlashPlayer_11.2.202.233_x64_EN_01.mst' -Parameters '/QN'

        Install an MSI, applying a transform and overriding the default MSI toolkit parameters.

    .EXAMPLE
        $ExecuteMSIResult = Start-ADTMsiProcess -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -PassThru

        Install an MSI and stores the result of the execution into a variable by using the -PassThru option.

    .EXAMPLE
        Start-ADTMsiProcess -Action 'Uninstall' -Path '{26923b43-4d38-484f-9b9e-de460746276c}'

        Uninstall an MSI using a product code.

    .EXAMPLE
        Start-ADTMsiProcess -Action 'Patch' -Path 'Adobe_Reader_11.0.3_EN.msp'

        Install an MSP.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    [OutputType([System.Int32])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateSet('Install', 'Uninstall', 'Patch', 'Repair', 'ActiveSetup')]
        [System.String]$Action = 'Install',

        [Parameter(Mandatory = $true, ParameterSetName = 'FilePath', ValueFromPipeline = $true, HelpMessage = 'Please enter either the path to the MSI/MSP file.')]
        [ValidateScript({
                if ([System.IO.Path]::GetExtension($_) -notmatch '^\.ms[ip]$')
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified input either has an invalid file extension or is not an MSI UUID.'))
                }
                return ![System.String]::IsNullOrWhiteSpace($_)
            })]
        [System.String]$FilePath,

        [Parameter(Mandatory = $true, ParameterSetName = 'ProductCode', ValueFromPipeline = $true, HelpMessage = 'Please supply the Product Code to process.')]
        [ValidateNotNullOrEmpty()]
        [System.Guid]$ProductCode,

        [Parameter(Mandatory = $true, ParameterSetName = 'InstalledApplication', ValueFromPipeline = $true, HelpMessage = 'Please supply the InstalledApplication object to process.')]
        [ValidateNotNullOrEmpty()]
        [PSADT.Types.InstalledApplication]$InstalledApplication,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Transforms,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ArgumentList,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$AdditionalArgumentList,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SecureArgumentList,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Patches,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LoggingOptions,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$LogFileName,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WorkingDirectory,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SkipMSIAlreadyInstalledCheck,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeUpdatesAndHotfixes,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$SuccessExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$RebootExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$IgnoreExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Diagnostics.ProcessPriorityClass]$PriorityClass = [System.Diagnostics.ProcessPriorityClass]::Normal,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$RepairFromSource
    )

    begin
    {
        $adtSession = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet; $adtConfig = Get-ADTConfig
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # Determine whether the input is a ProductCode or not.
                Write-ADTLogEntry -Message "Executing MSI action [$Action]..."

                # If the MSI is in the Files directory, set the full path to the MSI.
                $msiProduct = switch ($PSCmdlet.ParameterSetName)
                {
                    FilePath
                    {
                        if (Test-Path -LiteralPath $FilePath -PathType Leaf)
                        {
                            (Get-Item -LiteralPath $FilePath).FullName
                        }
                        elseif ($adtSession -and [System.IO.File]::Exists(($dirFilesPath = [System.IO.Path]::Combine($adtSession.DirFiles, $FilePath))))
                        {
                            $dirFilesPath
                        }
                        else
                        {
                            Write-ADTLogEntry -Message "Failed to find the file [$FilePath]." -Severity 3
                            $naerParams = @{
                                Exception = [System.IO.FileNotFoundException]::new("Failed to find the file [$FilePath].")
                                Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                                ErrorId = 'FilePathNotFound'
                                TargetObject = $FilePath
                                RecommendedAction = "Please confirm the path of the file and try again."
                            }
                            throw (New-ADTErrorRecord @naerParams)
                        }
                        break
                    }

                    ProductCode
                    {
                        $ProductCode.ToString('B')
                        break
                    }

                    InstalledApplication
                    {
                        $InstalledApplication.ProductCode.ToString('B')
                        break
                    }
                }

                # Fix up any bad file paths.
                if ([System.IO.Path]::GetExtension($msiProduct) -eq '.msi')
                {
                    # Iterate transforms.
                    if ($Transforms)
                    {
                        for ($i = 0; $i -lt $Transforms.Length; $i++)
                        {
                            if ([System.IO.File]::Exists(($fullPath = Join-Path -Path (Split-Path -Path $msiProduct -Parent) -ChildPath $Transforms[$i].Replace('.\', ''))))
                            {
                                $Transforms[$i] = $fullPath
                            }
                        }
                    }

                    # Iterate patches.
                    if ($Patches)
                    {
                        for ($i = 0; $i -lt $Patches.Length; $i++)
                        {
                            if ([System.IO.File]::Exists(($fullPath = Join-Path -Path (Split-Path -Path $msiProduct -Parent) -ChildPath $Patches[$i].Replace('.\', ''))))
                            {
                                $Patches[$i] = $fullPath
                            }
                        }
                    }
                }

                # If the provided MSI was a file path, get the Property table and store it.
                $msiPropertyTable = if ([System.IO.Path]::GetExtension($msiProduct) -eq '.msi')
                {
                    $gmtpParams = @{ Path = $msiProduct; Table = 'Property' }; if ($Transforms) { $gmtpParams.Add('TransformPath', $transforms) }
                    Get-ADTMsiTableProperty @gmtpParams
                }

                # Get the ProductCode of the MSI.
                $msiProductCode = if ($ProductCode)
                {
                    $ProductCode
                }
                elseif ($InstalledApplication)
                {
                    $InstalledApplication.ProductCode
                }
                elseif ($msiPropertyTable)
                {
                    $msiPropertyTable.ProductCode
                }

                # Check if the MSI is already installed. If no valid ProductCode to check or SkipMSIAlreadyInstalledCheck supplied, then continue with requested MSI action.
                $msiInstalled = if ($msiProductCode -and !$SkipMSIAlreadyInstalledCheck)
                {
                    if (!$InstalledApplication -and ($installedApps = Get-ADTApplication -FilterScript { $_.ProductCode -eq $msiProductCode } -IncludeUpdatesAndHotfixes:$IncludeUpdatesAndHotfixes))
                    {
                        $InstalledApplication = $installedApps
                    }
                    !!$InstalledApplication
                }
                else
                {
                    $Action -ne 'Install'
                }

                # Return early if we're installing an installed product, or anything else for a non-installed product.
                if ($msiInstalled -and ($Action -eq 'Install'))
                {
                    Write-ADTLogEntry -Message "The MSI is already installed on this system. Skipping action [$Action]..."
                    return $(if ($PassThru) { [PSADT.Types.ProcessResult]::new(1638, $null, $null) })
                }
                elseif (!$msiInstalled -and ($Action -ne 'Install'))
                {
                    Write-ADTLogEntry -Message "The MSI is not installed on this system. Skipping action [$Action]..."
                    return
                }

                # Set up the log file to use.
                $logFile = if ($PSBoundParameters.ContainsKey('LogFileName'))
                {
                    [System.IO.Path]::GetFileNameWithoutExtension($LogFileName)
                }
                elseif ($InstalledApplication)
                {
                    (Remove-ADTInvalidFileNameChars -Name ($InstalledApplication.DisplayName + '_' + $InstalledApplication.DisplayVersion)) -replace '\s+'
                }
                elseif ($msiPropertyTable)
                {
                    (Remove-ADTInvalidFileNameChars -Name ($msiPropertyTable.ProductName + '_' + $msiPropertyTable.ProductVersion)) -replace '\s+'
                }

                # Build the log path to use.
                $logPath = if ($logFile)
                {
                    if ($adtSession -and $adtConfig.Toolkit.CompressLogs)
                    {
                        Join-Path -Path $adtSession.LogTempFolder -ChildPath $logFile
                    }
                    else
                    {
                        # Create the Log directory if it doesn't already exist.
                        if (![System.IO.Directory]::Exists($adtConfig.MSI.LogPath))
                        {
                            $null = [System.IO.Directory]::CreateDirectory($adtConfig.MSI.LogPath)
                        }

                        # Build the log file path.
                        Join-Path -Path $adtConfig.MSI.LogPath -ChildPath $logFile
                    }
                }

                # Set the installation parameters.
                if ($adtSession -and $adtSession.IsNonInteractive())
                {
                    $msiInstallDefaultParams = $adtConfig.MSI.SilentParams
                    $msiUninstallDefaultParams = $adtConfig.MSI.SilentParams
                }
                else
                {
                    $msiInstallDefaultParams = $adtConfig.MSI.InstallParams
                    $msiUninstallDefaultParams = $adtConfig.MSI.UninstallParams
                }

                # Build the MSI parameters.
                switch ($action)
                {
                    Install
                    {
                        $option = '/i'
                        $msiLogFile = if ($logPath) { "$($logPath)_$($_)" }
                        $msiDefaultParams = $msiInstallDefaultParams
                        break
                    }
                    Uninstall
                    {
                        $option = '/x'
                        $msiLogFile = if ($logPath) { "$($logPath)_$($_)" }
                        $msiDefaultParams = $msiUninstallDefaultParams
                        break
                    }
                    Patch
                    {
                        $option = '/update'
                        $msiLogFile = if ($logPath) { "$($logPath)_$($_)" }
                        $msiDefaultParams = $msiInstallDefaultParams
                        break
                    }
                    Repair
                    {
                        $option = "/f$(if ($RepairFromSource) {'vomus'})"
                        $msiLogFile = if ($logPath) { "$($logPath)_$($_)" }
                        $msiDefaultParams = $msiInstallDefaultParams
                        break
                    }
                    ActiveSetup
                    {
                        $option = '/fups'
                        $msiLogFile = if ($logPath) { "$($logPath)_$($_)" }
                        $msiDefaultParams = $null
                        break
                    }
                }

                # Post-process the MSI log file variable.
                if ($msiLogFile)
                {
                    # Append the username to the log file name if the toolkit is not running as an administrator, since users do not have the rights to modify files in the ProgramData folder that belong to other users.
                    if (!(Test-ADTCallerIsAdmin))
                    {
                        $msiLogFile = $msiLogFile + '_' + (Remove-ADTInvalidFileNameChars -Name ([System.Environment]::UserName))
                    }

                    # Append ".log" to the MSI logfile path and enclose in quotes.
                    if ([IO.Path]::GetExtension($msiLogFile) -ne '.log')
                    {
                        $msiLogFile = "`"$($msiLogFile + '.log')`""
                    }
                }

                # Set the working directory of the MSI.
                if ($PSCmdlet.ParameterSetName.Equals('FilePath') -and !$workingDirectory)
                {
                    $WorkingDirectory = [System.IO.Path]::GetDirectoryName($msiProduct)
                }

                # Enumerate all transforms specified, qualify the full path if possible and enclose in quotes.
                $mstFile = if ($Transforms)
                {
                    "`"$($Transforms -join ';')`""
                }

                # Enumerate all patches specified, qualify the full path if possible and enclose in quotes.
                $mspFile = if ($Patches)
                {
                    "`"$($Patches -join ';')`""
                }

                # Start building the MsiExec command line starting with the base action and file.
                $argsMSI = "$option `"$msiProduct`""

                # Add MST.
                if ($mstFile)
                {
                    $argsMSI = "$argsMSI TRANSFORMS=$mstFile TRANSFORMSSECURE=1"
                }

                # Add MSP.
                if ($mspFile)
                {
                    $argsMSI = "$argsMSI PATCH=$mspFile"
                }

                # Replace default parameters if specified.
                $argsMSI = if ($ArgumentList)
                {
                    "$argsMSI $([System.String]::Join(' ', $ArgumentList))"
                }
                else
                {
                    "$argsMSI $msiDefaultParams"
                }

                # Add reinstallmode and reinstall variable for Patch.
                if ($action -eq 'Patch')
                {
                    $argsMSI = "$argsMSI REINSTALLMODE=ecmus REINSTALL=ALL"
                }

                # Append parameters to default parameters if specified.
                if ($AdditionalArgumentList)
                {
                    $argsMSI = "$argsMSI $([System.String]::Join(' ', $AdditionalArgumentList))"
                }

                # Add custom Logging Options if specified, otherwise, add default Logging Options from Config file.
                if ($msiLogFile)
                {
                    $argsMSI = if ($LoggingOptions)
                    {
                        "$argsMSI $LoggingOptions $msiLogFile"
                    }
                    else
                    {
                        "$argsMSI $($adtConfig.MSI.LoggingOptions) $msiLogFile"
                    }
                }

                # Build the hashtable with the options that will be passed to Start-ADTProcess using splatting.
                $ExecuteProcessSplat = @{
                    FilePath = "$([System.Environment]::SystemDirectory)\msiexec.exe"
                    ArgumentList = $argsMSI
                    WindowStyle = 'Normal'
                }
                if ($WorkingDirectory)
                {
                    $ExecuteProcessSplat.Add('WorkingDirectory', $WorkingDirectory)
                }
                if ($SecureArgumentList)
                {
                    $ExecuteProcessSplat.Add('SecureArgumentList', $SecureArgumentList)
                }
                if ($PassThru)
                {
                    $ExecuteProcessSplat.Add('PassThru', $PassThru)
                }
                if ($SuccessExitCodes)
                {
                    $ExecuteProcessSplat.Add('SuccessExitCodes', $SuccessExitCodes)
                }
                if ($RebootExitCodes)
                {
                    $ExecuteProcessSplat.Add('RebootExitCodes', $RebootExitCodes)
                }
                if ($IgnoreExitCodes)
                {
                    $ExecuteProcessSplat.Add('IgnoreExitCodes', $IgnoreExitCodes)
                }
                if ($PriorityClass)
                {
                    $ExecuteProcessSplat.Add('PriorityClass', $PriorityClass)
                }
                if ($NoWait)
                {
                    $ExecuteProcessSplat.Add('NoWait', $NoWait)
                }

                # Call the Start-ADTProcess function.
                $result = Start-ADTProcess @ExecuteProcessSplat

                # Refresh environment variables for Windows Explorer process as Windows does not consistently update environment variables created by MSIs.
                Update-ADTDesktop

                # Return the results if passing through.
                if ($PassThru -and $result)
                {
                    return $result
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
