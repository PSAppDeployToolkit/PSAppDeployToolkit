BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Remove-ADTIniValue' {
    BeforeAll {
        $IniContent = @"
[MySection]
MyKey=MyValue
MyOtherKey=MyOtherValue
"@
        $IniPath = "$TestDrive\IniFile.ini"
        Set-Content -Path $IniPath -Value $IniContent -Encoding Ascii -Force

        # Mock Set-ADTPreferenceVariables due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
    }

    Context 'Functionality' {
        It 'Should remove a Key' {
            Remove-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key 'MyKey'
            $IniPath | Should -FileContentMatchMultiline '\[MySection\]\r\nMyOtherKey=MyOtherValue\r\n'
            $IniPath  | Should -Not -FileContentMatchMultiline 'MyKey='
        }
    }

    Context 'Input Validation' {
        It 'Should verify that FilePath is not null, empty or whitespace' {
            { Remove-ADTIniValue -FilePath $null -Section 'Anything' -Key 'Anything' } | Should -Throw
            { Remove-ADTIniValue -FilePath '' -Section 'Anything' -Key 'Anything' } | Should -Throw
            { Remove-ADTIniValue -FilePath ' ' -Section 'Anything' -Key 'Anything' } | Should -Throw
        }
        It 'Should verify that FilePath exists' {
            { Remove-ADTIniValue -FilePath "$TestDrive\DoesNotExist.ini" -Section 'Anything' -Key 'Anything' } | Should -Throw
        }
        It 'Should verify that Section is not null, empty or whitespace' {
            { Remove-ADTIniValue -FilePath $IniPath -Section $null -Key 'Anything' } | Should -Throw
            { Remove-ADTIniValue -FilePath $IniPath -Section '' -Key 'Anything' } | Should -Throw
            { Remove-ADTIniValue -FilePath $IniPath -Section ' ' -Key 'Anything' } | Should -Throw

        }
        It 'Should verify that Key is not null, empty or whitespace' {
            { Remove-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key $null } | Should -Throw
            { Remove-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key '' } | Should -Throw
            { Remove-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key ' ' } | Should -Throw
        }
    }
}
