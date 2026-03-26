BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Test-ADTNetworkConnection' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Active Ethernet Connection' {
        BeforeAll {
            # InterfaceType must be [uint32] — the code casts to UInt32[] and calls .Contains(),
            # which uses boxed equality; Int32 would not equal UInt32(6).
            Mock -ModuleName PSAppDeployToolkit Get-NetAdapter {
                [PSCustomObject]@{ Status = 'Up'; InterfaceType = [uint32]6 }  # 6 = Ethernet
            }
        }

        It 'Returns $true when an Ethernet adapter is Up' {
            Test-ADTNetworkConnection | Should -BeTrue
        }

        It 'Returns a boolean' {
            Test-ADTNetworkConnection | Should -BeOfType ([System.Boolean])
        }

        It 'Does not throw' {
            { Test-ADTNetworkConnection } | Should -Not -Throw
        }
    }

    Context 'No Active Connection' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-NetAdapter {
                [PSCustomObject]@{ Status = 'Disconnected'; InterfaceType = [uint32]6 }
            }
        }

        It 'Returns $false when no adapter is Up' {
            Test-ADTNetworkConnection | Should -BeFalse
        }
    }

    Context 'Empty Adapter List' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-NetAdapter { }
        }

        It 'Returns $false when no adapters are present' {
            Test-ADTNetworkConnection | Should -BeFalse
        }
    }

    Context 'Custom InterfaceType' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-NetAdapter {
                [PSCustomObject]@{ Status = 'Up'; InterfaceType = [uint32]71 }  # 71 = Wireless80211
            }
        }

        It 'Returns $true when matching Wireless80211 adapter is Up' {
            Test-ADTNetworkConnection -InterfaceType Wireless80211 | Should -BeTrue
        }

        It 'Returns $false when adapter type does not match requested type' {
            # Adapter is Wireless (71) but we request Ethernet (6).
            Test-ADTNetworkConnection -InterfaceType Ethernet | Should -BeFalse
        }
    }

    Context 'Input Validation' {
        It 'Throws when InterfaceType is an empty array' {
            { Test-ADTNetworkConnection -InterfaceType @() } | Should -Throw
        }
    }
}
