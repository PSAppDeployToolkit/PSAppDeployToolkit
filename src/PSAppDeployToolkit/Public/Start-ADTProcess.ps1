﻿#-----------------------------------------------------------------------------
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
        The working directory used for executing the process. Defaults to DirFiles if there is an active DeploymentSession. The use of UseShellExecute affects this parameter.

    .PARAMETER Username
        A username to invoke the process as. Only supported while running as the SYSTEM account.

    .PARAMETER UseLinkedAdminToken
        Use a user's linked administrative token while running the process under their context.

    .PARAMETER InheritEnvironmentVariables
        Specifies whether the process running as a user should inherit the SYSTEM account's environment variables.

    .PARAMETER UseUnelevatedToken
        If the current process is elevated, starts the new process unelevated using the user's unelevated linked token.

    .PARAMETER UseShellExecute
        Specifies whether to use the operating system shell to start the process. $true if the shell should be used when starting the process; $false if the process should be created directly from the executable file.

        The word "Shell" in this context refers to a graphical shell (similar to the Windows shell) rather than command shells (for example, bash or sh) and lets users launch graphical applications or open documents. It lets you open a file or a url and the Shell will figure out the program to open it with.

        The WorkingDirectory property behaves differently depending on the value of the UseShellExecute property. When UseShellExecute is true, the WorkingDirectory property specifies the location of the executable. When UseShellExecute is false, the WorkingDirectory property is not used to find the executable. Instead, it is used only by the process that is started and has meaning only within the context of the new process.

        If you set UseShellExecute to $true, there will be no available output from the process.

    .PARAMETER Verb
        The verb to use when doing a ShellExecute invocation. Common usages are "runas" to trigger a UAC elevation of the process.

    .PARAMETER ExpandEnvironmentVariables
        Specifies whether to expand any Windows/DOS-style environment variables in the specified FilePath/ArgumentList.

    .PARAMETER WindowStyle
        Style of the window of the process executed. Options: Normal, Hidden, Maximized, Minimized. Only works for native Windows GUI applications. If the WindowStyle is set to Hidden, UseShellExecute should be set to $true.

        Note: Not all processes honor WindowStyle. WindowStyle is a recommendation passed to the process. They can choose to ignore it.

    .PARAMETER CreateNoWindow
        Specifies whether the process should be started with a new window to contain it.

    .PARAMETER StreamEncoding
        Specifies the encoding type to use when reading stdout/stderr. Some apps like WinGet encode using UTF8, which will corrupt if incorrectly set.

    .PARAMETER NoStreamLogging
        Don't log any available stdout/stderr data to the log file.

    .PARAMETER WaitForMsiExec
        Sometimes an EXE bootstrapper will launch an MSI install. In such cases, this variable will ensure that this function waits for the msiexec engine to become available before starting the install.

    .PARAMETER MsiExecWaitTime
        Specify the length of time in seconds to wait for the msiexec engine to become available.

    .PARAMETER WaitForChildProcesses
        Specifies whether the started process should be considered finished only when any child processes it spawns have finished also.

    .PARAMETER KillChildProcessesWithParent
        Specifies whether any child processes started by the provided executable should be closed when the provided executable closes. This is handy for application installs that open web browsers and other programs that cannot be suppressed.

    .PARAMETER Timeout
        How long to wait for the process before timing out.

    .PARAMETER TimeoutAction
        What action to take on timeout. Follows ErrorAction if not specified.

    .PARAMETER NoTerminateOnTimeout
        Indicates that the process should not be terminated on timeout. Only supported for GUI-based applications.

    .PARAMETER SuccessExitCodes
        List of exit codes to be considered successful. Defaults to values set during ADTSession initialization, otherwise: 0

    .PARAMETER RebootExitCodes
        List of exit codes to indicate a reboot is required. Defaults to values set during ADTSession initialization, otherwise: 1641, 3010

    .PARAMETER IgnoreExitCodes
        List the exit codes to ignore or * to ignore all exit codes. Where possible, please use `-SuccessExitCodes` and/or `-RebootExitCodes` instead.

    .PARAMETER PriorityClass
        Specifies priority class for the process. Options: Idle, Normal, High, AboveNormal, BelowNormal, RealTime.

    .PARAMETER ExitOnProcessFailure
        Automatically closes the active deployment session via Close-ADTSession in the event the process exits with a non-success or non-ignored exit code.

    .PARAMETER NoWait
        Immediately continue after executing the process.

    .PARAMETER PassThru
        If `-NoWait` is not specified, returns an object with ExitCode, StdOut, and StdErr output from the process. If `-NoWait` is specified, returns a task that can be awaited. Note that a failed execution will only return an object if either `-ErrorAction` is set to `SilentlyContinue`/`Ignore`, or if `-IgnoreExitCodes`/`-SuccessExitCodes` are used.

    .EXAMPLE
        Start-ADTProcess -FilePath 'setup.exe' -ArgumentList '/S' -SuccessExitCodes 1,2

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

    .EXAMPLE
        ```powershell
        $result = Start-ADTProcess -FilePath "setup.exe" -ArgumentList "-i -f `"$($adtSession.dirFiles)\$($adtSession.LicenseFile)`"" -ErrorAction SilentlyContinue -PassThru
        if ($result.ExitCode -ne 0)
        {
            Write-ADTLogEntry -Message "Installation was successful." -Severity 0
        }
        else
        {
            Write-ADTLogEntry -Message "Installation failed with exit code [$($result.ExitCode)]." -Severity 3
            Write-ADTLogEntry -Message "Standard Out [$($result.StdOut)]." -Severity 3
            Write-ADTLogEntry -Message "Standard Error [$($result.StdErr)]." -Severity 3
        }
        ```

        Launch "setup.exe" with -PassThru so we can write out the result to the log file.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Types.ProcessResult

        Returns an object with the results of the installation if -PassThru is specified.
        - ProcessId
        - ExitCode
        - StdOut
        - StdErr
        - Interleaved

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Start-ADTProcess
    #>

    [CmdletBinding(DefaultParameterSetName = 'Default_CreateWindow_Wait')]
    [OutputType([PSADT.ProcessManagement.ProcessHandle])]
    [OutputType([PSADT.ProcessManagement.ProcessResult])]
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
        [System.String]$WorkingDirectory = [System.Management.Automation.Language.NullString]::Value,

        # Identity: Username (only present in sets where identity is "Username")
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_CreateWindow_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_CreateWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_CreateWindow_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_WindowStyle_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_WindowStyle_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_CreateNoWindow_Timeout')]
        [ValidateNotNullOrEmpty()]
        [System.Security.Principal.NTAccount]$Username,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_WindowStyle_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateNoWindow_Timeout')]
        [System.Management.Automation.SwitchParameter]$UseLinkedAdminToken,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_WindowStyle_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateNoWindow_Timeout')]
        [System.Management.Automation.SwitchParameter]$InheritEnvironmentVariables,

        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [System.Management.Automation.SwitchParameter]$UseUnelevatedToken,

        # Identity: UseShellExecute (only present in sets where identity is "UseShellExecute")
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateWindow_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateWindow_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_WindowStyle_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_WindowStyle_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [System.Management.Automation.SwitchParameter]$UseShellExecute,

        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_WindowStyle_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Verb = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExpandEnvironmentVariables,

        # Window Option: WindowStyle (only in sets where window is "WindowStyle")
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_WindowStyle_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_WindowStyle_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_WindowStyle_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_WindowStyle_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_WindowStyle_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_WindowStyle_Timeout')]
        [ValidateNotNullOrEmpty()]
        [System.Diagnostics.ProcessWindowStyle]$WindowStyle,

        # Window Option: CreateNoWindow (only in sets where window is "CreateNoWindow")
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [System.Management.Automation.SwitchParameter]$CreateNoWindow,

        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [ValidateNotNullOrEmpty()]
        [System.Text.Encoding]$StreamEncoding,

        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [System.Management.Automation.SwitchParameter]$NoStreamLogging,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$WaitForMsiExec,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ($_ -le [System.TimeSpan]::Zero)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName MsiExecWaitTime -ProvidedValue $_ -ExceptionMessage "The provided `-MsiExecWaitTime` parameter value must be greater than zero."))
                }
                return !!$_
            })]
        [System.TimeSpan]$MsiExecWaitTime,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$WaitForChildProcesses,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$KillChildProcessesWithParent,

        # Wait Option: Timeout (only in sets where wait is "Timeout")
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateWindow_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_WindowStyle_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_CreateWindow_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_WindowStyle_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateWindow_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_WindowStyle_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [ValidateScript({
                if ($_.TotalMilliseconds -lt 1)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Timeout -ProvidedValue $_ -ExceptionMessage "The `-Timeout` parameter expects a `[System.TimeSpan]` object of 1ms or above; the supplied value of $($_.Ticks) ticks equates to $($_.TotalMilliSeconds) milliseconds. Try `-Timeout (New-Timespan -Seconds $($_.Ticks))` instead."))
                }
                return !!$_
            })]
        [System.TimeSpan]$Timeout,

        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ActionPreference]$TimeoutAction,

        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Username_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [System.Management.Automation.SwitchParameter]$NoTerminateOnTimeout,

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
        [System.Diagnostics.ProcessPriorityClass]$PriorityClass,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExitOnProcessFailure,

        # Wait Option: NoWait (only in sets where wait is "NoWait")
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_CreateWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Username_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateNoWindow_NoWait')]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        # Initalize function and get required objects.
        $adtSession = if (Test-ADTSessionActive)
        {
            Get-ADTSession
        }
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Set up defaults if not specified.
        if (!$PSBoundParameters.ContainsKey('MsiExecWaitTime'))
        {
            if (!$adtSession)
            {
                $null = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet
            }
            $MsiExecWaitTime = [System.TimeSpan]::FromSeconds((Get-ADTConfig).MSI.MutexWaitTime)
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
        }

        # Set up initial variables.
        $funcCaller = Get-PSCallStack | Select-Object -Skip 1 | Select-Object -First 1 | & { process { $_.InvocationInfo.MyCommand } }
        $extInvoker = !$funcCaller -or !$funcCaller.Source.StartsWith($MyInvocation.MyCommand.Module.Name) -or $funcCaller.Name.Equals('Start-ADTMsiProcess')
        $SEE_MASK_NOZONECHECKS = [System.Environment]::GetEnvironmentVariable('SEE_MASK_NOZONECHECKS')
        [System.Environment]::SetEnvironmentVariable('SEE_MASK_NOZONECHECKS', 1)

        # Set up cancellation token.
        $cancellationTokenSource = if ($Timeout)
        {
            [System.Threading.CancellationTokenSource]::new($Timeout)
        }
        $cancellationToken = if ($cancellationTokenSource)
        {
            $cancellationTokenSource.Token
        }
    }

    process
    {
        Write-ADTLogEntry -Message "Preparing to execute process [$FilePath]$(if (![System.String]::IsNullOrWhiteSpace($Username)) {" for user [$Username]"})..."
        if ($PSBoundParameters.ContainsKey('IgnoreExitCodes') -and !$($IgnoreExitCodes).Equals('*'))
        {
            Write-ADTLogEntry -Message "Please use [-SuccessExitCodes] and/or [-RebootExitCodes] to specify your process's exit codes."
        }
        $result = $null
        try
        {
            try
            {
                # Validate and find the fully qualified path for the $FilePath variable.
                if ((!$ExpandEnvironmentVariables -or !$Username) -and [System.IO.Path]::HasExtension($FilePath) -and ![System.IO.Path]::IsPathRooted($FilePath))
                {
                    if (!($fqPath = Get-Item -LiteralPath ("$WorkingDirectory;$($ExecutionContext.SessionState.Path.CurrentLocation.Path);$([System.Environment]::GetEnvironmentVariable('PATH'))".Split(';').Where({ ![System.String]::IsNullOrWhiteSpace($_) }).TrimEnd('\') -replace '$', "\$FilePath") -ErrorAction Ignore | Select-Object -ExpandProperty FullName -First 1))
                    {
                        $naerParams = @{
                            Exception = [System.IO.FileNotFoundException]::new("The file [$FilePath] is invalid or was unable to be found.")
                            Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                            ErrorId = 'FilePathNotFound'
                            TargetObject = $FilePath
                            RecommendedAction = "Please confirm the path of the specified file and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    Write-ADTLogEntry -Message "File path [$FilePath] successfully resolved to fully qualified path [$fqPath]."
                    $FilePath = $fqPath
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
                        $result = [PSADT.ProcessManagement.ProcessResult]::new(1618)
                        $naerParams = @{
                            Exception = [System.Threading.SynchronizationLockException]::new('Another MSI installation is already in progress and needs to be completed before proceeding with this installation.')
                            Category = [System.Management.Automation.ErrorCategory]::ResourceBusy
                            ErrorId = 'MsiExecUnavailable'
                            TargetObject = $FilePath
                            RecommendedAction = "Please wait for the current MSI operation to finish and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                }

                # Set up the process start flags.
                $startInfo = [PSADT.ProcessManagement.ProcessLaunchInfo]::new(
                    $FilePath,
                    $ArgumentList,
                    $WorkingDirectory,
                    $Username,
                    $UseLinkedAdminToken,
                    $false,
                    $InheritEnvironmentVariables,
                    $ExpandEnvironmentVariables,
                    $false,
                    $UseUnelevatedToken,
                    $UseShellExecute,
                    $Verb,
                    $CreateNoWindow,
                    $WaitForChildProcesses,
                    $KillChildProcessesWithParent,
                    $StreamEncoding,
                    $WindowStyle,
                    $PriorityClass,
                    $cancellationToken,
                    $NoTerminateOnTimeout
                )

                # Perform all logging.
                if ($startInfo.UseShellExecute)
                {
                    Write-ADTLogEntry -Message 'UseShellExecute is set to true, StdOut/StdErr streams will not be available.'
                    if ($PSBoundParameters.ContainsKey('PriorityClass') -and !(Test-ADTCallerIsAdmin))
                    {
                        Write-ADTLogEntry -Message "Setting a priority class on a ShellExecute process is only possible for administrators." -Severity 2
                    }
                }
                elseif (!$CreateNoWindow)
                {
                    Write-ADTLogEntry -Message 'CreateNoWindow not specified, StdOut/StdErr streams will not be available.'
                }
                if ($startInfo.WorkingDirectory)
                {
                    Write-ADTLogEntry -Message "Working Directory is [$WorkingDirectory]."
                }
                if ($ArgumentList)
                {
                    if ($SecureArgumentList)
                    {
                        Write-ADTLogEntry -Message "Executing [`"$FilePath`" (Parameters Hidden)]$(if ($Username) {" for user [$Username]"})..."
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Executing [`"$FilePath`" $(if ($ArgumentList.Length -gt 1) { [PSADT.ProcessManagement.CommandLineUtilities]::ArgumentListToCommandLine($ArgumentList) } else { $ArgumentList[0] })]$(if ($Username) {" for user [$Username]"})..."
                    }
                }
                else
                {
                    Write-ADTLogEntry -Message "Executing [`"$FilePath`"]$(if ($Username) {" for user [$Username]"})..."
                }

                # Start the process.
                ($execution = [PSADT.ProcessManagement.ProcessManager]::LaunchAsync($startInfo)) | Out-String | Out-Null

                # Handle if the returned value is null.
                if (!$execution)
                {
                    # A null result without using ShellExecute is entirely unexpected.
                    if (!$UseShellExecute)
                    {
                        $naerParams = @{
                            Exception = [System.InvalidOperationException]::new("The launching of the process returned a null result.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                            ErrorId = 'ProcessExecutionNullReturn'
                            TargetObject = $execution
                            RecommendedAction = "Please report this to the PSAppDeployToolkit development team for further review."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    Write-ADTLogEntry "Successfully executed the requested ShellExecute command."
                    return
                }

                # NoWait specified, return process details. If it isn't specified, start reading standard Output and Error streams.
                if ($NoWait)
                {
                    Write-ADTLogEntry -Message 'NoWait parameter specified. Continuing without waiting for exit code...'
                    if ($PassThru)
                    {
                        if (!$execution.Task.IsCompleted)
                        {
                            Write-ADTLogEntry -Message 'PassThru parameter specified, returning task for external tracking.'
                            return $execution
                        }
                        Write-ADTLogEntry -Message 'PassThru parameter specified, however the process has already exited.'
                        return $execution.Task.Result
                    }
                    return
                }
                if ($ArgumentList)
                {
                    if ($SecureArgumentList)
                    {
                        Write-ADTLogEntry -Message "Executed [$(if (($command = [PSADT.ProcessManagement.CommandLineUtilities]::CommandLineToArgumentList($execution.CommandLine)[0]).Contains(' ')) { [System.String]::Format('"{0}"', $command) } else { $command }) (Parameters Hidden)], awaiting completion..."
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Executed [$($execution.CommandLine)], awaiting completion..."
                    }
                }
                else
                {
                    Write-ADTLogEntry -Message "Executed [$($execution.CommandLine)], awaiting completion..."
                }
                $result = $execution.Task.GetAwaiter().GetResult()

                # Handle scenarios where we don't have a ProcessResult object (ShellExecute action, for instance).
                if (!$result)
                {
                    if ($PassThru)
                    {
                        Write-ADTLogEntry -Message 'PassThru parameter specified, however no result was available.'
                    }
                    return
                }

                # Check whether the process timed out.
                if (($null -eq $result.ExitCode) -or ($result.ExitCode -eq [PSADT.ProcessManagement.ProcessManager]::TimeoutExitCode))
                {
                    $naerParams = if ($NoTerminateOnTimeout)
                    {
                        @{
                            Exception = [System.TimeoutException]::new("Timed out waiting for process execution to complete.")
                            Category = [System.Management.Automation.ErrorCategory]::LimitsExceeded
                            ErrorId = 'ProcessExecutionTimedOut'
                            TargetObject = $result
                        }
                    }
                    else
                    {
                        @{
                            Exception = [System.OperationCanceledException]::new("Process terminated because execution took too long to complete.", $cancellationToken)
                            Category = [System.Management.Automation.ErrorCategory]::OperationStopped
                            ErrorId = 'ProcessExecutionCancelled'
                            TargetObject = $result
                        }
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Check to see whether we should ignore exit codes.
                $logEntry = if (($ignoreExitCode = $IgnoreExitCodes -and ($($IgnoreExitCodes).Equals('*') -or ([System.Int32[]]$IgnoreExitCodes).Contains($result.ExitCode))))
                {
                    Write-ADTLogEntry -Message "Execution completed and the exit code [$($result.ExitCode)] is being ignored." -PassThru
                }
                elseif ($SuccessExitCodes.Contains($result.ExitCode))
                {
                    Write-ADTLogEntry -Message "Execution completed successfully with exit code [$($result.ExitCode)]." -Severity 0 -PassThru
                }
                elseif ($RebootExitCodes.Contains($result.ExitCode))
                {
                    Write-ADTLogEntry -Message "Execution completed successfully with exit code [$($result.ExitCode)]. A reboot is required." -Severity 2 -PassThru
                }
                elseif (($result.ExitCode -eq 1605) -and ($FilePath -match 'msiexec'))
                {
                    Write-ADTLogEntry -Message "Execution failed with exit code [$($result.ExitCode)] because the product is not currently installed." -Severity 3 -PassThru
                }
                elseif (($result.ExitCode -eq -2145124329) -and ($FilePath -match 'wusa'))
                {
                    Write-ADTLogEntry -Message "Execution failed with exit code [$($result.ExitCode)] because the Windows Update is not applicable to this system." -Severity 3 -PassThru
                }
                elseif (($MsiExitCodeMessage = if ($FilePath -match 'msiexec') { Get-ADTMsiExitCodeMessage -MsiExitCode $result.ExitCode }))
                {
                    Write-ADTLogEntry -Message "Execution failed with exit code [$($result.ExitCode)]: $MsiExitCodeMessage" -Severity 3 -PassThru
                }
                else
                {
                    Write-ADTLogEntry -Message "Execution failed with exit code [$($result.ExitCode)].$(if ($CreateNoWindow -and !$NoStreamLogging) { " Please check the log file for any available stdout/stderr information." })" -Severity 3 -PassThru
                }

                # Log any stdout/stderr if it's available.
                if (!$NoStreamLogging)
                {
                    foreach ($property in ('StdOut', 'StdErr', 'Interleaved'))
                    {
                        $streamMessage = if ($result.$property)
                        {
                            if ($result.$property -gt 1)
                            {
                                "`n`n$([System.String]::Join("`n", $result.$property))"
                            }
                            else
                            {
                                $result.$property[0]
                            }
                        }
                        else
                        {
                            "N/A"
                        }
                        Write-ADTLogEntry -Message "$property Output from Execution: $streamMessage" -HostLogStream ([PSADT.Module.HostLogStream]::None)
                    }
                }

                # If we have an error in our process, throw it and let the catch block handle it.
                if ($logEntry.Message.StartsWith("Execution failed"))
                {
                    $naerParams = @{
                        Exception = [System.Runtime.InteropServices.ExternalException]::new($logEntry.Message, $result.ExitCode)
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

                # If the passthru switch is specified, return the exit code and any output from process.
                if ($PassThru)
                {
                    Write-ADTLogEntry -Message 'PassThru parameter specified, returning execution results object.'
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
            # Set up base parameters for Invoke-ADTFunctionErrorHandler.
            $iafehParams = @{
                Cmdlet = $PSCmdlet
                SessionState = $ExecutionContext.SessionState
                ErrorRecord = $_
            }

            # Switch on the exception type's name.
            switch -Regex ($_.Exception.GetType().FullName)
            {
                '^System\.Runtime\.InteropServices\.ExternalException$'
                {
                    # Handle requirements for when there's an active session.
                    if ($adtSession -and $extInvoker)
                    {
                        if ($OriginalErrorAction -notmatch '^(SilentlyContinue|Ignore)$')
                        {
                            $adtSession.SetExitCode($result.ExitCode)
                        }
                        if ($ExitOnProcessFailure)
                        {
                            $iafehParams.ErrorAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
                        }
                    }

                    # Process the error and potentially close out the session.
                    # This isn't logged as it's already been logged before the throw.
                    Invoke-ADTFunctionErrorHandler @iafehParams -Silent
                    if ($iafehParams.ContainsKey('ErrorAction'))
                    {
                        Close-ADTSession
                    }
                    break
                }
                '^(System\.(TimeoutException|OperationCanceledException))$'
                {
                    # Process the ErrorRecord.
                    if ($PSBoundParameters.ContainsKey('TimeoutAction'))
                    {
                        $iafehParams.ErrorAction = $TimeoutAction
                    }
                    Invoke-ADTFunctionErrorHandler @iafehParams -DisableErrorResolving
                    break
                }
                default
                {
                    # This is the handler for any other error/exception that may occur.
                    Invoke-ADTFunctionErrorHandler @iafehParams -LogMessage "Error occurred while attempting to start the specified process."
                    break
                }
            }

            # If the passthru switch is specified, return the exit code and any output from process.
            # Of course this will only work if the caller has specified a suitable ErrorAction.
            if ($PassThru)
            {
                if ($result)
                {
                    Write-ADTLogEntry -Message 'PassThru parameter specified, returning execution results object.'
                    return $result
                }
                Write-ADTLogEntry -Message 'PassThru parameter specified, however no result was available.'
            }
        }
        finally
        {
            if ($cancellationTokenSource)
            {
                $cancellationToken = $null
                $cancellationTokenSource.Dispose()
            }
            [System.Environment]::SetEnvironmentVariable('SEE_MASK_NOZONECHECKS', $SEE_MASK_NOZONECHECKS)
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
