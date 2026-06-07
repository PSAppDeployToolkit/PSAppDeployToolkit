BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
# NOTE: tests in this file can be individually slow (~2 s each) because the production
# silent-close path calls Thread.Sleep(2000). That sleep is deep inside the module and is
# not mocked here; it is by design so the UI has time to close gracefully.
Describe 'Show-ADTInstallationWelcome' {
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

        function script:New-MockConfig
        {
            return [PSCustomObject]@{
                Assets = [PSCustomObject]@{ Logo = $null; LogoDark = $null; Banner = $null; TaskbarIcon = $null }
                UI     = [PSCustomObject]@{
                    DialogStyle                 = 'Classic'
                    DefaultTimeout              = 60
                    DefaultExitCode             = 60012
                    DeferExitCode               = 60012
                    DefaultPromptPersistInterval = 60
                    PromptToSaveTimeout         = 60
                    FluentAccentColor           = $null
                }
            }
        }

        function script:New-MockStringTable
        {
            return [PSCustomObject]@{
                CloseAppsPrompt = [PSCustomObject]@{
                    Fluent        = [PSCustomObject]@{ Subtitle = [PSCustomObject]@{ Install = 'Close apps' } }
                    CustomMessage = 'Custom close-apps message'
                }
            }
        }

        # A duck-typed running-app stand-in. Get-ADTRunningProcessesUserCanClose returns this
        # list verbatim (the caller is not LocalSystem), so only .Process/.Description are read.
        function script:New-MockRunningApp
        {
            # .Process must be a real System.Diagnostics.Process so it survives binding to the
            # typed -InputObject parameter of the (mocked) Stop-Process. Stop-Process is mocked,
            # so the current process is never actually stopped.
            $proc = [System.Diagnostics.Process]::GetCurrentProcess()
            return [PSCustomObject]@{ Process = $proc; Description = 'Microsoft Word'; SID = $null }
        }

        # A fake active session covering the members the function touches.
        function script:New-MockSession
        {
            param ([System.Boolean]$Silent = $false)
            $sess = [PSCustomObject]@{
                InstallTitle                = 'Test Application'
                DeploymentType              = [PSAppDeployToolkit.Foundation.DeploymentType]::Install
                DeployMode                  = 'Interactive'
                DeployAppScriptSessionState = $null
            }
            $sess | Add-Member -MemberType ScriptMethod -Name IsSilent -Value ([scriptblock]::Create("return `$$Silent"))
            $sess | Add-Member -MemberType ScriptMethod -Name IsNonInteractive -Value ([scriptblock]::Create("return `$$Silent")) -PassThru
        }
    }

    Context 'Input Validation' {
        It 'Defaults to the no-modifying-options interactive parameter set' {
            (Get-Command Show-ADTInstallationWelcome).DefaultParameterSet | Should -Be 'Interactive, with no modifying options.'
        }

        It 'Has a non-mandatory <Name> parameter' -ForEach @(
            @{ Name = 'BlockExecution' }
            @{ Name = 'PromptToSave' }
            @{ Name = 'PersistPrompt' }
            @{ Name = 'MinimizeWindows' }
            @{ Name = 'PassThru' }
            @{ Name = 'CustomText' }
        ) {
            (Get-Command Show-ADTInstallationWelcome).Parameters[$Name].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Throws a transformation error when CloseProcessesCountdown is not an unsigned integer' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Show-ADTInstallationWelcome'
            }
            { Show-ADTInstallationWelcome -CloseProcesses winword -CloseProcessesCountdown 'soon' -Title 'T' } | Should @shouldParams
        }

        It 'Throws when CloseProcesses contains duplicate entries (ValidateUnique)' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'pd', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $pd = [PSADT.ProcessManagement.ProcessDefinition]::new('winword')
            { Show-ADTInstallationWelcome -CloseProcesses @($pd, $pd) -Title 'T' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }

    Context 'Silent mode forces processes closed without prompting (sessionless)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { return (New-MockConfig) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTStringTable { return (New-MockStringTable) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTRunningProcesses { return (New-MockRunningApp) }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeOptions' }
            Mock -ModuleName PSAppDeployToolkit Stop-Process { }
        }

        It 'Force-closes running applications via Stop-Process when -Silent is specified' {
            Show-ADTInstallationWelcome -CloseProcesses winword -Silent -Title 'T'
            Should -Invoke -CommandName Stop-Process -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }

        It 'Does not present a modal close-apps dialog in silent mode' {
            Show-ADTInstallationWelcome -CloseProcesses winword -Silent -Title 'T'
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 0 -Exactly -ParameterFilter {
                $ShowModalDialog -eq $true
            }
        }
    }

    Context 'No active user falls back to silent (sessionless)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { return (New-MockConfig) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTStringTable { return (New-MockStringTable) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
            Mock -ModuleName PSAppDeployToolkit Get-ADTRunningProcesses { return $null }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeOptions' }
            Mock -ModuleName PSAppDeployToolkit Stop-Process { }
        }

        It 'Logs that it is running silently because there is no active user' {
            Show-ADTInstallationWelcome -CloseProcesses winword -Title 'T'
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -ParameterFilter {
                $Message -like '*no active user*'
            }
        }

        It 'Does not present a modal close-apps dialog when there is no user' {
            Show-ADTInstallationWelcome -CloseProcesses winword -Title 'T'
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 0 -Exactly -ParameterFilter {
                $ShowModalDialog -eq $true
            }
        }
    }

    Context 'Interactive welcome forwards to the presenter and honours a Defer result (sessionless)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { return (New-MockConfig) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTStringTable { return (New-MockStringTable) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            # Running apps present so the no-defer branch presents the prompt.
            Mock -ModuleName PSAppDeployToolkit Get-ADTRunningProcesses { return (New-MockRunningApp) }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeOptions' }
            Mock -ModuleName PSAppDeployToolkit Close-ADTClientServerProcess { }
            # InitCloseAppsDialog returns truthy; ShowModalDialog returns a Defer result.
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation {
                if ($ShowModalDialog) { return [PSADT.UserInterface.DialogResults.CloseAppsDialogResult]::Defer }
                return $true
            }
        }

        It 'Initialises the close-apps dialog through the presenter' {
            Show-ADTInstallationWelcome -CloseProcesses winword -PassThru -Title 'T' | Out-Null
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -ParameterFilter {
                $InitCloseAppsDialog -eq $true
            }
        }

        It 'Presents the modal close-apps dialog through the presenter' {
            Show-ADTInstallationWelcome -CloseProcesses winword -PassThru -Title 'T' | Out-Null
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -ParameterFilter {
                ($ShowModalDialog -eq $true) -and ($DialogType -eq 'CloseAppsDialog')
            }
        }

        It 'Returns the Defer result to the caller when -PassThru is specified' {
            $result = Show-ADTInstallationWelcome -CloseProcesses winword -PassThru -Title 'T'
            $result | Should -Be ([PSADT.UserInterface.DialogResults.CloseAppsDialogResult]::Defer)
        }
    }

    Context 'BlockExecution forwarding (active session, silent)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $true }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return (New-MockSession -Silent $true) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig { return (New-MockConfig) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTStringTable { return (New-MockStringTable) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            # No running apps so the silent path skips Stop-Process and proceeds to BlockExecution.
            Mock -ModuleName PSAppDeployToolkit Get-ADTRunningProcesses { return $null }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeOptions' }
            Mock -ModuleName PSAppDeployToolkit Stop-Process { }
            Mock -ModuleName PSAppDeployToolkit Block-ADTAppExecution { }
        }

        It 'Calls Block-ADTAppExecution with the close-processes when -BlockExecution is specified and a session is active' {
            Show-ADTInstallationWelcome -CloseProcesses winword -Silent -BlockExecution
            Should -Invoke -CommandName Block-ADTAppExecution -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $null -ne $Processes
            }
        }

        It 'Does not call Block-ADTAppExecution when -BlockExecution is not specified' {
            Show-ADTInstallationWelcome -CloseProcesses winword -Silent
            Should -Invoke -CommandName Block-ADTAppExecution -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }
}
