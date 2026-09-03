BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}
Describe 'Set-ADTShortcut' {
    BeforeAll {
        $hotkeyString = 'CTRL+SHIFT+F'
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'shellLinkProperties', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $shellLinkProperties = @{
            LiteralPath = "$TestDrive\Shortcut.lnk"
            Arguments = 'Arguments'
            Description = 'Description'
            Hotkey = [PSADT.ShortcutManagement.ShortcutHotkey]::Parse($hotkeyString)
            IconIndex = 5
            IconLocation = (Join-Path -Path $PSHOME -ChildPath (('powershell.exe', 'pwsh.exe')[$PSVersionTable.PSEdition.Equals('Core')]))
            TargetPath = "$TestDrive\TargetPath"
            WindowStyle = [PSADT.ShortcutManagement.ShortcutWindowStyle]::MinimizedNoActivate
            WorkingDirectory = 'WorkingDirectory'
        }

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should create a .lnk shortcut' {
            Set-ADTShortcut @shellLinkProperties -Force

            $shell = New-Object -ComObject WScript.Shell
            try
            {
                $shortcut = $shell.CreateShortcut($shellLinkProperties.LiteralPath)
                try
                {
                    $shortcut.Arguments | Should -Be $shellLinkProperties.Arguments
                    $shortcut.Description | Should -Be $shellLinkProperties.Description
                    $shortcut.Hotkey | Should -Be $shellLinkProperties.Hotkey.ToString()
                    $shortcut.IconLocation | Should -Be "$($shellLinkProperties.IconLocation),$($shellLinkProperties.IconIndex)"
                    $shortcut.TargetPath | Should -Be $shellLinkProperties.TargetPath
                    $shortcut.WindowStyle | Should -Be $shellLinkProperties.WindowStyle.value__
                    $shortcut.WorkingDirectory | Should -Be $shellLinkProperties.WorkingDirectory
                }
                finally
                {
                    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($shortcut)
                }
            }
            finally
            {
                [System.Runtime.InteropServices.Marshal]::ReleaseComObject($shell)
            }
        }
        It 'Should return a ShellLinkInfo when -PassThru is provided' {
            Set-ADTShortcut @shellLinkProperties -Force | Should -BeNullOrEmpty

            $output = Set-ADTShortcut @shellLinkProperties -PassThru
            $output | Should -BeOfType ([PSADT.ShortcutManagement.ShellLinkInfo])
        }
    }

    Context 'Internet shortcuts' {
        BeforeEach {
            $script:UrlPath = "$TestDrive\Set$([System.Guid]::NewGuid().ToString('N')).url"
            [System.IO.File]::WriteAllLines($script:UrlPath, [System.String[]]@(
                    '[InternetShortcut]'
                    'URL=https://psappdeploytoolkit.com/'
                ))
        }

        It 'Should change the address it points at' {
            Set-ADTShortcut -LiteralPath $script:UrlPath -TargetPath 'https://github.com/PSAppDeployToolkit'
            (Get-ADTShortcut -LiteralPath $script:UrlPath).Url | Should -BeExactly 'https://github.com/PSAppDeployToolkit'
        }

        It 'Should change the icon' {
            Set-ADTShortcut -LiteralPath $script:UrlPath -IconLocation "$([System.Environment]::SystemDirectory)\shell32.dll" -IconIndex 7
            $shortcut = Get-ADTShortcut -LiteralPath $script:UrlPath
            $shortcut.IconFile | Should -BeExactly "$([System.Environment]::SystemDirectory)\shell32.dll"
            $shortcut.IconIndex | Should -Be 7
        }

        It 'Should change the description' {
            Set-ADTShortcut -LiteralPath $script:UrlPath -Description 'The toolkit'
            (Get-ADTShortcut -LiteralPath $script:UrlPath).Description | Should -BeExactly 'The toolkit'
        }

        It 'Should leave the address alone when changing something else' {
            # Each setting is written back into the same file, so one of them must not lose the others.
            Set-ADTShortcut -LiteralPath $script:UrlPath -Description 'The toolkit'
            (Get-ADTShortcut -LiteralPath $script:UrlPath).Url | Should -BeExactly 'https://psappdeploytoolkit.com/'
        }

        It 'Should ignore a property a .url has no place for' {
            # Arguments belong to a .lnk. Accepting and dropping them keeps a caller from having to know
            # which extension they are working with, so what matters is that nothing else is disturbed.
            Set-ADTShortcut -LiteralPath $script:UrlPath -Arguments 'these go nowhere'
            [System.IO.File]::ReadAllText($script:UrlPath) | Should -Not -BeLike '*these go nowhere*'
            (Get-ADTShortcut -LiteralPath $script:UrlPath).Url | Should -BeExactly 'https://psappdeploytoolkit.com/'
        }
    }

    Context 'Clearing a property of a .lnk' {
        BeforeEach {
            # Built with every property set, so that clearing one of them is visible and the others can be
            # checked to have survived.
            $script:LinkPath = "$TestDrive\Clear$([System.Guid]::NewGuid().ToString('N')).lnk"
            New-ADTShortcut -LiteralPath $script:LinkPath -TargetPath "$([System.Environment]::SystemDirectory)\cmd.exe" -Arguments '/c echo hello' -Description 'A description' -WorkingDirectory ([System.Environment]::SystemDirectory) -IconLocation "$([System.Environment]::SystemDirectory)\shell32.dll" -IconIndex 3 -WindowStyle Maximized -RunAsAdmin -Hotkey 'CTRL+SHIFT+F' -Force
        }

        It 'Empties -<Property>' -ForEach @(
            @{ Property = 'Arguments' }
            @{ Property = 'Description' }
            @{ Property = 'WorkingDirectory' }
            @{ Property = 'Hotkey' }
        ) {
            (Get-ADTShortcut -LiteralPath $script:LinkPath).$Property | Should -Not -BeNullOrEmpty
            Set-ADTShortcut -LiteralPath $script:LinkPath -Clear $Property
            (Get-ADTShortcut -LiteralPath $script:LinkPath).$Property | Should -BeNullOrEmpty
        }

        It 'Returns -WindowStyle to normal' {
            # There is no absent window style, so clearing it means the default a shortcut is created with.
            Set-ADTShortcut -LiteralPath $script:LinkPath -Clear WindowStyle
            (Get-ADTShortcut -LiteralPath $script:LinkPath).WindowStyle | Should -Be ([PSADT.ShortcutManagement.ShortcutWindowStyle]::Normal)
        }

        It 'Stops it running as administrator' {
            Set-ADTShortcut -LiteralPath $script:LinkPath -Clear RunAsAdmin
            (Get-ADTShortcut -LiteralPath $script:LinkPath).RunAsAdmin | Should -BeFalse
        }

        It 'Sets it to run as administrator' {
            Set-ADTShortcut -LiteralPath $script:LinkPath -RunAsAdmin:$false
            (Get-ADTShortcut -LiteralPath $script:LinkPath).RunAsAdmin | Should -BeFalse
            Set-ADTShortcut -LiteralPath $script:LinkPath -RunAsAdmin
            (Get-ADTShortcut -LiteralPath $script:LinkPath).RunAsAdmin | Should -BeTrue
        }

        It 'Returns -IconIndex to the first icon' {
            Set-ADTShortcut -LiteralPath $script:LinkPath -Clear IconIndex
            (Get-ADTShortcut -LiteralPath $script:LinkPath).IconIndex | Should -Be 0
        }

        It 'Leaves the properties it was not asked about alone' {
            Set-ADTShortcut -LiteralPath $script:LinkPath -Clear Description
            $shortcut = Get-ADTShortcut -LiteralPath $script:LinkPath
            $shortcut.Arguments | Should -BeExactly '/c echo hello'
            # Compared without regard to case, since the shell normalises the casing of what it stores.
            $shortcut.TargetPath | Should -Be "$([System.Environment]::SystemDirectory)\cmd.exe"
        }

        It 'Empties -IconLocation' {
            Set-ADTShortcut -LiteralPath $script:LinkPath -Clear IconLocation
            $shortcut = Get-ADTShortcut -LiteralPath $script:LinkPath
            $shortcut.HasIconLocation | Should -BeFalse
            $shortcut.IconLocation | Should -BeNullOrEmpty
            $shortcut.IconIndex | Should -BeNullOrEmpty
        }
    }

    Context 'Clearing a property of a .url' {
        BeforeEach {
            $script:UrlPath = "$TestDrive\Clear$([System.Guid]::NewGuid().ToString('N')).url"
            [System.IO.File]::WriteAllLines($script:UrlPath, [System.String[]]@(
                    '[InternetShortcut]'
                    'URL=https://psappdeploytoolkit.com/'
                ))
            Set-ADTShortcut -LiteralPath $script:UrlPath -Description 'A description' -IconLocation "$([System.Environment]::SystemDirectory)\shell32.dll" -IconIndex 3 -Hotkey 'CTRL+SHIFT+G'
        }

        It 'Empties -<Property>' -ForEach @(
            @{ Property = 'Description'; Reported = 'Description' }
            @{ Property = 'IconLocation'; Reported = 'IconFile' }
            @{ Property = 'Hotkey'; Reported = 'Hotkey' }
        ) {
            (Get-ADTShortcut -LiteralPath $script:UrlPath).$Reported | Should -Not -BeNullOrEmpty
            Set-ADTShortcut -LiteralPath $script:UrlPath -Clear $Property
            (Get-ADTShortcut -LiteralPath $script:UrlPath).$Reported | Should -BeNullOrEmpty
        }

        It 'Leaves the address alone when clearing something else' {
            Set-ADTShortcut -LiteralPath $script:UrlPath -Clear Description
            (Get-ADTShortcut -LiteralPath $script:UrlPath).Url | Should -BeExactly 'https://psappdeploytoolkit.com/'
        }

        It 'Returns -IconIndex to the first icon' {
            Set-ADTShortcut -LiteralPath $script:UrlPath -Clear IconIndex
            (Get-ADTShortcut -LiteralPath $script:UrlPath).IconIndex | Should -Be 0
        }

        It 'Writes no <Key> for a property it has nowhere to put' -ForEach @(
            @{ Splat = @{ WorkingDirectory = 'C:\Windows\System32' }; Key = 'WorkingDirectory' }
            @{ Splat = @{ WindowStyle = 'Maximized' }; Key = 'ShowCommand' }
        ) {
            # Nothing is launched from a directory, and the browser opens through the URL handler rather than
            # from a window state the shortcut carries, so neither means anything here. Accepted and dropped
            # the way arguments are, so a caller need not know which extension they are working with, rather
            # than written into a key the shell will then refuse to read back.
            $splat = $Splat
            Set-ADTShortcut -LiteralPath $script:UrlPath @splat
            [System.IO.File]::ReadAllLines($script:UrlPath) | Should -Not -Contain "$Key="
            [System.IO.File]::ReadAllText($script:UrlPath) | Should -Not -BeLike "*$Key=*"
            (Get-ADTShortcut -LiteralPath $script:UrlPath).Url | Should -BeExactly 'https://psappdeploytoolkit.com/'
        }
    }

    Context 'Changing a shortcut it was handed' {
        BeforeEach {
            $script:PipedPath = "$TestDrive\Piped$([System.Guid]::NewGuid().ToString('N')).lnk"
            New-ADTShortcut -LiteralPath $script:PipedPath -TargetPath "$([System.Environment]::SystemDirectory)\cmd.exe" -Description 'Before' -Force
        }

        It 'Takes a shortcut off the pipeline' {
            # Reading a shortcut, deciding from what it says, and writing it back is the ordinary way round,
            # and it saves the caller having to carry the path alongside.
            Get-ADTShortcut -LiteralPath $script:PipedPath | Set-ADTShortcut -Description 'After'
            (Get-ADTShortcut -LiteralPath $script:PipedPath).Description | Should -BeExactly 'After'
        }

        It 'Takes one as -InputObject' {
            Set-ADTShortcut -InputObject (Get-ADTShortcut -LiteralPath $script:PipedPath) -Description 'After'
            (Get-ADTShortcut -LiteralPath $script:PipedPath).Description | Should -BeExactly 'After'
        }
    }
    Context 'Input Validation' {
        It 'Should throw when the path provided to -LiteralPath does not exists and -Force is not specified' {
            { Set-ADTShortcut -LiteralPath "$TestDrive\DoesNotExist.lnk" -TargetPath 'test' } | Should -Throw -ExceptionType ([System.IO.FileNotFoundException]) -ErrorId 'LiteralPathNotFound,Set-ADTShortcut'
        }
        It 'Should validate that -TargetPath is specified when creating a new shortcut' {
            { Set-ADTShortcut -LiteralPath "$TestDrive\DoesNotExist.lnk" -Force } | Should -Throw -ExceptionType ([System.InvalidOperationException]) -ErrorId 'NoTargetPathForNonPreExistingShortcut,Set-ADTShortcut'
        }
        It 'Should not throw when the path provided to -LiteralPath does not exist and -Force is specified' {
            { Set-ADTShortcut -LiteralPath "$TestDrive\DoesNotExist.lnk" -TargetPath 'test' -Force } | Should -Not -Throw
        }
        It 'Should validate that the path provided to -LiteralPath has a valid shortcut extension' {
            { Set-ADTShortcut -LiteralPath "$TestDrive\WrongExtension.txt" -Force } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentValidationError,Set-ADTShortcut'
        }
        It 'Should validate that at least one property is modified' {
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath } | Should -Throw -ExceptionType ([System.InvalidOperationException]) -ErrorId 'FunctionCalledWithInsufficientParameters,Set-ADTShortcut'
        }
        It 'Should validate that -Hotkey is a valid hotkey' {
            Set-ADTShortcut @shellLinkProperties -Force
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Hotkey 'NotARealHotkey' } | Should -Throw -ExceptionType ([System.Management.Automation.SetValueInvocationException]) -ErrorId 'ExceptionWhenSetting,Set-ADTShortcut'
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Hotkey 'Ctrl+Shift+0' } | Should -Not -Throw
        }
        It 'Should validate that -LiteralPath is not null, empty, or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Set-ADTShortcut'
            }
            { Set-ADTShortcut -LiteralPath $null } | Should @shouldParams
            { Set-ADTShortcut -LiteralPath '' } | Should @shouldParams
            { Set-ADTShortcut -LiteralPath " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should validate that -Arguments is not null, empty, or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Set-ADTShortcut'
            }
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Arguments $null } | Should @shouldParams
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Arguments '' } | Should @shouldParams
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Arguments " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should validate that -Description is not null, empty, or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Set-ADTShortcut'
            }
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Description $null } | Should @shouldParams
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Description '' } | Should @shouldParams
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Description " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should validate that -Hotkey is not null, empty, or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Set-ADTShortcut'
            }
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Hotkey $null } | Should @shouldParams
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Hotkey '' } | Should @shouldParams
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Hotkey " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should validate that -IconIndex is not null' {
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -IconIndex $null } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentValidationError,Set-ADTShortcut'
        }
        It 'Should validate that -IconLocation is not null, empty, or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Set-ADTShortcut'
            }
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -IconLocation $null } | Should @shouldParams
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -IconLocation '' } | Should @shouldParams
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -IconLocation " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should validate that -TargetPath is not null, empty, or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Set-ADTShortcut'
            }
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -TargetPath $null } | Should @shouldParams
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -TargetPath '' } | Should @shouldParams
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -TargetPath " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should validate that -WorkingDirectory is not null, empty, or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Set-ADTShortcut'
            }
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -WorkingDirectory $null } | Should @shouldParams
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -WorkingDirectory '' } | Should @shouldParams
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -WorkingDirectory " `f`n`r`t`v" } | Should @shouldParams
        }
    }
}
