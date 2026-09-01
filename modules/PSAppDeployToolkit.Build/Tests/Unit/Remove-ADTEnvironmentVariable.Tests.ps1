BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Remove-ADTEnvironmentVariable' {
    BeforeEach {
        $script:Variable = "ADTTest$([System.Guid]::NewGuid().ToString('N'))"
        [System.Environment]::SetEnvironmentVariable($script:Variable, 'present')
    }

    AfterEach {
        [System.Environment]::SetEnvironmentVariable($script:Variable, $null)
    }

    Context 'Functionality' {
        It 'Removes the variable from this process' {
            Remove-ADTEnvironmentVariable -Variable $script:Variable
            [System.Environment]::GetEnvironmentVariable($script:Variable) | Should -BeNullOrEmpty
        }

        It 'Removes the variable from the nominated target' {
            Remove-ADTEnvironmentVariable -Variable $script:Variable -Target Process
            [System.Environment]::GetEnvironmentVariable($script:Variable) | Should -BeNullOrEmpty
        }

        It 'Takes it off the Env drive as well' {
            Remove-ADTEnvironmentVariable -Variable $script:Variable
            Test-Path -LiteralPath "Env:\$script:Variable" | Should -BeFalse
        }

        It 'Does not object to a variable that was never set' {
            # Deployments clear variables they may or may not have set, so this has to be a no-op.
            { Remove-ADTEnvironmentVariable -Variable "ADTNeverSet$([System.Guid]::NewGuid().ToString('N'))" } | Should -Not -Throw
        }

        It 'Returns nothing' {
            Remove-ADTEnvironmentVariable -Variable $script:Variable | Should -BeNullOrEmpty
        }

        It 'Leaves the variable alone with -WhatIf' {
            Remove-ADTEnvironmentVariable -Variable $script:Variable -WhatIf
            [System.Environment]::GetEnvironmentVariable($script:Variable) | Should -BeExactly 'present'
        }
    }

    Context 'Input Validation' {
        It 'Refuses a blank variable name' {
            { Remove-ADTEnvironmentVariable -Variable '   ' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Remove-ADTEnvironmentVariable'
        }

        It 'Refuses a target it does not know' {
            { Remove-ADTEnvironmentVariable -Variable $script:Variable -Target 'Everywhere' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
