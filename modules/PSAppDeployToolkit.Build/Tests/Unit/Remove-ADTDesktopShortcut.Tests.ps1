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

        # Expanded through the pipeline rather than read off the result, which is nothing at all once every
        # shortcut has been removed - the very state most of these tests are asserting.
        return Microsoft.PowerShell.Management\Get-ChildItem -LiteralPath $script:Desktop -Filter '*.lnk' | Select-Object -ExpandProperty Name
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

    Context 'A shortcut that will not delete' {
        BeforeEach {
            # A handle is held open on whichever shortcuts the case locks. Deleting a file that another
            # process has open without sharing delete access fails, which is the same refusal a shortcut
            # still held by Explorer produces.
            $script:Desktop = "$TestDrive\Locked$([System.Guid]::NewGuid().ToString('N'))"
            $null = New-Item -Path $script:Desktop -ItemType Directory -Force
            1..2 | ForEach-Object { Set-Content -LiteralPath "$script:Desktop\Shortcut$_.lnk" -Value 'shortcut' }
            Mock -ModuleName PSAppDeployToolkit Get-ChildItem { Microsoft.PowerShell.Management\Get-ChildItem -LiteralPath $script:Desktop -Filter '*.lnk' } -ParameterFilter { $Filter -eq '*.lnk' }
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'ShortcutLocks' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
            $script:Streams = [System.Collections.Generic.List[System.IO.FileStream]]::new()
        }

        AfterEach {
            $script:Streams | & { process { $_.Dispose() } }
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Stops at the first one it cannot delete' {
            # A caller who says nothing about errors gets the default of Stop, and so is told about the
            # failure rather than handed a partial result reported as a success.
            $script:Streams.Add([System.IO.File]::Open("$script:Desktop\Shortcut1.lnk", 'Open', 'Read', 'None'))
            { Remove-ADTDesktopShortcut -RemoveAllShortcuts } | Should -Throw -ErrorId 'IOException,Remove-ADTDesktopShortcut'
        }

        It 'Removes the rest when told to carry on' {
            $script:Streams.Add([System.IO.File]::Open("$script:Desktop\Shortcut1.lnk", 'Open', 'Read', 'None'))
            Remove-ADTDesktopShortcut -RemoveAllShortcuts -ErrorAction Continue -ErrorVariable removalErrors 2>$null
            Get-ShortcutName | Should -BeExactly 'Shortcut1.lnk'
            $removalErrors | Should -Not -BeNullOrEmpty
        }

        It 'Gathers the failures together when none of them could be removed' {
            # One error per shortcut is noise when the cause is the same for all of them, so a wholesale
            # failure is reported once with the individual failures carried inside it.
            1..2 | ForEach-Object { $script:Streams.Add([System.IO.File]::Open("$script:Desktop\Shortcut$_.lnk", 'Open', 'Read', 'None')) }
            Remove-ADTDesktopShortcut -RemoveAllShortcuts -ErrorAction Continue -ErrorVariable removalErrors 2>$null
            # The error variable also collects the intermediate objects each record passes through on its
            # way out, so the reported failure has to be picked out of it rather than read off the end.
            $aggregate = @($removalErrors | & { process { if (($_ -is [System.Management.Automation.ErrorRecord]) -and $_.FullyQualifiedErrorId.Equals('ShortcutDeletionFullFailure,Remove-ADTDesktopShortcut')) { return $_ } } })
            $aggregate | Should -Not -BeNullOrEmpty
            $aggregate[0].Exception | Should -BeOfType ([System.AggregateException])
            $aggregate[0].Exception.InnerExceptions.Count | Should -Be 2
            Get-ShortcutName | Should -Be @('Shortcut1.lnk', 'Shortcut2.lnk')
        }
    }
}
