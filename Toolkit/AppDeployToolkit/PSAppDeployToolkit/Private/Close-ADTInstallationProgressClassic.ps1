#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Close-ADTInstallationProgressClassic
{
    <#

    .SYNOPSIS
    Closes the dialog created by Show-ADTInstallationProgressClassic.

    .DESCRIPTION
    Closes the dialog created by Show-ADTInstallationProgressClassic.

    This function is called by the Close-ADTSession function to close a running instance of the progress dialog if found.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    Close-ADTInstallationProgressClassic

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    # Process the WPF window if it exists.
    if ($Script:Dialogs.Classic.ProgressWindow.SyncHash.ContainsKey('Window'))
    {
        if ($Script:Dialogs.Classic.ProgressWindow.Running)
        {
            Write-ADTLogEntry -Message 'Closing the installation progress dialog.'
            $Script:Dialogs.Classic.ProgressWindow.SyncHash.Window.Dispatcher.Invoke({ $Script:Dialogs.Classic.ProgressWindow.SyncHash.Window.Close() }, [System.Windows.Threading.DispatcherPriority]::Send)
            while (!$Script:Dialogs.Classic.ProgressWindow.Invocation.IsCompleted) {}
        }
        $Script:Dialogs.Classic.ProgressWindow.SyncHash.Clear()
    }

    # End the PowerShell instance if it's invoked.
    if ($Script:Dialogs.Classic.ProgressWindow.Invocation)
    {
        Write-ADTLogEntry -Message "Closing the installation progress dialog's invocation."
        $null = $Script:Dialogs.Classic.ProgressWindow.PowerShell.EndInvoke($Script:Dialogs.Classic.ProgressWindow.Invocation)
        $Script:Dialogs.Classic.ProgressWindow.Invocation = $null
    }

    # Process the PowerShell window.
    if ($Script:Dialogs.Classic.ProgressWindow.PowerShell)
    {
        # Close down the runspace.
        if ($Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace -and $Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace.RunspaceStateInfo.State.Equals([System.Management.Automation.Runspaces.RunspaceState]::Opened))
        {
            Write-ADTLogEntry -Message "Closing the installation progress dialog's runspace."
            $Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace.Close()
            $Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace.Dispose()
            $Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace = $null
        }

        # Dispose of remaining PowerShell variables.
        $Script:Dialogs.Classic.ProgressWindow.PowerShell.Dispose()
        $Script:Dialogs.Classic.ProgressWindow.PowerShell = $null
    }

    # Reset the state bool.
    $Script:Dialogs.Classic.ProgressWindow.Running = $false
}
