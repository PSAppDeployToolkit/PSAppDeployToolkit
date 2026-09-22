function Register-FluenceRunspaceExit
{
    <#
    .SYNOPSIS
        Registers terminal cleanup of the module-owned UI runspace from its caller runspace.
    .DESCRIPTION
        PowerShell.Exiting runs before PowerShell closes other local runspaces. Its callback can
        still submit work to the owned UI runspace, so that dispatcher is shut down on its own
        thread before PowerShell disposes the thread. The subscription survives module reimports;
        module removal alone must preserve the process WPF Application for reuse.
    .NOTES
        Only covers PowerShell-controlled exit of the primary ConsoleHost session. It does not run when a process is killed,
        and cannot shut down a dispatcher on the exiting primary runspace's own idle thread.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    param()

    # Exiting also fires when an ordinary child runspace is closed. Only the console's own
    # unpushed session may retire the process-wide UI thread; arbitrary hosts retain ownership.
    if ($Host.Name -ne 'ConsoleHost' -or
        $Host -isnot [System.Management.Automation.Host.IHostSupportsInteractiveSession] -or
        $Host.IsRunspacePushed -or
        -not [System.Object]::ReferenceEquals($Host.Runspace, [System.Management.Automation.Runspaces.Runspace]::DefaultRunspace))
    {
        return
    }

    $registrationSlot = 'Fluence.Wpf.PowerShell.ExitHook.' + [System.Management.Automation.Runspaces.Runspace]::DefaultRunspace.InstanceId
    if ([System.AppDomain]::CurrentDomain.GetData($registrationSlot))
    {
        return
    }

    $null = Register-EngineEvent -SourceIdentifier PowerShell.Exiting -SupportEvent -Action {
        # Resolve the current module instance: an earlier instance may have been removed/reimported.
        # Use only Core/Utility commands and .NET methods during the engine's exit notification.
        $module = Get-Module -Name Fluence.Wpf.PowerShell
        if ($null -ne $module)
        {
            & $module { Close-FluenceUiRunspace }
        }
        $ui = [System.AppDomain]::CurrentDomain.GetData('Fluence.Wpf.PowerShell.StaRunspace')
        if ($null -eq $ui -or $ui.RunspaceStateInfo.State -ne 'Opened' -or $ui.RunspaceAvailability -ne 'Available')
        {
            return
        }

        $pipeline = [powershell]::Create()
        $pipeline.Runspace = $ui
        $completed = $false
        try
        {
            $null = $pipeline.AddScript({
                $application = [System.Windows.Application]::Current
                if ($null -ne $application -and $application.Dispatcher.CheckAccess() -and -not $application.Dispatcher.HasShutdownStarted)
                {
                    $application.Dispatcher.InvokeShutdown()
                }
            })
            $pending = $pipeline.BeginInvoke()
            $completed = $pending.AsyncWaitHandle.WaitOne(3000)
            if ($completed)
            {
                $null = $pipeline.EndInvoke($pending)
            }
            else
            {
                Write-Warning 'The Fluence UI dispatcher did not finish terminal shutdown within three seconds.'
            }
        }
        catch
        {
            $completed = $true
            Write-Warning "Fluence terminal UI shutdown failed: $_"
        }
        finally
        {
            # Dispose only completed work; synchronous Stop/Dispose can block a stalled dispatcher.
            if ($completed)
            {
                $pipeline.Dispose()
            }
        }
    }
    [System.AppDomain]::CurrentDomain.SetData($registrationSlot, $true)
}
