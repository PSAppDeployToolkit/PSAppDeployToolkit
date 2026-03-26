BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Remove-ADTDesktopShortcut' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Session start is 1 hour ago — any shortcuts created now are "since session start".
        Mock -ModuleName PSAppDeployToolkit Get-ADTSession {
            return [pscustomobject]@{ CurrentDateTime = [datetime]::Now.AddHours(-1) }
        }

        # Redirect AllUsersDesktop to our script-level test path so Get-ChildItem scans TestDrive.
        Mock -ModuleName PSAppDeployToolkit Get-ADTEnvironmentTable {
            return [pscustomobject]@{
                envCommonDesktop = $Script:TestDesktopPath
                RunAsActiveUser  = $null
            }
        }
    }

    BeforeEach {
        # A fresh desktop folder per test, referenced by the mock above.
        $Script:TestDesktopPath = (New-Item -Path "$TestDrive\Desktop-$(New-Guid)" -ItemType Directory -Force).FullName

        New-Item -Path "$Script:TestDesktopPath\App1.lnk"    -ItemType File -Force | Out-Null
        New-Item -Path "$Script:TestDesktopPath\App2.lnk"    -ItemType File -Force | Out-Null
        New-Item -Path "$Script:TestDesktopPath\Notes.txt"    -ItemType File -Force | Out-Null
        New-Item -Path "$Script:TestDesktopPath\Tool.lnk"     -ItemType File -Force | Out-Null
    }

    Context 'RemoveAllShortcuts' {
        It 'Removes every .lnk file from the desktop' {
            Remove-ADTDesktopShortcut -RemoveAllShortcuts

            Get-ChildItem -Path $Script:TestDesktopPath -Filter *.lnk | Should -BeNullOrEmpty
        }

        It 'Leaves non-.lnk files intact' {
            Remove-ADTDesktopShortcut -RemoveAllShortcuts

            "$Script:TestDesktopPath\Notes.txt" | Should -Exist
        }

        It 'Does not throw when no shortcuts exist' {
            Remove-Item -Path "$Script:TestDesktopPath\*.lnk" -Force
            { Remove-ADTDesktopShortcut -RemoveAllShortcuts } | Should -Not -Throw
        }
    }

    Context 'FilterScript' {
        It 'Removes only shortcuts that match the FilterScript' {
            Remove-ADTDesktopShortcut -FilterScript { $_.Name -eq 'App1.lnk' }

            "$Script:TestDesktopPath\App1.lnk" | Should -Not -Exist
            "$Script:TestDesktopPath\App2.lnk" | Should -Exist
            "$Script:TestDesktopPath\Tool.lnk" | Should -Exist
        }

        It 'Removes multiple shortcuts matching a wildcard FilterScript' {
            Remove-ADTDesktopShortcut -FilterScript { $_.Name -like 'App*.lnk' }

            "$Script:TestDesktopPath\App1.lnk" | Should -Not -Exist
            "$Script:TestDesktopPath\App2.lnk" | Should -Not -Exist
            "$Script:TestDesktopPath\Tool.lnk" | Should -Exist
        }

        It 'Does not remove any shortcuts when the FilterScript matches nothing' {
            Remove-ADTDesktopShortcut -FilterScript { $_.Name -eq 'NonExistent.lnk' }

            Get-ChildItem -Path $Script:TestDesktopPath -Filter *.lnk | Measure-Object | Select-Object -ExpandProperty Count | Should -Be 3
        }
    }

    Context 'SinceSessionStart' {
        It 'Removes shortcuts created after the session start' {
            # All files were created in BeforeEach (just now), which is after the mocked session start (1 hour ago).
            Remove-ADTDesktopShortcut -SinceSessionStart

            Get-ChildItem -Path $Script:TestDesktopPath -Filter *.lnk | Should -BeNullOrEmpty
        }

        It 'Does not remove shortcuts older than the session start' {
            # Age all existing shortcuts to before the session started.
            Get-ChildItem -Path $Script:TestDesktopPath -Filter *.lnk | ForEach-Object {
                $_.LastWriteTime = [datetime]::Now.AddHours(-2)
            }

            Remove-ADTDesktopShortcut -SinceSessionStart

            Get-ChildItem -Path $Script:TestDesktopPath -Filter *.lnk | Measure-Object | Select-Object -ExpandProperty Count | Should -Be 3
        }
    }

    Context 'Scope' {
        It 'Defaults to AllUsersDesktop scope without throwing' {
            { Remove-ADTDesktopShortcut -RemoveAllShortcuts } | Should -Not -Throw
        }

        It 'Accepts explicit AllUsersDesktop scope' {
            { Remove-ADTDesktopShortcut -RemoveAllShortcuts -Scope AllUsersDesktop } | Should -Not -Throw
        }
    }

    Context 'WhatIf Support' {
        It 'Does not delete any shortcuts when -WhatIf is specified' {
            Remove-ADTDesktopShortcut -RemoveAllShortcuts -WhatIf

            Get-ChildItem -Path $Script:TestDesktopPath -Filter *.lnk | Measure-Object | Select-Object -ExpandProperty Count | Should -Be 3
        }

        It 'Does not delete FilterScript-matched shortcuts when -WhatIf is specified' {
            Remove-ADTDesktopShortcut -FilterScript { $_.Name -eq 'App1.lnk' } -WhatIf

            "$Script:TestDesktopPath\App1.lnk" | Should -Exist
        }
    }

    Context 'Input Validation' {
        It 'Throws when Scope is an invalid value' {
            { Remove-ADTDesktopShortcut -RemoveAllShortcuts -Scope 'InvalidScope' } | Should -Throw
        }

        It 'Throws when FilterScript is null' {
            { Remove-ADTDesktopShortcut -FilterScript $null } | Should -Throw
        }

        It 'Throws when no parameter set is specified' {
            # All three mode parameters are mandatory — omitting all of them must fail.
            { Remove-ADTDesktopShortcut } | Should -Throw
        }
    }
}
