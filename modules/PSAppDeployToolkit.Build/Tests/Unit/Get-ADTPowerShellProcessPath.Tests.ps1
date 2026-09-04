BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'Get-ADTPowerShellProcessPath' {
    Context 'Functionality' {
        It 'Returns a string' {
            Get-ADTPowerShellProcessPath | Should -BeOfType ([System.String])
        }

        It 'Points at a host that exists' {
            Test-Path -LiteralPath (Get-ADTPowerShellProcessPath) -PathType Leaf | Should -BeTrue
        }

        It 'Names the host for this edition' {
            # The function exists to pick between the two hosts, so the result has to follow the edition
            # rather than always naming one of them.
            $expected = if ($PSVersionTable.PSEdition -eq 'Core') { 'pwsh.exe' } else { 'powershell.exe' }
            [System.IO.Path]::GetFileName((Get-ADTPowerShellProcessPath)) | Should -BeExactly $expected
        }

        It 'Names the host actually running this test' {
            [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName | Should -BeExactly (Get-ADTPowerShellProcessPath)
        }

        It 'Returns something that runs' {
            & (Get-ADTPowerShellProcessPath) -NoProfile -NonInteractive -Command 'Write-Output ran' | Should -BeExactly 'ran'
        }
    }
}
