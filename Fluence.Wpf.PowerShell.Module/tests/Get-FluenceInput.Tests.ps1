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
