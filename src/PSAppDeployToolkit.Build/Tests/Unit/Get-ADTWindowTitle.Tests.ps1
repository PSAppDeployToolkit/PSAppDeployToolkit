BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTWindowTitle' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should not throw when called with no parameters' {
            # Skipped when no interactive user is present (function bypasses cleanly).
            { Get-ADTWindowTitle } | Should -Not -Throw
        }

        It 'Should not throw when -WindowTitle is specified' {
            { Get-ADTWindowTitle -WindowTitle 'ZzzzNonExistentWindow99999' } | Should -Not -Throw
        }

        It 'Should not throw when -ParentProcess is specified' {
            { Get-ADTWindowTitle -ParentProcess 'ZzzzNonExistentProcess99999' } | Should -Not -Throw
        }

        It 'Should return nothing when searching for a window title that does not exist' {
            $result = Get-ADTWindowTitle -WindowTitle 'ZzzzNonExistentWindow99999'
            $result | Should -BeNullOrEmpty
        }

        It 'Should return PSADT.WindowManagement.WindowInfo objects when windows are found' {
            $result = @(Get-ADTWindowTitle)
            if ($result.Count -eq 0)
            {
                Set-ItResult -Skipped -Because 'No windows with titles found; likely running without an interactive user.'
                return
            }
            $result[0] | Should -BeOfType ([PSADT.WindowManagement.WindowInfo])
        }

        It 'Should return objects with a non-empty WindowTitle property' {
            $result = @(Get-ADTWindowTitle)
            if ($result.Count -eq 0)
            {
                Set-ItResult -Skipped -Because 'No windows with titles found.'
                return
            }
            $result | ForEach-Object { $_.WindowTitle | Should -Not -BeNullOrEmpty }
        }

        It 'Should return objects with a non-empty ParentProcess property' {
            $result = @(Get-ADTWindowTitle)
            if ($result.Count -eq 0)
            {
                Set-ItResult -Skipped -Because 'No windows with titles found.'
                return
            }
            $result | ForEach-Object { $_.ParentProcess | Should -Not -BeNullOrEmpty }
        }

        It 'Should filter by WindowTitle and return only matching windows' {
            $all = @(Get-ADTWindowTitle)
            if ($all.Count -eq 0)
            {
                Set-ItResult -Skipped -Because 'No windows found to test WindowTitle filtering.'
                return
            }
            $firstTitle = $all[0].WindowTitle
            $escapedTitle = [System.Text.RegularExpressions.Regex]::Escape($firstTitle)
            $filtered = @(Get-ADTWindowTitle -WindowTitle $escapedTitle)
            $filtered.Count | Should -BeGreaterThan 0
            $filtered | ForEach-Object { $_.WindowTitle | Should -Match $escapedTitle }
        }

        It 'Should filter by ParentProcess and return only matching windows' {
            $all = @(Get-ADTWindowTitle)
            if ($all.Count -eq 0)
            {
                Set-ItResult -Skipped -Because 'No windows found to test ParentProcess filtering.'
                return
            }
            $processName = $all[0].ParentProcess
            $filtered = @(Get-ADTWindowTitle -ParentProcess $processName)
            $filtered.Count | Should -BeGreaterThan 0
            $filtered | ForEach-Object { $_.ParentProcess | Should -Be $processName }
        }
    }

    Context 'Input Validation' {
        It 'Should throw ParameterArgumentValidationError when -WindowTitle is empty string' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Get-ADTWindowTitle'
            }
            { Get-ADTWindowTitle -WindowTitle '' } | Should @shouldParams
        }

        It 'Should throw ParameterArgumentValidationError when -WindowTitle is whitespace' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Get-ADTWindowTitle'
            }
            { Get-ADTWindowTitle -WindowTitle '   ' } | Should @shouldParams
        }

        It 'Should throw ParameterArgumentValidationError when -WindowHandle is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Get-ADTWindowTitle -WindowHandle $null } | Should @shouldParams
        }
    }

    Context 'Metadata' {
        It 'Should declare OutputType of PSADT.WindowManagement.WindowInfo' {
            $outputTypes = (Get-Command Get-ADTWindowTitle).OutputType.Type
            $outputTypes | Should -Contain ([PSADT.WindowManagement.WindowInfo])
        }

        It 'Should expose the WindowTitle parameter' {
            (Get-Command Get-ADTWindowTitle).Parameters.ContainsKey('WindowTitle') | Should -BeTrue
        }

        It 'Should expose the WindowHandle parameter' {
            (Get-Command Get-ADTWindowTitle).Parameters.ContainsKey('WindowHandle') | Should -BeTrue
        }

        It 'Should expose the ParentProcess parameter' {
            (Get-Command Get-ADTWindowTitle).Parameters.ContainsKey('ParentProcess') | Should -BeTrue
        }

        It 'Should expose the ParentProcessId parameter' {
            (Get-Command Get-ADTWindowTitle).Parameters.ContainsKey('ParentProcessId') | Should -BeTrue
        }

        It 'Should expose the ParentProcessMainWindowHandle parameter' {
            (Get-Command Get-ADTWindowTitle).Parameters.ContainsKey('ParentProcessMainWindowHandle') | Should -BeTrue
        }
    }
}
