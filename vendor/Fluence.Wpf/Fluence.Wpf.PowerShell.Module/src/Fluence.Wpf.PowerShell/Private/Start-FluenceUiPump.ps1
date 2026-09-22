function Start-FluenceUiPump
{
    <#
    .SYNOPSIS
        Shows the progress window on the module-owned STA runspace and pumps its dispatcher until closed.
    .DESCRIPTION
        Runs asynchronously on the STA runspace (Show-FluenceProgress starts it with BeginInvoke on an
        MTA host). It builds and shows the progress window, then pushes a DispatcherFrame so the window
        stays live between the caller's updates. A 50 ms DispatcherTimer services two things inside that
        frame: work items queued by Invoke-InFluenceStaRunspace (each a scriptblock text plus arguments,
        run in module scope, with output and error handed back and a wait handle signalled), and the
        handle's CloseRequested flag, which closes the window and ends the frame. Any item still queued
        when the frame ends is failed so no caller waits forever.
    .PARAMETER Handle
        The Fluence.ProgressHandle whose Spec and State drive the window. Parts is filled in here.
    .PARAMETER Pump
        The synchronized pump hashtable: Queue (ConcurrentQueue), Shown (ManualResetEventSlim), Active.
    .NOTES
        Must run on the module-owned STA runspace thread. Blocks until Close-FluenceProgress runs.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Shows and pumps a transient in-process window on the module UI runspace; changes no external system state.')]
    param
    (
        [Parameter(Mandatory = $true)]
        [object]$Handle,

        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$Pump
    )

    $timer = $null
    $parts = $null
    try
    {
        Initialize-FluenceApplication -Theme $Handle.Spec.Theme -Backdrop $Handle.Spec.Backdrop -Accent $Handle.Spec.AccentColor

        $parts = New-FluenceProgressWindow -Spec $Handle.Spec -State $Handle.State
        $Handle.Parts = $parts
        $parts.Window.Show()

        $module = Get-Module -Name 'Fluence.Wpf.PowerShell'
        $frame = [System.Windows.Threading.DispatcherFrame]::new()
        $handle = $Handle
        $pump = $Pump

        $timer = [System.Windows.Threading.DispatcherTimer]::new()
        $timer.Interval = [timespan]::FromMilliseconds(50)
        $timer.add_Tick({
            $item = $null
            while ($pump.Queue.TryDequeue([ref]$item))
            {
                try
                {
                    $block = [scriptblock]::Create($item.Text)
                    $itemArgs = [object[]]$item.Args
                    $item.Output = & $module $block @itemArgs
                }
                catch
                {
                    $item.Error = $_
                }
                finally
                {
                    $item.Done.Set()
                }
            }

            if ($handle.State.CloseRequested)
            {
                $timer.Stop()
                if ($parts.Window.IsVisible)
                {
                    $parts.Window.Close()
                }
                $frame.Continue = $false
            }
        }.GetNewClosure())
        $timer.Start()

        $Pump.Shown.Set()
        [System.Windows.Threading.Dispatcher]::PushFrame($frame)
    }
    finally
    {
        if ($null -ne $timer)
        {
            $timer.Stop()
        }
        $Handle.State.CloseRequested = $true
        if ($null -ne $parts -and $parts.Window.IsVisible)
        {
            $parts.Window.Close()
        }
        $Handle.IsOpen = $false
        $Pump.Active = $false
        $straggler = $null
        while ($Pump.Queue.TryDequeue([ref]$straggler))
        {
            $straggler.Error = 'The Fluence UI pump ended before this work item ran.'
            $straggler.Done.Set()
        }
    }
}
