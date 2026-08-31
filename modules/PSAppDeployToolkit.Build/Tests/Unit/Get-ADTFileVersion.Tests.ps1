BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    $script:Notepad = "$env:SystemRoot\System32\notepad.exe"
}

Describe 'Get-ADTFileVersion' {
    Context 'Functionality' {
        It 'Returns the file version as a string' {
            $version = Get-ADTFileVersion -File $script:Notepad
            $version | Should -BeOfType ([System.String])
            $version | Should -BeExactly ([System.Diagnostics.FileVersionInfo]::GetVersionInfo($script:Notepad).FileVersion)
        }

        It 'Returns the product version with -ProductVersion' {
            # The two differ on plenty of Windows binaries, which is why the switch exists.
            Get-ADTFileVersion -File $script:Notepad -ProductVersion | Should -BeExactly ([System.Diagnostics.FileVersionInfo]::GetVersionInfo($script:Notepad).ProductVersion)
        }

        It 'Accepts a file from the pipeline' {
            Get-Item -LiteralPath $script:Notepad | Get-ADTFileVersion | Should -Not -BeNullOrEmpty
        }

        It 'Errors on a file that does not exist' {
            { Get-ADTFileVersion -File "$TestDrive\missing.exe" } | Should -Throw
        }

        It 'Errors on a file with no version information' {
            # A plain text file has no version resource, and the function says so rather than returning an
            # empty string a caller might compare against.
            $plain = "$TestDrive\plain.txt"
            Set-Content -LiteralPath $plain -Value 'no version resource here'
            { Get-ADTFileVersion -File $plain -ErrorAction Stop } | Should -Throw
        }
    }
}
