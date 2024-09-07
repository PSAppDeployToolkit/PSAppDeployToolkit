#-----------------------------------------------------------------------------
#
# MARK: Start-ADTProcessAsUser
#
#-----------------------------------------------------------------------------

function Start-ADTProcessAsUser
{
    [CmdletBinding(DefaultParameterSetName = 'SessionId')]
    [OutputType([System.Threading.Tasks.Task[System.Int32]])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$FilePath,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ArgumentList,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WorkingDirectory,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$HideWindow,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.PInvoke.CREATE_PROCESS]$ProcessCreationFlags,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$InheritEnvironmentVariables,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$Wait,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$SessionId,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$AllActiveUserSessions,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PrimaryActiveUserSession,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$UseLinkedAdminToken,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Microsoft.PowerShell.ExecutionPolicy]$PsExecutionPolicy,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$BypassPsExecutionPolicy,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32[]]$SuccessExitCodes,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$ConsoleTimeoutInSeconds,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IsGuiApplication,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoRedirectOutput,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$MergeStdErrAndStdOut,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$OutputDirectory,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoTerminateOnTimeout,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.IDictionary]$AdditionalEnvironmentVariables,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.ProcessEx.WaitType]$WaitOption
    )

    begin
    {
        # Initialise function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

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
