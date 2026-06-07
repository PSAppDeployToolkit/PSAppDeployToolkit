BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Show-ADTInstallationPrompt' {
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
    }

    Context 'Input Validation' {
        It 'Has a mandatory Message parameter' {
            (Get-Command Show-ADTInstallationPrompt).Parameters['Message'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Accepts the MessageAlignment enum value [<Value>]' -ForEach @(
            @{ Value = 'Left' }
            @{ Value = 'Center' }
            @{ Value = 'Right' }
        ) {
            { [PSADT.UserInterface.DialogMessageAlignment]$Value } | Should -Not -Throw
        }

        It 'Throws ParameterArgumentValidationError when Message is whitespace' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Show-ADTInstallationPrompt'
            }
            { Show-ADTInstallationPrompt -Message ' ' -Title 'T' -Subtitle 'S' -ButtonLeftText OK } | Should @shouldParams
        }

        It 'Throws ParameterArgumentTransformationError when MessageAlignment is not a valid enum value' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Show-ADTInstallationPrompt'
            }
            { Show-ADTInstallationPrompt -Message 'Hi' -MessageAlignment 'Sideways' -Title 'T' -Subtitle 'S' -ButtonLeftText OK } | Should @shouldParams
        }
    }

    Context 'Begin-block argument guards (sessionless)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Throws when no button is specified' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.ArgumentException]
                ErrorId       = 'MandatoryParameterMissing,Show-ADTInstallationPrompt'
            }
            { Show-ADTInstallationPrompt -Message 'Proceed?' -Title 'T' -Subtitle 'S' } | Should @shouldParams
        }

        It 'Throws SecureInputWithoutActiveSession when -SecureInput is used without an active session' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.InvalidOperationException]
                ErrorId       = 'SecureInputWithoutActiveSession,Show-ADTInstallationPrompt'
            }
            { Show-ADTInstallationPrompt -RequestInput -SecureInput -Message 'Password?' -ButtonRightText Submit -Title 'T' -Subtitle 'S' } | Should @shouldParams
        }

        It 'Throws DefaultIndexOutOfBoundsError when DefaultIndex is not less than the ListItems count' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.ArgumentOutOfRangeException]
                ErrorId       = 'DefaultIndexOutOfBoundsError,Show-ADTInstallationPrompt'
            }
            { Show-ADTInstallationPrompt -Message 'Pick' -ListItems @('A', 'B') -DefaultIndex 5 -ButtonRightText OK -Title 'T' -Subtitle 'S' } | Should @shouldParams
        }
    }

    Context 'No active user logged on (sessionless)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeOptions' }
        }

        It 'Logs a bypass message that mentions no active user' {
            Show-ADTInstallationPrompt -Message 'Proceed?' -ButtonLeftText OK -Title 'T' -Subtitle 'S'
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -ParameterFilter {
                $Message -like '*no active user*'
            }
        }

        It 'Does not forward any operation to the presenter when no user is present' {
            Show-ADTInstallationPrompt -Message 'Proceed?' -ButtonLeftText OK -Title 'T' -Subtitle 'S'
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }

    Context 'Active user, synchronous custom prompt (sessionless)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeOptions' }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { return 'OK' }
            Mock -ModuleName PSAppDeployToolkit Close-ADTInstallationProgress { }
        }

        It 'Forwards a ShowModalDialog/CustomDialog operation to the presenter' {
            Show-ADTInstallationPrompt -Message 'Proceed?' -ButtonLeftText OK -Title 'T' -Subtitle 'S'
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                ($ShowModalDialog -eq $true) -and ($DialogType -eq 'CustomDialog')
            }
        }

        It 'Builds custom dialog options via New-ADTDialogOptionsObject' {
            Show-ADTInstallationPrompt -Message 'Proceed?' -ButtonLeftText OK -Title 'T' -Subtitle 'S'
            Should -Invoke -CommandName New-ADTDialogOptionsObject -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Type -eq [PSADT.UserInterface.DialogOptions.CustomDialogOptions]
            }
        }

        It 'Returns the result produced by the presenter' {
            $result = Show-ADTInstallationPrompt -Message 'Proceed?' -ButtonLeftText OK -Title 'T' -Subtitle 'S'
            $result | Should -Be 'OK'
        }

        It 'Does not pass -NoWait to the presenter for a synchronous prompt' {
            Show-ADTInstallationPrompt -Message 'Proceed?' -ButtonLeftText OK -Title 'T' -Subtitle 'S'
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $NoWait -ne $true
            }
        }
    }

    Context 'Active user, input prompt routes through InputDialogOptions (sessionless)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeOptions' }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { return 'OK' }
            Mock -ModuleName PSAppDeployToolkit Close-ADTInstallationProgress { }
        }

        It 'Builds input dialog options when -RequestInput is specified' {
            Show-ADTInstallationPrompt -RequestInput -Message 'Why?' -ButtonRightText Submit -Title 'T' -Subtitle 'S'
            Should -Invoke -CommandName New-ADTDialogOptionsObject -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Type -eq [PSADT.UserInterface.DialogOptions.InputDialogOptions]
            }
        }

        It 'Forwards an InputDialog operation to the presenter' {
            Show-ADTInstallationPrompt -RequestInput -Message 'Why?' -ButtonRightText Submit -Title 'T' -Subtitle 'S'
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                ($ShowModalDialog -eq $true) -and ($DialogType -eq 'InputDialog')
            }
        }
    }

    Context 'Active user, asynchronous prompt (-NoWait, sessionless)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeOptions' }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Forwards the -NoWait switch to the presenter' {
            Show-ADTInstallationPrompt -Message 'Proceed?' -ButtonLeftText OK -NoWait -Title 'T' -Subtitle 'S'
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                ($ShowModalDialog -eq $true) -and ($NoWait -eq $true)
            }
        }

        It 'Produces no output when -NoWait is specified' {
            $result = Show-ADTInstallationPrompt -Message 'Proceed?' -ButtonLeftText OK -NoWait -Title 'T' -Subtitle 'S'
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Active session, non-interactive bypass' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $true }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession {
                $sess = [PSCustomObject]@{
                    InstallTitle                = 'Test Application'
                    DeploymentType              = [PSAppDeployToolkit.Foundation.DeploymentType]::Install
                    DeployMode                  = 'Silent'
                    DeployAppScriptSessionState = $null
                }
                $sess | Add-Member -MemberType ScriptMethod -Name IsNonInteractive -Value { return $true } -PassThru
            }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig {
                return [PSCustomObject]@{
                    Assets = [PSCustomObject]@{ Logo = $null; LogoDark = $null; Banner = $null; TaskbarIcon = $null }
                    UI     = [PSCustomObject]@{ DialogStyle = 'Classic'; DefaultTimeout = 60; DefaultExitCode = 60012; FluentAccentColor = $null }
                }
            }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            # Return a non-null sentinel so the do/until loop's $result.Equals(...) check is safe.
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { return 'OK' }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeOptions' }
            Mock -ModuleName PSAppDeployToolkit Close-ADTInstallationProgress { }
        }

        It 'Logs a bypass message and does not call the presenter when the session is non-interactive' {
            Show-ADTInstallationPrompt -Message 'Proceed?' -ButtonLeftText OK
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -ParameterFilter {
                $Message -like '*Bypassing*Mode:*'
            }
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }

        It 'Does not bypass when -Force is supplied even in non-interactive mode' {
            Show-ADTInstallationPrompt -Message 'Proceed?' -ButtonLeftText OK -Force
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $ShowModalDialog -eq $true
            }
        }
    }
}
