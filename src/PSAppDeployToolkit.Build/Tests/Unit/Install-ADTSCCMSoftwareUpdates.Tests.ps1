BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Install-ADTSCCMSoftwareUpdates' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Mock Invoke-ADTSCCMTask to intercept the initial software-updates scan trigger.
        Mock -ModuleName PSAppDeployToolkit Invoke-ADTSCCMTask { }

        # Mock Start-Sleep so the do-while poll loop (Start-Sleep -Seconds 60) does not block.
        # Note: the initial scan-wait uses the unmockable [System.Threading.Thread]::Sleep static;
        # all tests pass -SoftwareUpdatesScanWaitInSeconds 1 to keep that sleep to 1 second.
        Mock -ModuleName PSAppDeployToolkit Start-Sleep { }
    }

    Context 'Functionality - no missing updates' {
        BeforeAll {
            # Simulate CIM returning no missing updates.
            Mock -ModuleName PSAppDeployToolkit Get-CimInstance { return $null }

            # Ensure Invoke-CimMethod is NOT called when there is nothing to install.
            Mock -ModuleName PSAppDeployToolkit Invoke-CimMethod { }
        }

        It 'Returns no output when there are no missing updates' {
            $result = Install-ADTSCCMSoftwareUpdates -SoftwareUpdatesScanWaitInSeconds 1
            $result | Should -BeNullOrEmpty
        }

        It 'Does not call Invoke-CimMethod when there are no missing updates' {
            Install-ADTSCCMSoftwareUpdates -SoftwareUpdatesScanWaitInSeconds 1
            Should -Invoke -CommandName Invoke-CimMethod -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }

        It 'Calls Invoke-ADTSCCMTask to trigger the software-updates scan' {
            Install-ADTSCCMSoftwareUpdates -SoftwareUpdatesScanWaitInSeconds 1
            Should -Invoke -CommandName Invoke-ADTSCCMTask -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $ScheduleId -eq 'SoftwareUpdatesScan'
            }
        }

        It 'Queries Get-CimInstance against the ROOT\CCM\ClientSDK namespace for missing updates' {
            Install-ADTSCCMSoftwareUpdates -SoftwareUpdatesScanWaitInSeconds 1
            Should -Invoke -CommandName Get-CimInstance -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Namespace -eq 'ROOT\CCM\ClientSDK'
            }
        }
    }

    Context 'Functionality - with missing updates' {
        BeforeAll {
            # Simulate missing updates on the compliance query and empty pending poll.
            Mock -ModuleName PSAppDeployToolkit Get-CimInstance {
                return @(
                    [Microsoft.Management.Infrastructure.CimInstance]::new('CCM_SoftwareUpdate', 'ROOT\CCM\ClientSDK'),
                    [Microsoft.Management.Infrastructure.CimInstance]::new('CCM_SoftwareUpdate', 'ROOT\CCM\ClientSDK')
                )
            } -ParameterFilter { $Query -like '*ComplianceState*' }

            # Pending-updates poll returns nothing so the do-while exits immediately.
            Mock -ModuleName PSAppDeployToolkit Get-CimInstance {
                return $null
            } -ParameterFilter { $Query -like '*EvaluationState*' }

            # InstallUpdates method returns success.
            Mock -ModuleName PSAppDeployToolkit Invoke-CimMethod {
                return [PSCustomObject]@{ ReturnValue = 0 }
            }
        }

        It 'Calls Invoke-CimMethod once when missing updates are present' {
            Install-ADTSCCMSoftwareUpdates -SoftwareUpdatesScanWaitInSeconds 1
            Should -Invoke -CommandName Invoke-CimMethod -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }

        It 'Calls Invoke-CimMethod against ROOT\CCM\ClientSDK with InstallUpdates method on CCM_SoftwareUpdatesManager' {
            Install-ADTSCCMSoftwareUpdates -SoftwareUpdatesScanWaitInSeconds 1
            Should -Invoke -CommandName Invoke-CimMethod -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Namespace -eq 'ROOT\CCM\ClientSDK' -and $MethodName -eq 'InstallUpdates' -and $ClassName -eq 'CCM_SoftwareUpdatesManager'
            }
        }

        It 'Polls Get-CimInstance for pending updates after calling InstallUpdates' {
            Install-ADTSCCMSoftwareUpdates -SoftwareUpdatesScanWaitInSeconds 1
            Should -Invoke -CommandName Get-CimInstance -ModuleName PSAppDeployToolkit -ParameterFilter {
                $Namespace -eq 'ROOT\CCM\ClientSDK' -and $Query -like '*EvaluationState*'
            }
        }

        It 'Returns no output when missing updates are found and installed successfully' {
            $result = Install-ADTSCCMSoftwareUpdates -SoftwareUpdatesScanWaitInSeconds 1
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Functionality - InstallUpdates non-zero return value' {
        BeforeAll {
            # Return one missing update.
            Mock -ModuleName PSAppDeployToolkit Get-CimInstance {
                return @(
                    [Microsoft.Management.Infrastructure.CimInstance]::new('CCM_SoftwareUpdate', 'ROOT\CCM\ClientSDK')
                )
            } -ParameterFilter { $Query -like '*ComplianceState*' }

            # InstallUpdates method returns a non-zero error code.
            Mock -ModuleName PSAppDeployToolkit Invoke-CimMethod {
                return [PSCustomObject]@{ ReturnValue = 1 }
            }
        }

        It 'Throws when InstallUpdates returns a non-zero ReturnValue' {
            { Install-ADTSCCMSoftwareUpdates -SoftwareUpdatesScanWaitInSeconds 1 } | Should -Throw
        }
    }

    Context 'Input Validation' {
        It 'Should have a non-mandatory SoftwareUpdatesScanWaitInSeconds parameter' {
            (Get-Command Install-ADTSCCMSoftwareUpdates).Parameters['SoftwareUpdatesScanWaitInSeconds'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Should have a non-mandatory WaitForPendingUpdatesTimeout parameter' {
            (Get-Command Install-ADTSCCMSoftwareUpdates).Parameters['WaitForPendingUpdatesTimeout'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'SoftwareUpdatesScanWaitInSeconds parameter type is UInt32 and defaults to 180' {
            $cmd = Get-Command Install-ADTSCCMSoftwareUpdates
            $cmd.Parameters['SoftwareUpdatesScanWaitInSeconds'].ParameterType | Should -Be ([System.UInt32])
            $defaultText = $cmd.ScriptBlock.Ast.FindAll(
                { $args[0] -is [System.Management.Automation.Language.ParamBlockAst] }, $true
            ).Parameters | Where-Object { $_.Name.VariablePath.UserPath -eq 'SoftwareUpdatesScanWaitInSeconds' } |
                Select-Object -ExpandProperty DefaultValue |
                Select-Object -ExpandProperty Extent |
                Select-Object -ExpandProperty Text
            $defaultText | Should -Be '180'
        }

        It 'WaitForPendingUpdatesTimeout parameter type is TimeSpan and defaults to 45 minutes' {
            $cmd = Get-Command Install-ADTSCCMSoftwareUpdates
            $cmd.Parameters['WaitForPendingUpdatesTimeout'].ParameterType | Should -Be ([System.TimeSpan])
            $defaultText = $cmd.ScriptBlock.Ast.FindAll(
                { $args[0] -is [System.Management.Automation.Language.ParamBlockAst] }, $true
            ).Parameters | Where-Object { $_.Name.VariablePath.UserPath -eq 'WaitForPendingUpdatesTimeout' } |
                Select-Object -ExpandProperty DefaultValue |
                Select-Object -ExpandProperty Extent |
                Select-Object -ExpandProperty Text
            $defaultText | Should -BeLike '*FromMinutes(45)*'
        }

        It 'Skips Invoke-CimMethod when -WhatIf is supplied and missing updates are present' {
            Set-ItResult -Skipped -Because 'Install-ADTSCCMSoftwareUpdates wraps SupportsShouldProcess internally; -WhatIf propagation from outside the module scope is not supported in headless Pester contexts.'
        }
    }
}
