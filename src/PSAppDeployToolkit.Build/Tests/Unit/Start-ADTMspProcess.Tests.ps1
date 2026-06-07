BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Start-ADTMspProcess' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Input Validation' {
        It 'Has a mandatory FilePath parameter' {
            (Get-Command Start-ADTMspProcess).Parameters['FilePath'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws a parameter binding error when FilePath has an unsupported extension' {
            { Start-ADTMspProcess -FilePath 'patch.msi' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Start-ADTMspProcess'
        }

        It 'Throws a parameter binding error when AdditionalArgumentList contains a whitespace-only value' {
            { Start-ADTMspProcess -FilePath 'patch.msp' -AdditionalArgumentList '   ' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Start-ADTMspProcess'
        }
    }

    Context 'Forwarding to Start-ADTMsiProcess' {
        BeforeEach {
            Mock -ModuleName PSAppDeployToolkit Start-ADTMsiProcess { }
        }

        It 'Forwards to Start-ADTMsiProcess once' {
            Start-ADTMspProcess -FilePath 'patch.msp'
            Should -Invoke -CommandName Start-ADTMsiProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }

        It 'Forwards with Action Patch and the MSP FilePath unchanged' {
            Start-ADTMspProcess -FilePath 'patch.msp'
            Should -Invoke -CommandName Start-ADTMsiProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Action -eq 'Patch' -and $FilePath -eq 'patch.msp'
            }
        }

        It 'Forwards AdditionalArgumentList through to Start-ADTMsiProcess' {
            Start-ADTMspProcess -FilePath 'patch.msp' -AdditionalArgumentList 'ALLUSERS=1'
            Should -Invoke -CommandName Start-ADTMsiProcess -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $AdditionalArgumentList -contains 'ALLUSERS=1'
            }
        }

        It 'Returns the Start-ADTMsiProcess result when PassThru is specified' {
            Mock -ModuleName PSAppDeployToolkit Start-ADTMsiProcess { return [PSADT.ProcessManagement.ProcessResult]::new(0) }
            $result = Start-ADTMspProcess -FilePath 'patch.msp' -PassThru
            $result | Should -BeOfType ([PSADT.ProcessManagement.ProcessResult])
            $result.ExitCode | Should -Be 0
        }
    }

    Context 'WhatIf handling' {
        It 'Does not invoke Start-ADTMsiProcess when WhatIf is specified' {
            Mock -ModuleName PSAppDeployToolkit Start-ADTMsiProcess { }
            Start-ADTMspProcess -FilePath 'patch.msp' -WhatIf
            Should -Invoke -CommandName Start-ADTMsiProcess -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }
}
