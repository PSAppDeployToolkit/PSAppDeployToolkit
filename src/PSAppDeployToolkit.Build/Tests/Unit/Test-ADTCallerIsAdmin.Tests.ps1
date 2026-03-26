BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTCallerIsAdmin' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
        # Test-ADTCallerIsAdmin directly accesses [PSADT.AccountManagement.AccountUtilities]::CallerIsAdmin.
        # PowerShell resolves this type literal at compile time, requiring admin rights.
        $script:IsAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    }

    BeforeEach {
        if (!$script:IsAdmin) { Set-ItResult -Skipped -Because 'Requires admin rights (AccountUtilities static constructor triggered at compile time)'; return }
    }

    Context 'Functionality' {
        It 'Should return $true when the caller is in the Administrator role' {
            $callerIdentity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
            try
            {
                if ([System.Security.Principal.WindowsPrincipal]::new($callerIdentity).IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator))
                {
                    Test-ADTCallerIsAdmin | Should -BeTrue
                }
                else
                {
                    Test-ADTCallerIsAdmin | Should -BeFalse
                }
            }
            finally
            {
                $callerIdentity.Dispose()
            }
        }
    }
}
