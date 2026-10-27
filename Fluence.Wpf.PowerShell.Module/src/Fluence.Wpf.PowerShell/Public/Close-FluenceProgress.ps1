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


function Close-FluenceProgress
{
    <#
    .SYNOPSIS
        Closes a progress window opened by Show-FluenceProgress.
    .DESCRIPTION
        Closes the window on its UI thread and marks the handle closed. Closing an already closed
        handle is a no-op, so a finally block can call this unconditionally. On an MTA host this also
        ends the UI pump that kept the window live and releases the module's UI runspace for the next
        dialog.
    .PARAMETER Handle
        The Fluence.ProgressHandle returned by Show-FluenceProgress.
    .EXAMPLE
        try { $progress = Show-FluenceProgress -Message 'Working...'; Invoke-Work } finally { Close-FluenceProgress -Handle $progress }
    .NOTES
        Runs on the UI thread that owns the window. A window implies a running Application, so no
        application is created here.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Closes a transient in-process window the caller opened; makes no persistent or destructive system change.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [PSTypeName('Fluence.ProgressHandle')]
        [object]$Handle
    )

    $ownsPump = $null -ne $script:ProgressHandle -and $script:ProgressHandle.Id -eq $Handle.Id
    if (-not $Handle.IsOpen -and -not $ownsPump)
    {
        return
    }

    if ($Handle.Mode -eq 'Runspace' -and $ownsPump -and $null -ne $script:UiPump)
    {
        $pump = $script:UiPump
        $Handle.State.CloseRequested = $true
        if (-not $pump.AsyncResult.AsyncWaitHandle.WaitOne(5000))
        {
            throw 'The Fluence UI thread did not finish closing within five seconds. Its close request remains pending; retry after the active dialog or callback returns.'
        }
        try
        {
            $null = $pump.PowerShell.EndInvoke($pump.AsyncResult)
        }
        finally
        {
            $pump.PowerShell.Dispose()
            $pump.Shown.Dispose()
            $script:UiPump = $null
            $Handle.IsOpen = $false
            $script:ProgressHandle = $null
        }
        return
    }

    $null = Invoke-OnFluenceUi -Script {
        param($h)
        $h.State.CloseRequested = $true
        if ($null -ne $h.Parts -and $h.Parts.Window.IsVisible)
        {
            $h.Parts.Window.Close()
        }
        if ($h.Mode -eq 'Inline')
        {
            Invoke-FluenceDispatcherPump
        }
    } -ArgumentList @($Handle)
    $Handle.IsOpen = $false
    if ($null -ne $script:ProgressHandle -and $script:ProgressHandle.Id -eq $Handle.Id)
    {
        $script:ProgressHandle = $null
    }
}
