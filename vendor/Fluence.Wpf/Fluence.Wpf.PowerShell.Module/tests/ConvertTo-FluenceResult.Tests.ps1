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
    Describe 'ConvertTo-FluenceResult' {
        It 'projects the hashtable to a PSCustomObject with the same keys' {
            $h = @{ User = 'bob'; OK = $true; Cancel = $false; Cancelled = $false }
            $o = ConvertTo-FluenceResult -Result $h
            $o.User | Should -Be 'bob'
            $o.OK | Should -BeTrue
            $o.Cancelled | Should -BeFalse
        }
        It 'tags the object as Fluence.DialogResult' {
            (ConvertTo-FluenceResult -Result @{ Cancelled = $true }).PSObject.TypeNames[0] |
                Should -Be 'Fluence.DialogResult'
        }
        It 'keeps the Fluence.DialogResult type even when a result key is named PSTypeName' {
            $o = ConvertTo-FluenceResult -Result @{ PSTypeName = 'Hacked'; Cancelled = $true }
            $o.PSObject.TypeNames[0] | Should -Be 'Fluence.DialogResult'
        }
    }

}
