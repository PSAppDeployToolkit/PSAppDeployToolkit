BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Out-ADTPowerShellEncodedCommand' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
    }

    Context 'Encoding Correctness' {
        It 'Encodes a simple command and decodes back to the original' {
            $command = 'Get-Process'
            $encoded = Out-ADTPowerShellEncodedCommand -Command $command
            $decoded = [System.Text.Encoding]::Unicode.GetString([System.Convert]::FromBase64String($encoded))
            $decoded | Should -Be $command
        }

        It 'Encodes a multi-word command with a parameter and round-trips correctly' {
            $command = 'Get-Service -Name BITS'
            $encoded = Out-ADTPowerShellEncodedCommand -Command $command
            $decoded = [System.Text.Encoding]::Unicode.GetString([System.Convert]::FromBase64String($encoded))
            $decoded | Should -Be $command
        }

        It 'Encodes a command with double-quotes and round-trips correctly' {
            $command = 'Write-Host "Hello, World!" -ForegroundColor Green'
            $encoded = Out-ADTPowerShellEncodedCommand -Command $command
            $decoded = [System.Text.Encoding]::Unicode.GetString([System.Convert]::FromBase64String($encoded))
            $decoded | Should -Be $command
        }

        It 'Encodes a command with a path containing backslashes and round-trips correctly' {
            $command = 'Set-Location "C:\Program Files\App"'
            $encoded = Out-ADTPowerShellEncodedCommand -Command $command
            $decoded = [System.Text.Encoding]::Unicode.GetString([System.Convert]::FromBase64String($encoded))
            $decoded | Should -Be $command
        }

        It 'Preserves leading and trailing whitespace in the encoded command' {
            $command = '  Set-Location "C:\Temp"  '
            $encoded = Out-ADTPowerShellEncodedCommand -Command $command
            $decoded = [System.Text.Encoding]::Unicode.GetString([System.Convert]::FromBase64String($encoded))
            $decoded | Should -Be $command
        }

        It 'Different commands produce different encodings' {
            $a = Out-ADTPowerShellEncodedCommand -Command 'Get-Process'
            $b = Out-ADTPowerShellEncodedCommand -Command 'Get-Service'
            $a | Should -Not -Be $b
        }

        It 'The same command always produces the same encoding (deterministic)' {
            $a = Out-ADTPowerShellEncodedCommand -Command 'Get-Process'
            $b = Out-ADTPowerShellEncodedCommand -Command 'Get-Process'
            $a | Should -Be $b
        }
    }

    Context 'Return Value Type' {
        It 'Returns a System.String' {
            Out-ADTPowerShellEncodedCommand -Command 'Get-Process' | Should -BeOfType [System.String]
        }

        It 'Returns a valid Base64 string (no throw when decoding)' {
            $encoded = Out-ADTPowerShellEncodedCommand -Command 'Get-Process'
            { [System.Convert]::FromBase64String($encoded) } | Should -Not -Throw
        }
    }

    Context 'Unicode Encoding' {
        It 'Uses UTF-16LE (Unicode) encoding — result length is a multiple of 4' {
            # UTF-16LE encodes each character as 2 bytes; Base64 encodes 3 bytes as 4 chars.
            # A valid UTF-16LE Base64 string always has a length that is a multiple of 4.
            $encoded = Out-ADTPowerShellEncodedCommand -Command 'Hello'
            $encoded.Length % 4 | Should -Be 0
        }
    }

    Context 'Input Validation' {
        It 'Throws when Command is null' {
            { Out-ADTPowerShellEncodedCommand -Command $null } | Should -Throw
        }

        It 'Throws when Command is an empty string' {
            { Out-ADTPowerShellEncodedCommand -Command '' } | Should -Throw
        }
    }
}
