function Invoke-FluenceDispatcherPump
{
    <#
    .SYNOPSIS
        Processes the pending work of the current thread's dispatcher once, so a non-modal window repaints.
    .DESCRIPTION
        Pushes a nested DispatcherFrame that exits as soon as the dispatcher reaches Background
        priority, which is after all pending layout, render and input work has run. On the inline
        STA host nothing pumps the dispatcher between two cmdlet calls, so a Show()-n window built
        there would never paint without this; Show-FluenceProgress and Update-FluenceProgress call it
        after each change.
    .NOTES
        Must run on a thread that owns a dispatcher (the UI thread). Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    param()

    $frame = [System.Windows.Threading.DispatcherFrame]::new()
    $exit = [System.Windows.Threading.DispatcherOperationCallback] {
        param($f)
        $f.Continue = $false
        return $null
    }
    $null = [System.Windows.Threading.Dispatcher]::CurrentDispatcher.BeginInvoke(
        [System.Windows.Threading.DispatcherPriority]::Background, $exit, $frame)
    [System.Windows.Threading.Dispatcher]::PushFrame($frame)
}
