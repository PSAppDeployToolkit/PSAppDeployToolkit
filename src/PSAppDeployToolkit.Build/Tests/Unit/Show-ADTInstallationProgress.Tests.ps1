BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Show-ADTInstallationProgress' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Build a real RunAsActiveUser so the typed -RunAsActiveUser parameter on
        # Test-ADTInstallationProgressOpen binds without a real logon session.
        function script:New-MockRunAsActiveUser
        {
            return [PSADT.Foundation.RunAsActiveUser]::new(
                [System.Security.Principal.NTAccount]::new('TEST\user'),
                [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1-2-3-1001'),
                1,
                $null
            )
        }

        # A fake active session that satisfies the begin{} barrier without Open-ADTSession.
        function script:New-MockSession
        {
            param ([System.Boolean]$Silent = $false)
            $sess = [PSCustomObject]@{
                InstallTitle                = 'Test Application'
                DeploymentType              = [PSAppDeployToolkit.Foundation.DeploymentType]::Install
                DeployMode                  = 'Interactive'
                DeployAppScriptSessionState = $null
            }
            $sess | Add-Member -MemberType ScriptMethod -Name IsSilent -Value ([scriptblock]::Create("return `$$Silent")) -PassThru
        }
    }

    Context 'Input Validation' {
        It 'Has a non-mandatory <Name> parameter' -ForEach @(
            @{ Name = 'StatusMessage' }
            @{ Name = 'StatusMessageDetail' }
            @{ Name = 'StatusBarPercentage' }
            @{ Name = 'MessageAlignment' }
            @{ Name = 'WindowLocation' }
        ) {
            (Get-Command Show-ADTInstallationProgress).Parameters[$Name].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Coerces the MessageAlignment enum value [<Value>]' -ForEach @(
            @{ Value = 'Left' }
            @{ Value = 'Center' }
            @{ Value = 'Right' }
        ) {
            { [PSADT.UserInterface.DialogMessageAlignment]$Value } | Should -Not -Throw
        }

        It 'Throws ParameterArgumentTransformationError when MessageAlignment is not a valid enum value' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Show-ADTInstallationProgress'
            }
            { Show-ADTInstallationProgress -MessageAlignment 'NotAnAlignment' } | Should @shouldParams
        }

        It 'Throws ParameterArgumentValidationError when StatusMessage is whitespace' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Show-ADTInstallationProgress'
            }
            { Show-ADTInstallationProgress -StatusMessage ' ' } | Should @shouldParams
        }

        It 'Exposes the WindowTitle alias on the dynamic Title parameter' {
            (Get-Command Show-ADTInstallationProgress).Parameters['Title'].Aliases | Should -Contain 'WindowTitle'
        }
    }

    Context 'Active session, silent mode' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $true }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return (New-MockSession -Silent $true) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Logs a bypass message that mentions the mode' {
            Show-ADTInstallationProgress
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Message -like '*Bypassing*Mode:*'
            }
        }

        It 'Does not forward any operation to the presenter in silent mode' {
            Show-ADTInstallationProgress
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }

    Context 'No active user logged on' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $true }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return (New-MockSession -Silent $false) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return $null }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
        }

        It 'Logs a bypass message that mentions no active user' {
            Show-ADTInstallationProgress
            Should -Invoke -CommandName Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Message -like '*no active user*'
            }
        }

        It 'Does not forward any operation to the presenter when no user is present' {
            Show-ADTInstallationProgress
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }
    }

    Context 'Active user, creating a new progress dialog (Classic)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $true }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return (New-MockSession -Silent $false) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            # Dialog is not yet open -> creation path.
            Mock -ModuleName PSAppDeployToolkit Test-ADTInstallationProgressOpen { return $false }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeProgressOptions' }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Mock -ModuleName PSAppDeployToolkit Add-ADTModuleCallback { }
            # Classic style triggers a start balloon tip; mock it so nothing renders.
            Mock -ModuleName PSAppDeployToolkit Show-ADTBalloonTip { }
            Mock -ModuleName PSAppDeployToolkit Get-ADTConfig {
                return [PSCustomObject]@{
                    Assets = [PSCustomObject]@{ Logo = $null; LogoDark = $null; Banner = $null; TaskbarIcon = $null }
                    UI     = [PSCustomObject]@{ DialogStyle = 'Classic'; FluentAccentColor = $null; DefaultTimeout = 60 }
                }
            }
        }

        It 'Forwards a ShowProgressDialog operation to the presenter' {
            Show-ADTInstallationProgress
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $ShowProgressDialog -eq $true
            }
        }

        It 'Registers an OnFinish callback so the progress dialog is closed at session end' {
            Show-ADTInstallationProgress
            Should -Invoke -CommandName Add-ADTModuleCallback -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                $Hookpoint -eq 'OnFinish'
            }
        }

        It 'Shows the installation-started balloon tip with Classic style' {
            Show-ADTInstallationProgress
            Should -Invoke -CommandName Show-ADTBalloonTip -ModuleName PSAppDeployToolkit -Times 1 -Exactly
        }
    }

    Context 'Active user, updating an already-open progress dialog' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Test-ADTSessionActive { return $true }
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return (New-MockSession -Silent $false) }
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return (New-MockRunAsActiveUser) }
            # Dialog already open -> update path.
            Mock -ModuleName PSAppDeployToolkit Test-ADTInstallationProgressOpen { return $true }
            Mock -ModuleName PSAppDeployToolkit New-ADTDialogOptionsObject { return 'FakeProgressOptions' }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTClientServerOperation { }
            Mock -ModuleName PSAppDeployToolkit Add-ADTModuleCallback { }
        }

        It 'Forwards an UpdateProgressDialog operation carrying the new status message' {
            Show-ADTInstallationProgress -StatusMessage 'Halfway there'
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                ($UpdateProgressDialog -eq $true) -and ($ProgressMessage -eq 'Halfway there')
            }
        }

        It 'Does not create a new dialog or register a callback when updating' {
            Show-ADTInstallationProgress -StatusMessage 'Still going'
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 0 -Exactly -ParameterFilter {
                $ShowProgressDialog -eq $true
            }
            Should -Invoke -CommandName Add-ADTModuleCallback -ModuleName PSAppDeployToolkit -Times 0 -Exactly
        }

        It 'Forwards the StatusBarPercentage when supplied' {
            Show-ADTInstallationProgress -StatusBarPercentage 42
            Should -Invoke -CommandName Invoke-ADTClientServerOperation -ModuleName PSAppDeployToolkit -Times 1 -Exactly -ParameterFilter {
                ($UpdateProgressDialog -eq $true) -and ($ProgressPercentage -eq 42)
            }
        }
    }
}
