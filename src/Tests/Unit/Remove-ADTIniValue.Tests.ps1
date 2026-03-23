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

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
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
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTIniValue'
            }
            { Remove-ADTIniValue -FilePath $null -Section 'Anything' -Key 'Anything' } | Should @shouldParams
            { Remove-ADTIniValue -FilePath '' -Section 'Anything' -Key 'Anything' } | Should @shouldParams
            { Remove-ADTIniValue -FilePath " `f`n`r`t`v" -Section 'Anything' -Key 'Anything' } | Should @shouldParams
        }
        It 'Should verify that FilePath exists' {
            { Remove-ADTIniValue -FilePath "$TestDrive\DoesNotExist.ini" -Section 'Anything' -Key 'Anything' } | Should -Throw -ExceptionType ([System.ArgumentException]) -ExpectedMessage 'The specified file does not exist.*' -ErrorId 'InvalidFilePathParameterValue,Remove-ADTIniValue'
        }
        It 'Should verify that Section is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTIniValue'
            }
            { Remove-ADTIniValue -FilePath $IniPath -Section $null -Key 'Anything' } | Should @shouldParams
            { Remove-ADTIniValue -FilePath $IniPath -Section '' -Key 'Anything' } | Should @shouldParams
            { Remove-ADTIniValue -FilePath $IniPath -Section " `f`n`r`t`v" -Key 'Anything' } | Should @shouldParams

        }
        It 'Should verify that Key is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTIniValue'
            }
            { Remove-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key $null } | Should @shouldParams
            { Remove-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key '' } | Should @shouldParams
            { Remove-ADTIniValue -FilePath $IniPath -Section 'MySection' -Key " `f`n`r`t`v" } | Should @shouldParams
        }
    }
}
