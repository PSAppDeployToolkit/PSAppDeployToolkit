BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Clear-ADTModuleCallback' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        AfterEach {
            Clear-ADTModuleCallback -Hookpoint OnExit
            Clear-ADTModuleCallback -Hookpoint PostClose
        }

        It 'Removes all callbacks from the specified hookpoint' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb1', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb2', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb1 = Get-Command -Name 'Get-Process'
            $cb2 = Get-Command -Name 'Get-Item'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb1, $cb2
            Clear-ADTModuleCallback -Hookpoint OnExit
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            $result.Count | Should -Be 0
        }

        It 'Succeeds silently when called on a hookpoint that is already empty' {
            { Clear-ADTModuleCallback -Hookpoint OnExit } | Should -Not -Throw
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            $result.Count | Should -Be 0
        }

        It 'Only clears the specified hookpoint and leaves others intact' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb1', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb2', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb1 = Get-Command -Name 'Get-Process'
            $cb2 = Get-Command -Name 'Get-Item'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb1
            Add-ADTModuleCallback -Hookpoint PostClose -Callback $cb2
            Clear-ADTModuleCallback -Hookpoint OnExit
            $exitResult = Get-ADTModuleCallback -Hookpoint OnExit
            $closeResult = Get-ADTModuleCallback -Hookpoint PostClose
            $exitResult.Count | Should -Be 0
            $closeResult.Contains($cb2) | Should -BeTrue
        }

        It 'Allows new callbacks to be added after clearing' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb1', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb2', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb1 = Get-Command -Name 'Get-Process'
            $cb2 = Get-Command -Name 'Get-Item'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb1
            Clear-ADTModuleCallback -Hookpoint OnExit
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb2
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            $result.Count | Should -Be 1
            $result.Contains($cb2) | Should -BeTrue
        }

        It 'Produces no output' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb = Get-Command -Name 'Get-Process'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb
            $output = Clear-ADTModuleCallback -Hookpoint OnExit
            $output | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory Hookpoint parameter' {
            (Get-Command Clear-ADTModuleCallback).Parameters['Hookpoint'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws ParameterArgumentTransformationError when Hookpoint is an invalid enum value' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Clear-ADTModuleCallback'
            }
            { Clear-ADTModuleCallback -Hookpoint 'NotAValidHookpoint' } | Should @shouldParams
        }

        It 'Throws ParameterArgumentTransformationError when Hookpoint is empty string' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Clear-ADTModuleCallback'
            }
            { Clear-ADTModuleCallback -Hookpoint '' } | Should @shouldParams
        }
    }
}
