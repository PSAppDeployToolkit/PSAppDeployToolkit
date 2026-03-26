BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTFileVersion' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # cmd.exe is a well-known Windows executable that always carries both FileVersion
        # and ProductVersion, making it a reliable test fixture.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'CmdExeInfo', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
        $CmdExeInfo = [System.IO.FileInfo]"$env:SystemRoot\System32\cmd.exe"
    }

    Context 'FileVersion (default)' {
        It 'Returns a non-empty FileVersion string for a versioned executable' {
            $result = Get-ADTFileVersion -File $CmdExeInfo
            $result | Should -Not -BeNullOrEmpty
        }

        It 'Returns a string that resembles a version number' {
            $result = Get-ADTFileVersion -File $CmdExeInfo
            $result | Should -Match '^\d+\.\d+'
        }

        It 'Returns a System.String' {
            $result = Get-ADTFileVersion -File $CmdExeInfo
            $result | Should -BeOfType [System.String]
        }
    }

    Context 'ProductVersion' {
        It 'Returns a non-empty ProductVersion string when -ProductVersion is specified' {
            $result = Get-ADTFileVersion -File $CmdExeInfo -ProductVersion
            $result | Should -Not -BeNullOrEmpty
        }

        It 'Returns a System.String for ProductVersion' {
            $result = Get-ADTFileVersion -File $CmdExeInfo -ProductVersion
            $result | Should -BeOfType [System.String]
        }
    }

    Context 'Input Validation' {
        It 'Throws when the file does not exist' {
            { Get-ADTFileVersion -File ([System.IO.FileInfo]"$TestDrive\NonExistentFile.dll") } | Should -Throw
        }

        It 'Throws when the file has no version info' {
            # A plain text file carries no FileVersion or ProductVersion.
            $noVersionFile = [System.IO.FileInfo](New-Item -Path "$TestDrive\NoVersion-$(New-Guid).txt" -ItemType File -Force).FullName
            { Get-ADTFileVersion -File $noVersionFile } | Should -Throw
        }
    }
}
