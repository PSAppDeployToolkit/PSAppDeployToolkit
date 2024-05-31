function Close-ADTInstallationProgress
{
    <#

    .SYNOPSIS
    Closes the dialog created by Show-ADTInstallationProgress.

    .DESCRIPTION
    Closes the dialog created by Show-ADTInstallationProgress.

    This function is called by the Close-ADTSession function to close a running instance of the progress dialog if found.

    .PARAMETER WaitingTime
    How many seconds to wait, at most, for the InstallationProgress window to be initialized, before the function returns, without closing anything. Range: 1 - 60  Default: 5

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    Close-ADTInstallationProgress

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [ValidateRange(1, 60)]
        [System.UInt32]$WaitingTime = 5
    )

    begin {
        function Invoke-CloseInstProgressSleep
        {
            param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.String]$Message
            )

            Write-ADTLogEntry @PSBoundParameters -Severity 2
            for ($timeout = $WaitingTime; $timeout -gt 0; $timeout--)
            {
                [System.Threading.Thread]::Sleep(1000)
            }
        }

        $adtSession = Get-ADTSession
        Write-ADTDebugHeader
    }

    process {
        # Return early if we're silent, a window wouldn't have ever opened.
        if ($adtSession.DeployModeSilent)
        {
            Write-ADTLogEntry -Message "Bypassing Close-ADTInstallationProgress [Mode: $($adtSession.GetPropertyValue('deployMode'))]"
            return
        }

        # Process the WPF window if it exists.
        if ($Script:ProgressWindow.SyncHash -and $Script:ProgressWindow.SyncHash.ContainsKey('Window'))
        {
            # Check whether the window has been created and wait for up to $WaitingTime seconds if it does not.
            if (!$Script:ProgressWindow.SyncHash.Window.IsInitialized)
            {
                Invoke-CloseInstProgressSleep -Message "The installation progress dialog does not exist. Waiting up to $WaitingTime seconds..."
                if (!$Script:ProgressWindow.SyncHash.Window.IsInitialized)
                {
                    Write-ADTLogEntry -Message "The installation progress dialog was not created within $WaitingTime seconds." -Severity 2
                }
            }
            else
            {
                # If the thread is suspended, resume it.
                if ($Script:ProgressWindow.SyncHash.Window.Dispatcher.Thread.ThreadState -band [System.Threading.ThreadState]::Suspended)
                {
                    Write-ADTLogEntry -Message 'The thread for the installation progress dialog is suspended. Resuming the thread.'
                    try
                    {
                        $Script:ProgressWindow.SyncHash.Window.Dispatcher.Thread.Resume()
                    }
                    catch
                    {
                        Write-ADTLogEntry -Message 'Failed to resume the thread for the installation progress dialog.' -Severity 2
                    }
                }

                # If the thread is changing its state, wait.
                if ($Script:ProgressWindow.SyncHash.Window.Dispatcher.Thread.ThreadState -band ([System.Threading.ThreadState]::Aborted -bor [System.Threading.ThreadState]::AbortRequested -bor [System.Threading.ThreadState]::StopRequested -bor [System.Threading.ThreadState]::Unstarted -bor [System.Threading.ThreadState]::WaitSleepJoin))
                {
                    Invoke-CloseInstProgressSleep -Message "The thread for the installation progress dialog is changing its state. Waiting up to $WaitingTime seconds..."
                }

                # If the thread is running, stop it.
                if (!($Script:ProgressWindow.SyncHash.Window.Dispatcher.Thread.ThreadState -band ([System.Threading.ThreadState]::Stopped -bor [System.Threading.ThreadState]::Unstarted)))
                {
                    Write-ADTLogEntry -Message 'Closing the installation progress dialog.'
                    $Script:ProgressWindow.SyncHash.Window.Dispatcher.InvokeShutdown()
                    while (!$Script:ProgressWindow.SyncHash.Window.Dispatcher.HasShutdownFinished -and !$Script:ProgressWindow.Invocation.IsCompleted) {}
                    $Script:ProgressWindow.SyncHash.Clear()
                    $Script:ProgressWindow.Invocation = $null
                }
            }
        }

        # Process the PowerShell window.
        if ($Script:ProgressWindow.PowerShell -and $Script:ProgressWindow.PowerShell.Runspace)
        {
            # If the runspace is still opening, wait.
            if ($Script:ProgressWindow.PowerShell.Runspace.RunspaceStateInfo.State.Equals([System.Management.Automation.Runspaces.RunspaceState]::Opening) -or $Script:ProgressWindow.PowerShell.Runspace.RunspaceStateInfo.State.Equals([System.Management.Automation.Runspaces.RunspaceState]::BeforeOpen))
            {
                Invoke-CloseInstProgressSleep -Message "The runspace for the installation progress dialog is still opening. Waiting up to $WaitingTime seconds..."
            }

            # If the runspace is opened, close it.
            if ($Script:ProgressWindow.PowerShell.Runspace.RunspaceStateInfo.State.Equals([System.Management.Automation.Runspaces.RunspaceState]::Opened))
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

    end {
        Write-ADTDebugFooter
    }
}
