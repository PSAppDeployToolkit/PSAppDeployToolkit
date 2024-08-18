#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Update-ADTDesktop
{
    <#

    .SYNOPSIS
    Refresh the Windows Explorer Shell, which causes the desktop icons and the environment variables to be reloaded.

    .DESCRIPTION
    Refresh the Windows Explorer Shell, which causes the desktop icons and the environment variables to be reloaded.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return objects.

    .EXAMPLE
    Update-ADTDesktop

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

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
        try
        {
            try
            {
                [PSADT.Explorer]::RefreshDesktopAndEnvironmentVariables()
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to refresh the Desktop and the Windows Explorer environment process block."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
