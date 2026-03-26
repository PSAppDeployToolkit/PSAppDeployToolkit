BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    $script:Cmd64 = "$env:SystemRoot\System32\cmd.exe"
    $script:Cmd32 = "$env:SystemRoot\SysWOW64\cmd.exe"
}

Describe 'Get-ADTExecutableInfo' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'LiteralPath parameter set' {
        It 'Does not throw for a known executable (cmd.exe)' {
            { Get-ADTExecutableInfo -LiteralPath $script:Cmd64 } | Should -Not -Throw
        }

        It 'Returns a non-null result for cmd.exe' {
            $result = Get-ADTExecutableInfo -LiteralPath $script:Cmd64
            $result | Should -Not -BeNull
        }

        It 'Returns an object of type PSADT.FileSystem.ExecutableInfo' {
            $result = Get-ADTExecutableInfo -LiteralPath $script:Cmd64
            $result | Should -BeOfType ([PSADT.FileSystem.ExecutableInfo])
        }

        It 'Returns one result for a single file' {
            $result = @(Get-ADTExecutableInfo -LiteralPath $script:Cmd64)
            $result.Count | Should -Be 1
        }

        It 'Returns AMD64 machine type for System32\cmd.exe' {
            $result = Get-ADTExecutableInfo -LiteralPath $script:Cmd64
            $result.Machine | Should -Be ([PSADT.Interop.IMAGE_FILE_MACHINE]::IMAGE_FILE_MACHINE_AMD64)
        }

        It 'Returns I386 machine type for SysWOW64\cmd.exe' {
            if (!(Test-Path -LiteralPath $script:Cmd32))
            {
                Set-ItResult -Skipped -Because 'SysWOW64\cmd.exe not present on this system'
                return
            }
            $result = Get-ADTExecutableInfo -LiteralPath $script:Cmd32
            $result.Machine | Should -Be ([PSADT.Interop.IMAGE_FILE_MACHINE]::IMAGE_FILE_MACHINE_I386)
        }
    }

    Context 'Path parameter set' {
        It 'Does not throw using -Path with a valid executable path' {
            { Get-ADTExecutableInfo -Path $script:Cmd64 } | Should -Not -Throw
        }

        It 'Returns a non-null result using -Path' {
            $result = Get-ADTExecutableInfo -Path $script:Cmd64
            $result | Should -Not -BeNull
        }

        It 'Returns PSADT.FileSystem.ExecutableInfo using -Path' {
            $result = Get-ADTExecutableInfo -Path $script:Cmd64
            $result | Should -BeOfType ([PSADT.FileSystem.ExecutableInfo])
        }
    }

    Context 'InputObject parameter set (pipeline)' {
        It 'Accepts FileInfo via pipeline and does not throw' {
            $fileInfo = Get-Item -LiteralPath $script:Cmd64
            { $fileInfo | Get-ADTExecutableInfo } | Should -Not -Throw
        }

        It 'Accepts FileInfo via pipeline and returns ExecutableInfo' {
            $fileInfo = Get-Item -LiteralPath $script:Cmd64
            $result = $fileInfo | Get-ADTExecutableInfo
            $result | Should -BeOfType ([PSADT.FileSystem.ExecutableInfo])
        }

        It 'Accepts FileInfo via -InputObject and returns ExecutableInfo' {
            $fileInfo = Get-Item -LiteralPath $script:Cmd64
            $result = Get-ADTExecutableInfo -InputObject $fileInfo
            $result | Should -BeOfType ([PSADT.FileSystem.ExecutableInfo])
        }
    }
}
