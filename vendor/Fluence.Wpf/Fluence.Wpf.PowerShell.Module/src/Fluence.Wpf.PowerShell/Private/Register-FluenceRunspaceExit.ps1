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
