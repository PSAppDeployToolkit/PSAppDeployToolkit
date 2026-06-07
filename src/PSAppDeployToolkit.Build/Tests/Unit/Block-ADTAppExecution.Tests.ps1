BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Block-ADTAppExecution' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory Processes parameter' {
            (Get-Command Block-ADTAppExecution).Parameters['Processes'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should have a non-mandatory WindowLocation parameter' {
            (Get-Command Block-ADTAppExecution).Parameters['WindowLocation'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Throws ParameterArgumentValidationError when Processes is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Block-ADTAppExecution'
            }
            { Block-ADTAppExecution -Processes $null } | Should @shouldParams
        }

        It 'Throws when Processes array contains duplicate entries (ValidateUnique)' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'pd', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $pd = [PSADT.ProcessManagement.ProcessDefinition]::new('notepad.exe')
            { Block-ADTAppExecution -Processes @($pd, $pd) } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }

    Context 'Session requirement' {
        It 'Throws InvalidOperationException when no ADT session is active' {
            $pd = [PSADT.ProcessManagement.ProcessDefinition]::new('notepad.exe')
            { Block-ADTAppExecution -Processes $pd } | Should -Throw -ExceptionType ([System.InvalidOperationException])
        }
    }

    Context 'Behavioural' {
        BeforeAll {
            # Supply a fake session so the begin{} barrier is satisfied without Open-ADTSession.
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession {
                return [PSCustomObject]@{
                    InstallName                 = 'TestApp'
                    InstallTitle                = 'Test Application'
                    DeploymentType              = [PSAppDeployToolkit.Foundation.DeploymentType]::Install
                    DeployAppScriptSessionState = $null
                }
            }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig {
                return [PSCustomObject]@{
                    Assets = [PSCustomObject]@{
                        Logo        = $null
                        LogoDark    = $null
                        Banner      = $null
                        TaskbarIcon = $null
                    }
                    UI     = [PSCustomObject]@{
                        DefaultTimeout    = 60
                        DialogStyle       = 'Classic'
                        FluentAccentColor = $null
                    }
                }
            }
            Mock -ModuleName PSAppDeployToolkit Get-ADTStringTable {
                return [PSCustomObject]@{
                    BlockExecutionText = [PSCustomObject]@{
                        Subtitle = [PSCustomObject]@{ Install = 'Please wait...' }
                        Message  = [PSCustomObject]@{ Install = 'Installation in progress.' }
                    }
                }
            }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
            Mock -ModuleName PSAppDeployToolkit Set-ADTClientServerProcessPermissions { }
            # Return a string so DataSerialization::SerializeToString[T] receives a serialisable value.
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeDialogOptions' }
            Mock -ModuleName PSAppDeployToolkit Set-ADTRegistryKey { }
            Mock -ModuleName PSAppDeployToolkit Get-ADTPowerShellProcessPath { return 'pwsh.exe' }
            Mock -ModuleName PSAppDeployToolkit Out-ADTPowerShellEncodedCommand { return 'FAKEENCODED' }
            # Register-ScheduledTask is from ScheduledTasks module but mockable within PSAppDeployToolkit scope.
            Mock -ModuleName PSAppDeployToolkit Register-ScheduledTask { return [PSCustomObject]@{ } }
            Mock -ModuleName PSAppDeployToolkit Add-ADTModuleCallback { }
            Mock -ModuleName PSAppDeployToolkit Unblock-ADTAppExecution { }
            Mock -ModuleName PSAppDeployToolkit Complete-ADTFunction { }
            # Intercept the error handler so Registry::SetValue AccessDenied (post-task-creation)
            # does not propagate as a test failure; the seam under test is verified before that point.
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTFunctionErrorHandler { }
        }

        Context 'Non-admin bypass' {
            BeforeAll {
                Mock -ModuleName PSAppDeployToolkit Get-ADTEnvironmentTable {
                    return [PSCustomObject]@{
                        IsAdmin                              = $false
                        ProcessNTAccount                     = 'TEST\user'
                        appDeployToolkitName                 = 'PSAppDeployToolkit'
                        InvalidScheduledTaskNameCharsRegExPattern = [regex]::new('[^\w\-]')
                    }
                }
                Mock -ModuleName PSAppDeployToolkit Get-ScheduledTask { return $null }
            }

            It 'Logs a bypass message containing "not admin" when the caller lacks elevation' {
                $pd = [PSADT.ProcessManagement.ProcessDefinition]::new('notepad.exe')
                Block-ADTAppExecution -Processes $pd
                Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                    $Message -like '*not admin*'
                }
            }

            It 'Does not call Set-ADTRegistryKey when the caller lacks elevation' {
                $pd = [PSADT.ProcessManagement.ProcessDefinition]::new('notepad.exe')
                Block-ADTAppExecution -Processes $pd
                Should -Invoke -CommandName Set-ADTRegistryKey -ModuleName PSAppDeployToolkit -Times 0 -Exactly
            }

            It 'Produces no output when the caller lacks elevation' {
                $pd = [PSADT.ProcessManagement.ProcessDefinition]::new('notepad.exe')
                $result = Block-ADTAppExecution -Processes $pd
                $result | Should -BeNullOrEmpty
            }
        }

        Context 'Admin path - block-execution registry key' {
            BeforeAll {
                Mock -ModuleName PSAppDeployToolkit Get-ADTEnvironmentTable {
                    return [PSCustomObject]@{
                        IsAdmin                              = $true
                        ProcessNTAccount                     = 'TEST\admin'
                        appDeployToolkitName                 = 'PSAppDeployToolkit'
                        InvalidScheduledTaskNameCharsRegExPattern = [regex]::new('[^\w\-]')
                    }
                }
                Mock -ModuleName PSAppDeployToolkit Get-ScheduledTask { return $null }
            }

            It 'Calls Set-ADTRegistryKey to persist the block-execution arguments under the PSAppDeployToolkit registry path' {
                $pd = [PSADT.ProcessManagement.ProcessDefinition]::new('notepad.exe')
                # -WhatIf skips the ShouldProcess-gated scheduled-task and IFEO sections; Set-ADTRegistryKey is before that gate.
                Block-ADTAppExecution -Processes $pd -WhatIf
                Should -Invoke -CommandName Set-ADTRegistryKey -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                    $LiteralPath -like '*PSAppDeployToolkit*'
                }
            }

            It 'Does not call Register-ScheduledTask when -WhatIf is supplied' {
                $pd = [PSADT.ProcessManagement.ProcessDefinition]::new('notepad.exe')
                Block-ADTAppExecution -Processes $pd -WhatIf
                Should -Invoke -CommandName Register-ScheduledTask -ModuleName PSAppDeployToolkit -Times 0 -Exactly
            }

            It 'Calls Register-ScheduledTask to create the cleanup scheduled task when -WhatIf is not supplied' {
                $pd = [PSADT.ProcessManagement.ProcessDefinition]::new('notepad.exe')
                # The function continues past Register-ScheduledTask into the IFEO write loop, which calls
                # [Microsoft.Win32.Registry]::SetValue() — an unmockable .NET static that requires HKLM write
                # access. Invoke-ADTFunctionErrorHandler is mocked to absorb that access-denied error so the
                # mock assertion is still reachable and verifiable.
                Block-ADTAppExecution -Processes $pd
                Should -Invoke -CommandName Register-ScheduledTask -ModuleName PSAppDeployToolkit -Times 1 -Exactly
            }

            It 'Produces no output in the admin path with -WhatIf' {
                $pd = [PSADT.ProcessManagement.ProcessDefinition]::new('notepad.exe')
                $result = Block-ADTAppExecution -Processes $pd -WhatIf
                $result | Should -BeNullOrEmpty
            }
        }

        Context 'Admin path - IFEO Debugger value per process' {
            BeforeAll {
                Mock -ModuleName PSAppDeployToolkit Get-ADTEnvironmentTable {
                    return [PSCustomObject]@{
                        IsAdmin                              = $true
                        ProcessNTAccount                     = 'TEST\admin'
                        appDeployToolkitName                 = 'PSAppDeployToolkit'
                        InvalidScheduledTaskNameCharsRegExPattern = [regex]::new('[^\w\-]')
                    }
                }
                Mock -ModuleName PSAppDeployToolkit Get-ScheduledTask { return $null }
            }

            It 'Sets the IFEO Debugger registry value for process [<ProcessName>]' -ForEach @(
                @{ ProcessName = 'notepad.exe' }
                @{ ProcessName = 'calc.exe' }
            ) {
                Set-ItResult -Skipped -Because "Block-ADTAppExecution writes IFEO Debugger values via [Microsoft.Win32.Registry]::SetValue() — a .NET static method that cannot be intercepted by Pester. The write requires HKLM elevation and has no in-module mock seam. ProcessName: $ProcessName."
            }
        }

        Context 'Admin path - Add-ADTModuleCallback' {
            BeforeAll {
                Mock -ModuleName PSAppDeployToolkit Get-ADTEnvironmentTable {
                    return [PSCustomObject]@{
                        IsAdmin                              = $true
                        ProcessNTAccount                     = 'TEST\admin'
                        appDeployToolkitName                 = 'PSAppDeployToolkit'
                        InvalidScheduledTaskNameCharsRegExPattern = [regex]::new('[^\w\-]')
                    }
                }
                Mock -ModuleName PSAppDeployToolkit Get-ScheduledTask { return $null }
            }

            It 'Registers an OnFinish callback to Unblock-ADTAppExecution' {
                Set-ItResult -Skipped -Because 'Add-ADTModuleCallback (line 232) is reached only after the IFEO write loop at lines 211-229 completes without error. Those writes use [Microsoft.Win32.Registry]::SetValue() which requires HKLM elevation and cannot be mocked; they throw AccessDenied in headless contexts, causing the outer catch to handle the error before Add-ADTModuleCallback is ever invoked.'
            }
        }

        Context 'Existing blocked-apps task cleanup' {
            BeforeAll {
                Mock -ModuleName PSAppDeployToolkit Get-ADTEnvironmentTable {
                    return [PSCustomObject]@{
                        IsAdmin                              = $true
                        ProcessNTAccount                     = 'TEST\admin'
                        appDeployToolkitName                 = 'PSAppDeployToolkit'
                        InvalidScheduledTaskNameCharsRegExPattern = [regex]::new('[^\w\-]')
                    }
                }
                # Simulate a prior blocked-apps scheduled task already existing.
                Mock -ModuleName PSAppDeployToolkit Get-ScheduledTask {
                    $fakeTask = [Microsoft.Management.Infrastructure.CimInstance]::new('MSFT_ScheduledTask', 'ROOT\Microsoft\Windows\TaskScheduler')
                    $nameProp = [Microsoft.Management.Infrastructure.CimProperty]::Create('TaskName', 'PSAppDeployToolkit_TestApp_BlockedApps', [Microsoft.Management.Infrastructure.CimType]::String, [Microsoft.Management.Infrastructure.CimFlags]::None)
                    $fakeTask.CimInstanceProperties.Add($nameProp)
                    return $fakeTask
                }
            }

            It 'Calls Unblock-ADTAppExecution to clean up the prior blocked-apps state when a previous scheduled task already exists' {
                $pd = [PSADT.ProcessManagement.ProcessDefinition]::new('notepad.exe')
                Block-ADTAppExecution -Processes $pd -WhatIf
                Should -Invoke -CommandName Unblock-ADTAppExecution -ModuleName PSAppDeployToolkit -Times 1 -Exactly
            }
        }
    }
}
