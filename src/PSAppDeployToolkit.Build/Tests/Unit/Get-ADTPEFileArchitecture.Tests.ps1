BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTPEFileArchitecture' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Returns a PSADT.Interop.IMAGE_FILE_MACHINE enum value for a known system PE' -Skip:(-not (Test-Path "$env:SystemRoot\System32\kernel32.dll")) {
            $result = Get-ADTPEFileArchitecture -LiteralPath "$env:SystemRoot\System32\kernel32.dll"
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType ([PSADT.Interop.IMAGE_FILE_MACHINE])
        }

        It 'Returns IMAGE_FILE_MACHINE_AMD64 for the 64-bit System32 kernel32.dll' -Skip:(-not (Test-Path "$env:SystemRoot\System32\kernel32.dll")) {
            $result = Get-ADTPEFileArchitecture -LiteralPath "$env:SystemRoot\System32\kernel32.dll"
            $result | Should -Be ([PSADT.Interop.IMAGE_FILE_MACHINE]::IMAGE_FILE_MACHINE_AMD64)
        }

        It 'Returns IMAGE_FILE_MACHINE_I386 for the 32-bit SysWOW64 kernel32.dll' -Skip:(-not (Test-Path "$env:SystemRoot\SysWOW64\kernel32.dll")) {
            $result = Get-ADTPEFileArchitecture -LiteralPath "$env:SystemRoot\SysWOW64\kernel32.dll"
            $result | Should -Be ([PSADT.Interop.IMAGE_FILE_MACHINE]::IMAGE_FILE_MACHINE_I386)
        }

        It 'Returns a FileInfo object with BinaryType note property when -PassThru is specified' -Skip:(-not (Test-Path "$env:SystemRoot\System32\kernel32.dll")) {
            $result = Get-ADTPEFileArchitecture -LiteralPath "$env:SystemRoot\System32\kernel32.dll" -PassThru
            $result | Should -BeOfType ([System.IO.FileInfo])
            $result.BinaryType | Should -Not -BeNullOrEmpty
            $result.BinaryType | Should -BeOfType ([PSADT.Interop.IMAGE_FILE_MACHINE])
            $result.BinaryType | Should -Be ([PSADT.Interop.IMAGE_FILE_MACHINE]::IMAGE_FILE_MACHINE_AMD64)
        }

        It 'Accepts pipeline input via InputObject for a FileInfo object' -Skip:(-not (Test-Path "$env:SystemRoot\System32\kernel32.dll")) {
            $fileInfo = [System.IO.FileInfo]"$env:SystemRoot\System32\kernel32.dll"
            $result = $fileInfo | Get-ADTPEFileArchitecture
            $result | Should -BeOfType ([PSADT.Interop.IMAGE_FILE_MACHINE])
            $result | Should -Be ([PSADT.Interop.IMAGE_FILE_MACHINE]::IMAGE_FILE_MACHINE_AMD64)
        }
    }

    Context 'Input Validation' {
        It 'Should verify that LiteralPath is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Get-ADTPEFileArchitecture'
            }
            { Get-ADTPEFileArchitecture -LiteralPath $null } | Should @shouldParams
            { Get-ADTPEFileArchitecture -LiteralPath '' } | Should @shouldParams
            { Get-ADTPEFileArchitecture -LiteralPath " `f`n`r`t`v" } | Should @shouldParams
        }

        It 'Should verify that Path is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Get-ADTPEFileArchitecture'
            }
            { Get-ADTPEFileArchitecture -Path $null } | Should @shouldParams
            { Get-ADTPEFileArchitecture -Path '' } | Should @shouldParams
            { Get-ADTPEFileArchitecture -Path " `f`n`r`t`v" } | Should @shouldParams
        }
    }
}
