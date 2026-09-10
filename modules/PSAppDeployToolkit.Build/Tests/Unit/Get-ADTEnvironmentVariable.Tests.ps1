BeforeDiscovery {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"

    # The User target is read for whoever is signed in, not for the process, so the two are the same only
    # when the caller has a session of its own. LocalSystem does not: it reads the signed-in user's hive
    # where .NET reads the service profile's, which makes .NET the wrong oracle rather than the answer
    # wrong. Skipped rather than reworked, since checking it properly means reading another user's hive.
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'UserTargetIsTheCallers', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
    $script:UserTargetIsTheCallers = !(Get-ADTCallerSid).IsWellKnown([System.Security.Principal.WellKnownSidType]::LocalSystemSid)
}

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
            @{ Target = 'Machine' }
        ) {
            # Machine reads the registry rather than the process block, so the two can legitimately differ.
            # The oracle is .NET reading the same target.
            Get-ADTEnvironmentVariable -Variable 'PATH' -Target $Target | Should -BeExactly ([System.Environment]::GetEnvironmentVariable('PATH', $Target))
        }

        It 'Reads from the User target' -Skip:(!$script:UserTargetIsTheCallers) {
            # Separated from the two above because .NET is only the right oracle here while the caller is
            # the signed-in user. A deployment runs as LocalSystem and this reads the hive of whoever is
            # signed in, where .NET would read the service profile's.
            Get-ADTEnvironmentVariable -Variable 'PATH' -Target User | Should -BeExactly ([System.Environment]::GetEnvironmentVariable('PATH', [System.EnvironmentVariableTarget]::User))
        }

        It 'Returns nothing for a variable that is not set' {
            Get-ADTEnvironmentVariable -Variable 'ADTNoSuchEnvironmentVariable12345' | Should -BeNullOrEmpty
        }

        It 'Rejects a target that does not exist' {
            { Get-ADTEnvironmentVariable -Variable 'PATH' -Target 'NotATarget' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
