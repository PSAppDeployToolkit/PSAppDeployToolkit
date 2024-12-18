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
        # Get the active session and throw if we don't have it.
        try
        {
            $adtSession = Get-ADTSession
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }

        # Make this function continue on error and ensure the caller doesn't override ErrorAction.
        $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::SilentlyContinue
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Change the install phase since we've finished initialising. This should get overwritten shortly.
        $adtSession.InstallPhase = 'Finalization'

        # Update the session's exit code with the provided value.
        if ($PSBoundParameters.ContainsKey('ExitCode') -and (!$adtSession.GetExitCode() -or !$ExitCode.Equals(60001)))
        {
            $adtSession.SetExitCode($ExitCode)
        }

        # Invoke all callbacks and capture all errors.
        $callbackErrors = foreach ($callback in $($Script:ADT.Callbacks.Closing; if ($Script:ADT.Sessions.Count.Equals(1)) { $Script:ADT.Callbacks.Finishing }))
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
                New-Variable -Name ExitCode -Value $adtSession.Close() -Force -Confirm:$false
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failure occurred while closing ADTSession for [$($adtSession.InstallName)]."
            $ExitCode = 60001
        }

        # Hand over to our backend closure routine if this was the last session.
        if (!$Script:ADT.Sessions.Count)
        {
            Exit-ADTInvocation -ExitCode $ExitCode -Force:($Force -or ($Host.Name.Equals('ConsoleHost') -and $callbackErrors))
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
