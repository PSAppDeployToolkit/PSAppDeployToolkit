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
