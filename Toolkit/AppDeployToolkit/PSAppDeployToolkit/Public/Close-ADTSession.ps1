#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Close-ADTSession
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$ExitCode
    )

    begin
    {
        # Make this function continue on error and ensure the caller doesn't override ErrorAction.
        $ErrorActionPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction $ErrorActionPreference
    }

    process
    {
        # Return early if there's no active session to close.
        if (!(Test-ADTSessionActive))
        {
            return
        }
        $adtSession = Get-ADTSession
        $adtData = Get-ADTModuleData

        # Update the session's exit code with the provided value.
        if ($PSBoundParameters.ContainsKey('ExitCode'))
        {
            $adtSession.SetExitCode($ExitCode)
        }

        # Invoke all callbacks and capture all errors.
        $callbackErrors = foreach ($callback in $($adtData.Callbacks.Closing; if ($adtData.Sessions.Count.Equals(1)) {$adtData.Callbacks.Finishing}))
        {
            try
            {
                try
                {
                    & $callback
                }
                catch
                {
                    & $Script:CommandTable.'Write-Error' -ErrorRecord $_
                }
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failure occurred while invoking callback [$($callback.Name)]." -PassThru
            }
        }

        # Close out the active session and clean up session state.
        try
        {
            try
            {
                $adtSession.Close()
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failure occurred while closing ADTSession for [$($adtSession.InstallName)]."
        }
        finally
        {
            $null = $adtData.Sessions.Remove($adtSession)
        }

        # Return early if this wasn't the last session.
        if ($adtData.Sessions.Count)
        {
            return
        }

        # Flag the module as uninitialised upon last session closure.
        $adtData.Initialised = $false

        # Return early if this function was called from the command line.
        if ($adtSession.RunspaceOrigin)
        {
            return
        }

        # If a callback failed and we're in a proper console, forcibly exit the process.
        # The proper closure of a blocking dialog can stall a traditional exit indefinitely.
        if ($Host.Name.Equals('ConsoleHost') -and $callbackErrors)
        {
            [System.Environment]::Exit($adtData.LastExitCode)
        }
        exit $adtData.LastExitCode
    }

    end
    {
        # Finalise function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
