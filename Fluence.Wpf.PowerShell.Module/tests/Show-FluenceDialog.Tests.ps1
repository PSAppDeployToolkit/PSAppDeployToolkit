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

# Caller-thread contract of Show-FluenceDialog: everything that is validated before any UI work is
# dispatched. These cases never reach Invoke-OnFluenceUi, so no window opens and they belong to the
# logic lane. The render path is covered by Show-FluenceDialog.Render.Tests.ps1.

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'Show-FluenceDialog parameter validation' {
    Context 'Timeout and Countdown' {
        It 'rejects -Countdown without -Timeout before any UI work' {
            { Show-FluenceDialog -Message 'x' -Countdown } | Should -Throw '*-Countdown requires -Timeout*'
        }
        It 'rejects a -Timeout of <Value>' -ForEach @(
            @{ Value = 0 }
            @{ Value = -5 }
            @{ Value = 86401 }
        ) {
            { Show-FluenceDialog -Message 'x' -Timeout $Value } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
    Context 'Buttons' {
        It 'rejects an unsupported button item type before any UI work' {
            { Show-FluenceDialog -Message 'x' -Buttons @(42) } | Should -Throw '*unsupported item type*'
        }
    }
    Context 'Image and layout' {
        It 'rejects an -Image with the <Scheme> scheme before any UI work' -ForEach @(
            @{ Scheme = 'https'; Value = 'https://example.com/logo.png' }
            @{ Scheme = 'data'; Value = 'data:image/png;base64,iVBORw0KGgo=' }
        ) {
            { Show-FluenceDialog -Message 'x' -Image $Value } | Should -Throw "*unsupported scheme '$Scheme'*"
        }
        It 'rejects a missing -Image file before any UI work' {
            { Show-FluenceDialog -Message 'x' -Image (Join-Path $TestDrive 'nope.png') } | Should -Throw '*not found*'
        }
        It 'rejects an unknown -Position' {
            { Show-FluenceDialog -Message 'x' -Position Middle } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
        It 'rejects an unknown -MessageAlignment' {
            { Show-FluenceDialog -Message 'x' -MessageAlignment Justify } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
