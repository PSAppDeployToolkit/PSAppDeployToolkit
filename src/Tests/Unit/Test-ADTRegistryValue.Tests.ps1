BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTRegistryValue' {
    BeforeAll {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestRegistry', Justification = 'This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.')]
        $TestRegistry = (New-Item -Path 'TestRegistry:\TestLocation' -ItemType Directory).PSPath
        New-ItemProperty -LiteralPath $TestRegistry -Name 'Test' -Value 0 -PropertyType DWord | Out-Null

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should return $true' {
            Test-ADTRegistryValue -Key $TestRegistry -Name 'Test' | Should -BeTrue
        }
        It 'Should return $false' {
            Test-ADTRegistryValue -Key $TestRegistry -Name 'DoesNotExist' | Should -BeFalse
        }
    }

    Context 'Input Validation' {
        It 'Should verify that Key is not null, empty or whitespace' {
            { Test-ADTRegistryValue -Key $null -Name 'Anything' } | Should -Throw
            { Test-ADTRegistryValue -Key '' -Name 'Anything' } | Should -Throw
            { Test-ADTRegistryValue -Key ' ' -Name 'Anything' } | Should -Throw
        }
        It 'Should verify that Name is not null, empty or whitespace' {
            { Test-ADTRegistryValue -Key 'Anything' -Name $null } | Should -Throw
            { Test-ADTRegistryValue -Key 'Anything' -Name '' } | Should -Throw
            { Test-ADTRegistryValue -Key 'Anything' -Name ' ' } | Should -Throw
        }
        It 'Should verify that SID is not null or empty' {
            { Test-ADTRegistryValue -Key 'Anything' -Name 'Test' -SID $null } | Should -Throw
            { Test-ADTRegistryValue -Key 'Anything' -Name 'Test' -SID '' } | Should -Throw
            { Test-ADTRegistryValue -Key 'Anything' -Name 'Test' -SID ' ' } | Should -Throw
        }
    }
}
