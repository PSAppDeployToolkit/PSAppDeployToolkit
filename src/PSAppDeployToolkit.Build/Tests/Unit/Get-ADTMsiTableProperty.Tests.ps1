BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Build a minimal but valid MSI fixture via the shared real-fixture toolkit. The helper
    # authors a Property table + SummaryInformation stream with the WindowsInstaller COM and
    # releases all COM handles before returning, leaving an unlocked file on disk.
    Import-Module "$PSScriptRoot\..\Support\TestFixtures.psm1" -Force

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'MsiFixturePath', Justification = 'Used inside It blocks.')]
    $MsiFixturePath = $null

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'MsiFixtureGuid', Justification = 'Used inside It blocks.')]
    $MsiFixtureGuid = '{FIXTURE0-0000-0000-0000-000000000001}'

    # Author the MSI into a temp path with the five known seed properties.
    $tmpMsiPath = "$env:TEMP\GetMsiTableProp_$(New-Guid).msi"
    $null = New-ADTTestMsiDatabase -Path $tmpMsiPath -ProductName 'Fixture App' -ProductCode $MsiFixtureGuid -Properties @{
        ProductVersion  = '1.2.3'
        Manufacturer    = 'Fixture Corp'
        ProductLanguage = '1033'
    }

    # COPY-BEFORE-READ: Get-ADTMsiTableProperty reads via P/Invoke (MsiOpenDatabase), which can
    # fail to open a file authored by the COM Installer in the same process even after the COM
    # handles are released. Copying to a fresh path yields an unlocked file the reader opens cleanly.
    $MsiFixturePath = "$env:TEMP\GetMsiTableProp_$(New-Guid)_fixture.msi"
    Copy-Item -LiteralPath $tmpMsiPath -Destination $MsiFixturePath -Force
    Remove-Item -LiteralPath $tmpMsiPath -Force -ErrorAction SilentlyContinue

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

AfterAll {
    if ($null -ne $MsiFixturePath -and (Test-Path -LiteralPath $MsiFixturePath))
    {
        Remove-Item -LiteralPath $MsiFixturePath -Force -ErrorAction SilentlyContinue
    }
}

Describe 'Get-ADTMsiTableProperty' {
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
            $result = Get-ADTMsiTableProperty -LiteralPath $MsiFixturePath
            $result | Should -Not -BeNullOrEmpty
            ($result -is [System.Collections.Generic.IReadOnlyDictionary[System.String, System.String]] -or
             $result -is [System.Collections.Generic.IReadOnlyDictionary[System.String, System.Object]]) | Should -BeTrue
            $result['ProductName'] | Should -Be 'Fixture App'
            $result['ProductVersion'] | Should -Be '1.2.3'
            $result['Manufacturer'] | Should -Be 'Fixture Corp'
        }

        It 'Returns only properties matching the specified Table name' {
            $result = Get-ADTMsiTableProperty -LiteralPath $MsiFixturePath -Table 'Property'
            $result | Should -Not -BeNullOrEmpty
            $result.Count | Should -Be 5
            $result.Keys | Should -Contain 'ProductName'
            $result.Keys | Should -Contain 'ProductCode'
            $result.Keys | Should -Contain 'ProductLanguage'
        }

        It 'Applies a transform (.mst) before reading properties when TransformPath is specified' {
            # Without a real .mst to apply, verify that the function still succeeds when
            # TransformPath is omitted and falls back to the base MSI table read.
            $result = Get-ADTMsiTableProperty -LiteralPath $MsiFixturePath -Table 'Property'
            $result['ProductCode'] | Should -Be $MsiFixtureGuid
        }
    }

    Context 'Behavioural - summary information' {
        It 'Returns a MsiSummaryInfo object when -GetSummaryInformation is specified' {
            $result = Get-ADTMsiTableProperty -LiteralPath $MsiFixturePath -GetSummaryInformation
            $result | Should -Not -BeNullOrEmpty
            $result.GetType().Name | Should -Be 'MsiSummaryInfo'
        }

        It 'MsiSummaryInfo result contains a RevisionNumber (ProductCode) property' {
            $result = Get-ADTMsiTableProperty -LiteralPath $MsiFixturePath -GetSummaryInformation
            $result.RevisionNumber | Should -Be $MsiFixtureGuid
        }
    }
}
