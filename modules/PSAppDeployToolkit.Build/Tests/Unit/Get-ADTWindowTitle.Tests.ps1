BeforeDiscovery {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Every assertion below enumerates the windows of the logged-on session, and the context stands one of its
    # own up to assert against. A run with nobody logged on, or one from session zero, has neither available.
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'CallerOwnsItsSession', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
    $script:CallerOwnsItsSession = (Get-ADTLoggedOnUser | & { process { if ($_.IsCurrentSession) { return $_ } } } | Select-Object -First 1 -ExpandProperty SID) -eq (Get-ADTCallerSid)
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Get-ADTWindowTitle' {
    Context 'Functionality' -Skip:(!$script:CallerOwnsItsSession) {
        BeforeAll {
            # Stand up a window this test owns. Sampling whichever window happened to sort first meant asserting
            # against a title that any application is free to change while the run is still in progress.
            $script:SampleTitle = "ADTWindowTitleTest_$([System.Guid]::NewGuid().ToString('N'))"
            $script:SampleProcess = Start-Process -FilePath (Get-ADTPowerShellProcessPath) -PassThru -ArgumentList @(
                '-NoProfile'
                '-NonInteractive'
                '-Command'
                "Add-Type -AssemblyName System.Windows.Forms; `$form = [System.Windows.Forms.Form]::new(); `$form.Text = '$script:SampleTitle'; [System.Void]`$form.ShowDialog()"
            )

            # The child has to load WinForms and show the form before the window can be enumerated.
            $deadline = [System.DateTime]::UtcNow.AddSeconds(30)
            while ([System.DateTime]::UtcNow -lt $deadline)
            {
                if (($script:Sample = @(Get-ADTWindowTitle -WindowTitle $script:SampleTitle) | Select-Object -First 1))
                {
                    break
                }
                Start-Sleep -Milliseconds 250
            }
            if (!$script:Sample)
            {
                throw "The test window [$script:SampleTitle] did not appear within 30 seconds."
            }
            $script:Windows = @(Get-ADTWindowTitle)
        }

        AfterAll {
            if ($script:SampleProcess -and !$script:SampleProcess.HasExited)
            {
                $script:SampleProcess.Kill()
                $null = $script:SampleProcess.WaitForExit(5000)
            }
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
