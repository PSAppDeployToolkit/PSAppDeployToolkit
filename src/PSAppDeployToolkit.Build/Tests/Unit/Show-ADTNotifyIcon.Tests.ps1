BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Show-ADTNotifyIcon' {
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
                InstallName = 'TestApp'
                DeployMode  = 'Silent'
            } | Add-Member -MemberType ScriptMethod -Name IsSilent -Value ([System.Management.Automation.ScriptBlock]::Create("return `$$Silent")) -PassThru
        }

        function script:New-MockConfig
        {
            return [PSCustomObject]@{
                Toolkit = [PSCustomObject]@{ CompanyName = 'Test Co' }
                Assets  = [PSCustomObject]@{ Logo = 'logo.png'; TaskbarIcon = 'taskbar.ico' }
            }
        }
    }

    Context 'Parameters and metadata' {
        It 'Should expose the Force switch parameter' {
            (Get-Command Show-ADTNotifyIcon).Parameters.ContainsKey('Force') | Should -BeTrue
        }

        It 'Should expose the ToolTipText dynamic parameter' {
            $command = Get-Command Show-ADTNotifyIcon
            $command.Parameters.ContainsKey('ToolTipText') | Should -BeTrue
        }
    }

    Context 'No active user (bypass)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Initialize-ADTModuleIfUninitialized { New-MockSession }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { New-MockConfig }
            Mock -ModuleName PSAppDeployToolkit Test-ADTEspActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should not throw when there is no active user' {
            { Show-ADTNotifyIcon -ToolTipText 'Hello' } | Should -Not -Throw
        }

        It 'Should not forward an operation when there is no active user' {
            Show-ADTNotifyIcon -ToolTipText 'Hello'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 0 -Exactly
        }
    }

    Context 'ESP active (bypass)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Initialize-ADTModuleIfUninitialized { New-MockSession }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { New-MockConfig }
            Mock -ModuleName PSAppDeployToolkit Test-ADTEspActive { return $true }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Should not forward an operation when an ESP is active' {
            Show-ADTNotifyIcon -ToolTipText 'Hello'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 0 -Exactly
        }
    }

    Context 'Active user, no icon open yet (display)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Initialize-ADTModuleIfUninitialized { New-MockSession }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { New-MockConfig }
            Mock -ModuleName PSAppDeployToolkit Test-ADTEspActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Test-ADTNotifyIconOpen { return $false }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeOptions' }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Mock -ModuleName PSAppDeployToolkit Add-ADTModuleCallback { }
        }

        It 'Should forward a ShowNotifyIcon operation for the active user' {
            Show-ADTNotifyIcon -ToolTipText 'Hello'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter { $ShowNotifyIcon -and ($null -ne $User) }
        }

        It 'Should not forward an UpdateNotifyIcon operation when first displaying' {
            Show-ADTNotifyIcon -ToolTipText 'Hello'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter { -not $UpdateNotifyIcon }
        }

        It 'Should register an OnFinish callback to close the notification icon' {
            Show-ADTNotifyIcon -ToolTipText 'Hello'
            Should -Invoke -ModuleName PSAppDeployToolkit Add-ADTModuleCallback -Times 1 -Exactly -ParameterFilter { $Hookpoint -eq 'OnFinish' }
        }

        It 'Should pass the supplied tooltip text through to the dialog options' {
            Show-ADTNotifyIcon -ToolTipText 'My Tooltip'
            Should -Invoke -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject -Times 1 -Exactly -ParameterFilter { $Data.MessageText -eq 'My Tooltip' }
        }
    }

    Context 'Active user, icon already open (update)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Initialize-ADTModuleIfUninitialized { New-MockSession }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { New-MockConfig }
            Mock -ModuleName PSAppDeployToolkit Test-ADTEspActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Test-ADTNotifyIconOpen { return $true }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Mock -ModuleName PSAppDeployToolkit Add-ADTModuleCallback { }
        }

        It 'Should forward an UpdateNotifyIcon operation when an icon is already open' {
            Show-ADTNotifyIcon -ToolTipText 'Hello'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter { $UpdateNotifyIcon -and ($MessageText -eq 'Hello') }
        }

        It 'Should not register a callback when only updating an existing icon' {
            Show-ADTNotifyIcon -ToolTipText 'Hello'
            Should -Invoke -ModuleName PSAppDeployToolkit Add-ADTModuleCallback -Times 0 -Exactly
        }
    }

    Context 'Silent session bypass' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Initialize-ADTModuleIfUninitialized { New-MockSession -Silent $true }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { New-MockConfig }
            Mock -ModuleName PSAppDeployToolkit Test-ADTEspActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { New-MockRunAsActiveUser }
            Mock -ModuleName PSAppDeployToolkit Test-ADTNotifyIconOpen { return $false }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeOptions' }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Mock -ModuleName PSAppDeployToolkit Add-ADTModuleCallback { }
        }

        It 'Should bypass display in a silent session without -Force' {
            Show-ADTNotifyIcon -ToolTipText 'Hello'
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 0 -Exactly
        }

        It 'Should forcibly display in a silent session when -Force is supplied' {
            Show-ADTNotifyIcon -ToolTipText 'Hello' -Force
            Should -Invoke -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation -Times 1 -Exactly -ParameterFilter { $ShowNotifyIcon }
        }
    }
}
