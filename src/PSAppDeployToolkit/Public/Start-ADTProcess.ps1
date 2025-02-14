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

    .PARAMETER WindowStyle
        Style of the window of the process executed. Options: Normal, Hidden, Maximized, Minimized. Only works for native Windows GUI applications. If the WindowStyle is set to Hidden, UseShellExecute should be set to $true.

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
        Specify the length of time in seconds to wait for the msiexec engine to become available.

    .PARAMETER SuccessExitCodes
        List of exit codes to be considered successful. Defaults to values set during ADTSession initialization, otherwise: 0

    .PARAMETER RebootExitCodes
        List of exit codes to indicate a reboot is required. Defaults to values set during ADTSession initialization, otherwise: 1641, 3010

    .PARAMETER IgnoreExitCodes
        List the exit codes to ignore or * to ignore all exit codes.

    .PARAMETER PriorityClass
        Specifies priority class for the process. Options: Idle, Normal, High, AboveNormal, BelowNormal, RealTime.

    .PARAMETER UseShellExecute
        Specifies whether to use the operating system shell to start the process. $true if the shell should be used when starting the process; $false if the process should be created directly from the executable file.

        The word "Shell" in this context refers to a graphical shell (similar to the Windows shell) rather than command shells (for example, bash or sh) and lets users launch graphical applications or open documents. It lets you open a file or a url and the Shell will figure out the program to open it with.

        The WorkingDirectory property behaves differently depending on the value of the UseShellExecute property. When UseShellExecute is true, the WorkingDirectory property specifies the location of the executable. When UseShellExecute is false, the WorkingDirectory property is not used to find the executable. Instead, it is used only by the process that is started and has meaning only within the context of the new process.

        If you set UseShellExecute to $true, there will be no available output from the process.

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
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Start-ADTProcess
    #>

    [CmdletBinding()]
    [OutputType([PSADT.Types.ProcessResult])]
    [OutputType([PSADT.Types.ProcessInfo])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$FilePath,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ArgumentList,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SecureArgumentList,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
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
        [PSDefaultValue(Help = '(Get-ADTConfig).MSI.MutexWaitTime')]
        [System.UInt32]$MsiExecWaitTime,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$SuccessExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$RebootExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [SupportsWildcards()]
        [System.String[]]$IgnoreExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Diagnostics.ProcessPriorityClass]$PriorityClass = [System.Diagnostics.ProcessPriorityClass]::Normal,

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

        # Set up initial variables.
        $extInvoker = (Get-PSCallStack | Select-Object -Skip 1 | Select-Object -First 1 | & { process { $_.InvocationInfo.MyCommand.Source } }) -ne $MyInvocation.MyCommand.Module.Name
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
                # Validate and find the fully qualified path for the $FilePath variable.
                if ([System.IO.Path]::IsPathRooted($FilePath) -and [System.IO.Path]::HasExtension($FilePath))
                {
                    if (![System.IO.File]::Exists($FilePath))
                    {
                        Write-ADTLogEntry -Message "File [$FilePath] not found." -Severity 3
                        $naerParams = @{
                            Exception = [System.IO.FileNotFoundException]::new("File [$FilePath] not found.")
                            Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                            ErrorId = 'PathFileNotFound'
                            TargetObject = $FilePath
                            RecommendedAction = "Please confirm the path of the specified file and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    Write-ADTLogEntry -Message "[$FilePath] is a valid fully qualified path, continue."
                }
                else
                {
                    # Get the fully qualified path for the file using DirFiles, the current directory, then the system's path environment variable.
                    if (!($fqPath = Get-Item -Path ("$(if ($adtSession) { "$($adtSession.DirFiles);" })$($ExecutionContext.SessionState.Path.CurrentLocation.Path);$([System.Environment]::GetEnvironmentVariable('PATH'))".TrimEnd(';').Split(';').TrimEnd('\') -replace '$', "\$FilePath") -ErrorAction Ignore | Select-Object -ExpandProperty FullName -First 1))
                    {
                        Write-ADTLogEntry -Message "[$FilePath] contains an invalid path or file name." -Severity 3
                        $naerParams = @{
                            Exception = [System.IO.FileNotFoundException]::new("[$FilePath] contains an invalid path or file name.")
                            Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                            ErrorId = 'PathFileNotFound'
                            TargetObject = $FilePath
                            RecommendedAction = "Please confirm the path of the specified file and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    Write-ADTLogEntry -Message "[$FilePath] successfully resolved to fully qualified path [$fqPath]."
                    $FilePath = $fqPath
                }

                # Set the Working directory if not specified.
                if (!$WorkingDirectory)
                {
                    $WorkingDirectory = [System.IO.Path]::GetDirectoryName($FilePath)
                }

                # If the WindowStyle parameter is set to 'Hidden', set the UseShellExecute parameter to '$true' unless specifically specified.
                if ($WindowStyle.Equals([System.Diagnostics.ProcessWindowStyle]::Hidden) -and !$PSBoundParameters.ContainsKey('UseShellExecute'))
                {
                    $UseShellExecute = $true
                }

                # If MSI install, check to see if the MSI installer service is available or if another MSI install is already underway.
                # Please note that a race condition is possible after this check where another process waiting for the MSI installer
                # to become available grabs the MSI Installer mutex before we do. Not too concerned about this possible race condition.
                if (($FilePath -match 'msiexec') -or $WaitForMsiExec)
                {
                    $MsiExecAvailable = Test-ADTMutexAvailability -MutexName 'Global\_MSIExecute' -MutexWaitTime ([System.TimeSpan]::FromSeconds($MsiExecWaitTime))
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
                            TargetObject = $FilePath
                            RecommendedAction = "Please wait for the current MSI operation to finish and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                }

                try
                {
                    # Disable Zone checking to prevent warnings when running executables.
                    [System.Environment]::SetEnvironmentVariable('SEE_MASK_NOZONECHECKS', 1)

                    # Define process.
                    $process = [System.Diagnostics.Process]@{
                        StartInfo = [System.Diagnostics.ProcessStartInfo]@{
                            FileName = $FilePath
                            WorkingDirectory = $WorkingDirectory
                            UseShellExecute = $UseShellExecute
                            ErrorDialog = $false
                            RedirectStandardOutput = $true
                            RedirectStandardError = $true
                            CreateNoWindow = $CreateNoWindow
                            WindowStyle = $WindowStyle
                        }
                    }
                    if ($ArgumentList)
                    {
                        $process.StartInfo.Arguments = $ArgumentList
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
                    if ($ArgumentList)
                    {
                        if ($SecureArgumentList)
                        {
                            Write-ADTLogEntry -Message "Executing [$FilePath (Parameters Hidden)]..."
                        }
                        elseif ($ArgumentList -match '-Command \&')
                        {
                            Write-ADTLogEntry -Message "Executing [$FilePath [PowerShell ScriptBlock]]..."
                        }
                        else
                        {
                            Write-ADTLogEntry -Message "Executing [$FilePath $ArgumentList]..."
                        }
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Executing [$FilePath]..."
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

                    # Re-enable zone checking.
                    [System.Environment]::SetEnvironmentVariable('SEE_MASK_NOZONECHECKS', $null)
                }

                if (!$NoWait)
                {
                    # Open variable to store the error message if we failed as we need it when we're determining whether we throw or not.
                    $errorMessage = $null

                    # Check to see whether we should ignore exit codes.
                    if (($ignoreExitCode = $IgnoreExitCodes -and ($($IgnoreExitCodes).Equals('*') -or ([System.Int32[]]$IgnoreExitCodes).Contains($returnCode))))
                    {
                        Write-ADTLogEntry -Message "Execution completed and the exit code [$returnCode] is being ignored."
                    }
                    elseif ($RebootExitCodes.Contains($returnCode))
                    {
                        Write-ADTLogEntry -Message "Execution completed successfully with exit code [$returnCode]. A reboot is required." -Severity 2
                    }
                    elseif (($returnCode -eq 1605) -and ($FilePath -match 'msiexec'))
                    {
                        $errorMessage = "Execution failed with exit code [$returnCode] because the product is not currently installed."
                    }
                    elseif (($returnCode -eq -2145124329) -and ($FilePath -match 'wusa'))
                    {
                        $errorMessage = "Execution failed with exit code [$returnCode] because the Windows Update is not applicable to this system."
                    }
                    elseif (($returnCode -eq 17025) -and ($FilePath -match 'fullfile'))
                    {
                        $errorMessage = "Execution failed with exit code [$returnCode] because the Office Update is not applicable to this system."
                    }
                    elseif ($SuccessExitCodes.Contains($returnCode))
                    {
                        Write-ADTLogEntry -Message "Execution completed successfully with exit code [$returnCode]." -Severity 0
                    }
                    else
                    {
                        if (($MsiExitCodeMessage = if ($FilePath -match 'msiexec') { Get-ADTMsiExitCodeMessage -MsiExitCode $returnCode }))
                        {
                            $errorMessage = "Execution failed with exit code [$returnCode]: $MsiExitCodeMessage"
                        }
                        else
                        {
                            $errorMessage = "Execution failed with exit code [$returnCode]."
                        }
                    }

                    # Generate and store the PassThru data.
                    $passthruObj = [PSADT.Types.ProcessResult]::new(
                        $returnCode,
                        $(if (![System.String]::IsNullOrWhiteSpace($stdOut)) { $stdOut }),
                        $(if (![System.String]::IsNullOrWhiteSpace($stdErr)) { $stdErr })
                    )

                    # If we have an error in our process, throw it and let the catch block handle it.
                    if ($errorMessage)
                    {
                        $naerParams = @{
                            Exception = [System.Runtime.InteropServices.ExternalException]::new($errorMessage, $returnCode)
                            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                            ErrorId = 'ProcessExitCodeError'
                            TargetObject = $passthruObj
                            RecommendedAction = "Please review the exit code with the vendor's documentation and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }

                    # Update the session's last exit code with the value if externally called.
                    if ($adtSession -and $extInvoker -and !$ignoreExitCode)
                    {
                        $adtSession.SetExitCode($returnCode)
                    }

                    # If the passthru switch is specified, return the exit code and any output from process.
                    if ($PassThru)
                    {
                        Write-ADTLogEntry -Message 'PassThru parameter specified, returning execution results object.'
                        $PSCmdlet.WriteObject($passthruObj)
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
            # Set up parameters for Invoke-ADTFunctionErrorHandler.
            if ($null -ne $returnCode)
            {
                # Update the session's last exit code with the value if externally called.
                if ($adtSession -and $extInvoker -and ($OriginalErrorAction -notmatch '^(SilentlyContinue|Ignore)$'))
                {
                    $adtSession.SetExitCode($returnCode)
                }
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage $_.Exception.Message -DisableErrorResolving
            }
            else
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Error occurred while attempting to start the specified process."
            }

            # If the passthru switch is specified, return the exit code and any output from process.
            if ($PassThru)
            {
                Write-ADTLogEntry -Message 'PassThru parameter specified, returning execution results object.'
                $PSCmdlet.WriteObject($_.TargetObject)
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
