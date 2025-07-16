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
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTHelpConsole
    #>

    # Attempt to disable PowerShell from asking whether to update help or not. It's essential as we can't answer the question in the runspace.
    if (Test-ADTCallerIsAdmin)
    {
        [Microsoft.Win32.Registry]::SetValue('HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PowerShell', 'DisablePromptToUpdateHelp', 1, [Microsoft.Win32.RegistryValueKind]::DWord)
    }

    # Run this as no-wait dialog so it doesn't stall the main thread. This this uses WinForms, we don't care about the style.
    $null = Invoke-ADTClientServerOperation -ShowModalDialog -User (Get-ADTClientServerUser) -DialogType HelpConsole -DialogStyle Classic -Options ([PSADT.UserInterface.DialogOptions.HelpConsoleOptions]@{ ExecutionPolicy = [Microsoft.PowerShell.ExecutionPolicy](Get-ExecutionPolicy); ModulePaths = [System.Collections.ObjectModel.ReadOnlyCollection[System.String]][System.String[]](Get-Module -Name "$($MyInvocation.MyCommand.Module.Name)*").ModuleBase }) -NoWait
}
