BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTPresentationSettingsEnabledUsers' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Parameters' {
        It 'Should take no parameters beyond the common ones' {
            $common = [System.Management.Automation.PSCmdlet]::CommonParameters
            (Get-Command Get-ADTPresentationSettingsEnabledUsers).Parameters.Keys.Where({ $common -notcontains $_ }) | Should -BeNullOrEmpty
        }

        It 'Should declare an OutputType of PSADT.AccountManagement.UserProfileInfo' {
            (Get-Command Get-ADTPresentationSettingsEnabledUsers).OutputType.Type | Should -Contain ([PSADT.AccountManagement.UserProfileInfo])
        }
    }

    Context 'Behaviour' {
        It 'Should not throw' {
            { Get-ADTPresentationSettingsEnabledUsers } | Should -Not -Throw
        }

        It 'Should emit a deprecation warning' {
            Get-ADTPresentationSettingsEnabledUsers
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { ($Severity -eq 'Warning') -and ($Message -match 'deprecated') }
        }

        It 'Should return nothing when no profiles are returned' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTUserProfiles { return $null }
            Get-ADTPresentationSettingsEnabledUsers | Should -BeNullOrEmpty
        }

        It 'Should return the users reported as being in presentation mode' {
            $nt = [System.Security.Principal.NTAccount]::new('TEST\user')
            $sid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1-2-3-1001')
            $dir = [System.IO.DirectoryInfo]::new('C:\Users\test')
            $fakeProfile = [PSADT.AccountManagement.UserProfileInfo]::new($nt, $sid, $dir, $null, $null, $null, $null, $null, $null, $null, $null, $null)
            Mock -ModuleName PSAppDeployToolkit Get-ADTUserProfiles { return $fakeProfile }
            Mock -ModuleName PSAppDeployToolkit Invoke-ADTAllUsersRegistryAction { return $fakeProfile }
            $result = Get-ADTPresentationSettingsEnabledUsers
            $result.NTAccount.Value | Should -Be 'TEST\user'
        }
    }
}
