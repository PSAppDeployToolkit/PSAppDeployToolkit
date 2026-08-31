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

        It 'Errors on <Case> rather than reading a machine type out of it' -ForEach @(
            @{ Case = 'a text file' }
            @{ Case = 'an empty file' }
            @{ Case = 'a file with only a DOS header' }
        ) {
            # Without the signature checks the header offset is followed blindly, so whatever bytes sit at
            # the computed position become the answer. A text file yielded 8289, which a caller branching
            # on 32 versus 64-bit would have acted on.
            $path = "$TestDrive\$($Case -replace '\W').bin"
            switch ($Case)
            {
                'a text file' { Set-Content -LiteralPath $path -Value 'this is not a portable executable' }
                'an empty file' { [System.IO.File]::WriteAllBytes($path, [System.Byte[]]::new(0)) }
                'a file with only a DOS header' { [System.IO.File]::WriteAllBytes($path, [System.Byte[]](0x4D, 0x5A) + [System.Byte[]]::new(200)) }
            }
            { Get-ADTPEFileArchitecture -LiteralPath $path -ErrorAction Stop } | Should -Throw -ErrorId 'FileNotPortableExecutable,Get-ADTPEFileArchitecture'
        }

        It 'Errors on a file that does not exist' {
            { Get-ADTPEFileArchitecture -LiteralPath "$TestDrive\missing.exe" -ErrorAction Stop } | Should -Throw
        }

        It 'Rejects the same path twice' {
            { Get-ADTPEFileArchitecture -LiteralPath "$env:SystemRoot\System32\notepad.exe", "$env:SystemRoot\System32\notepad.exe" } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
