BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTMsiExitCodeMessage' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Returns a non-empty string for a known MSI exit code (1618)' {
            $result = Get-ADTMsiExitCodeMessage -MsiExitCode 1618
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType ([System.String])
        }
        It 'Returns nothing for an unknown/out-of-range exit code (999999)' {
            $result = Get-ADTMsiExitCodeMessage -MsiExitCode 999999
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Throws ParameterArgumentValidationError when MsiExitCode is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Get-ADTMsiExitCodeMessage'
            }
            { Get-ADTMsiExitCodeMessage -MsiExitCode $null } | Should @shouldParams
        }
    }
}
