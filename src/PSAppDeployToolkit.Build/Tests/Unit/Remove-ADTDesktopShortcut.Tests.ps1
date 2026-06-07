BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Remove-ADTDesktopShortcut' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # An active session is required; expose a controllable CurrentDateTime for the SinceSessionStart path.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'SessionDateTime', Justification = "This variable is used within script blocks that PSScriptAnalyzer has no visibility of.")]
        $SessionDateTime = [System.DateTime]::new(2026, 1, 1, 0, 0, 0)
        Mock -ModuleName PSAppDeployToolkit Get-ADTSession {
            [pscustomobject]@{ CurrentDateTime = $SessionDateTime }
        }
    }

    BeforeEach {
        # Redirect the common desktop to a per-test folder under $TestDrive so the real desktop is never touched.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'CommonDesktop', Justification = "This variable is used within script blocks that PSScriptAnalyzer has no visibility of.")]
        $CommonDesktop = (New-Item -Path "$TestDrive\CommonDesktop" -ItemType Directory -Force).FullName
        Mock -ModuleName PSAppDeployToolkit Get-ADTEnvironmentTable {
            @{
                envCommonDesktop = $CommonDesktop
                RunAsActiveUser = $null
            }
        }
    }

    AfterEach {
        Remove-Item -LiteralPath "$TestDrive\CommonDesktop" -Recurse -Force -ErrorAction SilentlyContinue
    }

    Context 'Functionality' {
        It 'Removes all shortcuts in scope when -RemoveAllShortcuts is specified' {
            New-Item -Path "$CommonDesktop\App1.lnk" -ItemType File -Force | Out-Null
            New-Item -Path "$CommonDesktop\App2.lnk" -ItemType File -Force | Out-Null

            Remove-ADTDesktopShortcut -RemoveAllShortcuts

            Get-ChildItem -LiteralPath $CommonDesktop -Filter '*.lnk' | Should -BeNullOrEmpty
        }

        It 'Only removes .lnk files, leaving other files intact' {
            New-Item -Path "$CommonDesktop\App1.lnk" -ItemType File -Force | Out-Null
            New-Item -Path "$CommonDesktop\notes.txt" -ItemType File -Force | Out-Null

            Remove-ADTDesktopShortcut -RemoveAllShortcuts

            Test-Path -LiteralPath "$CommonDesktop\App1.lnk" | Should -BeFalse
            Test-Path -LiteralPath "$CommonDesktop\notes.txt" | Should -BeTrue
        }

        It 'Removes only shortcuts matching the FilterScript' {
            New-Item -Path "$CommonDesktop\Keep.lnk" -ItemType File -Force | Out-Null
            New-Item -Path "$CommonDesktop\RemoveMe.lnk" -ItemType File -Force | Out-Null

            Remove-ADTDesktopShortcut -FilterScript { $_.Name -eq 'RemoveMe.lnk' }

            Test-Path -LiteralPath "$CommonDesktop\RemoveMe.lnk" | Should -BeFalse
            Test-Path -LiteralPath "$CommonDesktop\Keep.lnk" | Should -BeTrue
        }

        It 'Removes only shortcuts created after the session started when -SinceSessionStart is specified' {
            $oldShortcut = New-Item -Path "$CommonDesktop\Old.lnk" -ItemType File -Force
            $oldShortcut.LastWriteTime = $SessionDateTime.AddHours(-1)
            $newShortcut = New-Item -Path "$CommonDesktop\New.lnk" -ItemType File -Force
            $newShortcut.LastWriteTime = $SessionDateTime.AddHours(1)

            Remove-ADTDesktopShortcut -SinceSessionStart

            Test-Path -LiteralPath "$CommonDesktop\New.lnk" | Should -BeFalse
            Test-Path -LiteralPath "$CommonDesktop\Old.lnk" | Should -BeTrue
        }

        It 'Does not delete shortcuts when -WhatIf is specified' {
            New-Item -Path "$CommonDesktop\App1.lnk" -ItemType File -Force | Out-Null

            Remove-ADTDesktopShortcut -RemoveAllShortcuts -WhatIf

            Test-Path -LiteralPath "$CommonDesktop\App1.lnk" | Should -BeTrue
        }

        It 'Does not throw when there are no shortcuts to remove' {
            { Remove-ADTDesktopShortcut -RemoveAllShortcuts } | Should -Not -Throw
        }
    }

    Context 'Input Validation' {
        It 'Should only accept valid Scope values' {
            { Remove-ADTDesktopShortcut -Scope 'AllUsersDesktop' -RemoveAllShortcuts } | Should -Not -Throw
            { Remove-ADTDesktopShortcut -Scope 'Invalid' -RemoveAllShortcuts } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentValidationError,Remove-ADTDesktopShortcut'
        }

        It 'Should verify that FilterScript is not null or empty' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTDesktopShortcut'
            }
            { Remove-ADTDesktopShortcut -FilterScript $null } | Should @shouldParams
        }

        It 'Should reject duplicate Scope values' {
            { Remove-ADTDesktopShortcut -Scope 'AllUsersDesktop', 'AllUsersDesktop' -RemoveAllShortcuts } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
