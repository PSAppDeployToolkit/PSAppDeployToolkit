BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTIniValue' {
    BeforeAll {
        $IniContent = @"
[MySection]
MyKey=MyValue
MyEmptyKey=
MyWhitespaceKey=
"@
        $IniContent += ' '
        $IniPath = "$TestDrive\IniFile.ini"
        Set-Content -Path $IniPath -Value $IniContent -Encoding Ascii -Force

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should return the correct value for a valid Key' {
            Get-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key 'MyKey' | Should -Be 'MyValue'
        }
        It 'Should return empty for empty values' {
            Get-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key 'MyEmptyKey' | Should -Be ''
        }
        It 'Should return empty for whitespace values' {
            Get-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key 'MyWhitespaceKey' | Should -Be ''
        }
        It 'Should return null for a non-existent Section' {
            Get-ADTIniValue -FilePath $IniPath -Section 'NonExistentSection' -Key 'MyKey' | Should -BeNull
        }
        It 'Should return null for a non-existent Key' {
            Get-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key 'NonExistentKey' | Should -BeNull
        }
    }

    Context 'Input Validation' {
        It 'Should verify that FilePath is not null, empty or whitespace' {
            { Get-ADTIniValue -FilePath $null -Section 'Anything' -Key 'Anything' } | Should -Throw
            { Get-ADTIniValue -FilePath '' -Section 'Anything' -Key 'Anything' } | Should -Throw
            { Get-ADTIniValue -FilePath ' ' -Section 'Anything' -Key 'Anything' } | Should -Throw
        }
        It 'Should verify that FilePath exists' {
            { Get-ADTIniValue -FilePath "$TestDrive\DoesNotExist.ini" -Section 'Anything' -Key 'Anything' } | Should -Throw
        }
        It 'Should verify that Section is not null, empty or whitespace' {
            { Get-ADTIniValue -FilePath $IniPath -Section $null -Key 'Anything' } | Should -Throw
            { Get-ADTIniValue -FilePath $IniPath -Section '' -Key 'Anything' } | Should -Throw
            { Get-ADTIniValue -FilePath $IniPath -Section ' ' -Key 'Anything' } | Should -Throw
        }
        It 'Should verify that Key is not null, empty or whitespace' {
            { Get-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key $null } | Should -Throw
            { Get-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key '' } | Should -Throw
            { Get-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key ' ' } | Should -Throw
        }
    }
}
