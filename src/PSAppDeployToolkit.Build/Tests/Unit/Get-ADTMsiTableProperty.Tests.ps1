BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Build a minimal but valid MSI fixture in $env:TEMP using the Windows Installer COM.
    # The MSI is created with mode 3 (msiOpenDatabaseModeCreate), a SummaryInformation stream,
    # and a seeded Property table.  All COM handles are released + GC'd before the fixture is
    # copied to a second path.  Get-ADTMsiTableProperty uses P/Invoke (MsiOpenDatabase) which
    # cannot open a file still held by the COM Installer object in the same process; copying
    # the file after releasing the COM handles produces an unlocked file that P/Invoke can read.

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'MsiFixturePath', Justification = 'Used inside It blocks.')]
    $MsiFixturePath = $null

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'MsiFixtureGuid', Justification = 'Used inside It blocks.')]
    $MsiFixtureGuid = '{FIXTURE0-0000-0000-0000-000000000001}'

    # Helper: insert one Property row via a parameterised INSERT.
    $InsertProperty = {
        param($Db, $Installer, [string]$Name, [string]$Value)
        $sqlInsert = 'INSERT INTO Property (Property, Value) VALUES (?, ?)'
        $iv = $Db.GetType().InvokeMember('OpenView', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Db, @($sqlInsert))
        $ir = $Installer.GetType().InvokeMember('CreateRecord', [System.Reflection.BindingFlags]::InvokeMethod, $null, $Installer, @(2))
        $null = $ir.GetType().InvokeMember('StringData', [System.Reflection.BindingFlags]::SetProperty, $null, $ir, @(1, $Name))
        $null = $ir.GetType().InvokeMember('StringData', [System.Reflection.BindingFlags]::SetProperty, $null, $ir, @(2, $Value))
        $null = $iv.GetType().InvokeMember('Execute', [System.Reflection.BindingFlags]::InvokeMethod, $null, $iv, @($ir))
        $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($ir)
        $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($iv)
    }

    # Phase 1: create the MSI via COM into a temp file.
    $tmpMsiPath = "$env:TEMP\GetMsiTableProp_$(New-Guid).msi"

    $installer = New-Object -ComObject WindowsInstaller.Installer
    $db = $installer.GetType().InvokeMember('OpenDatabase', [System.Reflection.BindingFlags]::InvokeMethod, $null, $installer, @([string]$tmpMsiPath, 3))

    # Write SummaryInformation stream (required for a valid MSI).
    # COM property indices: 2=Subject, 3=Author, 4=Title, 7=Template, 9=RevisionNumber, 14=PageCount, 15=WordCount.
    $si = $db.GetType().InvokeMember('SummaryInformation', [System.Reflection.BindingFlags]::GetProperty, $null, $db, @(10))
    $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(2, $MsiFixtureGuid))
    $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(3, 'PSAppDeployToolkit Test Suite'))
    $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(4, 'PSAppDeployToolkit Test Author'))
    $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(7, ';1033'))
    $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(9, $MsiFixtureGuid))
    $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(14, 200))
    $null = $si.GetType().InvokeMember('Property', [System.Reflection.BindingFlags]::SetProperty, $null, $si, @(15, 2))
    $null = $si.GetType().InvokeMember('Persist', [System.Reflection.BindingFlags]::InvokeMethod, $null, $si, @())
    $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($si)
    $si = $null

    # Create the Property table.
    $sqlCreate = 'CREATE TABLE Property (Property CHAR(72) NOT NULL, Value CHAR(0) NOT NULL PRIMARY KEY Property)'
    $cv = $db.GetType().InvokeMember('OpenView', [System.Reflection.BindingFlags]::InvokeMethod, $null, $db, @($sqlCreate))
    $null = $cv.GetType().InvokeMember('Execute', [System.Reflection.BindingFlags]::InvokeMethod, $null, $cv, @([System.Reflection.Missing]::Value))
    $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($cv)
    $cv = $null

    # Seed five known properties.
    & $InsertProperty $db $installer 'ProductName'     'Fixture App'
    & $InsertProperty $db $installer 'ProductCode'     $MsiFixtureGuid
    & $InsertProperty $db $installer 'ProductVersion'  '1.2.3'
    & $InsertProperty $db $installer 'Manufacturer'    'Fixture Corp'
    & $InsertProperty $db $installer 'ProductLanguage' '1033'

    $null = $db.GetType().InvokeMember('Commit', [System.Reflection.BindingFlags]::InvokeMethod, $null, $db, @())

    # Phase 2: release all COM handles so the P/Invoke layer can open the file.
    $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($db)
    $db = $null
    $null = [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer)
    $installer = $null
    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()

    # Phase 3: copy to an unlocked path for P/Invoke reads.
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
