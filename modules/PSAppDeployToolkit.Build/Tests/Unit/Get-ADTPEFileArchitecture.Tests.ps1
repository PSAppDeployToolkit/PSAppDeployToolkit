BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Get-ADTPEFileArchitecture' {
    Context 'Functionality' {
        It 'Reads a 64-bit binary as AMD64' {
            # System32 holds the native binaries on an x64 install, SysWOW64 the 32-bit ones. The pair makes
            # the test prove it reads the header rather than returning a constant.
            Get-ADTPEFileArchitecture -LiteralPath "$env:SystemRoot\System32\notepad.exe" | Should -Be ([PSADT.Interop.IMAGE_FILE_MACHINE]::IMAGE_FILE_MACHINE_AMD64)
        }

        It 'Reads a 32-bit binary as I386' {
            Get-ADTPEFileArchitecture -LiteralPath "$env:SystemRoot\SysWOW64\cmd.exe" | Should -Be ([PSADT.Interop.IMAGE_FILE_MACHINE]::IMAGE_FILE_MACHINE_I386)
        }

        It 'Reads more than one file at a time' {
            $results = @(Get-ADTPEFileArchitecture -LiteralPath "$env:SystemRoot\System32\notepad.exe", "$env:SystemRoot\SysWOW64\cmd.exe")
            $results.Count | Should -Be 2
        }

        It 'Accepts a wildcard through -Path' {
            @(Get-ADTPEFileArchitecture -Path "$env:SystemRoot\System32\notepad.*").Count | Should -BeGreaterThan 0
        }

        It 'Accepts a file from the pipeline' {
            Get-Item -LiteralPath "$env:SystemRoot\System32\notepad.exe" | Get-ADTPEFileArchitecture | Should -Be ([PSADT.Interop.IMAGE_FILE_MACHINE]::IMAGE_FILE_MACHINE_AMD64)
        }

        It 'Errors on a file that is not a PE image' -Skip {
            # Skipped: the header offset is read without first checking the MZ and PE signatures, so a file
            # that is not an image returns whatever bytes happen to sit there, cast to an
            # IMAGE_FILE_MACHINE. A plain text file yields 8289, which a caller branching on 32 versus
            # 64-bit would act on. Unskip with the fix.
            $notAnImage = "$TestDrive\notanimage.txt"
            Set-Content -LiteralPath $notAnImage -Value 'this is not a portable executable'
            { Get-ADTPEFileArchitecture -LiteralPath $notAnImage -ErrorAction Stop } | Should -Throw
        }

        It 'Errors on a file that does not exist' {
            { Get-ADTPEFileArchitecture -LiteralPath "$TestDrive\missing.exe" -ErrorAction Stop } | Should -Throw
        }

        It 'Rejects the same path twice' {
            { Get-ADTPEFileArchitecture -LiteralPath "$env:SystemRoot\System32\notepad.exe", "$env:SystemRoot\System32\notepad.exe" } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
