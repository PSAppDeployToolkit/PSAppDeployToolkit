BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Remove-ADTModuleCallback' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        AfterEach {
            Clear-ADTModuleCallback -Hookpoint OnExit
            Clear-ADTModuleCallback -Hookpoint PostClose
        }

        It 'Removes a single callback so it no longer appears in Get-ADTModuleCallback' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb = Get-Command -Name 'Get-Process'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb
            Remove-ADTModuleCallback -Hookpoint OnExit -Callback $cb
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            $result.Count | Should -Be 0
        }

        It 'Removes only the specified callback leaving others intact' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb1', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb2', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb1 = Get-Command -Name 'Get-Process'
            $cb2 = Get-Command -Name 'Get-Item'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb1, $cb2
            Remove-ADTModuleCallback -Hookpoint OnExit -Callback $cb1
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            $result.Count | Should -Be 1
            $result.Contains($cb2) | Should -BeTrue
            $result.Contains($cb1) | Should -BeFalse
        }

        It 'Silently succeeds when removing a callback not present in the hookpoint' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb = Get-Command -Name 'Get-Process'
            { Remove-ADTModuleCallback -Hookpoint OnExit -Callback $cb } | Should -Not -Throw
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            $result.Count | Should -Be 0
        }

        It 'Removes multiple callbacks in a single call' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb1', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb2', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb3', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb1 = Get-Command -Name 'Get-Process'
            $cb2 = Get-Command -Name 'Get-Item'
            $cb3 = Get-Command -Name 'Get-ChildItem'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb1, $cb2, $cb3
            Remove-ADTModuleCallback -Hookpoint OnExit -Callback $cb1, $cb3
            $result = Get-ADTModuleCallback -Hookpoint OnExit
            $result.Count | Should -Be 1
            $result.Contains($cb2) | Should -BeTrue
        }

        It 'Does not affect callbacks registered on a different hookpoint' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb = Get-Command -Name 'Get-Process'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb
            Add-ADTModuleCallback -Hookpoint PostClose -Callback $cb
            Remove-ADTModuleCallback -Hookpoint OnExit -Callback $cb
            $exitResult = Get-ADTModuleCallback -Hookpoint OnExit
            $closeResult = Get-ADTModuleCallback -Hookpoint PostClose
            $exitResult.Count | Should -Be 0
            $closeResult.Contains($cb) | Should -BeTrue
        }

        It 'Produces no output' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb = Get-Command -Name 'Get-Process'
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cb
            $output = Remove-ADTModuleCallback -Hookpoint OnExit -Callback $cb
            $output | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Throws ParameterArgumentTransformationError when Hookpoint is an invalid enum value' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Remove-ADTModuleCallback'
            }
            { Remove-ADTModuleCallback -Hookpoint 'NotAValidHookpoint' -Callback (Get-Command -Name 'Get-Process') } | Should @shouldParams
        }

        It 'Throws ParameterArgumentValidationError when Callback is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Remove-ADTModuleCallback'
            }
            { Remove-ADTModuleCallback -Hookpoint OnExit -Callback $null } | Should @shouldParams
        }

        It 'Throws when Callback array contains duplicate entries (ValidateUnique)' {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'cb', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
            $cb = Get-Command -Name 'Get-Process'
            { Remove-ADTModuleCallback -Hookpoint OnExit -Callback $cb, $cb } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
