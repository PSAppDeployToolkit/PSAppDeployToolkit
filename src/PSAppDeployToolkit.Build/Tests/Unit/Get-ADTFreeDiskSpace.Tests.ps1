BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTFreeDiskSpace' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should return a Double when called with no parameters (system drive)' {
            $result = Get-ADTFreeDiskSpace
            $result | Should -BeOfType ([System.Double])
        }

        It 'Should return a positive numeric value for the system drive' {
            $result = Get-ADTFreeDiskSpace
            $result | Should -BeGreaterThan 0
        }

        It 'Should return a value in MB (reasonable range 1 MB - 10 TB) for the system drive' {
            $result = Get-ADTFreeDiskSpace
            $result | Should -BeGreaterThan 1
            $result | Should -BeLessThan 10485760  # 10 TB in MB
        }

        It 'Should return a Double when given an explicit drive letter for the system drive' {
            $sysDrive = [System.IO.DriveInfo]([System.IO.Path]::GetPathRoot([System.Environment]::SystemDirectory))
            $result = Get-ADTFreeDiskSpace -Drive $sysDrive
            $result | Should -BeOfType ([System.Double])
            $result | Should -BeGreaterThan 0
        }

        It 'Should accept a DriveInfo object for the C: drive' {
            $driveInfo = [System.IO.DriveInfo]'C'
            $result = Get-ADTFreeDiskSpace -Drive $driveInfo
            $result | Should -BeOfType ([System.Double])
            $result | Should -BeGreaterThan 0
        }

        It 'Should throw when a non-existent drive is specified' {
            # Find a drive letter that does not exist on this system.
            $missingLetter = [char[]]('D'..'Z') | & {
                process
                {
                    if (![System.IO.DriveInfo]::GetDrives().Name.TrimEnd('\').Contains("${_}:"))
                    {
                        return $_
                    }
                }
            } | Select-Object -First 1
            if (!$missingLetter)
            {
                Set-ItResult -Skipped -Because 'No unused drive letter found on this machine.'
                return
            }
            $missingDrive = [System.IO.DriveInfo]([string]$missingLetter)
            { Get-ADTFreeDiskSpace -Drive $missingDrive } | Should -Throw -ExceptionType ([System.ArgumentException]) -ErrorId 'InvalidDriveParameterValue,Get-ADTFreeDiskSpace'
        }
    }

    Context 'Metadata' {
        It 'Should declare -Drive as non-mandatory' {
            $isMandatory = (Get-Command Get-ADTFreeDiskSpace).Parameters['Drive'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory
            $isMandatory | Should -Contain $false
        }

        It 'Should declare OutputType of System.Double' {
            $outputTypes = (Get-Command Get-ADTFreeDiskSpace).OutputType.Type
            $outputTypes | Should -Contain ([System.Double])
        }
    }
}
