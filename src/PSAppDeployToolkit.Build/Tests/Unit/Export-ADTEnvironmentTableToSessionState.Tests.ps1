BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Export-ADTEnvironmentTableToSessionState' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # The environment table is only available once the module is initialized.
        Initialize-ADTModule

        # Helper to mint a fresh, isolated [System.Management.Automation.SessionState].
        function Get-IsolatedSessionState
        {
            $tmpMod = New-Module -Name "TmpExportTarget_$([System.Guid]::NewGuid().ToString('N'))" -ScriptBlock {
                function Get-LocalSessionState { $ExecutionContext.SessionState }
            } | Import-Module -PassThru
            $ss = & $tmpMod { $ExecutionContext.SessionState }
            return [PSCustomObject]@{ SessionState = $ss; Module = $tmpMod }
        }
    }

    AfterAll {
        # Leave the module initialized for sibling tests.
        Initialize-ADTModule
    }

    Context 'Exporting variables into a provided SessionState' {
        BeforeEach {
            $script:target = Get-IsolatedSessionState
        }

        AfterEach {
            Remove-Module $script:target.Module -Force -ErrorAction SilentlyContinue
        }

        It 'Creates the AppDeployToolkitName variable in the target SessionState' {
            $script:target.SessionState.PSVariable.GetValue('AppDeployToolkitName', $null) | Should -BeNullOrEmpty
            Export-ADTEnvironmentTableToSessionState -SessionState $script:target.SessionState
            $script:target.SessionState.PSVariable.GetValue('AppDeployToolkitName', $null) | Should -Not -BeNullOrEmpty
        }

        It 'Creates every variable from the environment table in the target SessionState' {
            Export-ADTEnvironmentTableToSessionState -SessionState $script:target.SessionState
            $envTable = Get-ADTEnvironmentTable
            foreach ($prop in $envTable.PSObject.Properties)
            {
                $script:target.SessionState.PSVariable.Get($prop.Name) | Should -Not -BeNullOrEmpty -Because "variable [$($prop.Name)] should have been exported"
            }
        }

        It 'Exports the variable values matching the environment table' {
            Export-ADTEnvironmentTableToSessionState -SessionState $script:target.SessionState
            $envTable = Get-ADTEnvironmentTable
            $script:target.SessionState.PSVariable.GetValue('AppDeployToolkitName') | Should -Be $envTable.AppDeployToolkitName
        }

        It 'Creates the variables as ReadOnly' {
            Export-ADTEnvironmentTableToSessionState -SessionState $script:target.SessionState
            $variable = $script:target.SessionState.PSVariable.Get('AppDeployToolkitName')
            ($variable.Options -band [System.Management.Automation.ScopedItemOptions]::ReadOnly) | Should -Be ([System.Management.Automation.ScopedItemOptions]::ReadOnly)
        }

        It 'Produces no pipeline output' {
            $result = Export-ADTEnvironmentTableToSessionState -SessionState $script:target.SessionState
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Throws ParameterArgumentValidationError when SessionState is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Export-ADTEnvironmentTableToSessionState'
            }
            { Export-ADTEnvironmentTableToSessionState -SessionState $null } | Should @shouldParams
        }

        It 'SessionState parameter is not mandatory' {
            $attrs = (Get-Command Export-ADTEnvironmentTableToSessionState).Parameters['SessionState'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] })
            # Guard: at least one [Parameter()] attribute must exist, otherwise the Mandatory check would
            # trivially pass even if the parameter had no [Parameter()] decoration at all.
            $attrs.Count | Should -BeGreaterThan 0
            $attrs.Mandatory | Should -Not -Contain $true
        }
    }
}
