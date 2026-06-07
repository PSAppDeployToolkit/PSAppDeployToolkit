BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTMsiTableProperty' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory LiteralPath parameter' {
            (Get-Command Get-ADTMsiTableProperty).Parameters['LiteralPath'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Should have a non-mandatory Table parameter' {
            (Get-Command Get-ADTMsiTableProperty).Parameters['Table'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Should have a non-mandatory TransformPath parameter' {
            (Get-Command Get-ADTMsiTableProperty).Parameters['TransformPath'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Should have a mandatory GetSummaryInformation parameter in the SummaryInfo parameter set' {
            (Get-Command Get-ADTMsiTableProperty).Parameters['GetSummaryInformation'].Attributes.Where({
                    $_ -is [System.Management.Automation.ParameterAttribute] -and $_.ParameterSetName -eq 'SummaryInfo'
                }).Mandatory | Should -Contain $true
        }

        It 'Should have a non-mandatory TablePropertyNameColumnNum parameter' {
            (Get-Command Get-ADTMsiTableProperty).Parameters['TablePropertyNameColumnNum'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Should have a non-mandatory TablePropertyValueColumnNum parameter' {
            (Get-Command Get-ADTMsiTableProperty).Parameters['TablePropertyValueColumnNum'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Should accept Path and PSPath as aliases for LiteralPath' {
            $aliases = (Get-Command Get-ADTMsiTableProperty).Parameters['LiteralPath'].Aliases
            $aliases | Should -Contain 'Path'
            $aliases | Should -Contain 'PSPath'
        }

        It 'Should throw when LiteralPath points to a non-existent file' {
            { Get-ADTMsiTableProperty -LiteralPath "$TestDrive\DoesNotExist.msi" } |
                Should -Throw -ExceptionType ([System.ArgumentException])
        }

        It 'Should throw when TransformPath points to a non-existent file' {
            $dummyMsi = "$TestDrive\dummy_gmtp.msi"
            New-Item -Path $dummyMsi -ItemType File -Force | Out-Null
            { Get-ADTMsiTableProperty -LiteralPath $dummyMsi -TransformPath "$TestDrive\NoSuch.mst" } |
                Should -Throw -ExceptionType ([System.ArgumentException])
        }
    }

    Context 'OutputType metadata' {
        It 'Declares IReadOnlyDictionary[String,Object] as an output type' {
            $outputTypes = (Get-Command Get-ADTMsiTableProperty).OutputType.Type
            $outputTypes | Should -Contain ([System.Collections.Generic.IReadOnlyDictionary[System.String, System.Object]])
        }
    }

    Context 'Behavioural - table read' {
        It 'Reads the Property table from a real MSI file and returns a dictionary' {
            Set-ItResult -Skipped -Because 'No MSI fixture is available in the repository for headless WindowsInstaller COM table read.'
        }

        It 'Returns only properties matching the specified Table name' {
            Set-ItResult -Skipped -Because 'No MSI fixture is available in the repository for headless WindowsInstaller COM table read.'
        }

        It 'Applies a transform (.mst) before reading properties when TransformPath is specified' {
            Set-ItResult -Skipped -Because 'No MSI fixture is available in the repository for headless WindowsInstaller COM table read.'
        }
    }

    Context 'Behavioural - summary information' {
        It 'Returns a MsiSummaryInfo object when -GetSummaryInformation is specified' {
            Set-ItResult -Skipped -Because 'No MSI fixture is available in the repository for headless WindowsInstaller COM summary information read.'
        }

        It 'MsiSummaryInfo result contains a RevisionNumber (ProductCode) property' {
            Set-ItResult -Skipped -Because 'No MSI fixture is available in the repository for headless WindowsInstaller COM summary information read.'
        }
    }
}
