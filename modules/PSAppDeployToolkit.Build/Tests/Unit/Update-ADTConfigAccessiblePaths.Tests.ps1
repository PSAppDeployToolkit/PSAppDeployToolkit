BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    function New-TestConfig
    {
        return @{
            Toolkit = @{
                PathsBasedOnSystemContext = $false
                TempPath = 'C:\Owned\Temp'
                TempPathNoAdminRights = 'C:\User\Temp'
                RegPath = 'HKLM:\SOFTWARE'
                RegPathNoAdminRights = 'HKCU:\SOFTWARE'
                LogPath = 'C:\Owned\Logs'
                LogPathNoAdminRights = 'C:\User\Logs'
                CachePath = 'C:\Owned\Cache'
                CachePathNoAdminRights = 'C:\User\Cache'
            }
            MSI = @{
                LogPath = 'C:\Owned\MsiLogs'
                LogPathNoAdminRights = 'C:\User\MsiLogs'
            }
        }
    }
}

Describe 'Update-ADTConfigAccessiblePaths' {
    Context 'When the caller does not own the configured paths' {
        It 'Swaps every path over to its no-admin-rights counterpart' {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Config = New-TestConfig } {
                Mock Test-ADTCallerOwnsConfiguredPaths { $false }
                Update-ADTConfigAccessiblePaths -Config $Config
                $Config.Toolkit.TempPath | Should -BeExactly "C:\User\Temp\$($ExecutionContext.SessionState.Module.Name)"
                $Config.Toolkit.RegPath | Should -BeExactly 'HKCU:\SOFTWARE'
                $Config.Toolkit.LogPath | Should -BeExactly 'C:\User\Logs'
                $Config.Toolkit.CachePath | Should -BeExactly 'C:\User\Cache'
                $Config.MSI.LogPath | Should -BeExactly 'C:\User\MsiLogs'
            }
        }

        It 'Keeps the configured <Section>.<Name> when its counterpart is <Description>' -ForEach @(
            @{ Section = 'Toolkit'; Name = 'LogPath'; Description = 'unset'; Value = $null; Expected = 'C:\Owned\Logs' }
            @{ Section = 'Toolkit'; Name = 'LogPath'; Description = 'only whitespace'; Value = '   '; Expected = 'C:\Owned\Logs' }
            @{ Section = 'MSI'; Name = 'LogPath'; Description = 'unset'; Value = $null; Expected = 'C:\Owned\MsiLogs' }
        ) {
            # A deployment that clears a no-admin-rights path is saying it has none to offer, which has to
            # leave the configured path standing rather than blanking it out.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Config = New-TestConfig; Section = $Section; Name = $Name; Value = $Value; Expected = $Expected } {
                Mock Test-ADTCallerOwnsConfiguredPaths { $false }
                $Config.$Section."$($Name)NoAdminRights" = $Value
                Update-ADTConfigAccessiblePaths -Config $Config
                $Config.$Section.$Name | Should -BeExactly $Expected
            }
        }
    }

    Context 'When the caller owns the configured paths' {
        It 'Leaves every path as configured' {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Config = New-TestConfig } {
                Mock Test-ADTCallerOwnsConfiguredPaths { $true }
                Update-ADTConfigAccessiblePaths -Config $Config
                $Config.Toolkit.TempPath | Should -BeExactly "C:\Owned\Temp\$($ExecutionContext.SessionState.Module.Name)"
                $Config.Toolkit.RegPath | Should -BeExactly 'HKLM:\SOFTWARE'
                $Config.Toolkit.LogPath | Should -BeExactly 'C:\Owned\Logs'
                $Config.Toolkit.CachePath | Should -BeExactly 'C:\Owned\Cache'
                $Config.MSI.LogPath | Should -BeExactly 'C:\Owned\MsiLogs'
            }
        }
    }

    Context 'The temporary path' {
        It 'Gains a folder named for the toolkit, whichever path was chosen' {
            # The append lives here rather than in either caller, so both arrive at the same folder, and it
            # is why the two cases above expect the temp path to carry a suffix the others do not. A bare
            # temp root is shared with everything else on the machine.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Config = New-TestConfig } {
                Mock Test-ADTCallerOwnsConfiguredPaths { $false }
                Update-ADTConfigAccessiblePaths -Config $Config
                [System.IO.Path]::GetFileName($Config.Toolkit.TempPath) | Should -BeExactly $ExecutionContext.SessionState.Module.Name
            }
        }
    }

    Context 'Behaviour' {
        It 'Updates the supplied config in place and returns nothing' {
            # Both callers hand it the config they are building and carry on with it, so anything returned
            # would land in their output instead.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Config = New-TestConfig } {
                Mock Test-ADTCallerOwnsConfiguredPaths { $false }
                Update-ADTConfigAccessiblePaths -Config $Config | Should -BeNullOrEmpty
                $Config.Toolkit.LogPath | Should -BeExactly 'C:\User\Logs'
            }
        }

        It 'Asks the ownership question against the config it was given' {
            # Not the seated one, which is the whole reason this is callable before the module is initialized.
            # Asserted as an equivalence rather than for one case, so it holds whatever account the tests run under.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Config = New-TestConfig } {
                $owned = Test-ADTCallerOwnsConfiguredPaths -Config $Config
                Update-ADTConfigAccessiblePaths -Config $Config
                $Config.Toolkit.LogPath | Should -BeExactly ('C:\User\Logs', 'C:\Owned\Logs')[$owned]
            }
        }
    }

    Context 'Input Validation' {
        It 'Requires a config' {
            Test-ADTMandatoryParameter -Command (InModuleScope PSAppDeployToolkit { Get-Command Update-ADTConfigAccessiblePaths }) -Parameter Config | Should -BeTrue
        }

        It 'Refuses an empty config' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Update-ADTConfigAccessiblePaths -Config @{} } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Update-ADTConfigAccessiblePaths'
            }
        }
    }
}
