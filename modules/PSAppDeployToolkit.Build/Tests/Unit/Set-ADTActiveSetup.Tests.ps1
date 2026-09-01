BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Set-ADTActiveSetup' {
    BeforeAll {
        # Every registry path the module builds is rewritten to sit under TestRegistry, so the machine's
        # own Active Setup registrations are neither read nor written.
        Mock -ModuleName PSAppDeployToolkit Convert-ADTRegistryPath {
            $output = & (Get-Command -Source PSAppDeployToolkit -CommandType Function -Name 'Convert-ADTRegistryPath') @PesterBoundParameters
            return $output -replace '^Microsoft\.PowerShell\.Core\\Registry::', "Microsoft.PowerShell.Core\Registry::$((Get-PSDrive -Name TestRegistry).Root)\"
        }
    }

    BeforeEach {
        $script:Key = "ADTTestOnly$([System.Guid]::NewGuid().ToString('N'))"
        $script:MachineKey = "TestRegistry:\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\$script:Key"
        $script:StubPath = "$TestDrive\Stub$([System.Guid]::NewGuid().ToString('N')).vbs"
        Set-Content -LiteralPath $script:StubPath -Value "' a stub"
    }

    Context 'Registering a stub' {
        # -NoExecute throughout, so the stub is registered for future logons rather than being run now.
        It 'Creates the machine registration' {
            Set-ADTActiveSetup -StubExePath $script:StubPath -Key $script:Key -Description 'Test only' -NoExecute
            Test-Path -LiteralPath $script:MachineKey | Should -BeTrue
        }

        It 'Records what Active Setup needs to run it' {
            # Windows compares Version against the per-user copy and runs StubPath when they differ, so a
            # registration missing either of them never fires.
            Set-ADTActiveSetup -StubExePath $script:StubPath -Key $script:Key -Description 'Test only' -NoExecute
            $values = (Get-Item -LiteralPath $script:MachineKey).GetValueNames()
            $values | Should -Contain 'StubPath'
            $values | Should -Contain 'Version'
            $values | Should -Contain 'IsInstalled'
        }

        It 'Marks the registration as installed' {
            Set-ADTActiveSetup -StubExePath $script:StubPath -Key $script:Key -Description 'Test only' -NoExecute
            (Get-Item -LiteralPath $script:MachineKey).GetValue('IsInstalled') | Should -Be 1
        }

        It 'Marks it as not installed when disabled' {
            # Disabling is how a deployment stops a registration from firing without removing it.
            Set-ADTActiveSetup -StubExePath $script:StubPath -Key $script:Key -Description 'Test only' -NoExecute -DisableActiveSetup
            (Get-Item -LiteralPath $script:MachineKey).GetValue('IsInstalled') | Should -Be 0
        }

        It 'Takes the version it was given' {
            Set-ADTActiveSetup -StubExePath $script:StubPath -Key $script:Key -Description 'Test only' -NoExecute -Version '1,2,3,4'
            (Get-Item -LiteralPath $script:MachineKey).GetValue('Version') | Should -BeExactly '1,2,3,4'
        }

        It 'Registers nothing with -WhatIf' {
            Set-ADTActiveSetup -StubExePath $script:StubPath -Key $script:Key -Description 'Test only' -NoExecute -WhatIf
            Test-Path -LiteralPath $script:MachineKey | Should -BeFalse
        }
    }

    Context 'Purging a registration' {
        It 'Removes the machine registration' {
            Set-ADTActiveSetup -StubExePath $script:StubPath -Key $script:Key -Description 'Test only' -NoExecute
            Set-ADTActiveSetup -Key $script:Key -PurgeActiveSetupKey
            Test-Path -LiteralPath $script:MachineKey | Should -BeFalse
        }

        It 'Does not object when there is nothing registered' {
            # Deployments purge a registration a previous version may never have created.
            { Set-ADTActiveSetup -Key $script:Key -PurgeActiveSetupKey } | Should -Not -Throw
        }

        It 'Removes nothing with -WhatIf' {
            Set-ADTActiveSetup -StubExePath $script:StubPath -Key $script:Key -Description 'Test only' -NoExecute
            Set-ADTActiveSetup -Key $script:Key -PurgeActiveSetupKey -WhatIf
            Test-Path -LiteralPath $script:MachineKey | Should -BeTrue
        }
    }

    Context 'Input Validation' {
        It 'Requires a key to register under' {
            { Set-ADTActiveSetup -StubExePath $script:StubPath -NoExecute } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Requires a stub to register' {
            { Set-ADTActiveSetup -Key $script:Key } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a stub that is not there' {
            { Set-ADTActiveSetup -StubExePath "$TestDrive\NeverExisted.vbs" -Key $script:Key -NoExecute } | Should -Throw
        }

        It 'Refuses an execution policy it does not know' {
            { Set-ADTActiveSetup -StubExePath $script:StubPath -Key $script:Key -NoExecute -ExecutionPolicy 'Whatever' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a purge alongside a registration' {
            # Purging and registering are opposite requests, and one of them has to win.
            { Set-ADTActiveSetup -StubExePath $script:StubPath -Key $script:Key -PurgeActiveSetupKey } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
