#-----------------------------------------------------------------------------
#
# MARK: Enable-ADTTerminalServerInstallMode
#
#-----------------------------------------------------------------------------

function Enable-ADTTerminalServerInstallMode
{
    <#
    .SYNOPSIS
        Changes the current Remote Desktop Session Host/Citrix server to user install mode.

    .DESCRIPTION
        The Enable-ADTTerminalServerInstallMode function changes the current Remote Desktop Session Host/Citrix server to user install mode. This is useful for ensuring that applications are installed in a way that is compatible with multi-user environments.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any objects.

    .EXAMPLE
        Enable-ADTTerminalServerInstallMode

        This example changes the current Remote Desktop Session Host/Citrix server to user install mode.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
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
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        if ([PSADT.LibraryInterfaces.Kernel32]::TermsrvAppInstallMode())
        {
            Write-ADTLogEntry -Message "This terminal server is already in user install mode."
            return
        }

        try
        {
            try
            {
                Invoke-ADTTerminalServerModeChange -Mode Install
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
