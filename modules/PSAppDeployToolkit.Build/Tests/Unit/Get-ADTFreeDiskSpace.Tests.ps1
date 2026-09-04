BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Get-ADTFreeDiskSpace' {
    Context 'Functionality' {
        It 'Returns a double' {
            Get-ADTFreeDiskSpace | Should -BeOfType ([System.Double])
        }

        It 'Reports the free space in megabytes' {
            # The unit is not obvious from the signature, and a caller comparing against a threshold has to
            # know it. Compared with a tolerance because the free space moves while the test runs.
            $drive = [System.IO.DriveInfo]::new([System.IO.Path]::GetPathRoot([System.Environment]::SystemDirectory))
            $expected = $drive.AvailableFreeSpace / 1MB
            Get-ADTFreeDiskSpace | Should -BeGreaterThan ($expected - 512)
            Get-ADTFreeDiskSpace | Should -BeLessThan ($expected + 512)
        }

        It 'Defaults to the drive Windows is installed on' {
            $systemDrive = [System.IO.Path]::GetPathRoot([System.Environment]::SystemDirectory)
            Get-ADTFreeDiskSpace | Should -Be (Get-ADTFreeDiskSpace -Drive $systemDrive)
        }

        It 'Accepts a drive given as <Case>' -ForEach @(
            @{ Case = 'a letter with a colon'; Value = 'C:' }
            @{ Case = 'a root path'; Value = 'C:\' }
        ) {
            Get-ADTFreeDiskSpace -Drive $Value | Should -BeGreaterThan 0
        }

        It 'Rejects a drive that does not exist' {
            # The parameter's ValidateScript looks at TotalSize, so a letter with no volume behind it is
            # refused rather than reported as zero free space.
            { Get-ADTFreeDiskSpace -Drive 'Q:' } | Should -Throw -ErrorId 'InvalidDriveParameterValue,Get-ADTFreeDiskSpace'
        }
    }
}
