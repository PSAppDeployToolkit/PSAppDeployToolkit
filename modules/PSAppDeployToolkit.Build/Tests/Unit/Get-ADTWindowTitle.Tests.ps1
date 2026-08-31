BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Get-ADTWindowTitle' {
    Context 'Functionality' {
        BeforeAll {
            $script:Windows = @(Get-ADTWindowTitle)
            $script:Sample = $script:Windows | & { process { if (![System.String]::IsNullOrWhiteSpace($_.WindowTitle)) { return $_ } } } | Select-Object -First 1
        }

        It 'Returns the windows open in the user session' {
            $script:Windows.Count | Should -BeGreaterThan 0
            $script:Windows[0] | Should -BeOfType ([PSADT.WindowManagement.WindowInfo])
        }

        It 'Reports the owning process for each window' {
            foreach ($window in $script:Windows)
            {
                $window.ParentProcessId | Should -BeGreaterThan 0
                $window.ParentProcess | Should -Not -BeNullOrEmpty
                $window.WindowHandle | Should -Not -Be ([System.IntPtr]::Zero)
            }
        }

        It 'Matches the title as a regular expression, not a wildcard' {
            # Documented as regex matching, and worth pinning: the same intent written as a wildcard is an
            # invalid pattern rather than a broader match.
            $needle = [System.Text.RegularExpressions.Regex]::Escape($script:Sample.WindowTitle)
            @(Get-ADTWindowTitle -WindowTitle $needle).WindowTitle | Should -Contain $script:Sample.WindowTitle
            @(Get-ADTWindowTitle -WindowTitle ".*$needle.*").WindowTitle | Should -Contain $script:Sample.WindowTitle
        }

        It 'Anchors like a regular expression' {
            $needle = [System.Text.RegularExpressions.Regex]::Escape($script:Sample.WindowTitle)
            @(Get-ADTWindowTitle -WindowTitle "^$needle$").WindowTitle | Should -Contain $script:Sample.WindowTitle
        }

        It 'Filters by <Parameter>' -ForEach @(
            @{ Parameter = 'ParentProcess' }
            @{ Parameter = 'ParentProcessId' }
            @{ Parameter = 'WindowHandle' }
        ) {
            $splat = @{ $Parameter = $script:Sample.$Parameter }
            $found = @(Get-ADTWindowTitle @splat)
            $found.Count | Should -BeGreaterThan 0
            $found.WindowHandle | Should -Contain $script:Sample.WindowHandle
        }

        It 'Returns nothing when the title matches no window' {
            Get-ADTWindowTitle -WindowTitle 'ADTNoSuchWindowTitleExists12345' | Should -BeNullOrEmpty
        }

        It 'Returns nothing for a process id that owns no window' {
            Get-ADTWindowTitle -ParentProcessId 4 | Should -BeNullOrEmpty
        }
    }
}
