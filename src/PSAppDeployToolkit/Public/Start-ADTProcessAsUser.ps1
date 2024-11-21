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
        Path to the executable to invoke.

    .PARAMETER ArgumentList
        Arguments for the invoked executable.

    .PARAMETER WorkingDirectory
        The 'start-in' directory for the invoked executable.

    .PARAMETER HideWindow
        Specifies whether the window should be hidden or not.

    .PARAMETER ProcessCreationFlags
        One or more flags to control the process's invocation.

    .PARAMETER InheritEnvironmentVariables
        Specifies whether the process should inherit the user's environment state.

    .PARAMETER Wait
        Specifies whether to wait for the invoked excecutable to finish.

    .PARAMETER Username
        The username of the user's session to invoke the executable in.

    .PARAMETER SessionId
        The session ID of the user to invoke the executable in.

    .PARAMETER AllActiveUserSessions
        Specifies that the executable should be invoked in all active sessions.

    .PARAMETER UseLinkedAdminToken
        Specifies that an admin token (if available) should be used for the invocation.

    .PARAMETER SuccessExitCodes
        Specifies one or more exit codes that the function uses to consider the invocation successful.

    .PARAMETER ConsoleTimeoutInSeconds
        Specifies the timeout in seconds to wait for a console application to finish its task.

    .PARAMETER IsGuiApplication
        Indicates that the executed application is a GUI-based app, not a console-based app.

    .PARAMETER NoRedirectOutput
        Specifies that stdout/stderr output should not be redirected to file.

    .PARAMETER MergeStdErrAndStdOut
        Specifies that the stdout/stderr streams should be merged into a single output.

    .PARAMETER OutputDirectory
        Specifies the output directory for the redirected stdout/stderr streams.

    .PARAMETER NoTerminateOnTimeout
        Specifies that the process shouldn't terminate on timeout.

    .PARAMETER AdditionalEnvironmentVariables
        Specifies additional environment variables to inject into the user's session.

    .PARAMETER WaitOption
        Specifies the wait type to use when waiting for an invoked executable to finish.

    .PARAMETER SecureArgumentList
        Hides all parameters passed to the executable from the Toolkit log file.

    .PARAMETER PassThru
        If NoWait is not specified, returns an object with ExitCode, STDOut and STDErr output from the process. If NoWait is specified, returns an object with Id, Handle and ProcessName.

    .EXAMPLE
        Start-ADTProcessAsUser -FilePath "$($adtSession.DirFiles)\setup.exe" -ArgumentList '/S' -SuccessExitCodes 0, 500

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Threading.Tasks.Task[System.Int32]

        Returns a task object indicating the process's result.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding(DefaultParameterSetName = 'PrimaryActiveUserSession')]
    [OutputType([System.Threading.Tasks.Task[System.Int32]])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $true, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $true, ParameterSetName = 'PrimaryActiveUserSession')]
        [ValidateNotNullOrEmpty()]
        [System.String]$FilePath,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ArgumentList,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [ValidateNotNullOrEmpty()]
        [System.String]$WorkingDirectory,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [System.Management.Automation.SwitchParameter]$HideWindow,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [ValidateNotNullOrEmpty()]
        [PSADT.PInvoke.CREATE_PROCESS]$ProcessCreationFlags,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [System.Management.Automation.SwitchParameter]$InheritEnvironmentVariables,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$Wait,

        [Parameter(Mandatory = $true, ParameterSetName = 'Username')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Username,

        [Parameter(Mandatory = $true, ParameterSetName = 'SessionId')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$SessionId,

        [Parameter(Mandatory = $true, ParameterSetName = 'AllActiveUserSessions')]
        [System.Management.Automation.SwitchParameter]$AllActiveUserSessions,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [System.Management.Automation.SwitchParameter]$UseLinkedAdminToken,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$SuccessExitCodes,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$ConsoleTimeoutInSeconds,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [System.Management.Automation.SwitchParameter]$IsGuiApplication,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [System.Management.Automation.SwitchParameter]$NoRedirectOutput,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [System.Management.Automation.SwitchParameter]$MergeStdErrAndStdOut,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [ValidateNotNullOrEmpty()]
        [System.String]$OutputDirectory,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [System.Management.Automation.SwitchParameter]$NoTerminateOnTimeout,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [ValidateNotNullOrEmpty()]
        [System.Collections.IDictionary]$AdditionalEnvironmentVariables,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [ValidateNotNullOrEmpty()]
        [PSADT.ProcessEx.WaitType]$WaitOption,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [System.Management.Automation.SwitchParameter]$SecureArgumentList,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        # Initialise function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Strip out parameters not destined for the C# code.
        $null = ('SecureArgumentList', 'PassThru').ForEach({
                if ($PSBoundParameters.ContainsKey($_))
                {
                    $PSBoundParameters.Remove($_)
                }
            })

        # If we're on the default parameter set, pass the right parameter through.
        if ($PSCmdlet.ParameterSetName.Equals('PrimaryActiveUserSession'))
        {
            $PSBoundParameters.Add('PrimaryActiveUserSession', [System.Management.Automation.SwitchParameter]$true)
        }

        # Translate a provided username into a session Id.
        if ($PSBoundParameters.ContainsKey('Username'))
        {
            $SessionId = Get-ADTLoggedOnUser | & { process { if ($_.NTAccount.EndsWith($Username, [System.StringComparison]::InvariantCultureIgnoreCase)) { return $_ } } } | Select-Object -First 1 -ExpandProperty SessionId
            $PSBoundParameters.Add('SessionId', $SessionId)
            $null = $PSBoundParameters.Remove('Username')
        }

        # Translate the environment variables into a dictionary. Using this type on the parameter is too hard on the caller.
        if ($PSBoundParameters.ContainsKey('AdditionalEnvironmentVariables'))
        {
            $AdditionalEnvironmentVariables = [System.Collections.Generic.Dictionary[System.String, System.String]]::new()
            $PSBoundParameters.AdditionalEnvironmentVariables.GetEnumerator() | & {
                process
                {
                    $AdditionalEnvironmentVariables.Add($_.Key, $_.Value)
                }
            }
            $PSBoundParameters.AdditionalEnvironmentVariables = $AdditionalEnvironmentVariables
        }

        # Translate switches that require negation for the LaunchOptions.
        $null = ('RedirectOutput', 'TerminateOnTimeout').Where({ $PSBoundParameters.ContainsKey("No$_") }).ForEach({
                $PSBoundParameters.$_ = !$PSBoundParameters."No$_"
                $PSBoundParameters.Remove("No$_")
            })

        # Unless explicitly provided, don't terminate on timeout.
        if (!$PSBoundParameters.ContainsKey('TerminateOnTimeout'))
        {
            $PSBoundParameters.TerminateOnTimeout = $false
        }

        # Translate the process flags into a list of flags. No idea why the backend is coded like this...
        if ($PSBoundParameters.ContainsKey('ProcessCreationFlags'))
        {
            $PSBoundParameters.ProcessCreationFlags = $PSBoundParameters.ProcessCreationFlags.ToString().Split(',').Trim()
        }
    }

    process
    {
        # Announce start.
        switch ($PSCmdlet.ParameterSetName)
        {
            Username
            {
                Write-ADTLogEntry -Message "Invoking [$FilePath$(if (!$SecureArgumentList) { " $ArgumentList" })] as user [$Username]$(if ($Wait) { ", and waiting for invocation to finish" })."
                break
            }
            SessionId
            {
                Write-ADTLogEntry -Message "Invoking [$FilePath$(if (!$SecureArgumentList) { " $ArgumentList" })] for session [$SessionId]$(if ($Wait) { ", and waiting for invocation to finish" })."
                break
            }
            AllActiveUserSessions
            {
                Write-ADTLogEntry -Message "Invoking [$FilePath$(if (!$SecureArgumentList) { " $ArgumentList" })] for all active user sessions$(if ($Wait) { ", and waiting for all invocations to finish" })."
                break
            }
            PrimaryActiveUserSession
            {
                Write-ADTLogEntry -Message "Invoking [$FilePath$(if (!$SecureArgumentList) { " $ArgumentList" })] for the primary user session$(if ($Wait) { ", and waiting for invocation to finish" })."
                break
            }
        }

        # Create a new process object and invoke an execution.
        try
        {
            try
            {
                if (($result = ($process = [PSADT.ProcessEx.StartProcess]::new()).ExecuteAndMonitorAsync($PSBoundParameters)) -and $PassThru)
                {
                    return $result
                }
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Object ensures the correct PositionMessage is used.
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
        finally
        {
            # Dispose of the process object to ensure things are cleaned up properly.
            $process.Dispose()
        }
    }

    end
    {
        # Finalise function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
