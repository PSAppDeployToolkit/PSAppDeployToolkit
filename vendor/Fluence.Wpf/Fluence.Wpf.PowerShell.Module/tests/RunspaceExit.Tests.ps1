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


#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }
BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1'
}

Describe 'Terminal cleanup ownership' {
    It 'does not install terminal cleanup on a child runspace using <HostKind>' -ForEach @(
        @{ HostKind = 'DefaultHost' }
        @{ HostKind = 'the parent console host' }
    ) {
        $runspace = if ($HostKind -eq 'DefaultHost')
        {
            [runspacefactory]::CreateRunspace()
        }
        else
        {
            [runspacefactory]::CreateRunspace($Host)
        }
        $pipeline = $null
        try
        {
            $runspace.Open()
            $pipeline = [powershell]::Create()
            $pipeline.Runspace = $runspace
            $null = $pipeline.AddScript({
                param($manifest)
                Import-Module $manifest
                & (Get-Module Fluence.Wpf.PowerShell) { Register-FluenceRunspaceExit }
                return @(Get-EventSubscriber -Force | Where-Object { $_.SourceIdentifier -eq 'PowerShell.Exiting' }).Count
            }).AddArgument($script:ModulePath)
            $output = $pipeline.Invoke()
            $pipeline.HadErrors | Should -BeFalse
            $output.Count | Should -Be 1
            $output[0] | Should -Be 0
        }
        finally
        {
            if ($null -ne $pipeline) { $pipeline.Dispose() }
            $runspace.Dispose()
        }
    }
}
