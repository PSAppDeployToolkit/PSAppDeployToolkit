BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Unblock-ADTAppExecution' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Mock Get-ScheduledTask so the default Tasks parameter value resolves to nothing.
        Mock -ModuleName PSAppDeployToolkit Get-ScheduledTask { return $null }

        # Mock Remove-ADTModuleCallback so the always-executed finally block does not error.
        Mock -ModuleName PSAppDeployToolkit Remove-ADTModuleCallback { }
    }

    Context 'Non-admin bypass' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTCallerIsAdmin { return $false }
        }

        It 'Returns without throwing when the caller is not an administrator' {
            { Unblock-ADTAppExecution } | Should -Not -Throw
        }

        It 'Logs a bypass message that mentions not-admin when the caller lacks elevation' {
            Unblock-ADTAppExecution
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Message -like '*not admin*'
            }
        }

        It 'Produces no output when the caller is not an administrator' {
            $result = Unblock-ADTAppExecution
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Admin path - no pending blocked tasks' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTCallerIsAdmin { return $true }

            # Mock the IFEO registry scan performed by Unblock-ADTAppExecutionInternal.
            Mock -ModuleName PSAppDeployToolkit Get-ItemProperty { return @() }

            # Mock Unregister-ScheduledTask so no real task changes occur.
            Mock -ModuleName PSAppDeployToolkit Unregister-ScheduledTask { }
        }

        It 'Completes without throwing when there are no blocked-app scheduled tasks' {
            { Unblock-ADTAppExecution } | Should -Not -Throw
        }

        It 'Produces no output when there are no blocked-app scheduled tasks' {
            $result = Unblock-ADTAppExecution
            $result | Should -BeNullOrEmpty
        }

        It 'Calls Remove-ADTModuleCallback with Hookpoint OnFinish in the finally block' {
            Unblock-ADTAppExecution
            Should -Invoke -CommandName Remove-ADTModuleCallback -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Hookpoint -eq 'OnFinish'
            }
        }

        It 'Does not call Unregister-ScheduledTask when no tasks are found' {
            Unblock-ADTAppExecution
            Should -Invoke -CommandName Unregister-ScheduledTask -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }

    Context 'Admin path - with an explicit blocked-app scheduled task' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTCallerIsAdmin { return $true }

            # Mock the IFEO registry scan so no real keys are read.
            Mock -ModuleName PSAppDeployToolkit Get-ItemProperty { return @() }

            # Mock Unregister-ScheduledTask to intercept and verify the call.
            Mock -ModuleName PSAppDeployToolkit Unregister-ScheduledTask { }
        }

        It 'Calls Unregister-ScheduledTask once when a single blocked-app task is supplied' {
            $fakeTask = [Microsoft.Management.Infrastructure.CimInstance]::new('MSFT_ScheduledTask', 'ROOT\Microsoft\Windows\TaskScheduler')
            $nameProp = [Microsoft.Management.Infrastructure.CimProperty]::Create('TaskName', 'PSAppDeployToolkit_TestApp_BlockedApps', [Microsoft.Management.Infrastructure.CimType]::String, [Microsoft.Management.Infrastructure.CimFlags]::None)
            $fakeTask.CimInstanceProperties.Add($nameProp)

            Unblock-ADTAppExecution -Tasks $fakeTask
            Should -Invoke -CommandName Unregister-ScheduledTask -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }

        It 'Calls Remove-ADTModuleCallback in the finally block when an explicit task is given' {
            $fakeTask = [Microsoft.Management.Infrastructure.CimInstance]::new('MSFT_ScheduledTask', 'ROOT\Microsoft\Windows\TaskScheduler')
            $nameProp = [Microsoft.Management.Infrastructure.CimProperty]::Create('TaskName', 'PSAppDeployToolkit_TestApp_BlockedApps', [Microsoft.Management.Infrastructure.CimType]::String, [Microsoft.Management.Infrastructure.CimFlags]::None)
            $fakeTask.CimInstanceProperties.Add($nameProp)

            Unblock-ADTAppExecution -Tasks $fakeTask
            Should -Invoke -CommandName Remove-ADTModuleCallback -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Hookpoint -eq 'OnFinish'
            }
        }

        It 'Produces no output when an explicit task is supplied' {
            $fakeTask = [Microsoft.Management.Infrastructure.CimInstance]::new('MSFT_ScheduledTask', 'ROOT\Microsoft\Windows\TaskScheduler')
            $nameProp = [Microsoft.Management.Infrastructure.CimProperty]::Create('TaskName', 'PSAppDeployToolkit_TestApp_BlockedApps', [Microsoft.Management.Infrastructure.CimType]::String, [Microsoft.Management.Infrastructure.CimFlags]::None)
            $fakeTask.CimInstanceProperties.Add($nameProp)

            $result = Unblock-ADTAppExecution -Tasks $fakeTask
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Should have a non-mandatory Tasks parameter' {
            (Get-Command Unblock-ADTAppExecution).Parameters['Tasks'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Throws ParameterArgumentValidationError when Tasks contains a null element' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Unblock-ADTAppExecution'
            }
            { Unblock-ADTAppExecution -Tasks $null } | Should @shouldParams
        }
    }
}
