#-----------------------------------------------------------------------------
#
# MARK: Start-ADTProcessAsUser
#
#-----------------------------------------------------------------------------

function Start-ADTProcessAsUser
{
    <#
    .SYNOPSIS
        Invokes a process in another user's session.

    .DESCRIPTION
        Invokes a process from SYSTEM in another user's session.

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

    .PARAMETER WindowStyle
        Style of the window of the process executed. Options: Normal, Hidden, Maximized, Minimized. Only works for native Windows GUI applications. If the WindowStyle is set to Hidden, UseShellExecute should be set to $true.

        Note: Not all processes honor WindowStyle. WindowStyle is a recommendation passed to the process. They can choose to ignore it.

    .PARAMETER CreateNoWindow
        Specifies whether the process should be started with a new window to contain it.

    .PARAMETER NoWait
        Immediately continue after executing the process.

    .PARAMETER WaitForMsiExec
        Sometimes an EXE bootstrapper will launch an MSI install. In such cases, this variable will ensure that this function waits for the msiexec engine to become available before starting the install.

    .PARAMETER Timeout
        How long to wait for the process before timing out.

    .PARAMETER TimeoutAction
        What action to take on timeout. Follows ErrorAction if not specified.

    .PARAMETER NoTerminateOnTimeout
        Indicates that the process should not be terminated on timeout. Only supported for GUI-based applications, or when -CreateNoWindow isn't specified.

    .PARAMETER SuccessExitCodes
        List of exit codes to be considered successful. Defaults to values set during ADTSession initialization, otherwise: 0

    .PARAMETER RebootExitCodes
        List of exit codes to indicate a reboot is required. Defaults to values set during ADTSession initialization, otherwise: 1641, 3010

    .PARAMETER IgnoreExitCodes
        List the exit codes to ignore or * to ignore all exit codes.

    .PARAMETER ExitOnProcessFailure
        Automatically closes the active deployment session via Close-ADTSession in the event the process exits with a non-success or non-ignored exit code.

    .PARAMETER PriorityClass
        Specifies priority class for the process. Options: Idle, Normal, High, AboveNormal, BelowNormal, RealTime.

    .PARAMETER PassThru
        If NoWait is not specified, returns an object with ExitCode, STDOut and STDErr output from the process. If NoWait is specified, returns an object with Id, Handle and ProcessName.

    .EXAMPLE
        Start-ADTProcessAsUser -FilePath 'setup.exe' -ArgumentList '/S' -IgnoreExitCodes 1,2

        Launch InstallShield "setup.exe" from the ".\Files" sub-directory.

    .EXAMPLE
        Start-ADTProcessAsUser -FilePath "$($adtSession.DirFiles)\Bin\setup.exe" -ArgumentList '/S' -WindowStyle 'Hidden'

        Launch InstallShield "setup.exe" from the ".\Files\Bin" sub-directory.

    .EXAMPLE
        Start-ADTProcessAsUser -FilePath 'uninstall_flash_player_64bit.exe' -ArgumentList '/uninstall' -WindowStyle 'Hidden'

        If the file is in the "Files" directory of the AppDeployToolkit, only the file name needs to be specified.

    .EXAMPLE
        Start-ADTProcessAsUser -FilePath 'setup.exe' -ArgumentList "-s -f2`"$((Get-ADTConfig).Toolkit.LogPath)\$($adtSession.InstallName).log`""

        Launch InstallShield "setup.exe" from the ".\Files" sub-directory and force log files to the logging folder.

    .EXAMPLE
        Start-ADTProcessAsUser -FilePath 'setup.exe' -ArgumentList "/s /v`"ALLUSERS=1 /qn /L* `"$((Get-ADTConfig).Toolkit.LogPath)\$($adtSession.InstallName).log`"`""

        Launch InstallShield "setup.exe" with embedded MSI and force log files to the logging folder.

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
    [OutputType([PSADT.Execution.ProcessResult])]
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
        [System.String]$WorkingDirectory,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Username,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$UseLinkedAdminToken,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$InheritEnvironmentVariables,

        # Window Option: WindowStyle (only in sets where window is "WindowStyle")
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_WindowStyle_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_WindowStyle_Timeout')]
        [ValidateNotNullOrEmpty()]
        [System.Diagnostics.ProcessWindowStyle]$WindowStyle,

        # Window Option: CreateNoWindow (only in sets where window is "CreateNoWindow")
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateNoWindow_Wait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateNoWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [Switch]$CreateNoWindow,

        # Wait Option: NoWait (only in sets where wait is "NoWait")
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateWindow_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_WindowStyle_NoWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateNoWindow_NoWait')]
        [Switch]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$WaitForMsiExec,

        # Wait Option: Timeout (only in sets where wait is "Timeout")
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateWindow_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_WindowStyle_Timeout')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$Timeout,

        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ActionPreference]$TimeoutAction,

        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateWindow_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_WindowStyle_Timeout')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Default_CreateNoWindow_Timeout')]
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

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    dynamicparam
    {
        # Set up the -MsiExecWaitTime parameter if the parameter set is appropriate.
        if (!$PSBoundParameters.ContainsKey('WaitForMsiExec') -or !$PSBoundParameters.WaitForMsiExec)
        {
            return
        }

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in parameters we need as mandatory when there's no active ADTSession.
        $paramDictionary.Add('MsiExecWaitTime', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'MsiExecWaitTime', [System.TimeSpan], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false; HelpMessage = "Specify the length of time in seconds to wait for the msiexec engine to become available." }
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
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Just farm it out to Start-ADTProcess as it can do it all.
        Write-ADTLogEntry -Message "Starting process for user [$Username]..."
        try
        {
            return Start-ADTProcess @PSBoundParameters
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
