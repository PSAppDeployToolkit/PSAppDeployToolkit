BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Get-ADTEnvironmentVariable' {
    Context 'Functionality' {
        It 'Reads a variable from the current process' {
            Get-ADTEnvironmentVariable -Variable 'PATH' | Should -BeExactly $env:PATH
        }

        It 'Returns a string' {
            Get-ADTEnvironmentVariable -Variable 'SystemRoot' | Should -BeOfType ([System.String])
        }

        It 'Matches the variable name without regard to case' {
            Get-ADTEnvironmentVariable -Variable 'systemroot' | Should -BeExactly $env:SystemRoot
        }

        It 'Reads from the <Target> target' -ForEach @(
            @{ Target = 'Process' }
            @{ Target = 'User' }
            @{ Target = 'Machine' }
        ) {
            # Machine and User read the registry rather than the process block, so the three can legitimately
            # differ. The oracle is .NET reading the same target.
            Get-ADTEnvironmentVariable -Variable 'PATH' -Target $Target | Should -BeExactly ([System.Environment]::GetEnvironmentVariable('PATH', $Target))
        }

        It 'Returns nothing for a variable that is not set' {
            Get-ADTEnvironmentVariable -Variable 'ADTNoSuchEnvironmentVariable12345' | Should -BeNullOrEmpty
        }

        It 'Rejects a target that does not exist' {
            { Get-ADTEnvironmentVariable -Variable 'PATH' -Target 'NotATarget' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
