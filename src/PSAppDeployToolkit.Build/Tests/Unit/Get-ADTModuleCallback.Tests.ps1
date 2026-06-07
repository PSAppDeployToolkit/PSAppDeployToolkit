BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTModuleCallback' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        AfterEach {
            Clear-ADTModuleCallback -Hookpoint OnExit
            Clear-ADTModuleCallback -Hookpoint PostClose
        }

        It 'Returns a ReadOnlyCollection with Count 0 when no callbacks have been added' {
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            # An empty ReadOnlyCollection is returned (not null) even when there are no callbacks.
            ($null -ne $result) | Should -BeTrue
            # Use -ActualValue to avoid pipeline unrolling that would lose the collection type.
            Should -ActualValue $result -BeOfType ([System.Collections.ObjectModel.ReadOnlyCollection[System.Management.Automation.CommandInfo]])
            $result.Count | Should -Be 0
        }

        It 'Returns a ReadOnlyCollection of CommandInfo after a callback is added' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb = Get-Command -Name 'Get-Process'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            # Use -ActualValue to avoid pipeline unrolling of the single-item collection.
            Should -ActualValue $result -BeOfType ([System.Collections.ObjectModel.ReadOnlyCollection[System.Management.Automation.CommandInfo]])
            $result.Count | Should -Be 1
            $result[0] | Should -Be $cb
        }

        It 'Returns only the callbacks registered for the requested hookpoint' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb1', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb2', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb1 = Get-Command -Name 'Get-Process'
            $cb2 = Get-Command -Name 'Get-Item'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb1
            Add-ADTModuleCallback -Hookpoint PostClose -Callback $cb2
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            $result.Contains($cb1) | Should -BeTrue
            $result.Contains($cb2) | Should -BeFalse
        }

        It 'Returns all callbacks that were added to the hookpoint' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb1', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb2', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb1 = Get-Command -Name 'Get-Process'
            $cb2 = Get-Command -Name 'Get-Item'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb1, $cb2
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            $result.Count | Should -Be 2
            $result.Contains($cb1) | Should -BeTrue
            $result.Contains($cb2) | Should -BeTrue
        }

        It 'Returns an empty collection for a hookpoint after Clear-ADTModuleCallback' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb = Get-Command -Name 'Get-Process'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb
            Clear-ADTModuleCallback -Hookpoint OnExit
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            $result.Count | Should -Be 0
        }

        It 'Output collection is read-only and throws when mutation is attempted' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb = Get-Command -Name 'Get-Process'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            # ReadOnlyCollection<T> does not expose RemoveAt/Insert on its public surface; PowerShell
            # surfaces MethodException/MethodCountCouldNotFindBest when attempting to call them.
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.MethodException]
                ErrorId       = 'MethodCountCouldNotFindBest'
            }
            { $result.RemoveAt(0) } | Should @shouldParams
        }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory Hookpoint parameter' {
            (Get-Command Get-ADTModuleCallback).Parameters['Hookpoint'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws ParameterArgumentTransformationError when Hookpoint is an invalid enum value' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Get-ADTModuleCallback'
            }
            { Get-ADTModuleCallback -Hookpoint 'NotAValidHookpoint' } | Should @shouldParams
        }

        It 'Throws ParameterArgumentTransformationError when Hookpoint is empty string' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Get-ADTModuleCallback'
            }
            { Get-ADTModuleCallback -Hookpoint '' } | Should @shouldParams
        }
    }
}
