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
