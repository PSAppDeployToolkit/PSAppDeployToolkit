BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Get-ADTMsiExitCodeMessage' {
    Context 'Functionality' {
        It 'Returns a string' {
            Get-ADTMsiExitCodeMessage -MsiExitCode 1618 | Should -BeOfType ([System.String])
        }

        It 'Describes exit code <MsiExitCode> as <Symbol>' -ForEach @(
            @{ MsiExitCode = 0; Symbol = 'ERROR_SUCCESS' }
            @{ MsiExitCode = 1603; Symbol = 'ERROR_INSTALL_FAILURE' }
            @{ MsiExitCode = 1605; Symbol = 'ERROR_UNKNOWN_PRODUCT' }
            @{ MsiExitCode = 1618; Symbol = 'ERROR_INSTALL_ALREADY_RUNNING' }
            @{ MsiExitCode = 3010; Symbol = 'ERROR_SUCCESS_REBOOT_REQUIRED' }
        ) {
            # The wording is Windows' own and is localised, so the oracle is the same message Win32 gives
            # for the code, which the function then qualifies with the symbolic name.
            $message = Get-ADTMsiExitCodeMessage -MsiExitCode $MsiExitCode
            $message | Should -BeLike "$([System.ComponentModel.Win32Exception]::new($MsiExitCode).Message.TrimEnd('.'))*"
            $message | Should -BeLike "*($Symbol)."
        }

        It 'Gives different codes different messages' {
            Get-ADTMsiExitCodeMessage -MsiExitCode 1618 | Should -Not -BeExactly (Get-ADTMsiExitCodeMessage -MsiExitCode 1603)
        }

        It 'Still returns something for a code Windows does not know' {
            Get-ADTMsiExitCodeMessage -MsiExitCode 16180339 | Should -Not -BeNullOrEmpty
        }

        It 'Accepts <Case>, which is above the signed range' -ForEach @(
            @{ Case = '0x80070005'; MsiExitCode = 2147942405 }
            @{ Case = '0xC0000005'; MsiExitCode = 3221225477 }
            @{ Case = 'the largest UInt32'; MsiExitCode = 4294967295 }
        ) {
            # -MsiExitCode is declared as a UInt32, so the whole of that range has to work. The underlying
            # method took an Int32, so anything past Int32.MaxValue used to fail with a MethodException.
            # A process can genuinely exit with a code such as 0xC0000005.
            Get-ADTMsiExitCodeMessage -MsiExitCode $MsiExitCode | Should -Not -BeNullOrEmpty
        }
    }
}
