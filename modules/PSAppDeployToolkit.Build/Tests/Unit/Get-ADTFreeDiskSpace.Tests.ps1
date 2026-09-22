BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Get-ADTFreeDiskSpace' {
    Context 'Functionality' {
        It 'Returns an unsigned 64-bit integer' {
            Get-ADTFreeDiskSpace | Should -BeOfType ([System.UInt64])
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
            # Asserted on the parameter default itself, as comparing free space cannot tell two drives apart
            # on a single-drive machine. Get-Command does not surface defaults, so it comes from the AST and
            # is evaluated in the module's scope, where it would resolve at runtime.
            $parameters = (Get-Command -Name Get-ADTFreeDiskSpace).ScriptBlock.Ast.Body.ParamBlock.Parameters
            $default = ($parameters | Where-Object { $_.Name.VariablePath.UserPath.Equals('Drive') }).DefaultValue
            $resolved = & (Get-Module -Name PSAppDeployToolkit) ([System.Management.Automation.ScriptBlock]::Create($default.Extent.Text))
            $resolved | Should -Be ([System.IO.Path]::GetPathRoot([System.Environment]::SystemDirectory))
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
