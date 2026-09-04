BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }

    # The function takes the caller's $PSCmdlet so it can rethrow against them, which a bare It block has
    # no way to supply.
    function Invoke-Probe
    {
        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $false)]
            [System.Collections.Hashtable]$Splat = @{}
        )

        return Initialize-ADTModuleIfUninitialized -Cmdlet $PSCmdlet @Splat
    }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Initialize-ADTModuleIfUninitialized' {
    Context 'When the module has not been initialised' {
        It 'Initialises it' {
            Test-ADTModuleInitialized | Should -BeFalse
            $null = Invoke-Probe -InformationAction SilentlyContinue
            Test-ADTModuleInitialized | Should -BeTrue
        }

        It 'Returns nothing of its own' {
            Invoke-Probe | Should -BeNullOrEmpty
        }
    }

    Context 'When the module is already initialised' {
        BeforeAll {
            Initialize-ADTTestModule -Path $TestDrive
            InModuleScope PSAppDeployToolkit { $ADT.Config.Toolkit.LogPath = 'C:\SentinelValue' }
        }

        It 'Leaves the existing config alone' {
            # The point of the function: a second caller in the same process must not reload config and
            # discard what the first one set up.
            $null = Invoke-Probe
            (Get-ADTConfig).Toolkit.LogPath | Should -BeExactly 'C:\SentinelValue'
        }
    }

    Context 'With an open session' {
        BeforeAll {
            Import-ADTModuleUnderTest -Force
            Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
            Initialize-ADTTestModule -Path $TestDrive
            $script:Opened = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'IfUninitProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Does not try to reinitialise' {
            # Initialize-ADTModule throws outright with a session open, so returning quietly is what lets a
            # nested caller run inside a deployment.
            { Invoke-Probe } | Should -Not -Throw
        }

        It 'Hands back the running session with -PassThruActiveSession' {
            (Invoke-Probe -Splat @{ PassThruActiveSession = $true }).InstallName | Should -BeExactly $script:Opened.InstallName
        }

        It 'Returns nothing without -PassThruActiveSession' {
            Invoke-Probe | Should -BeNullOrEmpty
        }
    }
}
