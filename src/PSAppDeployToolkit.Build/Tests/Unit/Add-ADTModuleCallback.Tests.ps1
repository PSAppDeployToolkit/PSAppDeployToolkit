BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Add-ADTModuleCallback' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        AfterEach {
            Clear-ADTModuleCallback -Hookpoint OnExit
            Clear-ADTModuleCallback -Hookpoint PostClose
        }

        It 'Adds a single callback to a hookpoint and Get-ADTModuleCallback returns it' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb = Get-Command -Name 'Get-Process'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            $result | Should -Contain $cb
        }

        It 'Does not duplicate a callback when the same CommandInfo is added twice' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb = Get-Command -Name 'Get-Process'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            ($result | Where-Object { $_ -eq $cb }).Count | Should -Be 1
        }

        It 'Inserts multiple callbacks in reverse order so first supplied is first in list' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb1', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb2', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb1 = Get-Command -Name 'Get-Process'
            $cb2 = Get-Command -Name 'Get-Item'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb1, $cb2
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            $result[0] | Should -Be $cb1
            $result[1] | Should -Be $cb2
        }

        It 'Adds callbacks independently to separate hookpoints without cross-contamination' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb1', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb2', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb1 = Get-Command -Name 'Get-Process'
            $cb2 = Get-Command -Name 'Get-Item'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb1
            Add-ADTModuleCallback -Hookpoint PostClose -Callback $cb2
            $exitResult = Get-ADTModuleCallback -Hookpoint OnExit
            $closeResult = Get-ADTModuleCallback -Hookpoint PostClose
            $exitResult | Should -Contain $cb1
            $exitResult | Should -Not -Contain $cb2
            $closeResult | Should -Contain $cb2
            $closeResult | Should -Not -Contain $cb1
        }

        It 'Produces no output' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb = Get-Command -Name 'Get-Process'
            $output = Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb
            $output | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Throws ParameterArgumentTransformationError when Hookpoint is an invalid enum value' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Add-ADTModuleCallback'
            }
            { Add-ADTModuleCallback -Hookpoint 'NotAValidHookpoint' -Callback (Get-Command -Name 'Get-Process') } | Should @shouldParams
        }

        It 'Throws ParameterArgumentValidationError when Callback is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Add-ADTModuleCallback'
            }
            { Add-ADTModuleCallback -Hookpoint OnExit -Callback $null } | Should @shouldParams
        }

        It 'Throws when Callback array contains duplicate entries (ValidateUnique)' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb = Get-Command -Name 'Get-Process'
            { Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb, $cb } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
