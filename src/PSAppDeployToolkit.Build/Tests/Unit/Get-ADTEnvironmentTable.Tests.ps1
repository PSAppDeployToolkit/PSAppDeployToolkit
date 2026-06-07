BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTEnvironmentTable' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    # This context runs before the module is initialized so the uninitialized
    # contract is observed against genuinely empty module state.
    Context 'Uninitialized module' {
        It 'Throws an InvalidOperationException when the environment table is not initialized' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.InvalidOperationException]
                ErrorId = 'ADTEnvironmentDatabaseEmpty,Get-ADTEnvironmentTable'
            }
            { Get-ADTEnvironmentTable } | Should @shouldParams
        }
    }

    Context 'Initialized module' {
        BeforeAll {
            Initialize-ADTModule
        }

        It 'Returns an EnvironmentTable object' {
            $result = Get-ADTEnvironmentTable
            $result | Should -BeOfType ([PSAppDeployToolkit.Foundation.EnvironmentTable])
        }

        It 'Exposes the well-known property <Property> with a non-null value' -ForEach @(
            @{ Property = 'EnvComputerName' }
            @{ Property = 'EnvOSName' }
            @{ Property = 'EnvOSVersion' }
            @{ Property = 'EnvUserName' }
            @{ Property = 'EnvSystemDrive' }
            @{ Property = 'EnvWinDir' }
        ) {
            $result = Get-ADTEnvironmentTable
            $result.$Property | Should -Not -BeNullOrEmpty
        }

        It 'Exposes Is64Bit as a Boolean' {
            $result = Get-ADTEnvironmentTable
            $result.Is64Bit | Should -BeOfType ([System.Boolean])
        }

        It 'Exposes IsMachinePartOfDomain as a Boolean' {
            $result = Get-ADTEnvironmentTable
            $result.IsMachinePartOfDomain | Should -BeOfType ([System.Boolean])
        }

        It 'Returns the same singleton reference on consecutive calls' {
            $first = Get-ADTEnvironmentTable
            $second = Get-ADTEnvironmentTable
            [System.Object]::ReferenceEquals($first, $second) | Should -BeTrue
        }
    }

    Context 'Metadata' {
        It 'Declares an OutputType of PSAppDeployToolkit.Foundation.EnvironmentTable' {
            (Get-Command Get-ADTEnvironmentTable).OutputType.Type | Should -Contain ([PSAppDeployToolkit.Foundation.EnvironmentTable])
        }

        It 'Has no public parameters beyond the common ones' {
            $common = [System.Management.Automation.PSCmdlet]::CommonParameters
            (Get-Command Get-ADTEnvironmentTable).Parameters.Keys | Where-Object { $_ -notin $common } | Should -BeNullOrEmpty
        }
    }
}
