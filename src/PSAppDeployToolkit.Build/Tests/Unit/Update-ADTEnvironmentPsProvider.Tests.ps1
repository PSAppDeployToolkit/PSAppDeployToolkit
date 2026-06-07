BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Update-ADTEnvironmentPsProvider' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Parameters' {
        It 'Should take no parameters beyond the common ones' {
            $common = [System.Management.Automation.PSCmdlet]::CommonParameters
            (Get-Command Update-ADTEnvironmentPsProvider).Parameters.Keys.Where({ $common -notcontains $_ }) | Should -BeNullOrEmpty
        }
    }

    Context 'Behaviour' {
        It 'Should not throw when refreshing the session environment' {
            { Update-ADTEnvironmentPsProvider } | Should -Not -Throw
        }

        It 'Should leave a non-empty PATH environment variable in the session' {
            Update-ADTEnvironmentPsProvider
            $env:PATH | Should -Not -BeNullOrEmpty
        }

        It 'Should base the user environment on the SID of the active user when present' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return [PSCustomObject]@{ SID = ([System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value) } }
            { Update-ADTEnvironmentPsProvider } | Should -Not -Throw
            Should -Invoke -ModuleName PSAppDeployToolkit Get-ADTClientServerUser -Times 1 -Exactly -ParameterFilter { $AllowSystemFallback -eq $true }
        }
    }
}
