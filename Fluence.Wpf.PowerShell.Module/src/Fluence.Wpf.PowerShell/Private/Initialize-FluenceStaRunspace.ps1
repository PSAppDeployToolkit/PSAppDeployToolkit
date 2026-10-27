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


function Initialize-FluenceStaRunspace
{
    <#
    .SYNOPSIS
        Returns the module-owned STA runspace, adopting a process-wide one or creating it, with the module imported.
    .DESCRIPTION
        The runspace persists for the process, not for one module instance. WPF allows exactly one
        Application per AppDomain for the life of the process, and the module creates that Application
        on this runspace's thread on an MTA host, so a later Import-Module -Force (a test run, a
        development loop) must reuse the same thread rather than start a second one. The open runspace
        is therefore published in AppDomain data; a new module instance adopts it and re-imports the
        module into it so the runspace runs the current code. A failed bootstrap import is torn down
        rather than left as an Opened-but-empty runspace that would fail every later call.
    .OUTPUTS
        System.Management.Automation.Runspaces.Runspace
    .NOTES
        Does not require a host application. Used on MTA hosts only.
    #>
    [CmdletBinding()]
    [OutputType([System.Management.Automation.Runspaces.Runspace])]
    param()

    if ($null -ne $script:StaRunspace -and $script:StaRunspace.RunspaceStateInfo.State -eq 'Opened')
    {
        return $script:StaRunspace
    }

    $shared = [System.AppDomain]::CurrentDomain.GetData($script:StaRunspaceSlot)
    $adopted = $false
    if ($shared -is [System.Management.Automation.Runspaces.Runspace] -and $shared.RunspaceStateInfo.State -eq 'Opened')
    {
        $script:StaRunspace = $shared
        $script:OwnsApplication = $true
        $adopted = $true
        Write-Verbose 'Adopted the process-wide Fluence UI runspace.'
    }
    else
    {
        $script:StaRunspace = [runspacefactory]::CreateRunspace()
        $script:StaRunspace.ApartmentState = 'STA'
        $script:StaRunspace.ThreadOptions = 'ReuseThread'
        $script:StaRunspace.Open()
    }

    if ($script:StaRunspace.RunspaceAvailability -ne 'Available')
    {
        $script:StaRunspace = $null
        throw 'The process-wide Fluence UI runspace is still busy. Close its dialog or allow the active callback to finish before reimporting the module.'
    }

    $bootstrap = [powershell]::Create()
    $bootstrap.Runspace = $script:StaRunspace
    $bootstrapFailed = $false
    $bootstrapError = $null
    try
    {
        $null = $bootstrap.AddCommand('Import-Module').AddParameter('Name', $script:ModuleManifestPath).AddParameter('Force').Invoke()
        $bootstrapFailed = $bootstrap.HadErrors
        if ($bootstrap.Streams.Error.Count -gt 0)
        {
            $bootstrapFailed = $true
            $bootstrapError = $bootstrap.Streams.Error[0]
        }
    }
    catch
    {
        $bootstrapFailed = $true
        $bootstrapError = $_
    }
    finally
    {
        $bootstrap.Dispose()
    }

    if ($bootstrapFailed)
    {
        if (-not $adopted)
        {
            $script:StaRunspace.Dispose()
        }
        $script:StaRunspace = $null
        if ($null -ne $bootstrapError)
        {
            throw $bootstrapError
        }
        throw 'Failed to import the Fluence.Wpf.PowerShell module into the STA runspace.'
    }

    [System.AppDomain]::CurrentDomain.SetData($script:StaRunspaceSlot, $script:StaRunspace)
    Register-FluenceRunspaceExit
    return $script:StaRunspace
}
