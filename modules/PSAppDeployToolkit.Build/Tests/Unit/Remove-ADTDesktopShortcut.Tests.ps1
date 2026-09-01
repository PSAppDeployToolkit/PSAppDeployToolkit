BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    function Get-ShortcutName
    {
        [CmdletBinding()]
        [OutputType([System.String])]
        param
        (
        )

        return (Microsoft.PowerShell.Management\Get-ChildItem -LiteralPath $script:Desktop -Filter '*.lnk').Name
    }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Remove-ADTDesktopShortcut' {
    Context 'With no session open' {
        It 'Tells the caller to open one first' {
            # It compares shortcut timestamps against when the deployment started, so there has to be one.
            { Remove-ADTDesktopShortcut -RemoveAllShortcuts -WhatIf } | Should -Throw -ErrorId 'ADTSessionBufferEmpty,Remove-ADTDesktopShortcut'
        }
    }

    Context 'Removing shortcuts' {
        BeforeEach {
            # The function offers no way to be pointed somewhere else, so the enumeration of the real
            # desktop is redirected instead. Nothing outside TestDrive is ever looked at or deleted.
            $script:Desktop = "$TestDrive\Desktop$([System.Guid]::NewGuid().ToString('N'))"
            $null = New-Item -Path $script:Desktop -ItemType Directory -Force
            1..3 | ForEach-Object { Set-Content -LiteralPath "$script:Desktop\Shortcut$_.lnk" -Value 'shortcut' }
            Set-Content -LiteralPath "$script:Desktop\NotAShortcut.txt" -Value 'not a shortcut'
            Mock -ModuleName PSAppDeployToolkit Get-ChildItem { Microsoft.PowerShell.Management\Get-ChildItem -LiteralPath $script:Desktop -Filter '*.lnk' } -ParameterFilter { $Filter -eq '*.lnk' }
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'ShortcutRemoval' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterEach {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Removes every shortcut when asked' {
            Remove-ADTDesktopShortcut -RemoveAllShortcuts
            Get-ShortcutName | Should -BeNullOrEmpty
        }

        It 'Leaves anything that is not a shortcut' {
            # An installer's readme sitting on the desktop is not the deployment's to delete.
            Remove-ADTDesktopShortcut -RemoveAllShortcuts
            Test-Path -LiteralPath "$script:Desktop\NotAShortcut.txt" | Should -BeTrue
        }

        It 'Removes only what the filter matched' {
            Remove-ADTDesktopShortcut -FilterScript { $_.Name -eq 'Shortcut2.lnk' }
            Get-ShortcutName | Should -Be 'Shortcut1.lnk', 'Shortcut3.lnk'
        }

        It 'Removes the shortcuts written since the deployment started' {
            # This is how a deployment tidies up after an installer that scatters shortcuts, without
            # touching the ones that were already there.
            (Get-Item -LiteralPath "$script:Desktop\Shortcut1.lnk").LastWriteTime = (Get-ADTSession).CurrentDateTime.AddMinutes(-10)
            (Get-Item -LiteralPath "$script:Desktop\Shortcut2.lnk").LastWriteTime = (Get-ADTSession).CurrentDateTime.AddMinutes(10)
            (Get-Item -LiteralPath "$script:Desktop\Shortcut3.lnk").LastWriteTime = (Get-ADTSession).CurrentDateTime.AddMinutes(-10)
            Remove-ADTDesktopShortcut -SinceSessionStart
            Get-ShortcutName | Should -Be 'Shortcut1.lnk', 'Shortcut3.lnk'
        }

        It 'Says so when nothing matched' {
            Remove-ADTDesktopShortcut -FilterScript { $false }
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*No shortcuts were found*' }
        }

        It 'Does not object when there is nothing to remove' {
            Remove-ADTDesktopShortcut -RemoveAllShortcuts
            { Remove-ADTDesktopShortcut -RemoveAllShortcuts } | Should -Not -Throw
        }

        It 'Removes nothing with -WhatIf' {
            Remove-ADTDesktopShortcut -RemoveAllShortcuts -WhatIf
            @(Get-ShortcutName).Count | Should -Be 3
        }
    }

    Context 'Input Validation' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'ShortcutValidation' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Requires the caller to say which shortcuts' {
            # Defaulting to all of them would make an unqualified call quietly destructive.
            { Remove-ADTDesktopShortcut -WhatIf } | Should -Throw -ErrorId 'AmbiguousParameterSet,Remove-ADTDesktopShortcut'
        }

        It 'Refuses two ways of choosing at once' {
            { Remove-ADTDesktopShortcut -RemoveAllShortcuts -SinceSessionStart -WhatIf } | Should -Throw -ErrorId 'AmbiguousParameterSet,Remove-ADTDesktopShortcut'
        }

        It 'Refuses a scope it does not know' {
            { Remove-ADTDesktopShortcut -Scope 'Everywhere' -RemoveAllShortcuts -WhatIf } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Remove-ADTDesktopShortcut'
        }

        It 'Refuses the same scope twice' {
            { Remove-ADTDesktopShortcut -Scope AllUsersDesktop, AllUsersDesktop -RemoveAllShortcuts -WhatIf } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Remove-ADTDesktopShortcut'
        }
    }
}
