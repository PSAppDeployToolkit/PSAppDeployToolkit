BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive

    function Open-Probe
    {
        param
        (
            [Parameter(Mandatory = $false)]
            [System.Collections.Hashtable]$Splat = @{}
        )

        $defaults = @{
            SessionState = $ExecutionContext.SessionState
            AppName = 'OpenProbe'
            DeployMode = 'Silent'
            PassThru = $true
            InformationAction = 'SilentlyContinue'
        }
        foreach ($pair in $Splat.GetEnumerator())
        {
            $defaults[$pair.Key] = $pair.Value
        }
        return Open-ADTSession @defaults
    }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Open-ADTSession' {
    Context 'Functionality' {
        AfterEach {
            # Whatever a test opened, unwind it so the next one starts from nothing.
            while (Test-ADTSessionActive)
            {
                Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            }
        }

        It 'Returns the session with -PassThru' {
            Open-Probe | Should -BeOfType ([PSAppDeployToolkit.Foundation.DeploymentSession])
        }

        It 'Returns nothing without -PassThru' {
            Open-Probe -Splat @{ PassThru = $false } | Should -BeNullOrEmpty
            Test-ADTSessionActive | Should -BeTrue
        }

        It 'Builds the install name from the vendor, name and version' {
            # The install name is what the log file and the deferral registry key are named after, so its
            # composition is part of the contract rather than cosmetic.
            (Open-Probe -Splat @{ AppVendor = 'Vendor'; AppName = 'Product'; AppVersion = '2.5' }).InstallName | Should -BeExactly 'Vendor_Product_2.5'
        }

        It 'Writes its log where the config points' {
            $null = Open-Probe -Splat @{ AppName = 'LogProbe' }
            @(Get-ChildItem -LiteralPath (Get-ADTConfig).Toolkit.LogPath -Recurse -File -Filter '*LogProbe*').Count | Should -BeGreaterThan 0
        }

        It 'Records the deployment type it was told' {
            (Open-Probe -Splat @{ DeploymentType = 'Uninstall' }).DeploymentType | Should -Be ([PSAppDeployToolkit.Foundation.DeploymentType]::Uninstall)
        }

        It 'Defaults the deployment type to an install' {
            (Open-Probe).DeploymentType | Should -Be ([PSAppDeployToolkit.Foundation.DeploymentType]::Install)
        }

        It 'Runs in the deploy mode it was given' {
            (Open-Probe -Splat @{ DeployMode = 'Silent' }).DeployMode | Should -Be ([PSAppDeployToolkit.Foundation.DeployMode]::Silent)
        }

        It 'Stacks a nested session on top rather than replacing it' {
            $outer = Open-Probe -Splat @{ AppName = 'Outer' }
            $inner = Open-Probe -Splat @{ AppName = 'Inner' }
            InModuleScope PSAppDeployToolkit { $ADT.Sessions.Count } | Should -Be 2
            (Get-ADTSession).InstallName | Should -BeExactly $inner.InstallName
            $outer.InstallName | Should -Not -BeExactly $inner.InstallName
        }

        It 'Hands the session back in the execution phase' {
            # Initialization covers the banner it writes while opening; by the time the caller has it, the
            # deployment proper has started, and Close-ADTSession moves it on to Finalization.
            (Open-Probe).InstallPhase | Should -BeExactly 'Execution'
        }
    }

    Context 'Input Validation' {
        It 'Rejects a script directory that does not exist' {
            { Open-Probe -Splat @{ ScriptDirectory = "$TestDrive\NoSuchDirectory" } } | Should -Throw
        }

        It 'Rejects an empty application name' {
            { Open-Probe -Splat @{ AppName = '' } } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Rejects a deploy mode it does not know' {
            { Open-Probe -Splat @{ DeployMode = 'NotAMode' } } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
