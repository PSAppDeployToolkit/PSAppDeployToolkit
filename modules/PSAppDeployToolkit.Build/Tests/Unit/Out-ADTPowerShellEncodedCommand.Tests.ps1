BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'Out-ADTPowerShellEncodedCommand' {
    Context 'Functionality' {
        It 'Round-trips the command it was given' -ForEach @(
            @{ Command = 'Get-Process' }
            @{ Command = 'Write-Host "hello world"; exit 0' }
            @{ Command = 'Write-Output "quotes '' and `"double`" and $dollar"' }
            # Built from code points so the case survives however this file is encoded. Includes a character
            # outside the BMP, which UTF-16 has to carry as a surrogate pair.
            @{ Command = "Write-Output `"$([System.Char]0x00E9)$([System.Char]0x2014)$([System.Char]0x65E5)$([System.Char]::ConvertFromUtf32(0x1F600))`"" }
        ) {
            # -EncodedCommand is specified as base64 of the UTF-16LE bytes, so decoding that way is the
            # oracle rather than a restatement of the implementation.
            $encoded = Out-ADTPowerShellEncodedCommand -Command $Command
            [System.Text.Encoding]::Unicode.GetString([System.Convert]::FromBase64String($encoded)) | Should -BeExactly $Command
        }

        It 'Returns a string' {
            Out-ADTPowerShellEncodedCommand -Command 'Get-Process' | Should -BeOfType ([System.String])
        }

        It 'Produces valid base64' {
            { [System.Convert]::FromBase64String((Out-ADTPowerShellEncodedCommand -Command 'Get-Process')) } | Should -Not -Throw
        }

        It 'Produces something PowerShell itself will run' {
            # The end use of the output: proving the encoding is the one -EncodedCommand expects, not merely
            # one this module can decode again.
            $encoded = Out-ADTPowerShellEncodedCommand -Command 'Write-Output "round-tripped"'
            & (Get-ADTPowerShellProcessPath) -NoProfile -NonInteractive -EncodedCommand $encoded | Should -BeExactly 'round-tripped'
        }
    }
}
