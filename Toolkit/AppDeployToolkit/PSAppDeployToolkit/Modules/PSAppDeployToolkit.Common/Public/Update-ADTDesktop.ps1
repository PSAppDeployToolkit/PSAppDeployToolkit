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
        [PSADT.Explorer]::RefreshDesktopAndEnvironmentVariables()
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
