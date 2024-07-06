function Enable-ADTTerminalServerInstallMode
{
    <#

    .SYNOPSIS
    Changes to user install mode for Remote Desktop Session Host/Citrix servers.

    .DESCRIPTION
    Changes to user install mode for Remote Desktop Session Host/Citrix servers.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return any objects.

    .EXAMPLE
    Enable-TerminalServerInstallMode

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
    )

    (Get-ADTModuleData).TerminalServerMode = Invoke-TerminalServerModeChange @PSBoundParameters -Mode Install
}
