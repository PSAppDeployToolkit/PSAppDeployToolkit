BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTFileVersion' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Returns a non-empty FileVersion string for a known system PE' -Skip:(-not (Test-Path "$env:SystemRoot\System32\kernel32.dll")) {
            $result = Get-ADTFileVersion -File "$env:SystemRoot\System32\kernel32.dll"
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType ([System.String])
        }

        It 'Returns a non-empty ProductVersion string when -ProductVersion is specified' -Skip:(-not (Test-Path "$env:SystemRoot\System32\kernel32.dll")) {
            $result = Get-ADTFileVersion -File "$env:SystemRoot\System32\kernel32.dll" -ProductVersion
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType ([System.String])
        }

        It 'Returns a version string matching the known version info for kernel32.dll' -Skip:(-not (Test-Path "$env:SystemRoot\System32\kernel32.dll")) {
            $expected = (Get-Item "$env:SystemRoot\System32\kernel32.dll").VersionInfo.FileVersion.Trim()
            $result = Get-ADTFileVersion -File "$env:SystemRoot\System32\kernel32.dll"
            $result | Should -Be $expected
        }

        It 'Accepts pipeline input for the File parameter' -Skip:(-not (Test-Path "$env:SystemRoot\System32\kernel32.dll")) {
            $result = [System.IO.FileInfo]"$env:SystemRoot\System32\kernel32.dll" | Get-ADTFileVersion
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType ([System.String])
        }
    }

    Context 'Input Validation' {
        It 'Throws with InvalidFileParameterValue when the file does not exist' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.ArgumentException]
                ErrorId = 'InvalidFileParameterValue,Get-ADTFileVersion'
            }
            { Get-ADTFileVersion -File 'C:\NonExistentFile12345.dll' } | Should @shouldParams
        }

        It 'Should verify that File is not null' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Get-ADTFileVersion'
            }
            { Get-ADTFileVersion -File $null } | Should @shouldParams
        }
    }
}
