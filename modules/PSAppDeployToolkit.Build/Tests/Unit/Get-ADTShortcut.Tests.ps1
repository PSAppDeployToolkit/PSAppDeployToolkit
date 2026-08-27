BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTShortcut' {
    BeforeAll {
        $hotkeyString = 'CTRL+SHIFT+F'
        $shellLinkProperties = @{
            FilePath = "$TestDrive\Shortcut.lnk"
            Arguments = 'Arguments'
            Description = 'Description'
            Hotkey = [PSADT.ShortcutManagement.ShortcutHotkey]::Parse($hotkeyString)
            IconIndex = 5
            IconLocation = (Join-Path -Path $PSHOME -ChildPath (('powershell.exe', 'pwsh.exe')[$PSVersionTable.PSEdition.Equals('Core')]))
            TargetPath = "$TestDrive\TargetPath"
            WindowStyle = [PSADT.ShortcutManagement.ShortcutWindowStyle]::MinimizedNoActivate
            WorkingDirectory = 'WorkingDirectory'
        }

        $shell = New-Object -ComObject WScript.Shell
        try
        {
            # Create a .lnk "ShellLink" shortcut file to test against
            $shortcut = $shell.CreateShortcut($shellLinkProperties.FilePath)
            try
            {
                $shortcut.Arguments = $shellLinkProperties.Arguments
                $shortcut.Description = $shellLinkProperties.Description
                $shortcut.Hotkey = $shellLinkProperties.Hotkey.ToString()
                $shortcut.IconLocation = "$($shellLinkProperties.IconLocation),$($shellLinkProperties.IconIndex)"
                $shortcut.TargetPath = $shellLinkProperties.TargetPath
                $shortcut.WindowStyle = $shellLinkProperties.WindowStyle.value__
                $shortcut.WorkingDirectory = $shellLinkProperties.WorkingDirectory
                $shortcut.Save()
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

        Copy-Item -LiteralPath $shellLinkProperties.FilePath -Destination "$TestDrive\WrongExtension.txt"

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should return a ShellLinkInfo' {
            $returnedShortcut = Get-ADTShortcut -LiteralPath $shellLinkProperties.FilePath
            $returnedShortcut | Should -BeOfType ([PSADT.ShortcutManagement.ShellLinkInfo])
            $returnedShortcut.FilePath | Should -Be $shellLinkProperties.FilePath
            $returnedShortcut.Arguments | Should -Be $shellLinkProperties.Arguments
            $returnedShortcut.Description | Should -Be $shellLinkProperties.Description
            # Validate that the Parse() and ToString() methods of the ShortcutHotkey class don't alter the provided hotkey string
            $shellLinkProperties.Hotkey.ToString() | Should -Be $hotkeyString
            $returnedShortcut.Hotkey | Should -Be $shellLinkProperties.Hotkey
            $returnedShortcut.IconLocation | Should -Be $shellLinkProperties.IconLocation
            $returnedShortcut.IconIndex | Should -Be $shellLinkProperties.IconIndex
            $returnedShortcut.WindowStyle | Should -Be $shellLinkProperties.WindowStyle
            $returnedShortcut.WorkingDirectory | Should -Be $shellLinkProperties.WorkingDirectory
        }
    }

    Context 'Input Validation' {
        It 'Should validate that the path provided to -LiteralPath exists' {
            { Get-ADTShortcut -LiteralPath "$TestDrive\DoesNotExist.lnk" } | Should -Throw -ExceptionType ([System.IO.FileNotFoundException]) -ErrorId 'LiteralPathNotFound,Get-ADTShortcut'
        }
        It 'Should validate that the path provided to -LiteralPath has a valid shortcut extension' {
            { Get-ADTShortcut -LiteralPath "$TestDrive\WrongExtension.txt" } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentValidationError,Get-ADTShortcut'
        }
        It 'Should validate that -LiteralPath is not null, empty, or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Get-ADTShortcut'
            }
            { Get-ADTShortcut -LiteralPath $null } | Should @shouldParams
            { Get-ADTShortcut -LiteralPath '' } | Should @shouldParams
            { Get-ADTShortcut -LiteralPath " `f`n`r`t`v" } | Should @shouldParams
        }
    }
}
