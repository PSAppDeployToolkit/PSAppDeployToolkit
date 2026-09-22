BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'New-FluenceButton' {
    It 'tags the object as Fluence.Button' {
        (New-FluenceButton -Text 'OK').PSObject.TypeNames[0] | Should -Be 'Fluence.Button'
    }
    It 'defaults Name to Text when Name omitted' {
        (New-FluenceButton -Text 'Save').Name | Should -Be 'Save'
    }
    It 'flags IsDefault and IsCancel' {
        $b = New-FluenceButton -Text 'Cancel' -IsCancel
        $b.IsCancel | Should -BeTrue
        $b.IsDefault | Should -BeFalse
    }
}
