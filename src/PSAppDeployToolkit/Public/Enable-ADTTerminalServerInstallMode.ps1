#-----------------------------------------------------------------------------
#
# MARK: Enable-ADTTerminalServerInstallMode
#
#-----------------------------------------------------------------------------

function Enable-ADTTerminalServerInstallMode
{
    <#
    .SYNOPSIS
        Changes to user install mode for Remote Desktop Session Host/Citrix servers.

    .DESCRIPTION
        The Enable-ADTTerminalServerInstallMode function changes the server mode to user install mode for Remote Desktop Session Host/Citrix servers. This is useful for ensuring that applications are installed in a way that is compatible with multi-user environments.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any objects.

    .EXAMPLE
        Enable-ADTTerminalServerInstallMode

        This example changes the server mode to user install mode for Remote Desktop Session Host/Citrix servers.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Enable-ADTTerminalServerInstallMode
    #>

    [CmdletBinding()]
    param
    (
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        if ($Script:ADT.TerminalServerMode)
        {
            return
        }

        try
        {
            try
            {
                Invoke-ADTTerminalServerModeChange -Mode Install
                $Script:ADT.TerminalServerMode = $true
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
