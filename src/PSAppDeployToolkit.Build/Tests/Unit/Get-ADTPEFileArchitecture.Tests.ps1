BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
    # Stable PE fixtures present on every 64-bit Windows installation.
    $script:Exe64 = "$env:SystemRoot\System32\cmd.exe"   # AMD64 on a 64-bit OS
    $script:Exe32 = "$env:SystemRoot\SysWOW64\cmd.exe"   # I386  on a 64-bit OS
}

Describe 'Get-ADTPEFileArchitecture' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Return Type' {
        It 'Returns a PSADT IMAGE_FILE_MACHINE enum value' {
            $result = Get-ADTPEFileArchitecture -LiteralPath $script:Exe64
            $result | Should -BeOfType ([PSADT.Interop.IMAGE_FILE_MACHINE])
        }

        It 'Does not throw for a valid 64-bit executable' {
            { Get-ADTPEFileArchitecture -LiteralPath $script:Exe64 } | Should -Not -Throw
        }
    }

    Context 'Architecture Detection' {
        It 'Detects AMD64 (0x8664) for a 64-bit System32 executable' {
            $result = Get-ADTPEFileArchitecture -LiteralPath $script:Exe64
            [int]$result | Should -Be 0x8664
        }

        It 'Detects I386 (0x014C) for a 32-bit SysWOW64 executable' {
            if (![System.IO.File]::Exists($script:Exe32))
            {
                Set-ItResult -Skipped -Because 'SysWOW64\cmd.exe is not present on this system'
                return
            }
            $result = Get-ADTPEFileArchitecture -LiteralPath $script:Exe32
            [int]$result | Should -Be 0x014C
        }

        It '64-bit and 32-bit executables return different values' {
            if (![System.IO.File]::Exists($script:Exe32))
            {
                Set-ItResult -Skipped -Because 'SysWOW64\cmd.exe is not present on this system'
                return
            }
            $r64 = Get-ADTPEFileArchitecture -LiteralPath $script:Exe64
            $r32 = Get-ADTPEFileArchitecture -LiteralPath $script:Exe32
            $r64 | Should -Not -Be $r32
        }
    }

    Context 'Parameter Sets' {
        It 'Accepts a path via -LiteralPath' {
            $result = Get-ADTPEFileArchitecture -LiteralPath $script:Exe64
            $result | Should -Not -BeNullOrEmpty
        }

        It 'Accepts a path via -Path' {
            $result = Get-ADTPEFileArchitecture -Path $script:Exe64
            $result | Should -Not -BeNullOrEmpty
        }

        It 'Accepts a FileInfo object via -InputObject' {
            $fileInfo = [System.IO.FileInfo]::new($script:Exe64)
            $result = Get-ADTPEFileArchitecture -InputObject $fileInfo
            $result | Should -Not -BeNullOrEmpty
        }

        It '-LiteralPath and -InputObject return the same architecture' {
            $fileInfo = [System.IO.FileInfo]::new($script:Exe64)
            $r1 = Get-ADTPEFileArchitecture -LiteralPath $script:Exe64
            $r2 = Get-ADTPEFileArchitecture -InputObject $fileInfo
            $r1 | Should -Be $r2
        }
    }

    Context '-PassThru' {
        It '-PassThru returns a FileInfo with a BinaryType NoteProperty' {
            $result = Get-ADTPEFileArchitecture -LiteralPath $script:Exe64 -PassThru
            $result | Should -BeOfType [System.IO.FileInfo]
            $result.BinaryType | Should -Not -BeNullOrEmpty
        }

        It '-PassThru BinaryType matches the value returned without -PassThru' {
            $arch = Get-ADTPEFileArchitecture -LiteralPath $script:Exe64
            $withPT = Get-ADTPEFileArchitecture -LiteralPath $script:Exe64 -PassThru
            $withPT.BinaryType | Should -Be $arch
        }
    }
}
