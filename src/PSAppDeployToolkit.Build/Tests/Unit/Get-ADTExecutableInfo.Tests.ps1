BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTExecutableInfo' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Returns a PSADT.FileSystem.ExecutableInfo object for a known system PE' -Skip:(-not (Test-Path "$env:SystemRoot\System32\kernel32.dll")) {
            $result = Get-ADTExecutableInfo -LiteralPath "$env:SystemRoot\System32\kernel32.dll"
            $result | Should -Not -BeNullOrEmpty
            $result | Should -BeOfType ([PSADT.FileSystem.ExecutableInfo])
        }

        It 'Returns an object with a populated Machine property' -Skip:(-not (Test-Path "$env:SystemRoot\System32\kernel32.dll")) {
            $result = Get-ADTExecutableInfo -LiteralPath "$env:SystemRoot\System32\kernel32.dll"
            $result.Machine | Should -Not -BeNullOrEmpty
            $result.Machine | Should -BeOfType ([PSADT.Interop.IMAGE_FILE_MACHINE])
        }

        It 'Returns IMAGE_SUBSYSTEM_WINDOWS_CUI subsystem for kernel32.dll' -Skip:(-not (Test-Path "$env:SystemRoot\System32\kernel32.dll")) {
            $result = Get-ADTExecutableInfo -LiteralPath "$env:SystemRoot\System32\kernel32.dll"
            $result.Subsystem | Should -BeOfType ([PSADT.Interop.IMAGE_SUBSYSTEM])
            $result.Subsystem | Should -Be ([PSADT.Interop.IMAGE_SUBSYSTEM]::IMAGE_SUBSYSTEM_WINDOWS_CUI)
        }

        It 'Returns an object with a populated FileInfo property pointing to the target file' -Skip:(-not (Test-Path "$env:SystemRoot\System32\kernel32.dll")) {
            $result = Get-ADTExecutableInfo -LiteralPath "$env:SystemRoot\System32\kernel32.dll"
            $result.FileInfo | Should -Not -BeNullOrEmpty
            $result.FileInfo | Should -BeOfType ([System.IO.FileInfo])
            $result.FileInfo.Name | Should -Be 'kernel32.dll'
        }

        It 'Identifies kernel32.dll as a non-.NET executable' -Skip:(-not (Test-Path "$env:SystemRoot\System32\kernel32.dll")) {
            $result = Get-ADTExecutableInfo -LiteralPath "$env:SystemRoot\System32\kernel32.dll"
            $result.IsDotNetExecutable | Should -BeFalse
        }

        It 'Returns AMD64 machine type for the 64-bit system kernel32.dll' -Skip:(-not (Test-Path "$env:SystemRoot\System32\kernel32.dll")) {
            $result = Get-ADTExecutableInfo -LiteralPath "$env:SystemRoot\System32\kernel32.dll"
            $result.Machine | Should -Be ([PSADT.Interop.IMAGE_FILE_MACHINE]::IMAGE_FILE_MACHINE_AMD64)
        }

        It 'Accepts pipeline input via InputObject for a FileInfo object' -Skip:(-not (Test-Path "$env:SystemRoot\System32\kernel32.dll")) {
            $fileInfo = [System.IO.FileInfo]"$env:SystemRoot\System32\kernel32.dll"
            $result = $fileInfo | Get-ADTExecutableInfo
            $result | Should -BeOfType ([PSADT.FileSystem.ExecutableInfo])
        }
    }

    Context 'Input Validation' {
        It 'Should verify that LiteralPath is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Get-ADTExecutableInfo'
            }
            { Get-ADTExecutableInfo -LiteralPath $null } | Should @shouldParams
            { Get-ADTExecutableInfo -LiteralPath '' } | Should @shouldParams
            { Get-ADTExecutableInfo -LiteralPath " `f`n`r`t`v" } | Should @shouldParams
        }

        It 'Should verify that Path is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Get-ADTExecutableInfo'
            }
            { Get-ADTExecutableInfo -Path $null } | Should @shouldParams
            { Get-ADTExecutableInfo -Path '' } | Should @shouldParams
            { Get-ADTExecutableInfo -Path " `f`n`r`t`v" } | Should @shouldParams
        }
    }
}
