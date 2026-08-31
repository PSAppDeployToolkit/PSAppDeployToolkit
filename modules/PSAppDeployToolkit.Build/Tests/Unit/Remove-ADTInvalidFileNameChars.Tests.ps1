BeforeDiscovery {
    # One case per character .NET considers invalid, so the set is read from the framework rather than
    # listed here. Each is embedded between two letters: a character that is white space on its own would
    # be rejected by the parameter's validator before the function ever saw it.
    $script:InvalidCharacters = foreach ($character in [System.IO.Path]::GetInvalidFileNameChars())
    {
        @{ Name = "a$([System.String]$character)b"; CodePoint = '0x{0:X2}' -f [System.Int32]$character }
    }
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'Remove-ADTInvalidFileNameChars' {
    Context 'Functionality' {
        It 'Finds invalid characters to check' -ForEach @{ Found = $script:InvalidCharacters.Count } {
            $Found | Should -BeGreaterThan 0
        }

        It 'Removes the character at <CodePoint>' -ForEach $script:InvalidCharacters {
            Remove-ADTInvalidFileNameChars -Name $Name | Should -BeExactly 'ab'
        }

        It 'Leaves a name that is already valid alone' {
            Remove-ADTInvalidFileNameChars -Name 'Already Valid Name.txt' | Should -BeExactly 'Already Valid Name.txt'
        }

        It 'Trims surrounding white space' {
            Remove-ADTInvalidFileNameChars -Name '  padded  ' | Should -BeExactly 'padded'
        }

        It 'Returns an empty string when nothing survives' {
            # Documented in the function's notes, and worth pinning: callers have to handle an empty result.
            $allInvalid = [System.String]::Join([System.String]::Empty, [System.IO.Path]::GetInvalidFileNameChars())
            Remove-ADTInvalidFileNameChars -Name $allInvalid | Should -BeExactly ([System.String]::Empty)
        }

        It 'Returns a string' {
            Remove-ADTInvalidFileNameChars -Name 'name' | Should -BeOfType ([System.String])
        }

        It 'Accepts pipeline input by value' {
            'a<b', 'c>d' | Remove-ADTInvalidFileNameChars | Should -Be @('ab', 'cd')
        }

        It 'Accepts pipeline input by property name' {
            [PSCustomObject]@{ Name = 'a|b' } | Remove-ADTInvalidFileNameChars | Should -BeExactly 'ab'
        }

        It 'Strips directory separators, so it cannot be used on a whole path' {
            # Called out in the function's notes. Both separators and the drive colon are invalid file name
            # characters, so a path passed here silently comes back as something else entirely.
            Remove-ADTInvalidFileNameChars -Name 'C:\Windows\Temp' | Should -BeExactly 'CWindowsTemp'
        }
    }
}
