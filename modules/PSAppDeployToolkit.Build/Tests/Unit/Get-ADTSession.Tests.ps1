BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Get-ADTSession' {
    Context 'With no session open' {
        It 'Tells the caller to open one first' {
            # Almost every toolkit function reaches for the session, so this is the error a script author
            # sees when they forget Open-ADTSession, and it names what to do.
            { Get-ADTSession } | Should -Throw -ErrorId 'ADTSessionBufferEmpty,Get-ADTSession'
        }
    }

    Context 'With a session open' {
        BeforeAll {
            $script:Opened = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'GetSessionProbe' -AppVendor 'Pester' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Returns the deployment session' {
            Get-ADTSession | Should -BeOfType ([PSAppDeployToolkit.Foundation.DeploymentSession])
        }

        It 'Returns the very session that was opened' {
            (Get-ADTSession).InstallName | Should -BeExactly $script:Opened.InstallName
        }

        It 'Returns the innermost session when several are nested' {
            # Sessions stack, and a nested deployment has to see its own rather than the one it started
            # inside.
            $nested = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'NestedProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
            try
            {
                (Get-ADTSession).InstallName | Should -BeExactly $nested.InstallName
                (Get-ADTSession).InstallName | Should -Not -BeExactly $script:Opened.InstallName
            }
            finally
            {
                Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            }
        }

        It 'Returns the outer session again once the nested one closes' {
            (Get-ADTSession).InstallName | Should -BeExactly $script:Opened.InstallName
        }
    }
}
