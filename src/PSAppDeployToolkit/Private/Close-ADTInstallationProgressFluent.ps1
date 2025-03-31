#-----------------------------------------------------------------------------
#
# MARK: Close-ADTInstallationProgressFluent
#
#-----------------------------------------------------------------------------

function Private:Close-ADTInstallationProgressFluent
{
    <#
    .SYNOPSIS
        Internal function to close the Installation Progress dialog using the Fluent UI.

    .DESCRIPTION
        Called by Close-ADTInstallationProgress. Uses the UnifiedAdtApplication C# class
        to close the currently displayed dialog (expected to be the Progress dialog).

    .OUTPUTS
        None
    #>
    # Close the current dialog (which should be the progress dialog) and reset the state flag.
    Write-ADTLogEntry -Message 'Closing the installation progress dialog (Fluent).'
    [PSADT.UserInterface.UnifiedADTApplication]::CloseCurrentDialog()
    $Script:Dialogs.Fluent.ProgressWindow.Running = $false
}
