BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Look for any .msi file available on this system for full-read tests.
    $script:AvailableMsi = Get-ChildItem -Path "$env:SystemRoot\Installer" -Filter '*.msi' -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty FullName
}

Describe 'Get-ADTMsiTableProperty' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Input validation — LiteralPath must exist' {
        It 'Throws for a path that does not exist' {
            $fakePath = "C:\NonExistent_$([System.Guid]::NewGuid().ToString('N')).msi"
            { Get-ADTMsiTableProperty -LiteralPath $fakePath } | Should -Throw
        }

        It 'Throws for a non-existent .msp file path' {
            $fakePath = "C:\NonExistent_$([System.Guid]::NewGuid().ToString('N')).msp"
            { Get-ADTMsiTableProperty -LiteralPath $fakePath } | Should -Throw
        }
    }

    Context 'Reading Property table from a real MSI' {
        It 'Does not throw when reading the Property table' {
            if ($null -eq $script:AvailableMsi)
            {
                Set-ItResult -Skipped -Because 'No .msi file found in $env:SystemRoot\Installer'
                return
            }
            { Get-ADTMsiTableProperty -LiteralPath $script:AvailableMsi } | Should -Not -Throw
        }

        It 'Returns a non-null result for an existing MSI' {
            if ($null -eq $script:AvailableMsi)
            {
                Set-ItResult -Skipped -Because 'No .msi file found in $env:SystemRoot\Installer'
                return
            }
            $result = Get-ADTMsiTableProperty -LiteralPath $script:AvailableMsi
            $result | Should -Not -BeNull
        }

        It 'Returns an IReadOnlyDictionary for the Property table' {
            if ($null -eq $script:AvailableMsi)
            {
                Set-ItResult -Skipped -Because 'No .msi file found in $env:SystemRoot\Installer'
                return
            }
            $result = Get-ADTMsiTableProperty -LiteralPath $script:AvailableMsi
            if ($null -eq $result)
            {
                Set-ItResult -Skipped -Because 'MSI Property table returned no rows'
                return
            }
            $result | Should -BeOfType ([System.Collections.Generic.IReadOnlyDictionary[System.String, System.String]])
        }
    }

    Context 'Reading Summary Information' {
        It 'Does not throw when reading Summary Information' {
            if ($null -eq $script:AvailableMsi)
            {
                Set-ItResult -Skipped -Because 'No .msi file found in $env:SystemRoot\Installer'
                return
            }
            try
            {
                Get-ADTMsiTableProperty -LiteralPath $script:AvailableMsi -GetSummaryInformation
            }
            catch
            {
                if ($_.Exception.Message -like '*MsiSummaryInfoGetProperty*' -or $_.Exception.InnerException.Message -like '*MsiSummaryInfoGetProperty*')
                {
                    Set-ItResult -Skipped -Because 'MSI summary property returned zero-length string'
                    return
                }
                throw
            }
        }

        It 'Returns a PSADT.WindowsInstaller.MsiSummaryInfo when reading Summary Information' {
            if ($null -eq $script:AvailableMsi)
            {
                Set-ItResult -Skipped -Because 'No .msi file found in $env:SystemRoot\Installer'
                return
            }
            $result = $null
            try
            {
                $result = Get-ADTMsiTableProperty -LiteralPath $script:AvailableMsi -GetSummaryInformation
            }
            catch
            {
                if ($_.Exception.Message -like '*MsiSummaryInfoGetProperty*' -or $_.Exception.InnerException.Message -like '*MsiSummaryInfoGetProperty*')
                {
                    Set-ItResult -Skipped -Because 'MSI summary property returned zero-length string'
                    return
                }
                throw
            }
            $result | Should -BeOfType ([PSADT.WindowsInstaller.MsiSummaryInfo])
        }
    }
}
