#-----------------------------------------------------------------------------
#
# MARK: Disable-ADTTerminalServerInstallMode
#
#-----------------------------------------------------------------------------

function Disable-ADTTerminalServerInstallMode
{
    <#
    .SYNOPSIS
        Changes the current Remote Desktop Session Host/Citrix server to user execute mode.

    .DESCRIPTION
        The Disable-ADTTerminalServerInstallMode function changes the current Remote Desktop Session Host/Citrix server to user execute mode. This is useful for ensuring that applications are installed in a way that is compatible with multi-user environments.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any objects.

    .EXAMPLE
        Disable-ADTTerminalServerInstallMode

        This example changes the current Remote Desktop Session Host/Citrix server to user execute mode.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Disable-ADTTerminalServerInstallMode
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
        if (![PSADT.LibraryInterfaces.Kernel32]::TermsrvAppInstallMode())
        {
            Write-ADTLogEntry -Message "This terminal server is already in user execute mode."
            return
        }

        try
        {
            try
            {
                Invoke-ADTTerminalServerModeChange -Mode Execute
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
