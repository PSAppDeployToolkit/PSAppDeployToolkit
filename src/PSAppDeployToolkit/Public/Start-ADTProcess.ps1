#-----------------------------------------------------------------------------
#
# MARK: Start-ADTProcess
#
#-----------------------------------------------------------------------------

function Start-ADTProcess
{
    <#
    .SYNOPSIS
        Execute a process with optional arguments, working directory, window style.

    .DESCRIPTION
        Executes a process, e.g. a file included in the Files directory of the App Deploy Toolkit, or a file on the local machine. Provides various options for handling the return codes (see Parameters).

    .PARAMETER FilePath
        Path to the file to be executed. If the file is located directly in the "Files" directory of the App Deploy Toolkit, only the file name needs to be specified.

        Otherwise, the full path of the file must be specified. If the files is in a subdirectory of "Files", use the "$($adtSession.DirFiles)" variable as shown in the example.

    .PARAMETER ArgumentList
        Arguments to be passed to the executable.

    .PARAMETER SecureArgumentList
        Hides all parameters passed to the executable from the Toolkit log file.

    .PARAMETER WorkingDirectory
        The working directory used for executing the process. Defaults to the directory of the file being executed. The use of UseShellExecute affects this parameter.

    .PARAMETER Username
        A username to invoke the process as. Only supported while running as the SYSTEM account.

    .PARAMETER UseLinkedAdminToken
        Use a user's linked administrative token while running the process under their context.

    .PARAMETER InheritEnvironmentVariables
        Specifies whether the process running as a user should inherit the SYSTEM account's environment variables.

    .PARAMETER UseShellExecute
        Specifies whether to use the operating system shell to start the process. $true if the shell should be used when starting the process; $false if the process should be created directly from the executable file.

        The word "Shell" in this context refers to a graphical shell (similar to the Windows shell) rather than command shells (for example, bash or sh) and lets users launch graphical applications or open documents. It lets you open a file or a url and the Shell will figure out the program to open it with.

        The WorkingDirectory property behaves differently depending on the value of the UseShellExecute property. When UseShellExecute is true, the WorkingDirectory property specifies the location of the executable. When UseShellExecute is false, the WorkingDirectory property is not used to find the executable. Instead, it is used only by the process that is started and has meaning only within the context of the new process.

        If you set UseShellExecute to $true, there will be no available output from the process.

    .PARAMETER Verb
        The verb to use when doing a ShellExecute invocation. Common usages are "runas" to trigger a UAC elevation of the process.

    .PARAMETER CreateNoWindow
        Specifies whether the process should be started with a new window to contain it.

    .PARAMETER WindowStyle
        Style of the window of the process executed. Options: Normal, Hidden, Maximized, Minimized. Only works for native Windows GUI applications. If the WindowStyle is set to Hidden, UseShellExecute should be set to $true.

        Note: Not all processes honor WindowStyle. WindowStyle is a recommendation passed to the process. They can choose to ignore it.

    .PARAMETER NoWait
        Immediately continue after executing the process.

    .PARAMETER WaitForMsiExec
        Sometimes an EXE bootstrapper will launch an MSI install. In such cases, this variable will ensure that this function waits for the msiexec engine to become available before starting the install.

    .PARAMETER SuccessExitCodes
        List of exit codes to be considered successful. Defaults to values set during ADTSession initialization, otherwise: 0

    .PARAMETER RebootExitCodes
        List of exit codes to indicate a reboot is required. Defaults to values set during ADTSession initialization, otherwise: 1641, 3010

    .PARAMETER IgnoreExitCodes
        List the exit codes to ignore or * to ignore all exit codes.

    .PARAMETER PriorityClass
        Specifies priority class for the process. Options: Idle, Normal, High, AboveNormal, BelowNormal, RealTime.

    .PARAMETER ExitOnProcessFailure
        Automatically closes the active deployment session via Close-ADTSession in the event the process exits with a non-success or non-ignored exit code.

    .PARAMETER PassThru
        If NoWait is not specified, returns an object with ExitCode, STDOut and STDErr output from the process. If NoWait is specified, returns an object with Id, Handle and ProcessName.

    .EXAMPLE
        Start-ADTProcess -FilePath 'setup.exe' -ArgumentList '/S' -IgnoreExitCodes 1,2

        Launch InstallShield "setup.exe" from the ".\Files" sub-directory.

    .EXAMPLE
        Start-ADTProcess -FilePath "$($adtSession.DirFiles)\Bin\setup.exe" -ArgumentList '/S' -WindowStyle 'Hidden'

        Launch InstallShield "setup.exe" from the ".\Files\Bin" sub-directory.

    .EXAMPLE
        Start-ADTProcess -FilePath 'uninstall_flash_player_64bit.exe' -ArgumentList '/uninstall' -WindowStyle 'Hidden'

        If the file is in the "Files" directory of the AppDeployToolkit, only the file name needs to be specified.

    .EXAMPLE
        Start-ADTProcess -FilePath 'setup.exe' -ArgumentList "-s -f2`"$((Get-ADTConfig).Toolkit.LogPath)\$($adtSession.InstallName).log`""

        Launch InstallShield "setup.exe" from the ".\Files" sub-directory and force log files to the logging folder.

    .EXAMPLE
        Start-ADTProcess -FilePath 'setup.exe' -ArgumentList "/s /v`"ALLUSERS=1 /qn /L* `"$((Get-ADTConfig).Toolkit.LogPath)\$($adtSession.InstallName).log`"`""

        Launch InstallShield "setup.exe" with embedded MSI and force log files to the logging folder.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Types.ProcessResult

        Returns an object with the results of the installation if -PassThru is specified.
        - ExitCode
        - StdOut
        - StdErr

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Start-ADTProcess
    #>

    [CmdletBinding(DefaultParameterSetName = 'CreateNoWindow')]
    [OutputType([PSADT.Types.ProcessResult])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = "Default")]
        [Parameter(Mandatory = $true, ParameterSetName = "DefaultWaitForMsiExec")]
        [Parameter(Mandatory = $true, ParameterSetName = "CreateNoWindow")]
        [Parameter(Mandatory = $true, ParameterSetName = "CreateNoWindowWaitForMsiExec")]
        [Parameter(Mandatory = $true, ParameterSetName = "Username")]
        [Parameter(Mandatory = $true, ParameterSetName = "UsernameWaitForMsiExec")]
        [Parameter(Mandatory = $true, ParameterSetName = "UseShellExecute")]
        [Parameter(Mandatory = $true, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [ValidateNotNullOrEmpty()]
        [System.String]$FilePath,

        [Parameter(Mandatory = $false, ParameterSetName = "Default")]
        [Parameter(Mandatory = $false, ParameterSetName = "DefaultWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindow")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindowWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "Username")]
        [Parameter(Mandatory = $false, ParameterSetName = "UsernameWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecute")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ArgumentList,

        [Parameter(Mandatory = $false, ParameterSetName = "Default")]
        [Parameter(Mandatory = $false, ParameterSetName = "DefaultWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindow")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindowWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "Username")]
        [Parameter(Mandatory = $false, ParameterSetName = "UsernameWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecute")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [System.Management.Automation.SwitchParameter]$SecureArgumentList,

        [Parameter(Mandatory = $false, ParameterSetName = "Default")]
        [Parameter(Mandatory = $false, ParameterSetName = "DefaultWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindow")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindowWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "Username")]
        [Parameter(Mandatory = $false, ParameterSetName = "UsernameWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecute")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [ValidateNotNullOrEmpty()]
        [System.String]$WorkingDirectory,

        [Parameter(Mandatory = $true, ParameterSetName = "Username")]
        [Parameter(Mandatory = $true, ParameterSetName = "UsernameWaitForMsiExec")]
        [ValidateNotNullOrEmpty()]
        [System.String]$Username,

        [Parameter(Mandatory = $false, ParameterSetName = "Username")]
        [Parameter(Mandatory = $false, ParameterSetName = "UsernameWaitForMsiExec")]
        [System.Management.Automation.SwitchParameter]$UseLinkedAdminToken,

        [Parameter(Mandatory = $false, ParameterSetName = "Username")]
        [Parameter(Mandatory = $false, ParameterSetName = "UsernameWaitForMsiExec")]
        [System.Management.Automation.SwitchParameter]$InheritEnvironmentVariables,

        [Parameter(Mandatory = $true, ParameterSetName = "UseShellExecute")]
        [Parameter(Mandatory = $true, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [System.Management.Automation.SwitchParameter]$UseShellExecute,

        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecute")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [ValidateNotNullOrEmpty()]
        [System.String]$Verb,

        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindow")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindowWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "Username")]
        [Parameter(Mandatory = $false, ParameterSetName = "UsernameWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecute")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [System.Management.Automation.SwitchParameter]$CreateNoWindow,

        [Parameter(Mandatory = $false, ParameterSetName = "Default")]
        [Parameter(Mandatory = $false, ParameterSetName = "DefaultWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "Username")]
        [Parameter(Mandatory = $false, ParameterSetName = "UsernameWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecute")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [ValidateNotNullOrEmpty()]
        [System.Diagnostics.ProcessWindowStyle]$WindowStyle,

        [Parameter(Mandatory = $false, ParameterSetName = "Default")]
        [Parameter(Mandatory = $false, ParameterSetName = "DefaultWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindow")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindowWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "Username")]
        [Parameter(Mandatory = $false, ParameterSetName = "UsernameWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecute")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $true, ParameterSetName = "DefaultWaitForMsiExec")]
        [Parameter(Mandatory = $true, ParameterSetName = "CreateNoWindowWaitForMsiExec")]
        [Parameter(Mandatory = $true, ParameterSetName = "UsernameWaitForMsiExec")]
        [Parameter(Mandatory = $true, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [System.Management.Automation.SwitchParameter]$WaitForMsiExec,

        [Parameter(Mandatory = $false, ParameterSetName = "Default")]
        [Parameter(Mandatory = $false, ParameterSetName = "DefaultWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindow")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindowWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "Username")]
        [Parameter(Mandatory = $false, ParameterSetName = "UsernameWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecute")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$SuccessExitCodes,

        [Parameter(Mandatory = $false, ParameterSetName = "Default")]
        [Parameter(Mandatory = $false, ParameterSetName = "DefaultWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindow")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindowWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "Username")]
        [Parameter(Mandatory = $false, ParameterSetName = "UsernameWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecute")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$RebootExitCodes,

        [Parameter(Mandatory = $false, ParameterSetName = "Default")]
        [Parameter(Mandatory = $false, ParameterSetName = "DefaultWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindow")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindowWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "Username")]
        [Parameter(Mandatory = $false, ParameterSetName = "UsernameWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecute")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [ValidateNotNullOrEmpty()]
        [SupportsWildcards()]
        [System.String[]]$IgnoreExitCodes,

        [Parameter(Mandatory = $false, ParameterSetName = "Default")]
        [Parameter(Mandatory = $false, ParameterSetName = "DefaultWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindow")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindowWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "Username")]
        [Parameter(Mandatory = $false, ParameterSetName = "UsernameWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecute")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [ValidateNotNullOrEmpty()]
        [System.Diagnostics.ProcessPriorityClass]$PriorityClass,

        [Parameter(Mandatory = $false, ParameterSetName = "Default")]
        [Parameter(Mandatory = $false, ParameterSetName = "DefaultWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindow")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindowWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "Username")]
        [Parameter(Mandatory = $false, ParameterSetName = "UsernameWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecute")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [System.Management.Automation.SwitchParameter]$ExitOnProcessFailure,

        [Parameter(Mandatory = $false, ParameterSetName = "Default")]
        [Parameter(Mandatory = $false, ParameterSetName = "DefaultWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindow")]
        [Parameter(Mandatory = $false, ParameterSetName = "CreateNoWindowWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "Username")]
        [Parameter(Mandatory = $false, ParameterSetName = "UsernameWaitForMsiExec")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecute")]
        [Parameter(Mandatory = $false, ParameterSetName = "UseShellExecuteWaitForMsiExec")]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    dynamicparam
    {
        # Set up the -MsiExecWaitTime parameter if the parameter set is appropriate.
        if (!$PSCmdlet.ParameterSetName.EndsWith("WaitForMsiExec"))
        {
            return
        }

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in parameters we need as mandatory when there's no active ADTSession.
        $paramDictionary.Add('MsiExecWaitTime', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'MsiExecWaitTime', [System.TimeSpan], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false; ParameterSetName = 'DefaultWaitForMsiExec'; HelpMessage = "Specify the length of time in seconds to wait for the msiexec engine to become available." }
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false; ParameterSetName = 'CreateNoWindowWaitForMsiExec'; HelpMessage = "Specify the length of time in seconds to wait for the msiexec engine to become available." }
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false; ParameterSetName = 'UsernameWaitForMsiExec'; HelpMessage = "Specify the length of time in seconds to wait for the msiexec engine to become available." }
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false; ParameterSetName = 'UseShellExecuteWaitForMsiExec'; HelpMessage = "Specify the length of time in seconds to wait for the msiexec engine to become available." }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                    ($defaultValue = [System.Management.Automation.PSDefaultValueAttribute]::new())
                    $defaultValue.Help = '(Get-ADTConfig).MSI.MutexWaitTime'
                )
            ))

        # Return the populated dictionary.
        return $paramDictionary
    }

    begin
    {
        # Initalize function and get required objects.
        $adtSession = if (Test-ADTSessionActive)
        {
            Get-ADTSession
        }
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Set up defaults if not specified.
        $MsiExecWaitTime = if ($PSCmdlet.ParameterSetName.EndsWith("WaitForMsiExec"))
        {
            if (!$PSBoundParameters.ContainsKey('MsiExecWaitTime'))
            {
                if (!$adtSession)
                {
                    $null = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet
                }
                [System.TimeSpan]::FromSeconds((Get-ADTConfig).MSI.MutexWaitTime)
            }
            else
            {
                $PSBoundParameters.MsiExecWaitTime
            }
        }

        if (!$PSBoundParameters.ContainsKey('SuccessExitCodes'))
        {
            $SuccessExitCodes = if ($adtSession)
            {
                $adtSession.AppSuccessExitCodes
            }
            else
            {
                0
            }
        }
        if (!$PSBoundParameters.ContainsKey('RebootExitCodes'))
        {
            $RebootExitCodes = if ($adtSession)
            {
                $adtSession.AppRebootExitCodes
            }
            else
            {
                1641, 3010
            }
        }
        if (!$PSBoundParameters.ContainsKey('WorkingDirectory'))
        {
            if ($adtSession -and ![System.String]::IsNullOrWhiteSpace($adtSession.DirFiles))
            {
                $WorkingDirectory = $adtSession.DirFiles
            }
            elseif ([System.IO.Path]::HasExtension($FilePath) -and [System.IO.Path]::IsPathRooted($FilePath))
            {
                $WorkingDirectory = [System.IO.Path]::GetDirectoryName($FilePath)
            }
        }

        # Set up initial variables.
        $funcCaller = Get-PSCallStack | Select-Object -Skip 1 | Select-Object -First 1 | & { process { $_.InvocationInfo.MyCommand } }
        $extInvoker = !$funcCaller -or !$funcCaller.Source.StartsWith($MyInvocation.MyCommand.Module.Name) -or $funcCaller.Name.Equals('Start-ADTMsiProcess')
    }

    process
    {
        $result = $null
        try
        {
            try
            {
                # Validate and find the fully qualified path for the $FilePath variable.
                if ([System.IO.Path]::HasExtension($FilePath) -and ![System.IO.Path]::IsPathRooted($FilePath))
                {
                    if (!($fqPath = Get-Item -LiteralPath ("$WorkingDirectory;$($ExecutionContext.SessionState.Path.CurrentLocation.Path);$([System.Environment]::GetEnvironmentVariable('PATH'))".TrimEnd(';').Split(';').TrimEnd('\') -replace '$', "\$FilePath") -ErrorAction Ignore | Select-Object -ExpandProperty FullName -First 1))
                    {
                        $naerParams = @{
                            Exception = [System.IO.FileNotFoundException]::new("The file [$FilePath] is invalid or was unable to be found.")
                            Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                            ErrorId = 'PathFileNotFound'
                            TargetObject = $FilePath
                            RecommendedAction = "Please confirm the path of the specified file and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    Write-ADTLogEntry -Message "File path [$FilePath] successfully resolved to fully qualified path [$fqPath]."
                    $FilePath = $fqPath
                }

                # Set the Working directory if not specified.
                if ([System.String]::IsNullOrWhiteSpace($WorkingDirectory))
                {
                    $WorkingDirectory = [System.IO.Path]::GetDirectoryName($FilePath)
                }

                # If MSI install, check to see if the MSI installer service is available or if another MSI install is already underway.
                # Please note that a race condition is possible after this check where another process waiting for the MSI installer
                # to become available grabs the MSI Installer mutex before we do. Not too concerned about this possible race condition.
                if (($FilePath -match 'msiexec') -or $WaitForMsiExec)
                {
                    $MsiExecAvailable = Test-ADTMutexAvailability -MutexName 'Global\_MSIExecute' -MutexWaitTime $MsiExecWaitTime
                    [System.Threading.Thread]::Sleep(1000)
                    if (!$MsiExecAvailable)
                    {
                        # Default MSI exit code for install already in progress.
                        Write-ADTLogEntry -Message 'Another MSI installation is already in progress and needs to be completed before proceeding with this installation.' -Severity 3
                        $result = [PSADT.Types.ProcessResult]::new(1618)
                        $naerParams = @{
                            Exception = [System.TimeoutException]::new('Another MSI installation is already in progress and needs to be completed before proceeding with this installation.')
                            Category = [System.Management.Automation.ErrorCategory]::ResourceBusy
                            ErrorId = 'MsiExecUnavailable'
                            TargetObject = $FilePath
                            RecommendedAction = "Please wait for the current MSI operation to finish and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                }

                # Set up the process start flags.
                $startInfo = [PSADT.ProcessEx.ProcessOptions]::new(
                    $FilePath,
                    $ArgumentList,
                    $WorkingDirectory,
                    $Username,
                    $UseLinkedAdminToken,
                    $InheritEnvironmentVariables,
                    $UseShellExecute,
                    $Verb,
                    $CreateNoWindow,
                    $WindowStyle,
                    $PriorityClass,
                    [System.Threading.CancellationToken]::None
                )

                # Perform all logging.
                if ($startInfo.UseShellExecute)
                {
                    Write-ADTLogEntry -Message 'UseShellExecute is set to true, standard output and error will not be available.'
                }
                if ($startInfo.WorkingDirectory)
                {
                    Write-ADTLogEntry -Message "Working Directory is [$WorkingDirectory]."
                }
                if ($startInfo.Arguments)
                {
                    if ($SecureArgumentList)
                    {
                        Write-ADTLogEntry -Message "Executing [$FilePath (Parameters Hidden)]..."
                    }
                    elseif ($startInfo.Arguments -match '-Command \&')
                    {
                        Write-ADTLogEntry -Message "Executing [$FilePath [PowerShell ScriptBlock]]..."
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Executing [$FilePath $($startInfo.Arguments)]..."
                    }
                }
                else
                {
                    Write-ADTLogEntry -Message "Executing [$FilePath]..."
                }

                # Start the process.
                $process = [PSADT.ProcessEx.ProcessExecutor]::LaunchAsync($startInfo)

                # NoWait specified, return process details. If it isn't specified, start reading standard Output and Error streams.
                if ($NoWait)
                {
                    Write-ADTLogEntry -Message 'NoWait parameter specified. Continuing without waiting for exit code...'
                    if ($PassThru)
                    {
                        if (!$process.IsCompleted)
                        {
                            Write-ADTLogEntry -Message 'PassThru parameter specified, returning task for external tracking.'
                            return $process
                        }
                        Write-ADTLogEntry -Message 'PassThru parameter specified, however the process has already exited.'
                        return $process.Result
                    }
                    return
                }
                $result = $process.GetAwaiter().GetResult()

                # Check to see whether we should ignore exit codes.
                $errorMessage = if (($ignoreExitCode = $IgnoreExitCodes -and ($($IgnoreExitCodes).Equals('*') -or ([System.Int32[]]$IgnoreExitCodes).Contains($result.ExitCode))))
                {
                    Write-ADTLogEntry -Message "Execution completed and the exit code [$($result.ExitCode)] is being ignored."
                }
                elseif ($RebootExitCodes.Contains($result.ExitCode))
                {
                    Write-ADTLogEntry -Message "Execution completed successfully with exit code [$($result.ExitCode)]. A reboot is required." -Severity 2
                }
                elseif (($result.ExitCode -eq 1605) -and ($FilePath -match 'msiexec'))
                {
                    "Execution failed with exit code [$($result.ExitCode)] because the product is not currently installed."
                }
                elseif (($result.ExitCode -eq -2145124329) -and ($FilePath -match 'wusa'))
                {
                    "Execution failed with exit code [$($result.ExitCode)] because the Windows Update is not applicable to this system."
                }
                elseif (($result.ExitCode -eq 17025) -and ($FilePath -match 'fullfile'))
                {
                    "Execution failed with exit code [$($result.ExitCode)] because the Office Update is not applicable to this system."
                }
                elseif ($SuccessExitCodes.Contains($result.ExitCode))
                {
                    Write-ADTLogEntry -Message "Execution completed successfully with exit code [$($result.ExitCode)]." -Severity 0
                }
                else
                {
                    if (($MsiExitCodeMessage = if ($FilePath -match 'msiexec') { Get-ADTMsiExitCodeMessage -MsiExitCode $result.ExitCode }))
                    {
                        "Execution failed with exit code [$($result.ExitCode)]: $MsiExitCodeMessage"
                    }
                    else
                    {
                        "Execution failed with exit code [$($result.ExitCode)]."
                    }
                }

                # If we have an error in our process, throw it and let the catch block handle it.
                if ($errorMessage)
                {
                    $naerParams = @{
                        Exception = [System.Runtime.InteropServices.ExternalException]::new($errorMessage, $result.ExitCode)
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'ProcessExitCodeError'
                        TargetObject = $result
                        RecommendedAction = "Please review the exit code with the vendor's documentation and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Update the session's last exit code with the value if externally called.
                if ($adtSession -and $extInvoker -and !$ignoreExitCode)
                {
                    $adtSession.SetExitCode($result.ExitCode)
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            # Set up parameters for Invoke-ADTFunctionErrorHandler.
            $iafehParams = @{
                Cmdlet = $PSCmdlet
                SessionState = $ExecutionContext.SessionState
                ErrorRecord = $_
            }

            # Handle the error differently if its a process exit code issue.
            if ($null -ne $result)
            {
                $iafehParams.LogMessage = $_.Exception.Message
                $iafehParams.DisableErrorResolving = $true
                if ($adtSession -and $extInvoker)
                {
                    # Update the session's last exit code with the value if externally called.
                    if ($OriginalErrorAction -notmatch '^(SilentlyContinue|Ignore)$')
                    {
                        $adtSession.SetExitCode($result.ExitCode)
                    }

                    # If the caller is opting to exit out of their deployment, we don't want Invoke-ADTFunctionErrorHandler to throw.
                    if ($ExitOnProcessFailure)
                    {
                        $iafehParams.ErrorAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
                        Invoke-ADTFunctionErrorHandler @iafehParams
                        Close-ADTSession
                    }
                    else
                    {
                        Invoke-ADTFunctionErrorHandler @iafehParams
                    }
                }
                else
                {
                    Invoke-ADTFunctionErrorHandler @iafehParams
                }
            }
            else
            {
                Invoke-ADTFunctionErrorHandler @iafehParams -LogMessage "Error occurred while attempting to start the specified process."
            }
        }
        finally
        {
            # If the passthru switch is specified, return the exit code and any output from process.
            if ($PassThru -and $result)
            {
                Write-ADTLogEntry -Message 'PassThru parameter specified, returning execution results object.'
                $PSCmdlet.WriteObject($result)
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
