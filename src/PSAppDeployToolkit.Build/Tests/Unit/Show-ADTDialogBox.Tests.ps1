BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Show-ADTDialogBox' {
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
        It 'Has a mandatory Text parameter' {
            (Get-Command Show-ADTDialogBox).Parameters['Text'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Has a non-mandatory <Name> parameter' -ForEach @(
            @{ Name = 'Buttons' }
            @{ Name = 'DefaultButton' }
            @{ Name = 'Icon' }
            @{ Name = 'NoWait' }
            @{ Name = 'ExitOnTimeout' }
            @{ Name = 'NotTopMost' }
            @{ Name = 'Force' }
        ) {
            (Get-Command Show-ADTDialogBox).Parameters[$Name].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Accepts the Buttons enum value [<Value>]' -ForEach @(
            @{ Value = 'Ok' }
            @{ Value = 'OkCancel' }
            @{ Value = 'AbortRetryIgnore' }
            @{ Value = 'YesNoCancel' }
            @{ Value = 'YesNo' }
            @{ Value = 'RetryCancel' }
            @{ Value = 'CancelTryContinue' }
        ) {
            { [PSADT.UserInterface.DialogBoxButtons]$Value } | Should -Not -Throw
        }

        It 'Accepts the DefaultButton enum value [<Value>]' -ForEach @(
            @{ Value = 'First' }
            @{ Value = 'Second' }
            @{ Value = 'Third' }
        ) {
            { [PSADT.UserInterface.DialogBoxDefaultButton]$Value } | Should -Not -Throw
        }

        It 'Accepts the Icon enum value [<Value>]' -ForEach @(
            @{ Value = 'Stop' }
            @{ Value = 'Question' }
            @{ Value = 'Exclamation' }
            @{ Value = 'Information' }
        ) {
            { [PSADT.UserInterface.DialogBoxIcon]$Value } | Should -Not -Throw
        }

        It 'Throws ParameterArgumentTransformationError when Buttons is not a valid enum value' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Show-ADTDialogBox'
            }
            { Show-ADTDialogBox -Text 'hi' -Buttons 'NotAButton' } | Should @shouldParams
        }

        It 'Throws ParameterArgumentValidationError when Text is whitespace' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Show-ADTDialogBox'
            }
            { Show-ADTDialogBox -Text ' ' } | Should @shouldParams
        }

    }

    Context 'No active user logged on' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Logs a bypass message that mentions no active user' {
            Show-ADTDialogBox -Text 'Proceed?' -Title 'Notice'
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Message -like '*no active user*'
            }
        }

        It 'Does not forward any operation to the presenter when no user is present' {
            Show-ADTDialogBox -Text 'Proceed?' -Title 'Notice'
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }

    Context 'Active user, synchronous dialog (sessionless)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { return [PSADT.UserInterface.DialogResults.DialogBoxResult]::OK }
        }

        It 'Forwards a ShowModalDialog/DialogBox operation to the presenter' {
            Show-ADTDialogBox -Text 'Proceed?' -Title 'Notice'
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                ($ShowModalDialog -eq $true) -and ($DialogType -eq 'DialogBox')
            }
        }

        It 'Returns the DialogBoxResult produced by the presenter' {
            $result = Show-ADTDialogBox -Text 'Proceed?' -Title 'Notice'
            $result | Should -Be ([PSADT.UserInterface.DialogResults.DialogBoxResult]::OK)
        }

        It 'Does not pass the -NoWait switch to the presenter for a synchronous call' {
            Show-ADTDialogBox -Text 'Proceed?' -Title 'Notice'
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $NoWait -ne $true
            }
        }
    }

    Context 'Active user, asynchronous dialog (-NoWait)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Forwards the -NoWait switch to the presenter and does not wait for a result' {
            Show-ADTDialogBox -Text 'Proceed?' -Title 'Notice' -NoWait
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                ($ShowModalDialog -eq $true) -and ($NoWait -eq $true)
            }
        }

        It 'Produces no output when -NoWait is specified' {
            $result = Show-ADTDialogBox -Text 'Proceed?' -Title 'Notice' -NoWait
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Timeout handling' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { return [PSADT.UserInterface.DialogResults.DialogBoxResult]::Timeout }
            Mock -ModuleName PSAppDeployToolkit Close-ADTSession { }
        }

        It 'Logs a timeout message but continues when -ExitOnTimeout is not specified' {
            Show-ADTDialogBox -Text 'Proceed?' -Title 'Notice'
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -ParameterFilter {
                $Message -like '*not specified. Continue*'
            }
            Should -Invoke -CommandName Close-ADTSession -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }

        It 'Closes the session on timeout when -ExitOnTimeout is specified and a session is active' {
            # With an active session, the dynamicparam resolves the session via Get-ADTSession.
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $true }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession {
                $sess = [PSCustomObject]@{ InstallTitle = 'Test Application'; DeployMode = 'Interactive' }
                $sess | Add-Member -MemberType ScriptMethod -Name IsNonInteractive -Value { return $false } -PassThru
            }
            # With a session deemed active, Initialize-ADTModuleIfUninitialized short-circuits real
            # init, so the dynamicparam's Get-ADTConfig must be supplied by a mock.
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig {
                return [PSCustomObject]@{ UI = [PSCustomObject]@{ DialogStyle = 'Classic'; DefaultTimeout = 60; DefaultExitCode = 60012; FluentAccentColor = $null } }
            }
            Show-ADTDialogBox -Text 'Proceed?' -Force -ExitOnTimeout
            Should -Invoke -CommandName Close-ADTSession -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }
    }
}
