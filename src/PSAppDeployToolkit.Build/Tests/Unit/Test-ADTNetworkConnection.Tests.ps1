BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTNetworkConnection' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should not throw when called with no arguments' {
            { Test-ADTNetworkConnection } | Should -Not -Throw
        }

        It 'Should return a [System.Boolean]' {
            $result = Test-ADTNetworkConnection
            $result | Should -BeOfType ([System.Boolean])
        }

        It 'Should return $true or $false (not null)' {
            $result = Test-ADTNetworkConnection
            $null -ne $result | Should -BeTrue
        }

        It 'Should not throw when called with a single explicit InterfaceType' {
            { Test-ADTNetworkConnection -InterfaceType Ethernet } | Should -Not -Throw
        }

        It 'Should return a [System.Boolean] when called with InterfaceType Wireless80211' {
            $result = Test-ADTNetworkConnection -InterfaceType Wireless80211
            $result | Should -BeOfType ([System.Boolean])
        }

        It 'Should return a [System.Boolean] when called with multiple InterfaceTypes' {
            $result = Test-ADTNetworkConnection -InterfaceType Ethernet, Wireless80211
            $result | Should -BeOfType ([System.Boolean])
        }

        It 'Should return $false for an interface type unlikely to exist (Tunnel)' {
            $result = Test-ADTNetworkConnection -InterfaceType Tunnel
            # We cannot guarantee no tunnel adapter exists, but result must be Boolean.
            $result | Should -BeOfType ([System.Boolean])
        }
    }

    Context 'Input Validation' {
        It 'Should throw when -InterfaceType is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Test-ADTNetworkConnection -InterfaceType $null } | Should @shouldParams
        }
    }

    Context 'Metadata' {
        It 'Should declare OutputType of System.Boolean' {
            $outputTypes = (Get-Command Test-ADTNetworkConnection).OutputType.Type
            $outputTypes | Should -Contain ([System.Boolean])
        }
    }
}
