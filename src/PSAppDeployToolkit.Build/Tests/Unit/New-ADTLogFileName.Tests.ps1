BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Build a mock session object that matches what the function expects.
    $script:MockLogPath = 'C:\TestLogs'
    $script:MockSession = [PSCustomObject]@{ LogPath = $script:MockLogPath }
    $script:MockSession | Add-Member -MemberType ScriptMethod -Name 'NewLogFileName' -Value {
        param([string]$Discriminator)
        return "${Discriminator}_tool.log"
    }
}

Describe 'New-ADTLogFileName' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
    }

    Context 'With Active Session' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $script:MockSession }
        }

        It 'Returns a System.String' {
            New-ADTLogFileName -Discriminator 'Setup' -FileNameOnly | Should -BeOfType [System.String]
        }

        It '-FileNameOnly returns just the filename (no directory separator)' {
            $result = New-ADTLogFileName -Discriminator 'Setup' -FileNameOnly
            $result | Should -Not -BeLike '*\*'
        }

        It '-FileNameOnly result equals the value from the session NewLogFileName method' {
            $result = New-ADTLogFileName -Discriminator 'Setup' -FileNameOnly
            $result | Should -Be 'Setup_tool.log'
        }

        It '-FileNameOnly:$false prepends the session LogPath' {
            $result = New-ADTLogFileName -Discriminator 'Setup' -FileNameOnly:$false
            $result | Should -BeLike "$($script:MockLogPath)\*"
        }

        It '-FileNameOnly:$false result is LogPath combined with the filename' {
            $result = New-ADTLogFileName -Discriminator 'Setup' -FileNameOnly:$false
            $result | Should -Be "$($script:MockLogPath)\Setup_tool.log"
        }

        It 'The Discriminator value appears in the returned filename' {
            $result = New-ADTLogFileName -Discriminator 'MyApp' -FileNameOnly
            $result | Should -BeLike 'MyApp*'
        }

        It 'Does not throw when a session is active' {
            { New-ADTLogFileName -Discriminator 'Setup' -FileNameOnly } | Should -Not -Throw
        }
    }

    Context 'Without Active Session' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { throw [System.Exception]::new('No active session') }
        }

        It 'Throws when no session is active' {
            { New-ADTLogFileName -Discriminator 'Test' -FileNameOnly } | Should -Throw
        }
    }
}
