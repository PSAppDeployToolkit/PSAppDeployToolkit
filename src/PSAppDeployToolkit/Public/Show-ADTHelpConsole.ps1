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
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: © 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTHelpConsole
    #>

    # Run this via a new PowerShell window so it doesn't stall the main thread.
    Start-Process -FilePath (Get-ADTPowerShellProcessPath) -NoNewWindow -ArgumentList "$(if (!(Test-ADTModuleIsReleaseBuild)) { "-ExecutionPolicy Bypass " })-NonInteractive -NoProfile -NoLogo -EncodedCommand $(Out-ADTPowerShellEncodedCommand -Command "& {$($Script:CommandTable.'Show-ADTHelpConsoleInternal'.ScriptBlock)} -ModuleBase '$([System.String]::Join("', '", (Get-Module -Name "$($MyInvocation.MyCommand.Module.Name)*").ModuleBase))'")"
}
