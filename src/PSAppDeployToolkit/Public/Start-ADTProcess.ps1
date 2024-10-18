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

    .PARAMETER Path
        Path to the file to be executed. If the file is located directly in the "Files" directory of the App Deploy Toolkit, only the file name needs to be specified.

        Otherwise, the full path of the file must be specified. If the files is in a subdirectory of "Files", use the "$dirFiles" variable as shown in the example.

    .PARAMETER Parameters
        Arguments to be passed to the executable.

    .PARAMETER SecureParameters
        Hides all parameters passed to the executable from the Toolkit log file.

    .PARAMETER WindowStyle
        Style of the window of the process executed. Options: Normal, Hidden, Maximized, Minimized. Default: Normal. Only works for native Windows GUI applications. If the WindowStyle is set to Hidden, UseShellExecute should be set to $true.

        Note: Not all processes honor WindowStyle. WindowStyle is a recommendation passed to the process. They can choose to ignore it.

    .PARAMETER CreateNoWindow
        Specifies whether the process should be started with a new window to contain it. Only works for Console mode applications. UseShellExecute should be set to $false. Default is false.

    .PARAMETER WorkingDirectory
        The working directory used for executing the process. Defaults to the directory of the file being executed. The use of UseShellExecute affects this parameter.

    .PARAMETER NoWait
        Immediately continue after executing the process.

    .PARAMETER PassThru
        If NoWait is not specified, returns an object with ExitCode, STDOut and STDErr output from the process. If NoWait is specified, returns an object with Id, Handle and ProcessName.

    .PARAMETER WaitForMsiExec
        Sometimes an EXE bootstrapper will launch an MSI install. In such cases, this variable will ensure that this function waits for the msiexec engine to become available before starting the install.

    .PARAMETER MsiExecWaitTime
        Specify the length of time in seconds to wait for the msiexec engine to become available. Default: 600 seconds (10 minutes).

    .PARAMETER SuccessCodes
        List of exit codes to be considered successful. Defaults to values set during ADTSession initialization, otherwise: 0

    .PARAMETER RebootCodes
        List of exit codes to indicate a reboot is required. Defaults to values set during ADTSession initialization, otherwise: 1641, 3010

    .PARAMETER IgnoreExitCodes
        List the exit codes to ignore or * to ignore all exit codes.

    .PARAMETER PriorityClass
        Specifies priority class for the process. Options: Idle, Normal, High, AboveNormal, BelowNormal, RealTime. Default: Normal

    .PARAMETER NoExitOnProcessFailure
        Specifies whether the function shouldn't call Close-ADTSession when the process returns an exit code that is considered an error/failure. Default: $false

    .PARAMETER UseShellExecute
        Specifies whether to use the operating system shell to start the process. $true if the shell should be used when starting the process; $false if the process should be created directly from the executable file.

        The word "Shell" in this context refers to a graphical shell (similar to the Windows shell) rather than command shells (for example, bash or sh) and lets users launch graphical applications or open documents. It lets you open a file or a url and the Shell will figure out the program to open it with.

        The WorkingDirectory property behaves differently depending on the value of the UseShellExecute property. When UseShellExecute is true, the WorkingDirectory property specifies the location of the executable. When UseShellExecute is false, the WorkingDirectory property is not used to find the executable. Instead, it is used only by the process that is started and has meaning only within the context of the new process.

        If you set UseShellExecute to $true, there will be no available output from the process.

    .EXAMPLE
        Start-ADTProcess -Path 'setup.exe' -Parameters '/S' -IgnoreExitCodes 1,2

    .EXAMPLE
        Start-ADTProcess -Path "$dirFiles\Bin\setup.exe" -Parameters '/S' -WindowStyle 'Hidden'

    .EXAMPLE
        Start-ADTProcess -Path 'uninstall_flash_player_64bit.exe' -Parameters '/uninstall' -WindowStyle 'Hidden'

        If the file is in the "Files" directory of the App Deploy Toolkit, only the file name needs to be specified.

    .EXAMPLE
        Start-ADTProcess -Path 'setup.exe' -Parameters "-s -f2`"$((Get-ADTConfig).Toolkit.LogPath)\$installName.log`""

        Launch InstallShield "setup.exe" from the ".\Files" sub-directory and force log files to the logging folder.

    .EXAMPLE
        Start-ADTProcess -Path 'setup.exe' -Parameters "/s /v`"ALLUSERS=1 /qn /L* \`"$((Get-ADTConfig).Toolkit.LogPath)\$installName.log`"`""

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

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    [OutputType([PSADT.Types.ProcessResult])]
    [OutputType([PSADT.Types.ProcessInfo])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [Alias('FilePath')]
        [System.String]$Path,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Alias('Arguments')]
        [System.String[]]$Parameters,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SecureParameters,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Normal', 'Hidden', 'Maximized', 'Minimized')]
        [System.Diagnostics.ProcessWindowStyle]$WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Normal,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$CreateNoWindow,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WorkingDirectory,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$WaitForMsiExec,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$MsiExecWaitTime,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$SuccessCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$RebootCodes,

        [Parameter(Mandatory = $false)]
        [SupportsWildcards()]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$IgnoreExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Idle', 'Normal', 'High', 'AboveNormal', 'BelowNormal', 'RealTime')]
        [System.Diagnostics.ProcessPriorityClass]$PriorityClass = [System.Diagnostics.ProcessPriorityClass]::Normal,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoExitOnProcessFailure,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$UseShellExecute
    )

    begin
    {
        # Initalize function and get required objects.
        $adtSession = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Set up defaults if not specified.
        if (!$PSBoundParameters.ContainsKey('MsiExecWaitTime'))
        {
            $MsiExecWaitTime = (Get-ADTConfig).MSI.MutexWaitTime
        }
        if (!$PSBoundParameters.ContainsKey('SuccessCodes'))
        {
            $SuccessCodes = if ($adtSession)
            {
                $adtSession.GetPropertyValue('AppExitCodes')
            }
            else
            {
                0
            }
        }
        if (!$PSBoundParameters.ContainsKey('RebootCodes'))
        {
            $RebootCodes = if ($adtSession)
            {
                $adtSession.GetPropertyValue('AppRebootCodes')
            }
            else
            {
                1641, 3010
            }
        }

        # Set up initial variables.
        $extInvoker = !(Get-PSCallStack)[1].InvocationInfo.MyCommand.Source.StartsWith($MyInvocation.MyCommand.Module.Name)
        $stdOutBuilder = [System.Text.StringBuilder]::new()
        $stdErrBuilder = [System.Text.StringBuilder]::new()
        $stdOutEvent = $stdErrEvent = $null
        $stdOut = $stdErr = $null
        $returnCode = $null
    }

    process
    {
        try
        {
            try
            {
                # Validate and find the fully qualified path for the $Path variable.
                if ([System.IO.Path]::IsPathRooted($Path) -and [System.IO.Path]::HasExtension($Path))
                {
                    if (![System.IO.File]::Exists($Path))
                    {
                        Write-ADTLogEntry -Message "File [$Path] not found." -Severity 3
                        $naerParams = @{
                            Exception = [System.IO.FileNotFoundException]::new("File [$Path] not found.")
                            Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                            ErrorId = 'PathFileNotFound'
                            TargetObject = $Path
                            RecommendedAction = "Please confirm the path of the specified file and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    Write-ADTLogEntry -Message "[$Path] is a valid fully qualified path, continue."
                }
                else
                {
                    # Get the fully qualified path for the file using DirFiles, the current directory, then the system's path environment variable.
                    if (!($fqPath = Get-Item -Path ("$(if ($adtSession) { "$($adtSession.GetPropertyValue('DirFiles'));" })$($PWD);$([System.Environment]::GetEnvironmentVariable('PATH'))".TrimEnd(';').Split(';').TrimEnd('\') -replace '$', "\$Path") -ErrorAction Ignore | Select-Object -ExpandProperty FullName -First 1))
                    {
                        Write-ADTLogEntry -Message "[$Path] contains an invalid path or file name." -Severity 3
                        $naerParams = @{
                            Exception = [System.IO.FileNotFoundException]::new("[$Path] contains an invalid path or file name.")
                            Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                            ErrorId = 'PathFileNotFound'
                            TargetObject = $Path
                            RecommendedAction = "Please confirm the path of the specified file and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    Write-ADTLogEntry -Message "[$Path] successfully resolved to fully qualified path [$fqPath]."
                    $Path = $fqPath
                }

                # Set the Working directory if not specified.
                if (!$WorkingDirectory)
                {
                    $WorkingDirectory = [System.IO.Path]::GetDirectoryName($Path)
                }

                # If the WindowStyle parameter is set to 'Hidden', set the UseShellExecute parameter to '$true' unless specifically specified.
                if ($WindowStyle.Equals([System.Diagnostics.ProcessWindowStyle]::Hidden) -and !$PSBoundParameters.ContainsKey('UseShellExecute'))
                {
                    $UseShellExecute = $true
                }

                # If MSI install, check to see if the MSI installer service is available or if another MSI install is already underway.
                # Please note that a race condition is possible after this check where another process waiting for the MSI installer
                # to become available grabs the MSI Installer mutex before we do. Not too concerned about this possible race condition.
                if (($Path -match 'msiexec') -or $WaitForMsiExec)
                {
                    $MsiExecAvailable = Test-ADTIsMutexAvailable -MutexName 'Global\_MSIExecute' -MutexWaitTime ([System.TimeSpan]::FromSeconds($MsiExecWaitTime))
                    [System.Threading.Thread]::Sleep(1000)
                    if (!$MsiExecAvailable)
                    {
                        # Default MSI exit code for install already in progress.
                        Write-ADTLogEntry -Message 'Another MSI installation is already in progress and needs to be completed before proceeding with this installation.' -Severity 3
                        $returnCode = 1618
                        $naerParams = @{
                            Exception = [System.TimeoutException]::new('Another MSI installation is already in progress and needs to be completed before proceeding with this installation.')
                            Category = [System.Management.Automation.ErrorCategory]::ResourceBusy
                            ErrorId = 'MsiExecUnavailable'
                            TargetObject = $Path
                            RecommendedAction = "Please wait for the current MSI operation to finish and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                }

                try
                {
                    # Disable Zone checking to prevent warnings when running executables.
                    $env:SEE_MASK_NOZONECHECKS = 1

                    # Define process.
                    $process = [System.Diagnostics.Process]@{
                        StartInfo = [System.Diagnostics.ProcessStartInfo]@{
                            FileName = $Path
                            WorkingDirectory = $WorkingDirectory
                            UseShellExecute = $UseShellExecute
                            ErrorDialog = $false
                            RedirectStandardOutput = $true
                            RedirectStandardError = $true
                            CreateNoWindow = $CreateNoWindow
                            WindowStyle = $WindowStyle
                        }
                    }
                    if ($Parameters)
                    {
                        $process.StartInfo.Arguments = $Parameters
                    }
                    if ($process.StartInfo.UseShellExecute)
                    {
                        Write-ADTLogEntry -Message 'UseShellExecute is set to true, standard output and error will not be available.'
                        $process.StartInfo.RedirectStandardOutput = $false
                        $process.StartInfo.RedirectStandardError = $false
                    }
                    else
                    {
                        # Add event handler to capture process's standard output redirection.
                        $processEventHandler = { $Event.MessageData.AppendLine($(if (![System.String]::IsNullOrWhiteSpace($EventArgs.Data)) { $EventArgs.Data })) }
                        $stdOutEvent = Register-ObjectEvent -InputObject $process -Action $processEventHandler -EventName OutputDataReceived -MessageData $stdOutBuilder
                        $stdErrEvent = Register-ObjectEvent -InputObject $process -Action $processEventHandler -EventName ErrorDataReceived -MessageData $stdErrBuilder
                    }

                    # Start Process.
                    Write-ADTLogEntry -Message "Working Directory is [$WorkingDirectory]."
                    if ($Parameters)
                    {
                        if ($SecureParameters)
                        {
                            Write-ADTLogEntry -Message "Executing [$Path (Parameters Hidden)]..."
                        }
                        elseif ($Parameters -match '-Command \&')
                        {
                            Write-ADTLogEntry -Message "Executing [$Path [PowerShell ScriptBlock]]..."
                        }
                        else
                        {
                            Write-ADTLogEntry -Message "Executing [$Path $Parameters]..."
                        }
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Executing [$Path]..."
                    }
                    $null = $process.Start()

                    # Set priority
                    if ($PriorityClass -ne 'Normal')
                    {
                        try
                        {
                            if (!$process.HasExited)
                            {
                                Write-ADTLogEntry -Message "Changing the priority class for the process to [$PriorityClass]"
                                $process.PriorityClass = $PriorityClass
                            }
                            else
                            {
                                Write-ADTLogEntry -Message "Cannot change the priority class for the process to [$PriorityClass], because the process has exited already." -Severity 2
                            }
                        }
                        catch
                        {
                            Write-ADTLogEntry -Message 'Failed to change the priority class for the process.' -Severity 2
                        }
                    }

                    # NoWait specified, return process details. If it isn't specified, start reading standard Output and Error streams.
                    if ($NoWait)
                    {
                        Write-ADTLogEntry -Message 'NoWait parameter specified. Continuing without waiting for exit code...'
                        if ($PassThru)
                        {
                            if (!$process.HasExited)
                            {
                                Write-ADTLogEntry -Message 'PassThru parameter specified, returning process details object.'
                                $PSCmdlet.WriteObject([PSADT.Types.ProcessInfo]::new(
                                        $process.Id,
                                        $process.Handle,
                                        $process.ProcessName
                                    ))
                            }
                            else
                            {
                                Write-ADTLogEntry -Message 'PassThru parameter specified, however the process has already exited.'
                            }
                        }
                    }
                    else
                    {
                        # Read all streams to end and wait for the process to exit.
                        if (!$process.StartInfo.UseShellExecute)
                        {
                            $process.BeginOutputReadLine()
                            $process.BeginErrorReadLine()
                        }
                        $process.WaitForExit()

                        # HasExited indicates that the associated process has terminated, either normally or abnormally. Wait until HasExited returns $true.
                        while (!$process.HasExited)
                        {
                            $process.Refresh()
                            [System.Threading.Thread]::Sleep(1000)
                        }

                        # Get the exit code for the process.
                        $returnCode = $process.ExitCode

                        # Update the session's last exit code with the value if externally called.
                        if ($adtSession -and $extInvoker)
                        {
                            $adtSession.SetExitCode($returnCode)
                        }

                        # Process all streams.
                        if (!$process.StartInfo.UseShellExecute)
                        {
                            # Unregister standard output and error event to retrieve process output.
                            if ($stdOutEvent)
                            {
                                Unregister-Event -SourceIdentifier $stdOutEvent.Name
                                $stdOutEvent = $null
                            }
                            if ($stdErrEvent)
                            {
                                Unregister-Event -SourceIdentifier $stdErrEvent.Name
                                $stdErrEvent = $null
                            }
                            $stdOut = $stdOutBuilder.ToString().Trim()
                            $stdErr = $stdErrBuilder.ToString().Trim()
                            if (![System.String]::IsNullOrWhiteSpace($stdErr))
                            {
                                Write-ADTLogEntry -Message "Standard error output from the process: $stdErr" -Severity 3
                            }
                        }
                    }
                }
                catch
                {
                    throw
                }
                finally
                {
                    # Make sure the standard output and error event is unregistered.
                    if ($process.StartInfo.UseShellExecute -eq $false)
                    {
                        if ($stdOutEvent)
                        {
                            Unregister-Event -SourceIdentifier $stdOutEvent.Name -ErrorAction Ignore
                            $stdOutEvent = $null
                        }
                        if ($stdErrEvent)
                        {
                            Unregister-Event -SourceIdentifier $stdErrEvent.Name -ErrorAction Ignore
                            $stdErrEvent = $null
                        }
                    }

                    # Free resources associated with the process, this does not cause process to exit.
                    if ($process)
                    {
                        $process.Dispose()
                    }

                    # Re-enable Zone checking.
                    Remove-Item -LiteralPath 'env:SEE_MASK_NOZONECHECKS' -ErrorAction Ignore
                }

                if (!$NoWait)
                {
                    # If the passthru switch is specified, return the exit code and any output from process.
                    if ($PassThru)
                    {
                        Write-ADTLogEntry -Message 'PassThru parameter specified, returning execution results object.'
                        $PSCmdlet.WriteObject([PSADT.Types.ProcessResult]::new(
                                $returnCode,
                                $(if (![System.String]::IsNullOrWhiteSpace($stdOut)) { $stdOut }),
                                $(if (![System.String]::IsNullOrWhiteSpace($stdErr)) { $stdErr })
                            ))
                    }

                    # Check to see whether we should ignore exit codes.
                    if ($IgnoreExitCodes -and ($($IgnoreExitCodes).Equals('*') -or ([System.Int32[]]$IgnoreExitCodes).Contains($returnCode)))
                    {
                        Write-ADTLogEntry -Message "Execution completed and the exit code [$returnCode] is being ignored."
                    }
                    elseif ($RebootCodes.Contains($returnCode))
                    {
                        Write-ADTLogEntry -Message "Execution completed successfully with exit code [$returnCode]. A reboot is required." -Severity 2
                    }
                    elseif (($returnCode -eq 1605) -and ($Path -match 'msiexec'))
                    {
                        Write-ADTLogEntry -Message "Execution failed with exit code [$returnCode] because the product is not currently installed." -Severity 3
                    }
                    elseif (($returnCode -eq -2145124329) -and ($Path -match 'wusa'))
                    {
                        Write-ADTLogEntry -Message "Execution failed with exit code [$returnCode] because the Windows Update is not applicable to this system." -Severity 3
                    }
                    elseif (($returnCode -eq 17025) -and ($Path -match 'fullfile'))
                    {
                        Write-ADTLogEntry -Message "Execution failed with exit code [$returnCode] because the Office Update is not applicable to this system." -Severity 3
                    }
                    elseif ($SuccessCodes.Contains($returnCode))
                    {
                        Write-ADTLogEntry -Message "Execution completed successfully with exit code [$returnCode]." -Severity 0
                    }
                    else
                    {
                        if (($MsiExitCodeMessage = if ($Path -match 'msiexec') { Get-ADTMsiExitCodeMessage -MsiExitCode $returnCode }))
                        {
                            Write-ADTLogEntry -Message "Execution failed with exit code [$returnCode]: $MsiExitCodeMessage" -Severity 3
                        }
                        else
                        {
                            Write-ADTLogEntry -Message "Execution failed with exit code [$returnCode]." -Severity 3
                        }

                        if ($adtSession -and !$NoExitOnProcessFailure)
                        {
                            Close-ADTSession -ExitCode $returnCode
                        }
                    }
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            if ($null -eq $returnCode)
            {
                $returnCode = 60002
            }
            if ($adtSession -and $extInvoker)
            {
                $adtSession.SetExitCode($returnCode)
            }

            if ($returnCode.Equals(60002))
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Function failed, setting exit code to [$returnCode]."
            }
            else
            {
                Write-ADTLogEntry -Message "Execution completed with exit code [$returnCode]. Function failed.`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity 3
            }

            if ($PassThru)
            {
                $PSCmdlet.WriteObject([PSADT.Types.ProcessResult]::new(
                        $returnCode,
                        $(if (![System.String]::IsNullOrWhiteSpace($stdOut)) { $stdOut }),
                        $(if (![System.String]::IsNullOrWhiteSpace($stdErr)) { $stdErr })
                    ))
            }

            if ($adtSession -and !$NoExitOnProcessFailure)
            {
                Close-ADTSession -ExitCode $returnCode
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
