BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTIniSection' {
    BeforeAll {
        $IniContent = @"
[MySection]
; This is a comment
# This is also a comment
MyKey=MyValue
MyKey2=MyValue2
[MyEmptySection]
"@
        $IniPath = "$TestDrive\IniFile.ini"
        Set-Content -Path $IniPath -Value $IniContent -Encoding Ascii -Force

        # Mock Set-ADTPreferenceVariables due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
    }

    Context 'Functionality' {
        It 'Should return a section' {
            $IniSection = Get-ADTIniSection -FilePath $IniPath -Section 'MySection'
            $IniSection | Should -BeOfType 'System.Collections.Specialized.OrderedDictionary' # May change to 'System.Collections.Generic.Dictionary[string,string]'
            $IniSection.MyKey | Should -Be 'MyValue'
            $IniSection.MyKey2 | Should -Be 'MyValue2'
        }
        It 'Should return null' {
            $IniSection = Get-ADTIniSection -FilePath $IniPath -Section 'MyEmptySection'
            $IniSection | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Should verify that FilePath is not null, empty or whitespace' {
            { Get-ADTIniSection -FilePath $null -Section 'Anything' } | Should -Throw
            { Get-ADTIniSection -FilePath '' -Section 'Anything' } | Should -Throw
            { Get-ADTIniSection -FilePath ' ' -Section 'Anything' } | Should -Throw
        }
        It 'Should verify that FilePath exists' {
            { Get-ADTIniSection -FilePath "$TestDrive\DoesNotExist.ini" -Section 'Anything' } | Should -Throw
        }
        It 'Should verify that Section is not null, empty or whitespace' {
            { Get-ADTIniSection -FilePath $IniPath -Section $null } | Should -Throw
            { Get-ADTIniSection -FilePath $IniPath -Section '' } | Should -Throw
            { Get-ADTIniSection -FilePath $IniPath -Section ' ' } | Should -Throw
        }
    }
}
