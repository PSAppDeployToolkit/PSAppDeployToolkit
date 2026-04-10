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
        The working directory used for executing the process. Defaults to DirFiles if there is an active DeploymentSession. The use of UseShellExecute affects this parameter.

    .PARAMETER RunAsActiveUser
        A RunAsActiveUser object to invoke the process as.

    .PARAMETER UseLinkedAdminToken
        Use a user's linked administrative token while running the process under their context.

    .PARAMETER UseHighestAvailableToken
        Use a user's linked administrative token if it's available while running the process under their context.

    .PARAMETER DenyUserTermination
        Specifies that users cannot terminate the process started in their context. The user will still be able to terminate the process if they're an administrator, though.

    .PARAMETER InheritEnvironmentVariables
        Specifies whether the process running as a user should inherit the SYSTEM account's environment variables.

    .PARAMETER UseUnelevatedToken
        If the current process is elevated, starts the new process unelevated using the user's unelevated linked token.

    .PARAMETER ExpandEnvironmentVariables
        Specifies whether to expand any Windows/DOS-style environment variables in the specified FilePath/ArgumentList.

    .PARAMETER UseShellExecute
        Specifies whether to use the operating system shell to start the process. $true if the shell should be used when starting the process; $false if the process should be created directly from the executable file.

        The word "Shell" in this context refers to a graphical shell (similar to the Windows shell) rather than command shells (for example, bash or sh) and lets users launch graphical applications or open documents. It lets you open a file or a url and the Shell will figure out the program to open it with.

        The WorkingDirectory property behaves differently depending on the value of the UseShellExecute property. When UseShellExecute is true, the WorkingDirectory property specifies the location of the executable. When UseShellExecute is false, the WorkingDirectory property is not used to find the executable. Instead, it is used only by the process that is started and has meaning only within the context of the new process.

        If you set UseShellExecute to $true, there will be no available output from the process.

    .PARAMETER Verb
        The verb to use when doing a ShellExecute invocation. Common usages are "runas" to trigger a UAC elevation of the process.

    .PARAMETER WindowStyle
        Style of the window of the process executed. Options: Normal, Hidden, Maximized, Minimized. Only works for native Windows GUI applications. If the WindowStyle is set to Hidden, UseShellExecute should be set to $true.

        Note: Not all processes honor WindowStyle. WindowStyle is a recommendation passed to the process. They can choose to ignore it.

    .PARAMETER CreateNoWindow
        Specifies whether the process should be started with a new window to contain it.

    .PARAMETER StreamEncoding
        Specifies the encoding type to use when reading stdin/stdout/stderr. Some apps like WinGet encode using UTF8, which will corrupt if incorrectly set.

    .PARAMETER StandardInput
        Specifies a string to write to the process's stdin stream. This is handy for answering known prompts, etc.

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
        List the exit codes to ignore or * to ignore all exit codes. Where possible, please use `-SuccessExitCodes` and/or `-RebootExitCodes` instead, or `-ErrorAction SilentlyContinue` as this parameter is deprecated and will be removed in PSAppDeployToolkit 4.3.0.

    .PARAMETER PriorityClass
        Specifies priority class for the process. Options: Idle, Normal, High, AboveNormal, BelowNormal, RealTime.

    .PARAMETER ExitOnProcessFailure
        Automatically closes the active deployment session via Close-ADTSession in the event the process exits with a non-success or non-ignored exit code.

    .PARAMETER NoWait
        Immediately continue after executing the process.

    .PARAMETER PassThru
        If `-NoWait` is not specified, returns an object with ExitCode, StdOut, and StdErr output from the process. If `-NoWait` is specified, returns a task that can be awaited. Note that a failed execution will only return an object if either `-ErrorAction` is set to `SilentlyContinue`/`Ignore`, or if `-SuccessExitCodes` is used.

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
        Start-ADTProcess -FilePath 'setup.exe' -ArgumentList "/s /v`"ALLUSERS=1 /qn /L* `"$((Get-ADTConfig).Toolkit.LogPath)\$($adtSession.InstallName).log`"`"" -Timeout 00:10:00

        Launch InstallShield "setup.exe" with embedded MSI and force log files to the logging folder, terminating the process if it takes longer than 10 minutes to complete.

    .EXAMPLE
        Start-ADTProcess -FilePath 'setup.exe' -ArgumentList "/s /v`"ALLUSERS=1 /qn /L* `"$((Get-ADTConfig).Toolkit.LogPath)\$($adtSession.InstallName).log`"`"" -Timeout (New-TimeSpan -Minutes 10)

        Launch InstallShield "setup.exe" with embedded MSI and force log files to the logging folder, terminating the process if it takes longer than 10 minutes to complete.

    .EXAMPLE
        $result = Start-ADTProcess -FilePath "setup.exe" -ArgumentList "-i -f `"$($adtSession.DirFiles)\licenseFile.lic`"" -CreateNoWindow -ErrorAction SilentlyContinue -PassThru

        Launch "setup.exe" with -PassThru so we can capture the exit code and stdout/stderr from the executable if it's a console application.

    .EXAMPLE
        $result = Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'echo Testing stdout capture. & exit 0' -CreateNoWindow -PassThru

        Launch cmd.exe to echo out a message to stdout, specifically taking advantage of our `-ArgumentList` array support to avoid escaped quote issues.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        By default, this function returns no output.

    .OUTPUTS
        PSADT.ProcessManagement.ProcessResult

        Returns an object with the results of the installation if -PassThru is specified.
        - ProcessId
        - ExitCode
        - StdOut
        - StdErr
        - Interleaved

    .OUTPUTS
        PSADT.ProcessManagement.ProcessHandle

        Returns an object with the handle of the installation process if -PassThru and -NoWait are specified.
        - Process
        - LaunchInfo
        - CommandLine
        - Task

    .NOTES
        An active ADT session is NOT required to use this function.

        This function supports the -WhatIf and -Confirm parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Start-ADTProcess
    #>

    [CmdletBinding(SupportsShouldProcess = $true, DefaultParameterSetName = 'Default_CreateWindow_Wait')]
    [OutputType([PSADT.ProcessManagement.ProcessResult])]
    [OutputType([PSADT.ProcessManagement.ProcessHandle])]
    param
    (
        [Parameter(Mandatory = $true)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$FilePath,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.AllowEmptyButNotNullOrWhiteSpace()]
        [System.String[]]$ArgumentList,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SecureArgumentList,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$WorkingDirectory,

        # Identity: RunAsActiveUser (only present in sets where identity is "RunAsActiveUser")
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_CreateWindow_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_CreateWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_CreateWindow_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_WindowStyle_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_WindowStyle_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
        [ValidateNotNullOrEmpty()]
        [PSADT.Foundation.RunAsActiveUser]$RunAsActiveUser,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
        [System.Management.Automation.SwitchParameter]$UseLinkedAdminToken,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
        [System.Management.Automation.SwitchParameter]$UseHighestAvailableToken,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
        [System.Management.Automation.SwitchParameter]$DenyUserTermination,

        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
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

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExpandEnvironmentVariables,

        # Identity: UseShellExecute (available in UseShellExecute and RunAsActiveUser sets)
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateWindow_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateWindow_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_WindowStyle_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_WindowStyle_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
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
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$Verb,

        # Window Option: WindowStyle (only in sets where window is "WindowStyle")
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_WindowStyle_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_WindowStyle_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_WindowStyle_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_WindowStyle_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_WindowStyle_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_WindowStyle_Timeout')]
        [ValidateNotNullOrEmpty()]
        [System.Diagnostics.ProcessWindowStyle]$WindowStyle,

        # Window Option: CreateNoWindow (only in sets where window is "CreateNoWindow")
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [System.Management.Automation.SwitchParameter]$CreateNoWindow,

        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
        [ValidateNotNullOrEmpty()]
        [System.Text.Encoding]$StreamEncoding,

        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String[]]$StandardInput,

        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
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
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_CreateWindow_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_WindowStyle_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
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
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ActionPreference]$TimeoutAction,

        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [System.Management.Automation.SwitchParameter]$NoTerminateOnTimeout,

        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_WindowStyle_Wait')]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$SuccessExitCodes,

        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_WindowStyle_Wait')]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$RebootExitCodes,

        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_WindowStyle_Wait')]
        [System.Obsolete("Please use '-ErrorAction SilentlyContinue' instead as this will be removed in PSAppDeployToolkit 4.3.0.")]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [SupportsWildcards()]
        [System.String[]]$IgnoreExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Diagnostics.ProcessPriorityClass]$PriorityClass,

        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'RunAsActiveUser_WindowStyle_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_CreateWindow_Wait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UseShellExecute_WindowStyle_Wait')]
        [System.Management.Automation.SwitchParameter]$ExitOnProcessFailure,

        # Wait Option: NoWait (only in sets where wait is "NoWait")
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_CreateWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'RunAsActiveUser_CreateNoWindow_NoWait')]
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
        $canSetExitCode = $true

        # Log the deprecation of -IgnoreExitCodes to the log.
        if ($PSBoundParameters.ContainsKey('IgnoreExitCodes'))
        {
            Write-ADTLogEntry -Message "The parameter [-IgnoreExitCodes] is obsolete and will be removed in PSAppDeployToolkit 4.3.0. Please use [-SuccessExitCodes]/[-RebootExitCodes] for known exit codes, or [-ErrorAction SilentlyContinue] for ignoring any exit code instead." -Severity Warning
        }

        # Set up defaults if not specified.
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
        else
        {
            $canSetExitCode = $false
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
        else
        {
            $canSetExitCode = $false
        }
        if (!$PSBoundParameters.ContainsKey('MsiExecWaitTime'))
        {
            if (!$adtSession)
            {
                Initialize-ADTModuleIfUninitialized -Cmdlet $PSCmdlet
            }
            $MsiExecWaitTime = [System.TimeSpan]::FromSeconds((Get-ADTConfig).MSI.MutexWaitTime)
        }

        # Set up initial variables.
        $funcCaller = Get-PSCallStack | Select-Object -Skip 1 | Select-Object -First 1 | & { process { $_.InvocationInfo.MyCommand } }
        $extInvoker = !$funcCaller -or !$funcCaller.Source.StartsWith($MyInvocation.MyCommand.Module.Name) -or $funcCaller.Name.Equals('Start-ADTMsiProcess')
        $SEE_MASK_NOZONECHECKS = [PSADT.Utilities.EnvironmentUtilities]::GetEnvironmentVariable('SEE_MASK_NOZONECHECKS')
        [PSADT.Utilities.EnvironmentUtilities]::SetEnvironmentVariable('SEE_MASK_NOZONECHECKS', 1)

        # Set up cancellation token.
        $cancellationTokenSource = if ($Timeout)
        {
            [System.Threading.CancellationTokenSource]::new($Timeout)
        }
        $cancellationToken = if ($cancellationTokenSource)
        {
            $cancellationTokenSource.Token
        }

        # Set up the elevation type to use for the new process.
        $elevatedTokenType = if ($RunAsActiveUser)
        {
            if ($UseLinkedAdminToken)
            {
                [PSADT.Security.ElevatedTokenType]::HighestMandatory
            }
            elseif ($UseHighestAvailableToken)
            {
                [PSADT.Security.ElevatedTokenType]::HighestAvailable
            }
            elseif ($RunAsActiveUser -eq [PSADT.AccountManagement.AccountUtilities]::CallerRunAsActiveUser)
            {
                [PSADT.Security.ElevatedTokenType]::None
            }
        }
        elseif ($UseUnelevatedToken)
        {
            [PSADT.Security.ElevatedTokenType]::None
        }

        # Internal worker function to set the session exit code.
        function Set-ADTSessionExitCode
        {
            [CmdletBinding()]
            param
            (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.Nullable[System.Int32]]$ExitCode
            )

            # Throw if there's no active session; the caller didn't do their homework.
            if (!$adtSession)
            {
                $naerParams = @{
                    Exception = [System.InvalidProgramException]::new("The function [Start-ADTProcess] attempted to set a session exit code, but no deployment session is active.")
                    Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                    ErrorId = 'NoActiveAdtDeploymentSession'
                    TargetObject = $ExitCode
                    RecommendedAction = "Please report this to the PSAppDeployToolkit team for further review."
                }
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }
            if (!$canSetExitCode)
            {
                $naerParams = @{
                    Exception = [System.InvalidProgramException]::new("The function [Start-ADTProcess] is attempting to set a session exit code when it shouldn't.")
                    Category = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                    ErrorId = 'SetAdtDeploymentSessionExitCodeError'
                    TargetObject = $ExitCode
                    RecommendedAction = "Please report this to the PSAppDeployToolkit team for further review."
                }
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }

            # Start working out whether we can set the exit code or not.
            $adtSessionStatus = $adtSession.GetDeploymentStatus()
            $isSuccessCode = $SuccessExitCodes.Contains($ExitCode)
            $isRestartCode = $RebootExitCodes.Contains($ExitCode)
            $isFailureCode = !$isSuccessCode -and !$isRestartCode
            if ($isFailureCode -and ($adtSessionStatus -le [PSAppDeployToolkit.Foundation.DeploymentStatus]::Error))
            {
                $adtSession.SetExitCode($ExitCode)
                return
            }
            if ($isRestartCode -and ($adtSessionStatus -le [PSAppDeployToolkit.Foundation.DeploymentStatus]::RestartRequired))
            {
                $adtSession.SetExitCode($ExitCode)
                return
            }
            if ($isSuccessCode -and ($adtSessionStatus -le [PSAppDeployToolkit.Foundation.DeploymentStatus]::Complete))
            {
                $adtSession.SetExitCode($ExitCode)
                return
            }
        }
    }

    process
    {
        # Commence the underlying execution process.
        Write-ADTLogEntry -Message "Preparing to execute process [$FilePath]$(if ($RunAsActiveUser) {" for user [$($RunAsActiveUser.NTAccount)]"})..."
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
                if ((!$FilePath.Contains('%') -or !$ExpandEnvironmentVariables) -and ![PSADT.FileSystem.FileSystemUtilities]::IsPathFullyQualified($FilePath))
                {
                    $rafspParams = @{
                        LiteralPath = $FilePath
                        File = $true
                        DefaultExtension = '.exe'
                    }
                    if ($PSBoundParameters.ContainsKey('WorkingDirectory'))
                    {
                        $rafspParams.Add('ExtraPaths', $WorkingDirectory)
                    }
                    $fqPath = try
                    {
                        Resolve-ADTFileSystemPath @rafspParams
                    }
                    catch
                    {
                        if (!$UseShellExecute)
                        {
                            throw
                        }
                    }
                    if ($fqPath)
                    {
                        Write-ADTLogEntry -Message "File path [$FilePath] successfully resolved to fully qualified path [$fqPath]."
                        $FilePath = $fqPath
                    }
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
                        Write-ADTLogEntry -Message 'Another MSI installation is already in progress and needs to be completed before proceeding with this installation.' -Severity Error
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

                # Set the working directory when running in a session if the caller hasn't specified one.
                # For non-msiexec situations, use the process's path for backwards compat, otherwise use $adtSession.DirFiles if defined.
                # We don't do this when a session isn't running so `Start-ADTProcess` works the way one should expect (i.e. like `Start-Process`).
                if ($adtSession -and !$PSBoundParameters.ContainsKey('WorkingDirectory'))
                {
                    if ([System.IO.Path]::HasExtension($FilePath) -and [PSADT.FileSystem.FileSystemUtilities]::IsPathFullyQualified($FilePath) -and ($FilePath -notmatch 'msiexec'))
                    {
                        $PSBoundParameters.WorkingDirectory = [System.IO.Path]::GetDirectoryName($FilePath)
                    }
                    elseif (![System.String]::IsNullOrWhiteSpace($adtSession.DirFiles))
                    {
                        $PSBoundParameters.WorkingDirectory = $adtSession.DirFiles
                    }
                }

                # Properly handle nullable strings before instantiating our launch info.
                if (!$PSBoundParameters.ContainsKey('WorkingDirectory'))
                {
                    $PSBoundParameters.Add('WorkingDirectory', [System.Management.Automation.Language.NullString]::Value)
                }
                if (!$PSBoundParameters.ContainsKey('Verb'))
                {
                    $PSBoundParameters.Add('Verb', [System.Management.Automation.Language.NullString]::Value)
                }

                # Set up the process start flags.
                $launchData = if (!($RunAsActiveUser -and $UseShellExecute))
                {
                    [PSADT.ProcessManagement.ProcessLaunchInfo]::new(
                        $FilePath,
                        $ArgumentList,
                        $PSBoundParameters.WorkingDirectory,
                        $RunAsActiveUser,
                        $InheritEnvironmentVariables,
                        $ExpandEnvironmentVariables,
                        $DenyUserTermination,
                        $elevatedTokenType,
                        $StandardInput,
                        $UseShellExecute,
                        $PSBoundParameters.Verb,
                        $CreateNoWindow,
                        $WaitForChildProcesses,
                        $KillChildProcessesWithParent,
                        $StreamEncoding,
                        $WindowStyle,
                        $PriorityClass,
                        $cancellationToken,
                        $NoTerminateOnTimeout
                    )
                }
                else
                {
                    [PSADT.ProcessManagement.UserShellExecuteOptions]::new(
                        $FilePath,
                        $ArgumentList,
                        $PSBoundParameters.WorkingDirectory,
                        $ExpandEnvironmentVariables,
                        $PSBoundParameters.Verb,
                        $CreateNoWindow,
                        $WaitForChildProcesses,
                        $KillChildProcessesWithParent,
                        $WindowStyle,
                        $PriorityClass
                    )
                }

                # Perform all logging.
                if ($UseShellExecute)
                {
                    Write-ADTLogEntry -Message 'UseShellExecute is set to true, StdOut/StdErr streams will not be available.'
                }
                elseif (!$CreateNoWindow)
                {
                    Write-ADTLogEntry -Message 'CreateNoWindow not specified, StdOut/StdErr streams will not be available.'
                }
                if (![System.String]::IsNullOrWhiteSpace($launchData.WorkingDirectory))
                {
                    Write-ADTLogEntry -Message "Working Directory is [$($launchData.WorkingDirectory)]."
                }
                if ($ArgumentList)
                {
                    if ($SecureArgumentList)
                    {
                        Write-ADTLogEntry -Message "Executing [`"$FilePath`" (Parameters Hidden)]$(if ($RunAsActiveUser) {" for user [$($RunAsActiveUser.NTAccount)]"})$(if ($NoWait) { " without waiting" })..."
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Executing [`"$FilePath`" $(if ($ArgumentList.Length -gt 1) { [PSADT.ProcessManagement.CommandLineUtilities]::ArgumentListToCommandLine($ArgumentList) } else { $ArgumentList[0] })]$(if ($RunAsActiveUser) {" for user [$($RunAsActiveUser.NTAccount)]"})$(if ($NoWait) { " without waiting" })..."
                    }
                }
                else
                {
                    Write-ADTLogEntry -Message "Executing [`"$FilePath`"]$(if ($RunAsActiveUser) {" for user [$($RunAsActiveUser.NTAccount)]"})$(if ($NoWait) { " without waiting" })..."
                }

                # Start the process.
                if (!$PSCmdlet.ShouldProcess("Process [$FilePath]", 'Execute'))
                {
                    return
                }
                $execution = if ($launchData -is [PSADT.ProcessManagement.UserShellExecuteOptions])
                {
                    Invoke-ADTClientServerOperation -User $RunAsActiveUser -ShellExecuteProcess -Options $launchData -NoWait:$NoWait
                }
                else
                {
                    [PSADT.ProcessManagement.ProcessManager]::LaunchAsync($launchData)
                }

                # Handle if the returned value is null. The `Out-String` setup primes the Process object.
                if ([System.String]::IsNullOrWhiteSpace(($execution | Out-String)))
                {
                    # A null result without using ShellExecute is entirely unexpected.
                    if (!$UseShellExecute)
                    {
                        $naerParams = @{
                            Exception = [System.InvalidProgramException]::new("The launching of the process returned a null result.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                            ErrorId = 'ProcessExecutionNullReturn'
                            TargetObject = $(if (!$SecureArgumentList) { $execution })
                            RecommendedAction = "Please report this to the PSAppDeployToolkit development team for further review."
                        }
                        throw (New-ADTErrorRecord @naerParams)
                    }
                    Write-ADTLogEntry "Successfully executed the requested ShellExecute command."
                    return
                }

                # NoWait specified, return process details. If it isn't specified, start reading standard Output and Error streams.
                if (($execution -is [PSADT.ProcessManagement.ProcessHandle]) -and $NoWait)
                {
                    if ($PassThru)
                    {
                        Write-ADTLogEntry -Message 'PassThru parameter specified, returning task for external tracking.'
                        return $execution
                    }
                    return
                }
                $result = if ($execution -is [PSADT.ProcessManagement.ProcessHandle])
                {
                    $execution.Task.GetAwaiter().GetResult()
                }
                else
                {
                    $execution
                }

                # Check whether the process timed out.
                if ($result.ExitCode -eq [PSADT.ProcessManagement.ProcessManager]::TimeoutExitCode)
                {
                    $naerParams = if ($NoTerminateOnTimeout)
                    {
                        @{
                            Exception = [System.TimeoutException]::new("Timed out waiting for process execution to complete.")
                            Category = [System.Management.Automation.ErrorCategory]::LimitsExceeded
                            ErrorId = 'ProcessExecutionTimedOut'
                            TargetObject = $(if (!$SecureArgumentList) { $result })
                        }
                    }
                    else
                    {
                        @{
                            Exception = [System.OperationCanceledException]::new("Process terminated because execution took too long to complete.", $cancellationToken)
                            Category = [System.Management.Automation.ErrorCategory]::OperationStopped
                            ErrorId = 'ProcessExecutionCancelled'
                            TargetObject = $(if (!$SecureArgumentList) { $result })
                        }
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Check to see whether we should ignore exit codes.
                if ($ArgumentList)
                {
                    if ($SecureArgumentList)
                    {
                        Write-ADTLogEntry -Message "Finished execution of [$(if (($command = [PSADT.ProcessManagement.CommandLineUtilities]::CommandLineToArgumentList($execution.CommandLine)[0]).Contains(' ')) { [System.String]::Format('"{0}"', $command) } else { $command }) (Parameters Hidden)]."
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Finished execution of [$($execution.CommandLine)]."
                    }
                }
                else
                {
                    Write-ADTLogEntry -Message "Finished execution of [$($execution.CommandLine)]."
                }
                $waleParams = if (($ignoreExitCode = $IgnoreExitCodes -and ($($IgnoreExitCodes).Equals('*') -or ([System.Int32[]]$IgnoreExitCodes).Contains($result.ExitCode))))
                {
                    @{ Message = "Execution completed and the exit code [$($result.ExitCode)] is being ignored."; Severity = [PSAppDeployToolkit.Logging.LogSeverity]::Info }
                }
                elseif ($SuccessExitCodes.Contains($result.ExitCode))
                {
                    @{ Message = "Execution completed successfully with exit code [$($result.ExitCode)]."; Severity = [PSAppDeployToolkit.Logging.LogSeverity]::Success }
                }
                elseif ($RebootExitCodes.Contains($result.ExitCode))
                {
                    @{ Message = "Execution completed successfully with exit code [$($result.ExitCode)]. A reboot is required."; Severity = [PSAppDeployToolkit.Logging.LogSeverity]::Warning }
                }
                elseif (($result.ExitCode -eq 1605) -and ($FilePath -match 'msiexec'))
                {
                    @{ Message = "Execution failed with exit code [$($result.ExitCode)] because the product is not currently installed."; Severity = [PSAppDeployToolkit.Logging.LogSeverity]::Error }
                }
                elseif (($result.ExitCode -eq -2145124329) -and ($FilePath -match 'wusa'))
                {
                    @{ Message = "Execution failed with exit code [$($result.ExitCode)] because the Windows Update is not applicable to this system."; Severity = [PSAppDeployToolkit.Logging.LogSeverity]::Error }
                }
                elseif (($MsiExitCodeMessage = if ($FilePath -match 'msiexec') { Get-ADTMsiExitCodeMessage -MsiExitCode $result.ExitCode }))
                {
                    @{ Message = "Execution failed with exit code [$($result.ExitCode)]: $MsiExitCodeMessage"; Severity = [PSAppDeployToolkit.Logging.LogSeverity]::Error }
                }
                else
                {
                    @{ Message = "Execution failed with exit code [$($result.ExitCode)].$(if ($CreateNoWindow -and !$NoStreamLogging) { " Please check the log file for any available stdout/stderr information." })"; Severity = [PSAppDeployToolkit.Logging.LogSeverity]::Error }
                }
                Write-ADTLogEntry @waleParams

                # Log any stdout/stderr if it's available.
                if (!$NoStreamLogging)
                {
                    foreach ($property in ('StdOut', 'StdErr', 'Interleaved'))
                    {
                        $streamMessage = if ($result.$property)
                        {
                            if ($result.$property.Count -gt 1)
                            {
                                "`n`n$([System.String]::Join([System.Environment]::NewLine, $result.$property))"
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
                        Write-ADTLogEntry -Message "$property Output from Execution: $streamMessage" -HostLogStreamType ([PSAppDeployToolkit.Logging.HostLogStreamType]::None)
                    }
                }

                # If we have an error in our process, throw it and let the catch block handle it.
                if ($waleParams.Message.StartsWith("Execution failed"))
                {
                    $naerParams = @{
                        Exception = [System.Runtime.InteropServices.ExternalException]::new($waleParams.Message, $result.ExitCode)
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'ProcessExitCodeError'
                        TargetObject = $(if (!$SecureArgumentList) { $result })
                        RecommendedAction = "Please review the exit code with the vendor's documentation and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Update the session's last exit code with the value if externally called.
                if ($adtSession -and $extInvoker -and !$ignoreExitCode -and $canSetExitCode)
                {
                    Set-ADTSessionExitCode -ExitCode $result.ExitCode
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
            if ($SecureArgumentList)
            {
                $iafehParams.Add('ResolveErrorProperties', ($Script:CommandTable.'Resolve-ADTErrorRecord'.ScriptBlock.Ast.Body.ParamBlock.Parameters.Where({ $_.Name.VariablePath.UserPath.Equals('Property') }).DefaultValue.Pipeline.PipelineElements.Expression.Elements.Value | & { process { if (!$_.Equals('PositionMessage')) { return $_ } } }))
            }

            # Switch on the exception type's name.
            $sessionClosed = $false
            switch -Regex ($_.Exception.GetType().FullName)
            {
                '^System\.(Runtime\.InteropServices\.ExternalException|Threading\.SynchronizationLockException)$'
                {
                    # Handle requirements for when there's an active session.
                    if ($adtSession -and $extInvoker)
                    {
                        if (($OriginalErrorAction -notmatch '^(SilentlyContinue|Ignore)$') -and $canSetExitCode)
                        {
                            Set-ADTSessionExitCode -ExitCode $result.ExitCode
                        }
                        if ($ExitOnProcessFailure -and !$PSBoundParameters.ContainsKey('ErrorAction'))
                        {
                            $iafehParams.ErrorAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
                        }
                    }

                    # Process the error and potentially close out the session.
                    # This isn't logged as it's already been logged before the throw.
                    Invoke-ADTFunctionErrorHandler @iafehParams -Silent
                    if ($ExitOnProcessFailure)
                    {
                        $sessionClosed = $true
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
                    $null = $iafehParams.Remove('ResolveErrorProperties')
                    Invoke-ADTFunctionErrorHandler @iafehParams -DisableErrorResolving
                    break
                }
                default
                {
                    # This is the handler for any other error/exception that may occur.
                    Invoke-ADTFunctionErrorHandler @iafehParams -LogMessage "Error occurred while attempting to start the specified process." -ErrorAction Stop
                    break
                }
            }

            # Break if the session has closed as Close-ADTSession won't be able to break out of the above switch.
            if ($sessionClosed)
            {
                break
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
            if ($SEE_MASK_NOZONECHECKS)
            {
                [PSADT.Utilities.EnvironmentUtilities]::SetEnvironmentVariable('SEE_MASK_NOZONECHECKS', $SEE_MASK_NOZONECHECKS)
            }
            else
            {
                [PSADT.Utilities.EnvironmentUtilities]::RemoveEnvironmentVariable('SEE_MASK_NOZONECHECKS')
            }
            if ($cancellationTokenSource)
            {
                $cancellationTokenSource.Dispose()
            }
            if ($result -and !$PassThru)
            {
                $result.Dispose()
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
