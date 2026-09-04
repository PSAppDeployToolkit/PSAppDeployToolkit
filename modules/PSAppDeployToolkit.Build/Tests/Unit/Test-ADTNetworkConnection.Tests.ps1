BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    function Test-InterfaceIsUp
    {
        param
        (
            [Parameter(Mandatory = $true)]
            [System.Net.NetworkInformation.NetworkInterfaceType[]]$InterfaceType
        )

        foreach ($nic in [System.Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces())
        {
            if (($nic.NetworkInterfaceType -in $InterfaceType) -and ($nic.OperationalStatus -eq [System.Net.NetworkInformation.OperationalStatus]::Up))
            {
                return $true
            }
        }
        return $false
    }
}

Describe 'Test-ADTNetworkConnection' {
    Context 'Functionality' {
        It 'Returns a boolean' {
            Test-ADTNetworkConnection | Should -BeOfType ([System.Boolean])
        }

        It 'Agrees with the interfaces Windows reports' {
            # The default is wired or wireless, so the oracle is the same pair read straight off .NET.
            Test-ADTNetworkConnection | Should -Be (Test-InterfaceIsUp -InterfaceType Ethernet, Wireless80211)
        }

        It 'Narrows to the interface types it is given' {
            Test-ADTNetworkConnection -InterfaceType Ethernet | Should -Be (Test-InterfaceIsUp -InterfaceType Ethernet)
        }

        It 'Reports false for a type this machine has no connected adapter of' {
            # Token ring is a safe stand-in for absent hardware.
            Test-ADTNetworkConnection -InterfaceType TokenRing | Should -BeFalse
        }

        It 'Rejects a repeated interface type' {
            { Test-ADTNetworkConnection -InterfaceType Ethernet, Ethernet } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Rejects an interface type that does not exist' {
            { Test-ADTNetworkConnection -InterfaceType 'NotAnInterfaceType' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
