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
        The file path to the MSI/MSP or the product code of the installed MSI.

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

    .PARAMETER NoExitOnProcessFailure
        Specifies whether the function shouldn't call Close-ADTSession when the process returns an exit code that is considered an error/failure.

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
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
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

        [Parameter(Mandatory = $true, ValueFromPipeline = $true, HelpMessage = 'Please enter either the path to the MSI/MSP file or the ProductCode')]
        [ValidateScript({
                if (($_ -notmatch (Get-ADTMsiProductCodeRegexPattern)) -and (('.msi', '.msp') -notcontains [System.IO.Path]::GetExtension($_)))
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified input either has an invalid file extension or is not an MSI UUID.'))
                }
                return !!$_
            })]
        [System.String]$FilePath,

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
        [ValidateSet('Idle', 'Normal', 'High', 'AboveNormal', 'BelowNormal', 'RealTime')]
        [System.Diagnostics.ProcessPriorityClass]$PriorityClass = 'Normal',

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoExitOnProcessFailure,

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
                # If the path matches a product code.
                if (($pathIsProductCode = $FilePath -match (Get-ADTEnvironment).MSIProductCodeRegExPattern))
                {
                    # Resolve the product code to a publisher, application name, and version.
                    Write-ADTLogEntry -Message 'Resolving product code to a publisher, application name, and version.'
                    $productCodeNameVersion = Get-ADTApplication -FilterScript { $_.ProductCode -eq $FilePath } -IncludeUpdatesAndHotfixes:$IncludeUpdatesAndHotfixes | Select-Object -Property Publisher, DisplayName, DisplayVersion -First 1 -ErrorAction Ignore

                    # Build the log file name.
                    if (!$LogFileName)
                    {
                        $LogFileName = if ($productCodeNameVersion)
                        {
                            if ($productCodeNameVersion.Publisher)
                            {
                                (Remove-ADTInvalidFileNameChars -Name ($productCodeNameVersion.Publisher + '_' + $productCodeNameVersion.DisplayName + '_' + $productCodeNameVersion.DisplayVersion)) -replace ' '
                            }
                            else
                            {
                                (Remove-ADTInvalidFileNameChars -Name ($productCodeNameVersion.DisplayName + '_' + $productCodeNameVersion.DisplayVersion)) -replace ' '
                            }
                        }
                        else
                        {
                            # Out of other options, make the Product Code the name of the log file.
                            $FilePath
                        }
                    }
                }
                elseif (!$LogFileName)
                {
                    # Get the log file name without file extension.
                    $LogFileName = ([System.IO.FileInfo]$FilePath).BaseName
                }
                else
                {
                    while ('.log', '.txt' -contains [System.IO.Path]::GetExtension($LogFileName))
                    {
                        $LogFileName = [System.IO.Path]::GetFileNameWithoutExtension($LogFileName)
                    }
                }

                # Build the log file path.
                $logPath = if ($adtSession -and $adtConfig.Toolkit.CompressLogs)
                {
                    Join-Path -Path $adtSession.GetPropertyValue('LogTempFolder') -ChildPath $LogFileName
                }
                else
                {
                    # Create the Log directory if it doesn't already exist.
                    if (![System.IO.Directory]::Exists($adtConfig.MSI.LogPath))
                    {
                        $null = [System.IO.Directory]::CreateDirectory($adtConfig.MSI.LogPath)
                    }

                    # Build the log file path.
                    Join-Path -Path $adtConfig.MSI.LogPath -ChildPath $LogFileName
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
                        $msiLogFile = "$logPath" + '_Install'
                        $msiDefaultParams = $msiInstallDefaultParams
                        break
                    }
                    Uninstall
                    {
                        $option = '/x'
                        $msiLogFile = "$logPath" + '_Uninstall'
                        $msiDefaultParams = $msiUninstallDefaultParams
                        break
                    }
                    Patch
                    {
                        $option = '/update'
                        $msiLogFile = "$logPath" + '_Patch'
                        $msiDefaultParams = $msiInstallDefaultParams
                        break
                    }
                    Repair
                    {
                        $option = "/f$(if ($RepairFromSource) {'vomus'})"
                        $msiLogFile = "$logPath" + '_Repair'
                        $msiDefaultParams = $msiInstallDefaultParams
                        break
                    }
                    ActiveSetup
                    {
                        $option = '/fups'
                        $msiLogFile = "$logPath" + '_ActiveSetup'
                        $msiDefaultParams = $null
                        break
                    }
                }

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

                # If the MSI is in the Files directory, set the full path to the MSI.
                $msiFile = if ($adtSession -and [System.IO.File]::Exists(($dirFilesPath = [System.IO.Path]::Combine($adtSession.GetPropertyValue('DirFiles'), $FilePath))))
                {
                    $dirFilesPath
                }
                elseif (Test-Path -LiteralPath $FilePath)
                {
                    (Get-Item -LiteralPath $FilePath).FullName
                }
                elseif ($pathIsProductCode)
                {
                    $FilePath
                }
                else
                {
                    Write-ADTLogEntry -Message "Failed to find MSI file [$FilePath]." -Severity 3
                    $naerParams = @{
                        Exception = [System.IO.FileNotFoundException]::new("Failed to find MSI file [$FilePath].")
                        Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                        ErrorId = 'MsiFileNotFound'
                        TargetObject = $FilePath
                        RecommendedAction = "Please confirm the path of the MSI file and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Set the working directory of the MSI.
                if (!$pathIsProductCode -and !$workingDirectory)
                {
                    $WorkingDirectory = [System.IO.Path]::GetDirectoryName($msiFile)
                }

                # Enumerate all transforms specified, qualify the full path if possible and enclose in quotes.
                $mstFile = if ($Transforms)
                {
                    # Fix up any bad file paths.
                    for ($i = 0; $i -lt $Transforms.Length; $i++)
                    {
                        if (($FullPath = Join-Path -Path (Split-Path -Path $msiFile -Parent) -ChildPath $Transforms[$i].Replace('.\', '')) -and [System.IO.File]::Exists($FullPath))
                        {
                            $Transforms[$i] = $FullPath
                        }
                    }

                    # Echo an msiexec.exe compatible string back out with all transforms.
                    "`"$($Transforms -join ';')`""
                }

                # Enumerate all patches specified, qualify the full path if possible and enclose in quotes.
                $mspFile = if ($Patches)
                {
                    # Fix up any bad file paths.
                    for ($i = 0; $i -lt $patches.Length; $i++)
                    {
                        if (($FullPath = Join-Path -Path (Split-Path -Path $msiFile -Parent) -ChildPath $patches[$i].Replace('.\', '')) -and [System.IO.File]::Exists($FullPath))
                        {
                            $Patches[$i] = $FullPath
                        }
                    }

                    # Echo an msiexec.exe compatible string back out with all patches.
                    "`"$($Patches -join ';')`""
                }

                # Get the ProductCode of the MSI.
                $MSIProductCode = If ($pathIsProductCode)
                {
                    $FilePath
                }
                elseif ([System.IO.Path]::GetExtension($msiFile) -eq '.msi')
                {
                    try
                    {
                        $GetMsiTablePropertySplat = @{ Path = $msiFile; Table = 'Property' }; if ($Transforms) { $GetMsiTablePropertySplat.Add('TransformPath', $transforms) }
                        (Get-ADTMsiTableProperty @GetMsiTablePropertySplat).ProductCode
                    }
                    catch
                    {
                        Write-ADTLogEntry -Message "Failed to get the ProductCode from the MSI file. Continue with requested action [$Action]..."
                    }
                }

                # Start building the MsiExec command line starting with the base action and file.
                $argsMSI = "$option `"$msiFile`""

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
                If ($action -eq 'Patch')
                {
                    $argsMSI = "$argsMSI REINSTALLMODE=ecmus REINSTALL=ALL"
                }

                # Append parameters to default parameters if specified.
                if ($AdditionalArgumentList)
                {
                    $argsMSI = "$argsMSI $([System.String]::Join(' ', $AdditionalArgumentList))"
                }

                # Add custom Logging Options if specified, otherwise, add default Logging Options from Config file.
                $argsMSI = if ($LoggingOptions)
                {
                    "$argsMSI $LoggingOptions $msiLogFile"
                }
                else
                {
                    "$argsMSI $($adtConfig.MSI.LoggingOptions) $msiLogFile"
                }

                # Check if the MSI is already installed. If no valid ProductCode to check or SkipMSIAlreadyInstalledCheck supplied, then continue with requested MSI action.
                $IsMsiInstalled = if ($MSIProductCode -and !$SkipMSIAlreadyInstalledCheck)
                {
                    !!(Get-ADTApplication -FilterScript { $_.ProductCode -eq $MSIProductCode } -IncludeUpdatesAndHotfixes:$IncludeUpdatesAndHotfixes)
                }
                else
                {
                    $Action -ne 'Install'
                }

                # Bypass if we're installing and the MSI is already installed, otherwise proceed.
                $ExecuteResults = if ($IsMsiInstalled -and ($Action -eq 'Install'))
                {
                    Write-ADTLogEntry -Message "The MSI is already installed on this system. Skipping action [$Action]..."
                    [PSADT.Types.ProcessResult]::new(1638, $null, $null)
                }
                elseif ((!$IsMsiInstalled -and ($Action -eq 'Install')) -or $IsMsiInstalled)
                {
                    # Build the hashtable with the options that will be passed to Start-ADTProcess using splatting.
                    Write-ADTLogEntry -Message "Executing MSI action [$Action]..."
                    $ExecuteProcessSplat = @{
                        Path = "$([System.Environment]::SystemDirectory)\msiexec.exe"
                        Parameters = $argsMSI
                        WindowStyle = 'Normal'
                        NoExitOnProcessFailure = $NoExitOnProcessFailure
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
                    Start-ADTProcess @ExecuteProcessSplat

                    # Refresh environment variables for Windows Explorer process as Windows does not consistently update environment variables created by MSIs.
                    Update-ADTDesktop
                }
                else
                {
                    Write-ADTLogEntry -Message "The MSI is not installed on this system. Skipping action [$Action]..."
                }

                # Return the results if passing through.
                if ($PassThru -and $ExecuteResults)
                {
                    return $ExecuteResults
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
