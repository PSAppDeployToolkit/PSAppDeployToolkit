#-----------------------------------------------------------------------------
#
# MARK: Show-ADTHelpConsole
#
#-----------------------------------------------------------------------------

function Show-ADTHelpConsole
{
    <#
    .SYNOPSIS
        Displays a help console for the ADT module.

    .DESCRIPTION
        Displays a help console for the ADT module in a new PowerShell window. The console provides a graphical interface to browse and view detailed help information for all commands exported by the ADT module. The help console includes a list box to select commands and a text box to display the full help content for the selected command.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Show-ADTHelpConsole

        Opens a new PowerShell window displaying the help console for the ADT module.

    .NOTES
        This function can be called without an active ADT session.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    # Run this via a new PowerShell window so it doesn't stall the main thread.
    & $Script:CommandTable.'Start-Process' -FilePath (& $Script:CommandTable.'Get-ADTPowerShellProcessPath') -NoNewWindow -ArgumentList "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -EncodedCommand $(& $Script:CommandTable.'Out-ADTPowerShellEncodedCommand' -Command "& {$($Script:CommandTable.'Show-ADTHelpConsoleInternal'.ScriptBlock)} -ModulePath '$($Script:PSScriptRoot)\$($MyInvocation.MyCommand.Module.Name).psd1'")"
}
