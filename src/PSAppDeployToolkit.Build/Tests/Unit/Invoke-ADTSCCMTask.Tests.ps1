BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Invoke-ADTSCCMTask' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Mock Invoke-CimMethod to intercept the TriggerSchedule call and return a success result.
        Mock -ModuleName PSAppDeployToolkit Invoke-CimMethod {
            return [PSCustomObject]@{ ReturnValue = $null }
        }
    }

    Context 'Functionality' {
        It 'Invokes Invoke-CimMethod once per ScheduleId call' -ForEach @(
            @{ ScheduleId = 'HardwareInventory' }
            @{ ScheduleId = 'SoftwareUpdatesScan' }
        ) {
            Invoke-ADTSCCMTask -ScheduleId $ScheduleId
            Should -Invoke -CommandName Invoke-CimMethod -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }

        It 'Calls Invoke-CimMethod against the ROOT\CCM namespace with TriggerSchedule method' {
            Invoke-ADTSCCMTask -ScheduleId HardwareInventory
            Should -Invoke -CommandName Invoke-CimMethod -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Namespace -eq 'ROOT\CCM' -and $MethodName -eq 'TriggerSchedule' -and $ClassName -eq 'SMS_Client'
            }
        }

        It 'Sends the correct schedule GUID for HardwareInventory (S-1 = 0x0001)' {
            Invoke-ADTSCCMTask -ScheduleId HardwareInventory
            Should -Invoke -CommandName Invoke-CimMethod -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Arguments['sScheduleID'] -eq '{00000000-0000-0000-0000-000000000001}'
            }
        }

        It 'Sends the correct schedule GUID for SoftwareUpdatesScan (S-275 = 0x0113)' {
            Invoke-ADTSCCMTask -ScheduleId SoftwareUpdatesScan
            Should -Invoke -CommandName Invoke-CimMethod -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Arguments['sScheduleID'] -eq '{00000000-0000-0000-0000-000000000113}'
            }
        }

        It 'Produces no output' {
            $result = Invoke-ADTSCCMTask -ScheduleId HardwareInventory
            $result | Should -BeNullOrEmpty
        }

        It 'Skips Invoke-CimMethod when -WhatIf is supplied' {
            Invoke-ADTSCCMTask -ScheduleId HardwareInventory -WhatIf
            Should -Invoke -CommandName Invoke-CimMethod -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory ScheduleId parameter' {
            (Get-Command Invoke-ADTSCCMTask).Parameters['ScheduleId'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws ParameterArgumentTransformationError when ScheduleId is not a valid TriggerScheduleId value' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Invoke-ADTSCCMTask -ScheduleId 'NotAValidScheduleId' } | Should @shouldParams
        }
    }
}
