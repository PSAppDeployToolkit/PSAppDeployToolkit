function Close-FluenceUiRunspace
{
    <#
    .SYNOPSIS
        Closes module progress and releases UI resources during module removal.
    .DESCRIPTION
        Requests a cooperative progress close and waits at most five seconds for an MTA pump.
        Preserves the runspace that owns the process WPF Application so a later import can reuse
        it. An idle runspace with no Application is shut down and disposed.
    .NOTES
        Registered as OnRemove. A busy callback is not forcibly aborted: its close request stays
        pending and its background runspace remains alive until the process exits.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    param()

    # Request a cooperative close before waiting. PowerShell.Stop cannot interrupt WPF's
    # unmanaged message loop reliably, so unload never blocks on a synchronous Stop call.
    if ($null -ne $script:ProgressHandle)
    {
        $script:ProgressHandle.State.CloseRequested = $true
        if ($script:ProgressHandle.Mode -ne 'Runspace')
        {
            try
            {
                Close-FluenceProgress -Handle $script:ProgressHandle
            }
            catch
            {
                Write-Warning "Could not close the Fluence progress window during unload: $_"
            }
        }
    }
    if ($null -ne $script:UiPump)
    {
        $pump = $script:UiPump
        if ($null -ne $pump.AsyncResult -and $pump.AsyncResult.AsyncWaitHandle.WaitOne(5000))
        {
            try
            {
                $null = $pump.PowerShell.EndInvoke($pump.AsyncResult)
            }
            catch
            {
                Write-Verbose "Close-FluenceUiRunspace (pump): $_"
            }
            finally
            {
                $pump.PowerShell.Dispose()
                $pump.Shown.Dispose()
            }
        }
        else
        {
            Write-Warning 'The Fluence UI thread is still busy. Its close request remains pending; the runspace is retained until the process exits.'
        }
        $script:UiPump = $null
    }
    $script:ProgressHandle = $null

    if ($null -ne $script:StaRunspace)
    {
        try
        {
            # A runspace whose thread owns the process's WPF Application is kept alive and left
            # published in AppDomain data for the next module instance to adopt. WPF allows exactly one
            # Application per AppDomain for the life of the process, so that thread can never be
            # replaced; ending it would strand every later import (Import-Module -Force in a test run
            # or a development loop) with an Application it can neither use nor recreate. The thread
            # is a background thread and dies with the process.
            #
            # A runspace that never created the Application is torn down. Its dispatcher, if one was
            # ever touched, is shut down first: a thread that exits with a live Dispatcher still owns a
            # hidden message window, and Windows destroys it during thread teardown by calling back into
            # managed code after the runtime has released the thread, which is fatal on .NET (Core).
            # Never synchronously close a busy runspace: a callback may still be in a modal
            # loop. Keeping its thread alive also preserves the process-wide WPF Application.
            $hostsApplication = ($script:StaRunspace.RunspaceStateInfo.State -eq 'Opened' -and
                $script:StaRunspace.RunspaceAvailability -ne 'Available')
            if ($script:StaRunspace.RunspaceStateInfo.State -eq 'Opened' -and
                $script:StaRunspace.RunspaceAvailability -eq 'Available')
            {
                $probe = [powershell]::Create()
                $probe.Runspace = $script:StaRunspace
                $null = $probe.AddScript({
                        $app = [System.Windows.Application]::Current
                        if ($null -ne $app -and $app.Dispatcher.CheckAccess())
                        {
                            return $true
                        }
                        $dispatcher = [System.Windows.Threading.Dispatcher]::FromThread([System.Threading.Thread]::CurrentThread)
                        if ($null -ne $dispatcher -and -not $dispatcher.HasShutdownStarted)
                        {
                            $dispatcher.InvokeShutdown()
                        }
                        return $false
                    })
                $probeResult = $probe.Invoke()
                $probe.Dispose()
                if ($probeResult.Count -gt 0 -and $probeResult[$probeResult.Count - 1] -eq $true)
                {
                    $hostsApplication = $true
                }
            }

            if ($hostsApplication)
            {
                Write-Verbose 'Close-FluenceUiRunspace: the UI runspace hosts the WPF Application and stays alive for the process.'
                [System.AppDomain]::CurrentDomain.SetData($script:StaRunspaceSlot, $script:StaRunspace)
            }
            else
            {
                if ([System.Object]::ReferenceEquals([System.AppDomain]::CurrentDomain.GetData($script:StaRunspaceSlot), $script:StaRunspace))
                {
                    [System.AppDomain]::CurrentDomain.SetData($script:StaRunspaceSlot, $null)
                }
                $script:StaRunspace.Close()
                $script:StaRunspace.Dispose()
            }
        }
        catch
        {
            # Best-effort teardown on module unload; a runspace already faulted/closed must not
            # throw out of OnRemove.
            Write-Verbose "Close-FluenceUiRunspace: $_"
        }
        finally
        {
            $script:StaRunspace = $null
        }
    }
}
