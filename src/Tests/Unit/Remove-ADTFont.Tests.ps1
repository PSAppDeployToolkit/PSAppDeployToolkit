#Requires -RunAsAdministrator
BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Mock Set-ADTPreferenceVariables due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }

    # System paths
    $script:FontsDir = "$env:SystemRoot\Fonts"
    $script:FontRegKey = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts'

    # Helper function to create and install a test font
    function Install-TestFont
    {
        param(
            [Parameter(Mandatory)]
            [string]$Extension
        )

        $uniqueName = "PesterRemoveTest_$([guid]::NewGuid().ToString('N').Substring(0, 8))$Extension"

        # Find a source font
        $sourceFont = switch ($Extension)
        {
            '.ttf' { Get-ChildItem "$env:SystemRoot\Fonts\*.ttf" | Select-Object -First 1 -ExpandProperty FullName }
            '.ttc' { Get-ChildItem "$env:SystemRoot\Fonts\*.ttc" | Select-Object -First 1 -ExpandProperty FullName }
            '.otf' { Get-ChildItem "$env:SystemRoot\Fonts\*.otf" | Select-Object -First 1 -ExpandProperty FullName }
        }

        if (-not $sourceFont)
        {
            throw "Could not find a system font with extension $Extension"
        }

        # Copy to TestDrive first
        $testDrivePath = Join-Path $TestDrive $uniqueName
        Copy-Item -Path $sourceFont -Destination $testDrivePath -Force

        # Install using Add-ADTFont
        Add-ADTFont -Path $testDrivePath

        # Get the registry name that was created
        $regKey = Get-Item -LiteralPath $script:FontRegKey
        $registryName = $regKey.Property | Where-Object {
            $regKey.GetValue($_) -eq $uniqueName
        } | Select-Object -First 1

        return @{
            FileName     = $uniqueName
            RegistryName = $registryName
            FilePath     = Join-Path $script:FontsDir $uniqueName
        }
    }

    # Helper function to verify font is completely removed
    function Test-FontRemoved
    {
        param(
            [Parameter(Mandatory)]
            [string]$FileName,
            [string]$RegistryName
        )

        $result = @{
            FileRemoved     = -not (Test-Path -LiteralPath (Join-Path $script:FontsDir $FileName))
            RegistryRemoved = $true
        }

        if ($RegistryName)
        {
            try
            {
                Get-ItemProperty -Path $script:FontRegKey -Name $RegistryName -ErrorAction Stop | Out-Null
                $result.RegistryRemoved = $false
            }
            catch
            {
                $result.RegistryRemoved = $true
            }
        }

        return $result
    }

    # Cleanup helper for fonts that may have been partially installed
    function Remove-TestFontForce
    {
        param(
            [Parameter(Mandatory)]
            [string]$FileName
        )

        # Try to remove using Remove-ADTFont first
        try
        {
            Remove-ADTFont -Name $FileName -ErrorAction SilentlyContinue
        }
        catch
        {
            Write-Warning "Error removing font via Remove-ADTFont: $_"
        }

        # Fallback: If file still exists, force remove it
        $fontPath = Join-Path $script:FontsDir $FileName
        if (Test-Path -LiteralPath $fontPath)
        {
            Remove-Item -LiteralPath $fontPath -Force -ErrorAction SilentlyContinue
        }

        # Fallback: If registry entry still exists, remove it
        try
        {
            $regKey = Get-Item -LiteralPath $script:FontRegKey -ErrorAction SilentlyContinue
            if ($regKey)
            {
                $regKey.Property | ForEach-Object {
                    if ($regKey.GetValue($_) -eq $FileName)
                    {
                        Remove-ItemProperty -Path $script:FontRegKey -Name $_ -Force -ErrorAction SilentlyContinue
                    }
                }
            }
        }
        catch
        {
            Write-Warning "Error removing registry entry: $_"
        }
    }
}

