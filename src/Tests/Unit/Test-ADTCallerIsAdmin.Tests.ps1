BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTCallerIsAdmin' {
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
