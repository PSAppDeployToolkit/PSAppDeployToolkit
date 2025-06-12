BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Remove-ADTIniSection' {
    BeforeAll {
        $IniContent = @"
[MySection]
MyKey=MyValue
[MyOtherSection]
MyOtherKey=MyOtherValue
"@
        $IniPath = "$TestDrive\IniFile.ini"
        Set-Content -Path $IniPath -Value $IniContent -Encoding Ascii -Force

        # Mock Set-ADTPreferenceVariables due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
    }

    Context 'Functionality' {
        It 'Should remove an entire section, leaving other sections intact' {
            Remove-ADTIniSection -FilePath $IniPath -Section 'MySection'
            $IniPath | Should -Not -FileContentMatch '\[MySection\]'
            $IniPath | Should -Not -FileContentMatch 'MyKey=MyValue'
            $IniPath | Should -FileContentMatchMultiline '\[MyOtherSection\]\r\nMyOtherKey=MyOtherValue\r\n'
        }
    }

    Context 'Input Validation' {
        It 'Should verify that FilePath is not null, empty or whitespace' {
            { Remove-ADTIniSection -FilePath $null -Section 'Anything' } | Should -Throw
            { Remove-ADTIniSection -FilePath '' -Section 'Anything' } | Should -Throw
            { Remove-ADTIniSection -FilePath ' ' -Section 'Anything' } | Should -Throw
        }
        It 'Should verify that FilePath exists' {
            { Remove-ADTIniSection -FilePath "$TestDrive\DoesNotExist.ini" -Section 'Anything' } | Should -Throw
        }
        It 'Should verify that Section is not null, empty or whitespace' {
            { Remove-ADTIniSection -FilePath $IniPath -Section $null } | Should -Throw
            { Remove-ADTIniSection -FilePath $IniPath -Section '' } | Should -Throw
            { Remove-ADTIniSection -FilePath $IniPath -Section ' ' } | Should -Throw
        }
    }
}
