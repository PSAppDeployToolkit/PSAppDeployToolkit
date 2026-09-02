BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Invoke-ADTSCCMTask' {
    Context 'Functionality' {
        It 'Triggers nothing with -WhatIf' {
            # Every path beyond the confirmation reaches the Configuration Manager client, so -WhatIf is
            # how the parameter handling is exercised without asking the client to do anything.
            { Invoke-ADTSCCMTask -ScheduleId HardwareInventory -WhatIf } | Should -Not -Throw
        }

        It 'Accepts every task the client knows about' {
            # The identifiers are turned into a schedule GUID by arithmetic on the underlying value, so a
            # name the enum carries but the conversion cannot handle would only show up here.
            foreach ($scheduleId in [System.Enum]::GetNames([PSADT.ConfigMgr.TriggerScheduleId]))
            {
                { Invoke-ADTSCCMTask -ScheduleId $scheduleId -WhatIf } | Should -Not -Throw
            }
        }

        It 'Reports a client that is not there' -Skip:(!!(Get-CimInstance -Namespace 'ROOT\CCM' -ClassName SMS_Client -ErrorAction Ignore)) {
            # Skipped where a Configuration Manager client is installed, since triggering a real task is a
            # change to the machine rather than something to assert against.
            { Invoke-ADTSCCMTask -ScheduleId HardwareInventory } | Should -Throw
        }
    }

    Context 'Input Validation' {
        It 'Requires a task to trigger' {
            Test-ADTMandatoryParameter -Command (Get-Command Invoke-ADTSCCMTask) -Parameter ScheduleId | Should -BeTrue
        }

        It 'Refuses a task it does not know' {
            { Invoke-ADTSCCMTask -ScheduleId 'NotATask' } | Should -Throw -ErrorId 'ParameterArgumentTransformationError,Invoke-ADTSCCMTask'
        }
    }
}
