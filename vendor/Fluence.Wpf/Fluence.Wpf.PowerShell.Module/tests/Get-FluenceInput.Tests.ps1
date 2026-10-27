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

# Caller-thread contract of Get-FluenceInput: the prompt and button specifications are built, and
# -Countdown is validated, before Show-FluenceDialog reaches any UI work, so these cases open no
# window. The rendering of each input type is covered by New-FluenceInputControl.Tests.ps1.

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'Get-FluenceInput parameter surface' {
    # Get-FluenceInput forwards -InputType verbatim to New-FluencePrompt, so a value it accepts and
    # New-FluencePrompt rejects would fail at binding inside the wrapper, far from the caller.
    It 'offers only input types New-FluencePrompt accepts' {
        $wrapper = (Get-Command Get-FluenceInput).Parameters['InputType'].Attributes.ValidValues
        $prompt = (Get-Command New-FluencePrompt).Parameters['InputType'].Attributes.ValidValues
        $wrapper | Should -Not -BeNullOrEmpty
        foreach ($value in $wrapper)
        {
            $prompt | Should -Contain $value
        }
    }
    # List is the one type New-FluencePrompt has that the single-value wrapper deliberately omits;
    # Show-FluenceListSelection is the cmdlet for it, and it carries -MultiSelect.
    It 'omits the List input type, which Show-FluenceListSelection owns' {
        (Get-Command Get-FluenceInput).Parameters['InputType'].Attributes.ValidValues |
            Should -Not -Contain 'List'
        (Get-Command Show-FluenceListSelection).Name | Should -Be 'Show-FluenceListSelection'
    }
    # A Choice prompt needs its values, so the wrapper has to carry -ValidateSet and -As through or
    # every -InputType Choice call throws 'A Choice prompt requires -ValidateSet.'
    It 'carries the Choice parameters -ValidateSet and -As' {
        (Get-Command Get-FluenceInput).Parameters.Keys | Should -Contain 'ValidateSet'
        (Get-Command Get-FluenceInput).Parameters.Keys | Should -Contain 'As'
        (Get-Command Get-FluenceInput).Parameters['As'].Attributes.ValidValues | Should -Be @('Combo', 'Radio')
    }
    It 'rejects an unknown -InputType' {
        { Get-FluenceInput -Message 'x' -InputType Wizard } |
            Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
    }
    It 'rejects a -Timeout outside 1 to 86400' {
        { Get-FluenceInput -Message 'x' -Timeout 0 } |
            Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
    }
}

Describe 'Get-FluenceInput pre-UI validation' {
    It 'rejects -Countdown without -Timeout' {
        { Get-FluenceInput -Message 'x' -Countdown } | Should -Throw '*-Countdown requires -Timeout*'
    }
    It 'rejects -InputType Choice without -ValidateSet' {
        { Get-FluenceInput -Message 'x' -InputType Choice } | Should -Throw '*Choice prompt requires -ValidateSet*'
    }
    It 'rejects a -DefaultValue that does not coerce to the input type' {
        { Get-FluenceInput -Message 'x' -InputType Number -DefaultValue 'abc' } |
            Should -Throw "*is not valid for an InputType of 'Number'*"
    }
}
