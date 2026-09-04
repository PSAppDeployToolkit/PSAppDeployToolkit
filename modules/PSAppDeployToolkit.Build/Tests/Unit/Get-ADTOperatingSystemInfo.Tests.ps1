BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'Get-ADTOperatingSystemInfo' {
    Context 'Functionality' {
        BeforeAll {
            $script:Info = Get-ADTOperatingSystemInfo
            $script:Cim = Get-CimInstance -ClassName Win32_OperatingSystem
        }

        It 'Returns an OperatingSystemInfo' {
            $script:Info | Should -BeOfType ([PSADT.DeviceManagement.OperatingSystemInfo])
        }

        It 'Names the operating system as Windows reports it' {
            # A prefix rather than an exact match: the name is built from the product name and edition, so
            # it omits the suffixes CIM appends, such as "Insider Preview" on a prerelease build.
            $script:Info.Name | Should -Not -BeNullOrEmpty
            $script:Cim.Caption.Trim() | Should -BeLike "$($script:Info.Name)*"
        }

        It 'Reports the build Windows itself reports' {
            # RtlGetVersion rather than the shimmed GetVersionEx, so the build must match what CIM says
            # rather than whatever compatibility would hand back.
            $script:Info.Version.Build | Should -Be ([System.Environment]::OSVersion.Version.Build)
        }

        It 'Agrees with the process about the architecture' {
            $script:Info.Is64BitOperatingSystem | Should -Be ([System.Environment]::Is64BitOperatingSystem)
        }

        It 'Classifies the product type exactly once' {
            # Workstation, server and domain controller are mutually exclusive, and the toolkit branches on
            # them, so exactly one has to be set.
            @($script:Info.IsWorkstation, $script:Info.IsServer, $script:Info.IsDomainController).Where({ $_ }).Count | Should -Be 1
        }

        It 'Agrees with CIM about which product type it is' {
            # Win32_OperatingSystem.ProductType: 1 workstation, 2 domain controller, 3 server.
            switch ($script:Cim.ProductType)
            {
                1 { $script:Info.IsWorkstation | Should -BeTrue }
                2 { $script:Info.IsDomainController | Should -BeTrue }
                3 { $script:Info.IsServer | Should -BeTrue }
            }
        }

        It 'Fills in the edition and display version' {
            $script:Info.Edition | Should -Not -BeNullOrEmpty
            $script:Info.DisplayVersion | Should -Not -BeNullOrEmpty
        }

        It 'Only claims multi-session on a workstation' {
            # Enterprise multi-session is a workstation edition, so the two cannot disagree.
            if ($script:Info.IsWorkstationEnterpriseMultiSessionOS)
            {
                $script:Info.IsWorkstation | Should -BeTrue
            }
        }
    }
}
