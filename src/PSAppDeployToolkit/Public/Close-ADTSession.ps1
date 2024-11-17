#-----------------------------------------------------------------------------
#
# MARK: Close-ADTSession
#
#-----------------------------------------------------------------------------

function Close-ADTSession
{
    <#
    .SYNOPSIS
        Closes the active ADT session.

    .DESCRIPTION
        The Close-ADTSession function closes the active ADT session, updates the session's exit code if provided, invokes all registered callbacks, and cleans up the session state. If this is the last session, it flags the module as uninitialized and exits the process with the last exit code.

    .PARAMETER ExitCode
        The exit code to set for the session.

    .PARAMETER Force
        Forcibly exits PowerShell upon closing of the final session.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Close-ADTSession

        This example closes the active ADT session without setting an exit code.

    .EXAMPLE
        Close-ADTSession -ExitCode 0

        This example closes the active ADT session and sets the exit code to 0.

    .NOTES
        An active ADT session is required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$ExitCode,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Force
    )

    begin
    {
        # Make this function continue on error and ensure the caller doesn't override ErrorAction.
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
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

        # Change the install phase since we've finished initialising. This should get overwritten shortly.
        $adtSession.SetPropertyValue('InstallPhase', 'Finalization')

        # Update the session's exit code with the provided value.
        if ($PSBoundParameters.ContainsKey('ExitCode'))
        {
            $adtSession.SetExitCode($ExitCode)
        }

        # Invoke all callbacks and capture all errors.
        $callbackErrors = foreach ($callback in $($adtData.Callbacks.Closing; if ($adtData.Sessions.Count.Equals(1)) { $adtData.Callbacks.Finishing }))
        {
            try
            {
                try
                {
                    & $callback
                }
                catch
                {
                    Write-Error -ErrorRecord $_
                }
            }
            catch
            {
                $_; Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failure occurred while invoking callback [$($callback.Name)]."
            }
        }

        # Close out the active session and clean up session state.
        try
        {
            try
            {
                $ExitCode = $adtSession.Close()
            }
            catch
            {
                Write-Error -ErrorRecord $_
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

        # Attempt to close down any progress dialog here as an additional safety item.
        $progressOpen = if (Test-ADTInstallationProgressRunning)
        {
            try
            {
                Close-ADTInstallationProgress
            }
            catch
            {
                $_
            }
        }

        # Flag the module as uninitialized upon last session closure.
        $adtData.Initialized = $false

        # Return early if this function was called from the command line.
        if ($adtSession.RunspaceOrigin -and !$Force)
        {
            return
        }

        # If a callback failed and we're in a proper console, forcibly exit the process.
        # The proper closure of a blocking dialog can stall a traditional exit indefinitely.
        if ($Force -or ($Host.Name.Equals('ConsoleHost') -and ($callbackErrors -or $progressOpen)))
        {
            [System.Environment]::Exit($ExitCode)
        }
        exit $ExitCode
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
