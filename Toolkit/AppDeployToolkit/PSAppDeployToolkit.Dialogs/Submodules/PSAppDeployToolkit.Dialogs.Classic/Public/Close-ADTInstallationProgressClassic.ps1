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
    if ($Script:ProgressWindow.SyncHash.ContainsKey('Window'))
    {
        if ($Script:ProgressWindow.Running)
        {
            Write-ADTLogEntry -Message 'Closing the installation progress dialog.'
            $Script:ProgressWindow.SyncHash.Window.Dispatcher.Invoke({$Script:ProgressWindow.SyncHash.Window.Close()}, [System.Windows.Threading.DispatcherPriority]::Send)
            while (!$Script:ProgressWindow.Invocation.IsCompleted) {}
        }
        $Script:ProgressWindow.SyncHash.Clear()
    }

    # End the PowerShell instance if it's invoked.
    if ($Script:ProgressWindow.Invocation)
    {
        Write-ADTLogEntry -Message "Closing the installation progress dialog's invocation."
        $null = $Script:ProgressWindow.PowerShell.EndInvoke($Script:ProgressWindow.Invocation)
        $Script:ProgressWindow.Invocation = $null
    }

    # Process the PowerShell window.
    if ($Script:ProgressWindow.PowerShell)
    {
        # Close down the runspace.
        if ($Script:ProgressWindow.PowerShell.Runspace -and $Script:ProgressWindow.PowerShell.Runspace.RunspaceStateInfo.State.Equals([System.Management.Automation.Runspaces.RunspaceState]::Opened))
        {
            Write-ADTLogEntry -Message "Closing the installation progress dialog's runspace."
            $Script:ProgressWindow.PowerShell.Runspace.Close()
            $Script:ProgressWindow.PowerShell.Runspace.Dispose()
            $Script:ProgressWindow.PowerShell.Runspace = $null
        }

        # Dispose of remaining PowerShell variables.
        $Script:ProgressWindow.PowerShell.Dispose()
        $Script:ProgressWindow.PowerShell = $null
    }

    # Reset the state bool.
    $Script:ProgressWindow.Running = $false
}
