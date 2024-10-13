#-----------------------------------------------------------------------------
#
# MARK: Close-ADTInstallationProgressClassic
#
#-----------------------------------------------------------------------------

function Close-ADTInstallationProgressClassic
{
    # Process the WPF window if it exists.
    if ($Script:Dialogs.Classic.ProgressWindow.SyncHash.ContainsKey('Window'))
    {
        if (!$Script:Dialogs.Classic.ProgressWindow.Invocation.IsCompleted)
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
