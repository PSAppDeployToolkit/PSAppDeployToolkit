function Get-FluenceUiHostMode
{
    <#
    .SYNOPSIS
        Selects the caller's STA thread or the process-wide module UI runspace.
    .DESCRIPTION
        An existing application can be used on its own dispatcher thread. An application owned by
        the module's published STA runspace is reached through that runspace, even when the caller
        itself is STA. A foreign dispatcher on another thread is rejected: invoking a PowerShell
        delegate there does not establish a PowerShell execution context.
    .OUTPUTS
        System.String
    .NOTES
        Reads thread and application state only. Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param()

    $app = [System.Windows.Application]::Current
    if ($null -ne $app)
    {
        if ($app.Dispatcher.HasShutdownStarted -or -not $app.Dispatcher.Thread.IsAlive)
        {
            throw 'The process WPF Application dispatcher has shut down. Start a new PowerShell process before using Fluence UI commands.'
        }
        if ($app.Dispatcher.CheckAccess())
        {
            return 'Inline'
        }
        $shared = [System.AppDomain]::CurrentDomain.GetData($script:StaRunspaceSlot)
        if ($shared -is [System.Management.Automation.Runspaces.Runspace] -and $shared.RunspaceStateInfo.State -eq 'Opened')
        {
            return 'Runspace'
        }
        throw 'The existing WPF Application belongs to another UI thread. Invoke Fluence commands from a PowerShell runspace on that application dispatcher, or use a separate PowerShell process. Automatic dispatch to a foreign WPF thread is not supported.'
    }
    if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -eq [System.Threading.ApartmentState]::STA)
    {
        return 'Inline'
    }
    return 'Runspace'
}
