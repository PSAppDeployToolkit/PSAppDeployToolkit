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
        # Initialise function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
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
        $callbackErrors = $(
            # Invoke closing session callbacks.
            foreach ($callback in $($adtData.Callbacks.Closing))
            {
                try
                {
                    & $callback
                }
                catch
                {
                    $_
                }
            }

            # Invoke final callbacks.
            if ($adtData.Sessions.Count.Equals(1))
            {
                foreach ($callback in $($adtData.Callbacks.Finishing))
                {
                    try
                    {
                        & $callback
                    }
                    catch
                    {
                        $_
                    }
                }
            }
        )

        # Close out the active session and clean up session state.
        $sessionCloseError = try
        {
            $adtSession.Close()
        }
        catch
        {
            $_
        }
        finally
        {
            $null = $adtData.Sessions.Remove($adtSession)
        }

        # Clean up environment if this was the last session.
        if (!$adtData.Sessions.Count)
        {
            # Flag the module as uninitialised upon last session closure.
            $adtData.Initialised = $false

            # Exit out if this function was called within a script.
            if (!$adtSession.RunspaceOrigin)
            {
                if ($Host.Name.Equals('ConsoleHost') -and ($callbackErrors -or (& $Script:CommandTable.'Get-Job' | & {process {if ($_.State.Equals('Running')) {$_}}})))
                {
                    [System.Environment]::Exit($adtData.LastExitCode)
                }
                exit $adtData.LastExitCode
            }
        }

        # If this wasn't the last session, process any captured errors.
        try
        {
            # If we had callback errors, throw out the first one.
            foreach ($callbackError in $callbackErrors)
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $callbackError
            }

            # If the session closure failed, terminate out.
            if ($sessionCloseError)
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $sessionCloseError
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        # Finalise function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
