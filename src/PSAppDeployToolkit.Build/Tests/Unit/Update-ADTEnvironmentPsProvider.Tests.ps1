BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Update-ADTEnvironmentPsProvider' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Update-ADTEnvironmentPsProvider references [PSADT.AccountManagement.AccountUtilities]::CallerSid
        # in its function body. PowerShell resolves all type literals at compile time (first invocation),
        # so this fails without admin rights even though the else-branch may not execute at runtime.
        $script:IsAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
        Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { }
    }

    BeforeEach {
        if (!$script:IsAdmin) { Set-ItResult -Skipped -Because 'Requires admin rights (AccountUtilities static constructor triggered at compile time)'; return }
    }

    Context 'Basic invocation' {
        It 'Does not throw' {
            { Update-ADTEnvironmentPsProvider } | Should -Not -Throw
        }

        It 'Returns no output' {
            $result = Update-ADTEnvironmentPsProvider
            $result | Should -BeNull
        }

        It 'Can be called multiple times consecutively without error' {
            { Update-ADTEnvironmentPsProvider; Update-ADTEnvironmentPsProvider } | Should -Not -Throw
        }
    }

    Context 'Logging' {
        It 'Calls Write-ADTLogEntry at least once per invocation' {
            Update-ADTEnvironmentPsProvider
            Should -Invoke Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Scope It
        }
    }
}
