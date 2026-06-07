BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Show-ADTBalloonTip' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        function script:New-MockRunAsActiveUser
        {
            $nt = [System.Security.Principal.NTAccount]::new('TEST\user')
            $sid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1-2-3-1001')
            return [PSADT.Foundation.RunAsActiveUser]::new($nt, $sid, [System.UInt32]1, $null)
        }

        function script:New-MockSession
        {
            param([System.Boolean]$Silent = $false)
            return [PSCustomObject]@{
                InstallName  = 'TestApp'
                InstallTitle = 'Test Application'
                DeployMode   = 'Silent'
            } | Add-Member -MemberType ScriptMethod -Name IsSilent -Value ([System.Management.Automation.ScriptBlock]::Create("return `$$Silent")) -PassThru
        }

        function script:New-MockConfig
        {
            param([System.Boolean]$BalloonNotifications = $true)
            return [PSCustomObject]@{
                UI      = [PSCustomObject]@{ BalloonNotifications = $BalloonNotifications }
                Toolkit = [PSCustomObject]@{ CompanyName = 'Test Co' }
                Assets  = [PSCustomObject]@{ Logo = 'logo.png'; TaskbarIcon = 'taskbar.ico' }
            }
        }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory Text parameter' {
            (Get-Command Show-ADTBalloonTip).Parameters['Text'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should throw ParameterArgumentValidationError when Text is empty or whitespace' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Show-ADTBalloonTip'
            }
            { Show-ADTBalloonTip -Text '' -Title 'T' } | Should @shouldParams
            { Show-ADTBalloonTip -Text " `f`n`r`t`v" -Title 'T' } | Should @shouldParams
        }

        It 'Should accept the valid Icon value [<Icon>]' -ForEach @(
            @{ Icon = 'None' }
            @{ Icon = 'Info' }
            @{ Icon = 'Warning' }
            @{ Icon = 'Error' }
        ) {
            $command = Get-Command Show-ADTBalloonTip
            [System.Enum]::GetNames([PSADT.UserInterface.BalloonTipIcon]) | Should -Contain $Icon
            $command.Parameters['Icon'].ParameterType | Should -Be ([PSADT.UserInterface.BalloonTipIcon])
        }

        It 'Should reject an invalid Icon value' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Show-ADTBalloonTip'
            }
            { Show-ADTBalloonTip -Text 'Hello' -Title 'T' -Icon 'NotAnIcon' } | Should @shouldParams
        }
    }

    Context 'Config bypass' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Initialize-ADTModuleIfUninitialized { New-MockSession }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { New-MockConfig -BalloonNotifications $false }
            Mock -ModuleName PSAppDeployToolkit Test-ADTEspActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Show-ADTNotifyIcon { }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should bypass when balloon notifications are disabled in config' {
            Show-ADTBalloonTip -Text 'Hello' -Title 'T'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 0 -Exactly
        }
    }

    Context 'ESP bypass' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Initialize-ADTModuleIfUninitialized { New-MockSession }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { New-MockConfig }
            Mock -ModuleName PSAppDeployToolkit Test-ADTEspActive { return $true }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Show-ADTNotifyIcon { }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should bypass when an ESP is active' {
            Show-ADTBalloonTip -Text 'Hello' -Title 'T'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 0 -Exactly
        }
    }

    Context 'No active user bypass' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Initialize-ADTModuleIfUninitialized { New-MockSession }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { New-MockConfig }
            Mock -ModuleName PSAppDeployToolkit Test-ADTEspActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
            Mock -ModuleName PSAppDeployToolkit Show-ADTNotifyIcon { }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should bypass when there is no active user' {
            Show-ADTBalloonTip -Text 'Hello' -Title 'T'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 0 -Exactly
        }
    }

    Context 'Display path' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Initialize-ADTModuleIfUninitialized { New-MockSession }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { New-MockConfig }
            Mock -ModuleName PSAppDeployToolkit Test-ADTEspActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Test-ADTNotifyIconOpen { return $false }
            Mock -ModuleName PSAppDeployToolkit Show-ADTNotifyIcon { }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeOptions' }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should set up the notification icon before displaying the balloon tip' {
            Show-ADTBalloonTip -Text 'Hello' -Title 'My Title'
            Should -Invoke -ModuleName PSAppDeployToolkit Show-ADTNotifyIcon -Times 1 -Exactly -ParameterFilter { $ToolTipText -eq 'My Title - Hello' }
        }

        It 'Should forward a ShowBalloonTip operation for the active user' {
            Show-ADTBalloonTip -Text 'Hello' -Title 'My Title'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter { $ShowBalloonTip -and ($null -ne $User) }
        }

        It 'Should pass Text, Title and Icon through to the balloon tip dialog options' {
            Show-ADTBalloonTip -Text 'Hello' -Title 'My Title' -Icon Warning
            Should -Invoke -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject -Times 1 -Exactly -ParameterFilter {
                ($Data.Text -eq 'Hello') -and ($Data.Title -eq 'My Title') -and ($Data.Icon -eq [PSADT.UserInterface.BalloonTipIcon]::Warning)
            }
        }

        It 'Should default the Icon to Info when not specified' {
            Show-ADTBalloonTip -Text 'Hello' -Title 'My Title'
            Should -Invoke -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject -Times 1 -Exactly -ParameterFilter {
                $Data.Icon -eq [PSADT.UserInterface.BalloonTipIcon]::Info
            }
        }
    }

    Context 'Silent session' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Initialize-ADTModuleIfUninitialized { New-MockSession -Silent $true }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { New-MockConfig }
            Mock -ModuleName PSAppDeployToolkit Test-ADTEspActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Test-ADTNotifyIconOpen { return $false }
            Mock -ModuleName PSAppDeployToolkit Show-ADTNotifyIcon { }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeOptions' }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should bypass display in a silent session without -Force' {
            Show-ADTBalloonTip -Text 'Hello' -Title 'My Title'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 0 -Exactly
        }

        It 'Should forcibly display in a silent session when -Force is supplied' {
            Show-ADTBalloonTip -Text 'Hello' -Title 'My Title' -Force
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter { $ShowBalloonTip }
        }
    }
}
