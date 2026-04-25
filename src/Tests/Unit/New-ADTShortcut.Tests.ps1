BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTShortcut' {
    BeforeAll {
        $hotkeyString = 'CTRL+SHIFT+F'
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'shellLinkProperties', Justification = 'This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.')]
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
            New-ADTShortcut @shellLinkProperties

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
        It 'Should return a IShortcutLinkInfo when -PassThru is provided' {
            New-ADTShortcut @shellLinkProperties -Force | Should -BeNullOrEmpty

            $output = New-ADTShortcut @shellLinkProperties -Force -PassThru
            $output | Should -BeOfType ([PSADT.ShortcutManagement.ShellLinkInfo])
        }
    }

    Context 'Input Validation' {
        It 'Should throw when the path provided to -LiteralPath already exists and -Force is not specified' {
            New-ADTShortcut @shellLinkProperties
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -TargetPath 'test' } | Should -Throw -ExceptionType ([System.IO.IOException]) -ErrorId 'ShortcutPathIsPreExisting,New-ADTShortcut'
        }
        It 'Should validate that the path provided to -LiteralPath has a valid shortcut extension' {
            { New-ADTShortcut -LiteralPath "$TestDrive\WrongExtension.txt" } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentValidationError,New-ADTShortcut'
        }
        It 'Should validate that -Hotkey is a valid hotkey' {
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Hotkey 'NotARealHotkey' -Force } | Should -Throw -ExceptionType ([System.Management.Automation.SetValueInvocationException]) -ErrorId 'ExceptionWhenSetting,Set-ADTShortcut'
            { Set-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Hotkey 'Ctrl+Shift+0' -Force } | Should -Not -Throw
        }
        It 'Should validate that -LiteralPath is not null, empty, or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,New-ADTShortcut'
            }
            { New-ADTShortcut -LiteralPath $null } | Should @shouldParams
            { New-ADTShortcut -LiteralPath '' } | Should @shouldParams
            { New-ADTShortcut -LiteralPath " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should validate that -Arguments is not null, empty, or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,New-ADTShortcut'
            }
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Arguments $null } | Should @shouldParams
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Arguments '' } | Should @shouldParams
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Arguments " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should validate that -Description is not null, empty, or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,New-ADTShortcut'
            }
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Description $null } | Should @shouldParams
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Description '' } | Should @shouldParams
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Description " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should validate that -Hotkey is not null, empty, or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,New-ADTShortcut'
            }
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Hotkey $null } | Should @shouldParams
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Hotkey '' } | Should @shouldParams
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -Hotkey " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should validate that -IconIndex is not null' {
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -IconIndex $null } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentValidationError,New-ADTShortcut'
        }
        It 'Should validate that -IconLocation is not null, empty, or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,New-ADTShortcut'
            }
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -IconLocation $null } | Should @shouldParams
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -IconLocation '' } | Should @shouldParams
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -IconLocation " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should validate that -TargetPath is not null, empty, or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,New-ADTShortcut'
            }
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -TargetPath $null } | Should @shouldParams
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -TargetPath '' } | Should @shouldParams
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -TargetPath " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should validate that -WorkingDirectory is not null, empty, or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,New-ADTShortcut'
            }
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -WorkingDirectory $null } | Should @shouldParams
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -WorkingDirectory '' } | Should @shouldParams
            { New-ADTShortcut -LiteralPath $shellLinkProperties.LiteralPath -WorkingDirectory " `f`n`r`t`v" } | Should @shouldParams
        }
    }
}
