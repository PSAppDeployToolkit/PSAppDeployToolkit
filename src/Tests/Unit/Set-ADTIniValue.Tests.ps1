BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Set-ADTIniValue' {
    BeforeAll {
        $IniContent = @"
[MySection]
MyKey=MyValue
"@
        $IniPath = "$TestDrive\IniFile.ini"

        # Mock Set-ADTPreferenceVariables due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
    }
    BeforeEach {
        Set-Content -Path $IniPath -Value $IniContent -Encoding Ascii -Force
    }

    Context 'Functionality' {
        It 'Should update a value in an existing file' {
            Set-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key 'MyKey' -Value 'NewValue'
            $IniPath | Should -FileContentMatchMultiline '\[MySection\]\r\nMyKey=NewValue'
        }
        It 'Should add a new key to an existing section' {
            Set-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key 'NewKey' -Value 'NewValue'
            $IniPath | Should -FileContentMatchMultiline '\[MySection\]\r\nMyKey=MyValue\r\nNewKey=NewValue'
        }
        It 'Should set a null Value' {
            Set-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key 'NullKey' -Value $null
            $IniPath | Should -FileContentMatchMultiline '\[MySection\]\r\nMyKey=MyValue\r\nNullKey=\r\n'
        }
        It 'Should set an empty Value' {
            Set-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key 'EmptyKey' -Value ''
            $IniPath | Should -FileContentMatchMultiline '\[MySection\]\r\nMyKey=MyValue\r\nEmptyKey=\r\n'
        }
        It 'Should set a whitespace Value' {
            Set-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key 'WhitespaceKey' -Value ' '
            $IniPath | Should -FileContentMatchMultiline '\[MySection\]\r\nMyKey=MyValue\r\nWhitespaceKey=\s\r\n'
        }
        It 'Should add a new section if it does not exist' {
            Set-ADTIniValue -FilePath $IniPath -Section 'NewSection' -Key 'NewKey' -Value 'NewValue'
            $IniPath | Should -FileContentMatchMultiline '\[NewSection\]\r\nNewKey=NewValue'
        }
        It 'Should create a new file if it does not exist when Force is specified' {
            $newIniPath = "$TestDrive\NewIniFile.ini"
            Set-ADTIniValue -FilePath $newIniPath -Section 'NewSection' -Key 'NewKey' -Value 'NewValue' -Force
            $newIniPath | Should -FileContentMatchMultiline '\[NewSection\]\r\nNewKey=NewValue'
            Remove-Item -Path $newIniPath -Force
        }
    }

    Context 'Input Validation' {
        It 'Should verify that FilePath is not null, empty or whitespace' {
            { Set-ADTIniValue -FilePath $null -Section 'Anything' -Key 'Anything' -Value 'Anything' } | Should -Throw
            { Set-ADTIniValue -FilePath '' -Section 'Anything' -Key 'Anything' -Value 'Anything' } | Should -Throw
            { Set-ADTIniValue -FilePath ' ' -Section 'Anything' -Key 'Anything' -Value 'Anything' } | Should -Throw
        }
        It 'Should verify that FilePath exists if Force is not specified' {
            { Set-ADTIniValue -FilePath "$TestDrive\DoesNotExist.ini" -Section 'Anything' -Key 'Anything' -Value 'Anything' } | Should -Throw
        }
        It 'Should verify that Section is not null, empty or whitespace' {
            { Set-ADTIniValue -FilePath $IniPath -Section $null -Key 'Anything' -Value 'Anything' } | Should -Throw
            { Set-ADTIniValue -FilePath $IniPath -Section '' -Key 'Anything' -Value 'Anything' } | Should -Throw
            { Set-ADTIniValue -FilePath $IniPath -Section ' ' -Key 'Anything' -Value 'Anything' } | Should -Throw
        }
        It 'Should verify that Key is not null, empty or whitespace' {
            { Set-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key $null -Value 'Anything' } | Should -Throw
            { Set-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key '' -Value 'Anything' } | Should -Throw
            { Set-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key ' ' -Value 'Anything' } | Should -Throw
        }
        It 'Should allow null/empty/whitespace as Value' {
            { Set-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key 'NullKey' -Value $null } | Should -Not -Throw
            { Set-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key 'EmptyKey' -Value '' } | Should -Not -Throw
            { Set-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key 'WhitespaceKey' -Value ' ' } | Should -Not -Throw
        }
    }
}
