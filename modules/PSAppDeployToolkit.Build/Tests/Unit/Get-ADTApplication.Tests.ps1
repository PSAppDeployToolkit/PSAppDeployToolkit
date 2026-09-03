BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Get-ADTApplication' {
    Context 'Functionality' {
        BeforeAll {
            $script:Applications = @(Get-ADTApplication)
            $script:Sample = $script:Applications | & { process { if (![System.String]::IsNullOrWhiteSpace($_.DisplayName)) { return $_ } } } | Select-Object -First 1
        }

        It 'Finds the applications installed on this machine' {
            $script:Applications.Count | Should -BeGreaterThan 0
            $script:Sample | Should -Not -BeNullOrEmpty
        }

        It 'Gives every result a display name and the key it came from' {
            foreach ($application in $script:Applications)
            {
                $application.DisplayName | Should -Not -BeNullOrEmpty
                $application.PSChildName | Should -Not -BeNullOrEmpty
            }
        }


        It 'Matches a name by <NameMatch>' -ForEach @(
            @{ NameMatch = 'Contains' }
            @{ NameMatch = 'Exact' }
            @{ NameMatch = 'Wildcard' }
            @{ NameMatch = 'Regex' }
        ) {
            # The same installed application has to be findable under each mode, with the needle written the
            # way that mode expects.
            $name = $script:Sample.DisplayName
            $needle = switch ($NameMatch)
            {
                'Contains' { $name }
                'Exact' { $name }
                'Wildcard' { "*$name*" }
                'Regex' { [System.Text.RegularExpressions.Regex]::Escape($name) }
            }
            $found = @(Get-ADTApplication -Name $needle -NameMatch $NameMatch)
            $found.DisplayName | Should -Contain $name
        }

        It 'Returns nothing for a name nothing matches' {
            Get-ADTApplication -Name 'ADTNoSuchApplicationIsInstalled12345' | Should -BeNullOrEmpty
        }

        It 'Applies a -FilterScript' {
            $script:WantedName = $script:Sample.DisplayName
            $found = @(Get-ADTApplication -FilterScript { $_.DisplayName -eq $script:WantedName })
            $found.DisplayName | Should -Contain $script:WantedName
        }

        It 'Narrows to <ApplicationType> installers' -ForEach @(
            @{ ApplicationType = 'MSI' }
            @{ ApplicationType = 'EXE' }
        ) {
            # An MSI entry is the one with a product code behind it, which is what decides whether the
            # toolkit uninstalls by code or by running the uninstall string.
            foreach ($application in @(Get-ADTApplication -ApplicationType $ApplicationType))
            {
                if ($ApplicationType -eq 'MSI')
                {
                    $application.WindowsInstaller | Should -BeTrue
                }
                else
                {
                    $application.WindowsInstaller | Should -BeFalse
                }
            }
        }

        It 'Rejects a match mode it does not know' {
            { Get-ADTApplication -Name 'anything' -NameMatch 'NotAMatchMode' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }

    Context 'Searching against entries written for the purpose' {
        # Every test above reads whatever this machine happens to have installed, which can only ever be
        # asserted loosely. These write the entry they then search for, so the answer is known in advance.
        AfterEach {
            Remove-ADTTestApplicationEntries
        }

        It 'Leaves out an entry it reads as a Microsoft update' {
            $name = New-ADTTestApplicationName -Suffix 'Security Update'
            New-ADTTestApplicationEntry -Name $name
            Get-ADTApplication -Name $name -NameMatch Exact | Should -BeNullOrEmpty
            (Get-ADTApplication -Name $name -NameMatch Exact -IncludeUpdatesAndHotfixes).DisplayName | Should -BeExactly $name
        }

        It 'Reads <Suffix> as a Microsoft update' -ForEach @(
            @{ Suffix = 'KB5000001' }
            @{ Suffix = 'Cumulative Update' }
            @{ Suffix = 'Security Update' }
            @{ Suffix = 'Hotfix' }
        ) {
            # The four things the filter looks for, each checked on its own so that one of them silently
            # ceasing to match does not hide behind the others.
            $name = New-ADTTestApplicationName -Suffix $Suffix
            New-ADTTestApplicationEntry -Name $name
            Get-ADTApplication -Name $name -NameMatch Exact | Should -BeNullOrEmpty
        }

        It 'Says how many entries it passed over as updates' -ForEach @(
            @{ Count = 1; Expected = 'Skipped 1 entry while searching' }
            @{ Count = 2; Expected = 'Skipped 2 entries while searching' }
        ) {
            # Reported singular or plural, which is why there are two ways of writing it to get wrong.
            $names = 1..$Count | ForEach-Object { New-ADTTestApplicationName -Suffix 'Hotfix' }
            foreach ($name in $names)
            {
                New-ADTTestApplicationEntry -Name $name
            }
            $null = Get-ADTApplication -Name $names -NameMatch Exact
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { ($Message -join [System.Environment]::NewLine).Contains($Expected) } -Times 1 -Exactly
        }

        It 'Treats an uninstall string of nothing but quotes as absent' {
            # Entries carrying a pair of empty quotes exist, and reporting that as an uninstall string
            # would have a caller try to run it.
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ UninstallString = '""'; QuietUninstallString = '  ' }
            $found = Get-ADTApplication -Name $name -NameMatch Exact
            $found.UninstallString | Should -BeNullOrEmpty
            $found.QuietUninstallString | Should -BeNullOrEmpty
        }

        It 'Hides an entry flagged SystemComponent unless forced' {
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ SystemComponent = 1 }
            Get-ADTApplication -Name $name -NameMatch Exact | Should -BeNullOrEmpty
            (Get-ADTApplication -Name $name -NameMatch Exact -Force).DisplayName | Should -BeExactly $name
        }

        It 'Reports an entry under the user''s own hive as neither 32-bit nor 64-bit' {
            # A per-user install says nothing about which it is, so the answer is nothing rather than a
            # guess. The two machine hives are where that question has an answer.
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name
            (Get-ADTApplication -Name $name -NameMatch Exact).Is64BitApplication | Should -BeNullOrEmpty
        }

        It 'Passes over an entry that does not carry the product code asked for' {
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ WindowsInstaller = 1 }
            Get-ADTApplication -ProductCode ([System.Guid]::NewGuid()) | Should -BeNullOrEmpty
        }

        It 'Falls back to the key''s own timestamp for an entry with no install date' {
            # InstallDate is optional and often malformed, so the key's last write time stands in for it.
            $name = New-ADTTestApplicationName
            New-ADTTestApplicationEntry -Name $name -Values @{ InstallDate = 'not-a-date' }
            (Get-ADTApplication -Name $name -NameMatch Exact).InstallDate | Should -Be ([System.DateTime]::Now.Date)
        }
    }
}
