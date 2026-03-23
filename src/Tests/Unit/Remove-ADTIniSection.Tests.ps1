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

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
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
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTIniSection'
            }
            { Remove-ADTIniSection -FilePath $null -Section 'Anything' } | Should @shouldParams
            { Remove-ADTIniSection -FilePath '' -Section 'Anything' } | Should @shouldParams
            { Remove-ADTIniSection -FilePath " `f`n`r`t`v" -Section 'Anything' } | Should @shouldParams
        }
        It 'Should verify that FilePath exists' {
            { Remove-ADTIniSection -FilePath "$TestDrive\DoesNotExist.ini" -Section 'Anything' } | Should -Throw -ExceptionType ([System.ArgumentException]) -ExpectedMessage "The specified file does not exist.*" -ErrorId 'InvalidFilePathParameterValue,Remove-ADTIniSection'
        }
        It 'Should verify that Section is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTIniSection'
            }
            { Remove-ADTIniSection -FilePath $IniPath -Section $null } | Should @shouldParams
            { Remove-ADTIniSection -FilePath $IniPath -Section '' } | Should @shouldParams
            { Remove-ADTIniSection -FilePath $IniPath -Section " `f`n`r`t`v" } | Should @shouldParams
        }
    }
}
