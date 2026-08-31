BeforeDiscovery {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"

    # Windows keeps a cached copy of every installed package under its Installer directory, which is the
    # only MSI guaranteed to be on hand. Reading it needs elevation, so the tests skip without it rather
    # than shipping an MSI into the repository purely to be read.
    $script:HasMsi = (Test-ADTCallerElevated) -and !!(Get-ChildItem -LiteralPath "$env:SystemRoot\Installer" -Filter '*.msi' -ErrorAction Ignore | Select-Object -First 1)
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Get-ADTMsiTableProperty' -Skip:(!$script:HasMsi) {
    Context 'Functionality' {
        BeforeAll {
            $script:MsiPath = (Get-ChildItem -LiteralPath "$env:SystemRoot\Installer" -Filter '*.msi' | Select-Object -First 1).FullName
            $script:Properties = Get-ADTMsiTableProperty -LiteralPath $script:MsiPath
        }

        It 'Reads the Property table into a read-only dictionary' {
            # A dictionary rather than a PSCustomObject, so a caller can ask ContainsKey rather than having
            # to poke at PSObject.Properties.
            $script:Properties | Should -BeOfType ([System.Collections.Generic.IReadOnlyDictionary[System.String, System.Object]])
            $script:Properties.Count | Should -BeGreaterThan 0
            { $script:Properties.Add('Anything', 'value') } | Should -Throw
        }

        It 'Carries the identifying properties every package has' {
            # ProductCode and ProductName are what the toolkit matches an installed application on, so an
            # MSI without them would be unusable to it.
            $script:Properties.ProductCode | Should -Not -BeNullOrEmpty
            $script:Properties.ProductName | Should -Not -BeNullOrEmpty
        }

        It 'Returns a product code shaped like a GUID' {
            { [System.Guid]::new($script:Properties.ProductCode) } | Should -Not -Throw
        }

        It 'Reads the summary information instead with -GetSummaryInformation' {
            # A different table entirely, so the switch has to change what comes back rather than adding to
            # it. Revision number is where the summary stream keeps the package code.
            $summary = Get-ADTMsiTableProperty -LiteralPath $script:MsiPath -GetSummaryInformation
            $summary | Should -Not -BeNullOrEmpty
            $summary.Keys | Should -Not -Contain 'ProductCode'
        }

        It 'Reads a named table' {
            (Get-ADTMsiTableProperty -LiteralPath $script:MsiPath -Table 'Property').ProductCode | Should -BeExactly $script:Properties.ProductCode
        }

        It 'Rejects a file that is not an MSI' {
            $plain = "$TestDrive\notanmsi.msi"
            Set-Content -LiteralPath $plain -Value 'not a windows installer database'
            { Get-ADTMsiTableProperty -LiteralPath $plain -ErrorAction Stop } | Should -Throw
        }

        It 'Rejects a file that does not exist' {
            { Get-ADTMsiTableProperty -LiteralPath "$TestDrive\missing.msi" } | Should -Throw
        }

        It 'Rejects -Table alongside -GetSummaryInformation' {
            { Get-ADTMsiTableProperty -LiteralPath $script:MsiPath -Table 'Property' -GetSummaryInformation } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
