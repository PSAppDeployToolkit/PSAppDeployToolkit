BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Set-ADTMsiProperty' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory Database parameter' {
            (Get-Command Set-ADTMsiProperty).Parameters['Database'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should have a mandatory PropertyName parameter' {
            (Get-Command Set-ADTMsiProperty).Parameters['PropertyName'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should have a mandatory PropertyValue parameter' {
            (Get-Command Set-ADTMsiProperty).Parameters['PropertyValue'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should throw ParameterArgumentValidationError when PropertyName is null, empty or whitespace' {
            Set-ItResult -Skipped -Because 'Database parameter requires a live System.__ComObject which cannot be constructed headlessly; PropertyName validation cannot be reached without a valid Database instance.'
        }

        It 'Should throw ParameterArgumentValidationError when PropertyValue is null, empty or whitespace' {
            Set-ItResult -Skipped -Because 'Database parameter requires a live System.__ComObject which cannot be constructed headlessly; PropertyValue validation cannot be reached without a valid Database instance.'
        }

        It 'Should throw ParameterBindingException when Database is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Set-ADTMsiProperty'
            }
            { Set-ADTMsiProperty -Database $null -PropertyName 'ALLUSERS' -PropertyValue '1' } | Should @shouldParams
        }
    }

    Context 'Behavioural' {
        It 'Updates an existing MSI property via OpenView/Execute (UPDATE path)' {
            Set-ItResult -Skipped -Because 'No MSI fixture available; System.__ComObject database handle cannot be constructed headlessly for WindowsInstaller COM property write.'
        }

        It 'Inserts a new MSI property via OpenView/Execute (INSERT path)' {
            Set-ItResult -Skipped -Because 'No MSI fixture available; System.__ComObject database handle cannot be constructed headlessly for WindowsInstaller COM property write.'
        }

        It 'Escapes single quotes in PropertyName before building the SQL query' {
            Set-ItResult -Skipped -Because 'No MSI fixture available; System.__ComObject database handle cannot be constructed headlessly for WindowsInstaller COM property write.'
        }

        It 'Escapes single quotes in PropertyValue before building the SQL query' {
            Set-ItResult -Skipped -Because 'No MSI fixture available; System.__ComObject database handle cannot be constructed headlessly for WindowsInstaller COM property write.'
        }

        It 'Produces no output on success' {
            Set-ItResult -Skipped -Because 'No MSI fixture available; System.__ComObject database handle cannot be constructed headlessly for WindowsInstaller COM property write.'
        }

        It 'Logs a deprecation warning on invocation' {
            Set-ItResult -Skipped -Because 'No MSI fixture available; System.__ComObject database handle cannot be constructed headlessly for WindowsInstaller COM property write.'
        }
    }
}
