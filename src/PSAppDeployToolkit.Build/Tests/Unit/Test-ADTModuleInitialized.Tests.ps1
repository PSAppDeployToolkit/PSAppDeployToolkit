BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTModuleInitialized' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Returns a Boolean value without throwing' {
            # Call directly; Should -Not -Throw is then a separate assertion on the type.
            $result = Test-ADTModuleInitialized
            $result | Should -BeOfType ([System.Boolean])
        }

        It 'Returns $true after Initialize-ADTModule succeeds' {
            Initialize-ADTModule
            Test-ADTModuleInitialized | Should -BeTrue
        }

        It 'Produces no output other than a single Boolean' {
            $result = @(Test-ADTModuleInitialized)
            $result.Count | Should -Be 1
            $result[0] | Should -BeOfType ([System.Boolean])
        }

        It 'Is not a terminating operation even before the module is initialized' {
            # Unload and reload to guarantee a fresh, uninitialized state.
            Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
            Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
            Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
            { Test-ADTModuleInitialized } | Should -Not -Throw
        }
    }

    Context 'Metadata' {
        It 'Has no parameters' {
            (Get-Command Test-ADTModuleInitialized).Parameters.Keys |
                Where-Object { $_ -notin [System.Management.Automation.Cmdlet]::CommonParameters } |
                Should -BeNullOrEmpty
        }

        It 'Is exported from the module' {
            (Get-Module PSAppDeployToolkit).ExportedFunctions.Keys | Should -Contain 'Test-ADTModuleInitialized'
        }
    }
}
