BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    # The registry calls are mocked rather than pointed at a test key, because the function reads and
    # writes a real HKLM Edge policy value and would otherwise change machine policy to run.
    $script:EdgePolicyKey = 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge'
}

Describe 'Get-ADTEdgeExtensions' {
    Context 'Functionality' {
        It 'Returns the configured extensions as an object' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock Test-ADTRegistryValue { return $true }
                Mock Get-ADTRegistryKey { return '{"abcdef":{"installation_mode":"blocked"}}' }

                $result = Get-ADTEdgeExtensions
                $result.abcdef.installation_mode | Should -BeExactly 'blocked'
            }
        }

        It 'Seeds the policy value and returns an empty object when nothing is configured' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock Test-ADTRegistryValue { return $false }
                Mock Set-ADTRegistryKey { }
                Mock Get-ADTRegistryKey { throw 'should not be read when the value is absent' }

                $result = Get-ADTEdgeExtensions
                $result | Should -BeOfType ([System.Management.Automation.PSCustomObject])
                @($result.PSObject.Properties).Count | Should -Be 0
                Should -Invoke Set-ADTRegistryKey -Times 1 -Exactly
            }
        }

        It 'Seeds a value it can read back' {
            # Whatever the seeding writes is what the next call parses, so a value that comes back as
            # nothing leaves every later caller with nothing to work from.
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock Test-ADTRegistryValue { return $false }
                Mock Set-ADTRegistryKey { }

                $null = Get-ADTEdgeExtensions
                Should -Invoke Set-ADTRegistryKey -Times 1 -Exactly -ParameterFilter { ![System.String]::IsNullOrWhiteSpace($Value) -and ($null -ne ($Value | ConvertFrom-Json)) }
            }
        }

        # A value that is present but carries nothing means the same as no extensions at all, and
        # callers index into what comes back without checking it for null.
        It 'Returns an empty object rather than nothing when the stored value is blank' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock Test-ADTRegistryValue { return $true }
                Mock Get-ADTRegistryKey { return [System.String]::Empty }

                Get-ADTEdgeExtensions | Should -BeOfType ([System.Management.Automation.PSCustomObject])
            }
        }

        It 'Reads the value rather than creating it when one is already there' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock Test-ADTRegistryValue { return $true }
                Mock Get-ADTRegistryKey { return '{}' }
                Mock Set-ADTRegistryKey { }

                $null = Get-ADTEdgeExtensions
                Should -Invoke Set-ADTRegistryKey -Times 0 -Exactly
            }
        }

        It 'Surfaces malformed JSON rather than returning nothing' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock Test-ADTRegistryValue { return $true }
                Mock Get-ADTRegistryKey { return 'not json at all' }

                { Get-ADTEdgeExtensions -ErrorAction Stop } | Should -Throw
            }
        }
    }
}
