#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Close-ADTInstallationProgressFluent
{
    <#

    .SYNOPSIS
    Closes the dialog created by Show-ADTInstallationProgressFluent.

    .DESCRIPTION
    Closes the dialog created by Show-ADTInstallationProgressFluent.

    This function is called by the Close-ADTSession function to close a running instance of the progress dialog if found.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    Close-ADTInstallationProgressFluent

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    # Hide the dialog and reset the state bool.
    Write-ADTLogEntry -Message 'Closing the installation progress dialog.'
    $Script:Dialogs.Fluent.ProgressWindow.Window.HideDialog()
    $Script:Dialogs.Fluent.ProgressWindow.Running = $false
}
