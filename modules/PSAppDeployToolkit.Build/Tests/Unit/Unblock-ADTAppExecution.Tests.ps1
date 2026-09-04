BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
BeforeDiscovery {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"

    # Unblocking removes any Image File Execution Options entry the toolkit put there, which on a machine
    # that has one would be a change to it rather than something to assert against. The tests only run
    # where there is nothing blocked to begin with, so every call is a no-op.
    $script:NothingBlocked = !(Get-ItemProperty -Path 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\*' -Name Debugger -ErrorAction Ignore | & {
            process
            {
                if ($_.Debugger -like '*PSAppDeployToolkit*')
                {
                    return $_
                }
            }
        })
}

Describe 'Unblock-ADTAppExecution' -Skip:(!$script:NothingBlocked) {
    Context 'With nothing blocked' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'UnblockProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Does not object' {
            # Deployments unblock in their finally blocks whether or not they ever blocked anything.
            { Unblock-ADTAppExecution } | Should -Not -Throw
        }

        It 'Returns nothing' {
            Unblock-ADTAppExecution | Should -BeNullOrEmpty
        }

        It 'Leaves the execution options alone' {
            Unblock-ADTAppExecution
            @(Get-ItemProperty -Path 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\*' -Name Debugger -ErrorAction Ignore | & { process { if ($_.Debugger -like '*PSAppDeployToolkit*') { return $_ } } }).Count | Should -Be 0
        }

        It 'Can be called repeatedly' {
            { 1..2 | ForEach-Object { Unblock-ADTAppExecution } } | Should -Not -Throw
        }
    }
}
