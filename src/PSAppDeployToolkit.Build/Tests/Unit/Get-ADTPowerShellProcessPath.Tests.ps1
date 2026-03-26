BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTPowerShellProcessPath' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
    }

    Context 'Return Value' {
        It 'Returns a non-empty string' {
            Get-ADTPowerShellProcessPath | Should -Not -BeNullOrEmpty
        }

        It 'Returns a System.String' {
            Get-ADTPowerShellProcessPath | Should -BeOfType [System.String]
        }

        It 'Returned path points to an existing file' {
            $path = Get-ADTPowerShellProcessPath
            $path | Should -Exist
        }
    }

    Context 'Path Correctness' {
        It 'Returned file is inside $PSHOME' {
            $path = Get-ADTPowerShellProcessPath
            $path | Should -BeLike "$PSHOME*"
        }

        It 'Returns pwsh.exe when running under PowerShell Core' {
            # Tests always run under PowerShell Core in this project.
            if ($PSVersionTable.PSEdition -ne 'Core')
            {
                Set-ItResult -Skipped -Because 'Not running under PowerShell Core'
                return
            }
            Split-Path -Path (Get-ADTPowerShellProcessPath) -Leaf | Should -Be 'pwsh.exe'
        }

        It 'Returns powershell.exe when running under Windows PowerShell' {
            if ($PSVersionTable.PSEdition -ne 'Desktop')
            {
                Set-ItResult -Skipped -Because 'Not running under Windows PowerShell'
                return
            }
            Split-Path -Path (Get-ADTPowerShellProcessPath) -Leaf | Should -Be 'powershell.exe'
        }
    }

    Context 'Determinism' {
        It 'Returns the same path on consecutive calls' {
            $first = Get-ADTPowerShellProcessPath
            $second = Get-ADTPowerShellProcessPath
            $first | Should -Be $second
        }
    }
}
