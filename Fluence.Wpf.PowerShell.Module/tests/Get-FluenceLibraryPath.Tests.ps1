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


# Imported at discovery time, not in BeforeAll: InModuleScope below needs the module loaded
# while Pester is still discovering the cases.
Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

# The subject is a module-private helper. AGENTS.md forbids dot-sourcing a copy of one, so the
# cases run inside the imported module instead, where the private functions already resolve.
InModuleScope 'Fluence.Wpf.PowerShell' {
    BeforeAll {
        $script:root = Join-Path $TestDrive 'modroot'
        New-Item -ItemType Directory -Path (Join-Path $script:root 'lib/net472') -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $script:root 'lib/net8.0-windows10.0.26100.0') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $script:root 'lib/net472/Fluence.Wpf.dll') -Force | Out-Null
        New-Item -ItemType File -Path (Join-Path $script:root 'lib/net8.0-windows10.0.26100.0/Fluence.Wpf.dll') -Force | Out-Null
    }

    Describe 'Get-FluenceLibraryPath' {
        Context 'Edition selection' {
            It 'returns the net8.0-windows path for Core' {
                $p = Get-FluenceLibraryPath -ModuleRoot $script:root -Edition 'Core'
                $p | Should -Match 'net8\.0-windows'
            }
            It 'returns the net472 path for Desktop' {
                $p = Get-FluenceLibraryPath -ModuleRoot $script:root -Edition 'Desktop'
                $p | Should -Match 'net472'
            }
        }
        Context 'Missing assembly' {
            It 'throws when the dll is absent' {
                $empty = Join-Path $TestDrive 'empty'
                New-Item -ItemType Directory -Path $empty -Force | Out-Null
                { Get-FluenceLibraryPath -ModuleRoot $empty -Edition 'Core' } | Should -Throw '*not found*'
            }
        }
    }

}
