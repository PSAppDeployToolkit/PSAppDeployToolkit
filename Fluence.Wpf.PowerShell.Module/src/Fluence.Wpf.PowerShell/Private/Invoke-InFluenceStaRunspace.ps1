function Invoke-InFluenceStaRunspace
{
    <#
    .SYNOPSIS
        Runs a script on the persistent module-owned STA runspace (for MTA hosts such as pwsh -mta).
    .DESCRIPTION
        Each call runs synchronously. While no progress window is open the script is invoked directly
        on the runspace; ShowDialog pumps its own modal loop on the STA thread. While a progress window
        is open the runspace is busy pumping that window's dispatcher (see Start-FluenceUiPump), so the
        script is queued instead and the pump runs it on the STA thread between ticks; this call waits
        for the result. Either way the script is transported by text and re-created inside the module's
        session state so module-private functions resolve.
    .PARAMETER Script
        The script to run on the STA runspace. It is transported by text, so it cannot capture the
        caller's variables, functions or closures.
    .PARAMETER ArgumentList
        Positional arguments bound to the script's param block.
    .NOTES
        The runspace persists for the session so the WPF Application and dispatcher survive between
        dialogs.
    #>
    [CmdletBinding()]
    [OutputType([object])]
    param
    (
        [Parameter(Mandatory = $true)]
        [scriptblock]$Script,

        [Parameter()]
        [object[]]$ArgumentList = @()
    )

    $null = Initialize-FluenceStaRunspace

    if ($null -ne $script:UiPump -and $script:UiPump.Active)
    {
        $item = [hashtable]::Synchronized(@{
                Text   = $Script.ToString()
                Args   = [object[]]$ArgumentList
                Done   = [System.Threading.ManualResetEventSlim]::new($false)
                Output = $null
                Error  = $null
            })
        $script:UiPump.Queue.Enqueue($item)

        # Wait in slices so a pump whose pipeline has ended (a faulted window build, a stopped
        # runspace) surfaces as an error instead of a hang.
        try
        {
            while (-not $item.Done.Wait(250))
            {
                $pipelineState = $script:UiPump.PowerShell.InvocationStateInfo.State
                if ($pipelineState -in @('Completed', 'Failed', 'Stopped'))
                {
                    if ($item.Done.IsSet)
                    {
                        break
                    }
                    throw "The Fluence UI pump ended ($pipelineState) before the queued UI work ran."
                }
            }
        }
        finally
        {
            $item.Done.Dispose()
        }

        if ($null -ne $item.Error)
        {
            throw $item.Error
        }
        return $item.Output
    }

    # Transport the scriptblock by TEXT and re-create it inside the module's session state on the STA
    # runspace. PowerShell.AddScript has no ScriptBlock overload, so a [scriptblock] passed directly
    # would recompile at the runspace's global scope where module-PRIVATE functions are invisible.
    # Invoking the re-created block through the module object (& $module $inner @args) runs it in
    # module scope, so private helpers such as Invoke-FluenceWindow resolve.
    $ps = [powershell]::Create()
    $ps.Runspace = $script:StaRunspace
    $null = $ps.AddScript({
            param($ScriptText, $ScriptArgs)
            $module = Get-Module -Name 'Fluence.Wpf.PowerShell'
            $inner = [scriptblock]::Create($ScriptText)
            & $module $inner @ScriptArgs
        })
    $null = $ps.AddArgument($Script.ToString())
    $null = $ps.AddArgument([object[]]$ArgumentList)
    try
    {
        $output = $ps.Invoke()
        if ($ps.HadErrors -and $ps.Streams.Error.Count -gt 0)
        {
            throw $ps.Streams.Error[0]
        }
        return $output
    }
    finally
    {
        $ps.Dispose()
    }
}
