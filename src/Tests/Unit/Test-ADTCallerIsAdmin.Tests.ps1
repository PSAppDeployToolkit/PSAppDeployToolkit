BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTCallerIsAdmin' {
    Context 'Functionality' {
        It 'Should return $true when the caller is in the Administrator role' {
            $callerIsAdmin = [System.Security.Principal.WindowsPrincipal]::new([System.Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)

            if ($callerIsAdmin)
            {
                Test-ADTCallerIsAdmin | Should -BeTrue
            }
            else
            {
                Test-ADTCallerIsAdmin | Should -BeFalse
            }
        }
    }
}
