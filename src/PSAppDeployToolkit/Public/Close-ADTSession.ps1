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

    .PARAMETER NoShellExit
        Doesn't exit PowerShell upon closing of the final session.

    .PARAMETER Force
        Forcibly exits PowerShell upon closing of the final session.

    .PARAMETER PassThru
        Returns the exit code of the session being closed.

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

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Close-ADTSession
    #>

    [CmdletBinding(DefaultParameterSetName = 'None')]
    param
    (
        [Parameter(Mandatory = $false, ParameterSetName = 'None')]
        [Parameter(Mandatory = $false, ParameterSetName = 'NoShellExit')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Force')]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.Int32]]$ExitCode,

        [Parameter(Mandatory = $true, ParameterSetName = 'NoShellExit')]
        [System.Management.Automation.SwitchParameter]$NoShellExit,

        [Parameter(Mandatory = $true, ParameterSetName = 'Force')]
        [System.Management.Automation.SwitchParameter]$Force,

        [Parameter(Mandatory = $false, ParameterSetName = 'None')]
        [Parameter(Mandatory = $false, ParameterSetName = 'NoShellExit')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Force')]
        [System.Management.Automation.SwitchParameter]$PassThru
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
        # Change the install phase now that we're on the way out.
        $adtSession.InstallPhase = 'Finalization'

        # Update the session's exit code with the provided value.
        if ($PSBoundParameters.ContainsKey('ExitCode') -and (!$adtSession.GetExitCode() -or !$ExitCode.Equals(60001)))
        {
            $adtSession.SetExitCode($ExitCode)
        }

        # Invoke all callbacks and capture all errors.
        $preCloseErrors = $(
            foreach ($callback in $($Script:ADT.Callbacks.([PSADT.Module.CallbackType]::PreClose)))
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
                    $_; Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failure occurred while invoking pre-close callback [$($callback.Name)]."
                }
            }
            foreach ($callback in $(if ($Script:ADT.Sessions.Count.Equals(1)) { $Script:ADT.Callbacks.([PSADT.Module.CallbackType]::OnFinish) }))
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
                    $_; Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failure occurred while invoking on-finish callback [$($callback.Name)]."
                }
            }
        )

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
            $ExitCode = 60001
        }
        finally
        {
            # Invoke close callbacks before we remove the session, the callback owner may still need it.
            $postCloseErrors = foreach ($callback in $($Script:ADT.Callbacks.([PSADT.Module.CallbackType]::PostClose)))
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
                    $_; Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failure occurred while invoking post-close callback [$($callback.Name)]."
                }
            }
            $null = $Script:ADT.Sessions.Remove($adtSession)
        }

        # Forcibly set the LASTEXITCODE so it's available if we're breaking
        # or running Close-ADTSession from a PowerShell runspace, etc.
        $Global:LASTEXITCODE = $ExitCode

        # Hand over to our backend closure routine if this was the last session.
        if (!$Script:ADT.Sessions.Count)
        {
            Exit-ADTInvocation -ExitCode $ExitCode -NoShellExit:($NoShellExit -or !$adtSession.CanExitOnClose()) -Force:($Force -or ($Host.Name.Equals('ConsoleHost') -and ($preCloseErrors -or $postCloseErrors)))
        }

        # If we're still here and are to pass through the exit code, do so.
        if ($PassThru)
        {
            return $ExitCode
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
