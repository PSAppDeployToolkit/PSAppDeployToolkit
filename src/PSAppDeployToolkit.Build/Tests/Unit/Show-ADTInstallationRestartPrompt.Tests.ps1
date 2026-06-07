BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Show-ADTInstallationRestartPrompt' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        function script:New-MockRunAsActiveUser
        {
            return [PSADT.Foundation.RunAsActiveUser]::new(
                [System.Security.Principal.NTAccount]::new('TEST\user'),
                [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1-2-3-1001'),
                1,
                $null
            )
        }

        function script:New-MockRestartStringTable
        {
            return [PSCustomObject]@{
                RestartPrompt = [PSCustomObject]@{
                    Title         = 'PSADT Restart Prompt Unique Title'
                    Subtitle      = [PSCustomObject]@{ Install = 'Restart required' }
                    CustomMessage = 'Custom restart message'
                }
            }
        }

        function script:New-MockConfig
        {
            return [PSCustomObject]@{
                Assets = [PSCustomObject]@{ Logo = $null; LogoDark = $null; Banner = $null; TaskbarIcon = $null }
                UI     = [PSCustomObject]@{ DialogStyle = 'Classic'; DefaultTimeout = 60; DefaultExitCode = 60012; FluentAccentColor = $null }
            }
        }
    }

    Context 'Input Validation' {
        It 'Has a non-mandatory <Name> parameter' -ForEach @(
            @{ Name = 'CountdownSeconds' }
            @{ Name = 'CountdownNoHideSeconds' }
            @{ Name = 'SilentCountdownSeconds' }
            @{ Name = 'WindowLocation' }
            @{ Name = 'CustomText' }
            @{ Name = 'NotTopMost' }
            @{ Name = 'AllowMove' }
        ) {
            (Get-Command Show-ADTInstallationRestartPrompt).Parameters[$Name].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Defaults to the Countdown parameter set' {
            (Get-Command Show-ADTInstallationRestartPrompt).DefaultParameterSet | Should -Be 'Countdown'
        }

        It 'Throws a validation error when CountdownSeconds exceeds the 86,400 second ceiling' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.ArgumentException]
                ErrorId       = 'InvalidCountdownSecondsParameterValue,Show-ADTInstallationRestartPrompt'
            }
            { Show-ADTInstallationRestartPrompt -CountdownSeconds 90000 -Title 'T' -Subtitle 'S' } | Should @shouldParams
        }

        It 'Throws ParameterArgumentValidationError when CountdownSeconds is zero (ValidateGreaterThanZero)' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Show-ADTInstallationRestartPrompt'
            }
            { Show-ADTInstallationRestartPrompt -CountdownSeconds 0 -Title 'T' -Subtitle 'S' } | Should @shouldParams
        }
    }

    Context 'An existing restart prompt is already displayed' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTStringTable { return (New-MockRestartStringTable) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { return (New-MockConfig) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            # Simulate a window already titled to match the restart prompt.
            Mock -ModuleName PSAppDeployToolkit Get-Process {
                return [PSCustomObject]@{ MainWindowTitle = 'PSADT Restart Prompt Unique Title' }
            }
        }

        It 'Logs a cancellation warning and does not forward to the presenter' {
            Show-ADTInstallationRestartPrompt -Title 'T' -Subtitle 'S'
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -ParameterFilter {
                $Message -like '*existing restart prompt was detected*'
            }
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }

    Context 'Active session, no active user logged on' {
        # With a session present, the no-user branch sets RestartOnExitCountdown rather than
        # invoking the [PSADT.AccountManagement.AccountUtilities]::CallerRunAsActiveUser .NET
        # static (which is unmockable and returns no usable user in a headless context).
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $true }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession {
                $sess = [PSCustomObject]@{
                    InstallTitle                = 'Test Application'
                    DeploymentType              = [PSAppDeployToolkit.Foundation.DeploymentType]::Install
                    DeployMode                  = 'Interactive'
                    DeployAppScriptSessionState = $null
                }
                $sess | Add-Member -MemberType ScriptMethod -Name IsSilent -Value { return $false } -PassThru
            }
            Mock -ModuleName PSAppDeployToolkit Get-ADTStringTable { return (New-MockRestartStringTable) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { return (New-MockConfig) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
            Mock -ModuleName PSAppDeployToolkit Get-Process { }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeRestartOptions' }
        }

        It 'Logs that it is triggering a silent restart because no user is logged on' {
            Show-ADTInstallationRestartPrompt
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -ParameterFilter {
                $Message -like '*no active user*'
            }
        }

        It 'Does not forward any operation to the presenter when no user is present (session sets countdown instead)' {
            Show-ADTInstallationRestartPrompt
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }

    Context 'Active user, synchronous restart dialog (sessionless)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTStringTable { return (New-MockRestartStringTable) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { return (New-MockConfig) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            Mock -ModuleName PSAppDeployToolkit Get-Process { }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeRestartOptions' }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Forwards a ShowModalDialog/RestartDialog operation synchronously (no -NoWait) when sessionless' {
            Show-ADTInstallationRestartPrompt -Title 'T' -Subtitle 'S'
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                ($ShowModalDialog -eq $true) -and ($DialogType -eq 'RestartDialog') -and ($NoWait -ne $true)
            }
        }

        It 'Builds restart dialog options via New-ADTDialogOptionsObject' {
            Show-ADTInstallationRestartPrompt -Title 'T' -Subtitle 'S'
            Should -Invoke -CommandName New-ADTDialogOptionsObject -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Type -eq [PSADT.UserInterface.DialogOptions.RestartDialogOptions]
            }
        }

        It 'Produces no output' {
            $result = Show-ADTInstallationRestartPrompt -Title 'T' -Subtitle 'S'
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Active session, interactive restart dialog forwards asynchronously' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $true }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession {
                $sess = [PSCustomObject]@{
                    InstallTitle                = 'Test Application'
                    DeploymentType              = [PSAppDeployToolkit.Foundation.DeploymentType]::Install
                    DeployMode                  = 'Interactive'
                    DeployAppScriptSessionState = $null
                }
                $sess | Add-Member -MemberType ScriptMethod -Name IsSilent -Value { return $false } -PassThru
            }
            Mock -ModuleName PSAppDeployToolkit Get-ADTStringTable { return (New-MockRestartStringTable) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { return (New-MockConfig) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            Mock -ModuleName PSAppDeployToolkit Get-Process { }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeRestartOptions' }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Forwards the restart dialog with -NoWait when a session is active' {
            Show-ADTInstallationRestartPrompt
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                ($ShowModalDialog -eq $true) -and ($DialogType -eq 'RestartDialog') -and ($NoWait -eq $true)
            }
        }
    }

    Context 'Active session, silent mode' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $true }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession {
                $sess = [PSCustomObject]@{
                    InstallTitle                = 'Test Application'
                    DeploymentType              = [PSAppDeployToolkit.Foundation.DeploymentType]::Install
                    DeployMode                  = 'Silent'
                    DeployAppScriptSessionState = $null
                }
                $sess | Add-Member -MemberType ScriptMethod -Name IsSilent -Value { return $true } -PassThru
            }
            Mock -ModuleName PSAppDeployToolkit Get-ADTStringTable { return (New-MockRestartStringTable) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { return (New-MockConfig) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            Mock -ModuleName PSAppDeployToolkit Get-Process { }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Logs that the restart is skipped when silent and -SilentRestart is not specified' {
            Show-ADTInstallationRestartPrompt
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -ParameterFilter {
                $Message -like '*Skipping restart*'
            }
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }

        It 'Logs that the restart is triggered silently when -SilentRestart is specified' {
            Show-ADTInstallationRestartPrompt -SilentRestart -SilentCountdownSeconds 5
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -ParameterFilter {
                $Message -like '*Triggering restart silently*'
            }
        }
    }
}
