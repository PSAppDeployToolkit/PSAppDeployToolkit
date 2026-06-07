BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTPowerPoint' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should not throw' {
            { Test-ADTPowerPoint } | Should -Not -Throw
        }

        It 'Should return a [System.Boolean] or nothing (null) depending on session state' {
            $result = Test-ADTPowerPoint
            # The function may return $null when there is no logged-on user (bypass path),
            # or $true/$false when a user is present.
            ($null -eq $result -or $result -is [System.Boolean]) | Should -BeTrue
        }

        It 'Should return $false when PowerPoint is not running and a user is logged on' {
            # Skip when no user is present (null return means bypass was taken).
            $result = Test-ADTPowerPoint
            if ($null -eq $result)
            {
                Set-ItResult -Skipped -Because 'No active user session; function bypassed'
            }
            # PowerPoint should not be running in a CI/test environment.
            if (Get-Process -Name POWERPNT -ErrorAction Ignore)
            {
                Set-ItResult -Skipped -Because 'POWERPNT.EXE is currently running; cannot assert $false'
            }
            $result | Should -BeFalse
        }

        It 'Should return $true or $false (never throw) when PowerPoint IS running' {
            Set-ItResult -Skipped -Because 'PowerPoint slideshow state is environment-specific; cannot safely start/stop POWERPNT in a unit test'
        }
    }

    Context 'Metadata' {
        It 'Should declare OutputType of System.Boolean' {
            $outputTypes = (Get-Command Test-ADTPowerPoint).OutputType.Type
            $outputTypes | Should -Contain ([System.Boolean])
        }

        It 'Should accept no parameters beyond common parameters' {
            $params = (Get-Command Test-ADTPowerPoint).Parameters.Keys |
                Where-Object { $_ -notin [System.Management.Automation.PSCmdlet]::CommonParameters }
            @($params).Count | Should -Be 0
        }
    }
}
