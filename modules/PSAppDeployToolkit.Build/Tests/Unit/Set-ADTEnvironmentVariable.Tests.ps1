BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Set-ADTEnvironmentVariable' {
    BeforeEach {
        $script:Variable = "ADTTest$([System.Guid]::NewGuid().ToString('N'))"
    }

    AfterEach {
        [System.Environment]::SetEnvironmentVariable($script:Variable, $null)
    }

    Context 'This process' {
        It 'Sets the variable' {
            Set-ADTEnvironmentVariable -Variable $script:Variable -Value 'first'
            [System.Environment]::GetEnvironmentVariable($script:Variable) | Should -BeExactly 'first'
        }

        It 'Replaces an existing value' {
            [System.Environment]::SetEnvironmentVariable($script:Variable, 'first')
            Set-ADTEnvironmentVariable -Variable $script:Variable -Value 'second'
            [System.Environment]::GetEnvironmentVariable($script:Variable) | Should -BeExactly 'second'
        }

        It 'Leaves the variable alone with -WhatIf' {
            Set-ADTEnvironmentVariable -Variable $script:Variable -Value 'first' -WhatIf
            [System.Environment]::GetEnvironmentVariable($script:Variable) | Should -BeNullOrEmpty
        }
    }

    Context 'A nominated target' {
        It 'Sets the variable for this process' {
            Set-ADTEnvironmentVariable -Variable $script:Variable -Value 'first' -Target Process
            [System.Environment]::GetEnvironmentVariable($script:Variable) | Should -BeExactly 'first'
        }

        It 'Shows up on the Env drive' {
            # Deployment scripts read these back as $env:Something, so the change has to be visible to
            # the provider and not just to the .NET API.
            Set-ADTEnvironmentVariable -Variable $script:Variable -Value 'first' -Target Process
            (Get-Item -LiteralPath "Env:\$script:Variable").Value | Should -BeExactly 'first'
        }

        It 'Appends to an existing list' {
            [System.Environment]::SetEnvironmentVariable($script:Variable, 'first')
            Set-ADTEnvironmentVariable -Variable $script:Variable -Value 'second' -Target Process -Append
            [System.Environment]::GetEnvironmentVariable($script:Variable) | Should -BeExactly "first$([System.IO.Path]::PathSeparator)second"
        }

        It 'Leaves a list alone when what was appended is already in it' {
            [System.Environment]::SetEnvironmentVariable($script:Variable, 'first')
            Set-ADTEnvironmentVariable -Variable $script:Variable -Value 'first' -Target Process -Append
            [System.Environment]::GetEnvironmentVariable($script:Variable) | Should -BeExactly 'first'
        }

        It 'Removes one entry from a list' {
            [System.Environment]::SetEnvironmentVariable($script:Variable, "first$([System.IO.Path]::PathSeparator)second")
            Set-ADTEnvironmentVariable -Variable $script:Variable -Value 'first' -Target Process -Remove
            [System.Environment]::GetEnvironmentVariable($script:Variable) | Should -BeExactly 'second'
        }

        It 'Leaves the variable alone with -WhatIf' {
            Set-ADTEnvironmentVariable -Variable $script:Variable -Value 'first' -Target Process -WhatIf
            [System.Environment]::GetEnvironmentVariable($script:Variable) | Should -BeNullOrEmpty
        }
    }

    Context 'A target that is not this process' {
        # Appending and removing read the variable's current value before writing it back, and that read
        # has to come from the scope being written to. Every test above nominates the process target,
        # where reading the process scope is right by definition, so the distinction only shows up
        # against another one. The user scope is used because it needs no elevation and is taken away
        # again immediately; the machine scope is never written to here.
        AfterEach {
            [System.Environment]::SetEnvironmentVariable($script:Variable, $null, [System.EnvironmentVariableTarget]::User)
        }

        It 'Appends to what the target holds, not to what this process holds' {
            [System.Environment]::SetEnvironmentVariable($script:Variable, 'target', [System.EnvironmentVariableTarget]::User)
            [System.Environment]::SetEnvironmentVariable($script:Variable, 'process')
            Set-ADTEnvironmentVariable -Variable $script:Variable -Value 'added' -Target User -Append
            [System.Environment]::GetEnvironmentVariable($script:Variable, [System.EnvironmentVariableTarget]::User) | Should -BeExactly "target$([System.IO.Path]::PathSeparator)added"
        }

        It 'Removes from what the target holds, not from what this process holds' {
            [System.Environment]::SetEnvironmentVariable($script:Variable, "keep$([System.IO.Path]::PathSeparator)drop", [System.EnvironmentVariableTarget]::User)
            [System.Environment]::SetEnvironmentVariable($script:Variable, "process$([System.IO.Path]::PathSeparator)drop")
            Set-ADTEnvironmentVariable -Variable $script:Variable -Value 'drop' -Target User -Remove
            [System.Environment]::GetEnvironmentVariable($script:Variable, [System.EnvironmentVariableTarget]::User) | Should -BeExactly 'keep'
        }

        It 'Appends nothing to a target holding nothing, whatever this process holds' {
            # The case that loses data on a machine PATH: what this process holds is the machine and user
            # values already merged, so appending to that writes every one of those entries into the hive.
            [System.Environment]::SetEnvironmentVariable($script:Variable, 'process')
            Set-ADTEnvironmentVariable -Variable $script:Variable -Value 'added' -Target User -Append
            [System.Environment]::GetEnvironmentVariable($script:Variable, [System.EnvironmentVariableTarget]::User) | Should -BeExactly 'added'
        }
    }

    Context 'Input Validation' {
        It 'Refuses a blank variable name' {
            { Set-ADTEnvironmentVariable -Variable '   ' -Value 'first' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Set-ADTEnvironmentVariable'
        }

        It 'Refuses a blank value' {
            # Clearing a variable is what Remove-ADTEnvironmentVariable is for.
            { Set-ADTEnvironmentVariable -Variable $script:Variable -Value '   ' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Set-ADTEnvironmentVariable'
        }

        It 'Refuses appending and removing at once' {
            { Set-ADTEnvironmentVariable -Variable $script:Variable -Value 'first' -Append -Remove } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a target it does not know' {
            { Set-ADTEnvironmentVariable -Variable $script:Variable -Value 'first' -Target 'Everywhere' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
