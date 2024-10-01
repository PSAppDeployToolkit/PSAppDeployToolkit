#-----------------------------------------------------------------------------
#
# MARK: Start-ADTProcessAsUser
#
#-----------------------------------------------------------------------------

function Start-ADTProcessAsUser
{
    [CmdletBinding()]
    [OutputType([System.Threading.Tasks.Task[System.Int32]])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $true, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $true, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [ValidateNotNullOrEmpty()]
        [System.String]$FilePath,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ArgumentList,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [ValidateNotNullOrEmpty()]
        [System.String]$WorkingDirectory,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [System.Management.Automation.SwitchParameter]$HideWindow,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [ValidateNotNullOrEmpty()]
        [PSADT.PInvoke.CREATE_PROCESS]$ProcessCreationFlags,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [System.Management.Automation.SwitchParameter]$InheritEnvironmentVariables,

        [Parameter(Mandatory = $true, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $true, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$Wait,

        [Parameter(Mandatory = $true, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $true, ParameterSetName = 'UsernameWithWait')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Username,

        [Parameter(Mandatory = $true, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $true, ParameterSetName = 'SessionIdWithWait')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$SessionId,

        [Parameter(Mandatory = $true, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $true, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [System.Management.Automation.SwitchParameter]$AllActiveUserSessions,

        [Parameter(Mandatory = $true, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $true, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [System.Management.Automation.SwitchParameter]$PrimaryActiveUserSession,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [System.Management.Automation.SwitchParameter]$UseLinkedAdminToken,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [ValidateNotNullOrEmpty()]
        [Microsoft.PowerShell.ExecutionPolicy]$PsExecutionPolicy,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [System.Management.Automation.SwitchParameter]$BypassPsExecutionPolicy,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$SuccessExitCodes,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$ConsoleTimeoutInSeconds,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [System.Management.Automation.SwitchParameter]$IsGuiApplication,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [System.Management.Automation.SwitchParameter]$NoRedirectOutput,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [System.Management.Automation.SwitchParameter]$MergeStdErrAndStdOut,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [ValidateNotNullOrEmpty()]
        [System.String]$OutputDirectory,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [System.Management.Automation.SwitchParameter]$NoTerminateOnTimeout,

        [Parameter(Mandatory = $false, ParameterSetName = 'Username')]
        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionId')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSession')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [ValidateNotNullOrEmpty()]
        [System.Collections.IDictionary]$AdditionalEnvironmentVariables,

        [Parameter(Mandatory = $false, ParameterSetName = 'UsernameWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'SessionIdWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllActiveUserSessionsWithWait')]
        [Parameter(Mandatory = $false, ParameterSetName = 'PrimaryActiveUserSessionWithWait')]
        [ValidateNotNullOrEmpty()]
        [PSADT.ProcessEx.WaitType]$WaitOption
    )

    begin
    {
        # Initialise function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Translate a provided username into a session Id.
        if ($PSBoundParameters.ContainsKey('Username'))
        {
            $SessionId = [PSADT.QueryUser]::GetUserSessionInfo() | & { process { if ($_.NTAccount -eq $Username) { return $_ } } } | & $Script:CommandTable.'Select-Object' -First 1 -ExpandProperty SessionId
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
        ('RedirectOutput', 'TerminateOnTimeout').Where({$PSBoundParameters.ContainsKey("No$_")}).ForEach({
                $PSBoundParameters.$_ = !$PSBoundParameters."No$_"
                $null = $PSBoundParameters.Remove("No$_")
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
        try
        {
            try
            {
                # Create a new process object and invoke an execution.
                return ($process = [PSADT.ProcessEx.StartProcess]::new()).ExecuteAndMonitorAsync($PSBoundParameters)
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Object ensures the correct PositionMessage is used.
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
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