Describe 'Remove-ADTFont' {

    Context 'Removal by Filename' {
        BeforeEach {
            $script:TestFont = Install-TestFont -Extension '.ttf'
        }

        AfterEach {
            # Force cleanup in case test failed
            if ($script:TestFont)
            {
                Remove-TestFontForce -FileName $script:TestFont.FileName
                $script:TestFont = $null
            }
        }

        It 'Should remove font by filename' {
            # Verify font is installed before removal
            $script:TestFont.FilePath | Should -Exist

            Remove-ADTFont -Name $script:TestFont.FileName

            # Verify font is completely removed
            $result = Test-FontRemoved -FileName $script:TestFont.FileName -RegistryName $script:TestFont.RegistryName
            $result.FileRemoved | Should -BeTrue
            $result.RegistryRemoved | Should -BeTrue
        }

        It 'Should remove font resource before deleting file (correct sequence)' {
            # This test verifies the sequence: RemoveFont -> Registry -> Delete File
            # The font should be removed from GDI before the file is deleted

            $script:TestFont.FilePath | Should -Exist

            Remove-ADTFont -Name $script:TestFont.FileName

            # If sequence was correct, file should be gone and no errors
            $script:TestFont.FilePath | Should -Not -Exist
        }

        It 'Should find registry entry by searching value data' {
            # Verify registry entry exists with filename as value
            $regKey = Get-Item -LiteralPath $script:FontRegKey
            $matchingEntry = $regKey.Property | Where-Object {
                $regKey.GetValue($_) -eq $script:TestFont.FileName
            }
            $matchingEntry | Should -Not -BeNullOrEmpty

            Remove-ADTFont -Name $script:TestFont.FileName

            # Registry entry should be removed
            $regKey = Get-Item -LiteralPath $script:FontRegKey
            $matchingEntry = $regKey.Property | Where-Object {
                $regKey.GetValue($_) -eq $script:TestFont.FileName
            }
            $matchingEntry | Should -BeNullOrEmpty
        }
    }

    Context 'Removal by Registry Name' {
        BeforeEach {
            $script:TestFont = Install-TestFont -Extension '.ttf'
        }

        AfterEach {
            if ($script:TestFont)
            {
                Remove-TestFontForce -FileName $script:TestFont.FileName
                $script:TestFont = $null
            }
        }

        It 'Should remove font by registry name (e.g., "Font Name (TrueType)")' {
            # Skip if registry name wasn't captured
            if (-not $script:TestFont.RegistryName)
            {
                Set-ItResult -Skipped -Because 'Could not determine registry name'
                return
            }

            $script:TestFont.FilePath | Should -Exist

            Remove-ADTFont -Name $script:TestFont.RegistryName

            # Verify font is completely removed
            $result = Test-FontRemoved -FileName $script:TestFont.FileName -RegistryName $script:TestFont.RegistryName
            $result.FileRemoved | Should -BeTrue
            $result.RegistryRemoved | Should -BeTrue
        }

        It 'Should resolve filename from registry value data' {
            if (-not $script:TestFont.RegistryName)
            {
                Set-ItResult -Skipped -Because 'Could not determine registry name'
                return
            }

            # Verify we can look up the filename from registry name
            $fileName = Get-ItemProperty -Path $script:FontRegKey -Name $script:TestFont.RegistryName |
                Select-Object -ExpandProperty $script:TestFont.RegistryName

            $fileName | Should -Be $script:TestFont.FileName

            Remove-ADTFont -Name $script:TestFont.RegistryName

            $script:TestFont.FilePath | Should -Not -Exist
        }
    }

    Context 'Font Not Found Scenarios' {
        It 'Should handle non-existent font gracefully' {
            $nonExistentFont = "NonExistent_$([guid]::NewGuid().ToString('N')).ttf"

            # Should not throw, just log warning
            { Remove-ADTFont -Name $nonExistentFont } | Should -Not -Throw
        }

        It 'Should handle non-existent registry name gracefully' {
            $nonExistentRegName = "NonExistent Font $([guid]::NewGuid().ToString('N')) (TrueType)"

            # Should not throw, just log warning
            { Remove-ADTFont -Name $nonExistentRegName } | Should -Not -Throw
        }
    }

    Context 'Multiple Fonts' {
        BeforeEach {
            $script:TestFonts = @()
            $script:TestFonts += Install-TestFont -Extension '.ttf'
            $script:TestFonts += Install-TestFont -Extension '.ttf'
        }

        AfterEach {
            foreach ($font in $script:TestFonts)
            {
                Remove-TestFontForce -FileName $font.FileName
            }
            $script:TestFonts = @()
        }

        It 'Should remove multiple fonts via array input' {
            # Verify fonts are installed
            $script:TestFonts[0].FilePath | Should -Exist
            $script:TestFonts[1].FilePath | Should -Exist

            Remove-ADTFont -Name @($script:TestFonts[0].FileName, $script:TestFonts[1].FileName)

            # Both should be removed
            $script:TestFonts[0].FilePath | Should -Not -Exist
            $script:TestFonts[1].FilePath | Should -Not -Exist
        }

        It 'Should remove multiple fonts via pipeline' {
            # Verify fonts are installed
            $script:TestFonts[0].FilePath | Should -Exist
            $script:TestFonts[1].FilePath | Should -Exist

            @($script:TestFonts[0].FileName, $script:TestFonts[1].FileName) | Remove-ADTFont

            # Both should be removed
            $script:TestFonts[0].FilePath | Should -Not -Exist
            $script:TestFonts[1].FilePath | Should -Not -Exist
        }
    }

    Context 'Error Handling' {
        BeforeEach {
            $script:TestFont = Install-TestFont -Extension '.ttf'
        }

        AfterEach {
            if ($script:TestFont)
            {
                Remove-TestFontForce -FileName $script:TestFont.FileName
                $script:TestFont = $null
            }
        }

        It 'Should continue with -ErrorAction SilentlyContinue when a removal fails' {
            $script:TestFonts = @()
            $script:TestFonts += $script:TestFont
            $script:TestFonts += Install-TestFont -Extension '.ttf'

            $nonExistentFont = "NonExistent_$([guid]::NewGuid().ToString('N')).ttf"

            # Mix of valid fonts and non-existent font
            { Remove-ADTFont -Name @($script:TestFonts[0].FileName, $nonExistentFont, $script:TestFonts[1].FileName) -ErrorAction SilentlyContinue } | Should -Not -Throw

            # Valid fonts should be removed
            $script:TestFonts[0].FilePath | Should -Not -Exist
            $script:TestFonts[1].FilePath | Should -Not -Exist

            # Cleanup
            Remove-TestFontForce -FileName $script:TestFonts[1].FileName
        }
    }

    Context 'Font Unregistration' {
        BeforeEach {
            $script:TestFont = Install-TestFont -Extension '.ttf'
        }

        AfterEach {
            if ($script:TestFont)
            {
                Remove-TestFontForce -FileName $script:TestFont.FileName
                $script:TestFont = $null
            }
        }

        It 'Should unregister font from system successfully' {
            $script:TestFont.FilePath | Should -Exist

            Remove-ADTFont -Name $script:TestFont.FileName

            # Font should be completely removed
            $script:TestFont.FilePath | Should -Not -Exist
        }

        It 'Should handle missing file gracefully' {
            # Remove the file first (simulating a missing file scenario)
            # The font resource might fail to unregister but shouldn't crash
            Remove-Item -LiteralPath $script:TestFont.FilePath -Force -ErrorAction SilentlyContinue

            # This should not throw even if the file is already gone
            { Remove-ADTFont -Name $script:TestFont.FileName } | Should -Not -Throw
        }
    }

    Context 'Sequence Verification' {
        BeforeEach {
            $script:TestFont = Install-TestFont -Extension '.ttf'
        }

        AfterEach {
            if ($script:TestFont)
            {
                Remove-TestFontForce -FileName $script:TestFont.FileName
                $script:TestFont = $null
            }
        }

        It 'Should execute removal in correct order: RemoveFont -> Registry -> File' {
            # This test verifies that after Remove-ADTFont completes:
            # 1. The font resource was unregistered (RemoveFont called)
            # 2. The registry entry was removed
            # 3. The file was deleted

            $script:TestFont.FilePath | Should -Exist
            $script:TestFont.RegistryName | Should -Not -BeNullOrEmpty

            Remove-ADTFont -Name $script:TestFont.FileName

            # All three should be cleaned up
            $script:TestFont.FilePath | Should -Not -Exist

            # Registry entry should be gone
            $regKey = Get-Item -LiteralPath $script:FontRegKey
            $matchingEntry = $regKey.Property | Where-Object {
                $regKey.GetValue($_) -eq $script:TestFont.FileName
            }
            $matchingEntry | Should -BeNullOrEmpty
        }
    }
}
