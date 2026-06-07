BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTPowerShellProcessPath' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should return a non-empty string' {
            $result = Get-ADTPowerShellProcessPath
            $result | Should -BeOfType ([System.String])
            $result | Should -Not -BeNullOrEmpty
        }

        It 'Should return a path to an existing executable' {
            Get-ADTPowerShellProcessPath | Should -Exist
        }

        It 'Should return a path ending in powershell.exe or pwsh.exe' {
            $result = Get-ADTPowerShellProcessPath
            $result | Should -Match '(powershell\.exe|pwsh\.exe)$'
        }

        It 'Should return a path consistent with the current PSEdition' {
            $result = Get-ADTPowerShellProcessPath
            if ($PSVersionTable.PSEdition -eq 'Core')
            {
                $result | Should -Match 'pwsh\.exe$'
            }
            else
            {
                $result | Should -Match 'powershell\.exe$'
            }
        }

        It 'Should return a path under $PSHOME' {
            $result = Get-ADTPowerShellProcessPath
            $result | Should -BeLike "$PSHOME*"
        }
    }
}
