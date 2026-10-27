<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


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
