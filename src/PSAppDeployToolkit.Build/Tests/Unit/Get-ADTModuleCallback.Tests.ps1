BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    function global:Invoke-ADTTestCallback1 { [CmdletBinding()] param() }
    function global:Invoke-ADTTestCallback2 { [CmdletBinding()] param() }
}

AfterAll {
    Remove-Item -Path 'Function:\Invoke-ADTTestCallback1' -ErrorAction SilentlyContinue
    Remove-Item -Path 'Function:\Invoke-ADTTestCallback2' -ErrorAction SilentlyContinue
}

Describe 'Get-ADTModuleCallback' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
    }

    AfterEach {
        Clear-ADTModuleCallback -Hookpoint PostOpen
        Clear-ADTModuleCallback -Hookpoint OnExit
    }

    Context 'Return Value' {
        It 'Does not throw when called' {
            { Get-ADTModuleCallback -Hookpoint PostOpen } | Should -Not -Throw
        }

        It 'Returns a non-null result for an empty hookpoint' {
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            # Use -is rather than piping: an empty IReadOnlyList enumerates to nothing in the pipeline.
            ($null -ne $result) | Should -Be $true
        }

        It 'Returns 0 count for a freshly-cleared hookpoint' {
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result.Count | Should -Be 0
        }
    }

    Context 'Return Type' {
        It 'Returns an IReadOnlyList[CommandInfo]' {
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            # Use -is rather than piping: an empty IReadOnlyList enumerates to nothing in the pipeline.
            ($result -is [System.Collections.Generic.IReadOnlyList[System.Management.Automation.CommandInfo]]) | Should -Be $true
        }

        It 'The runtime type name contains ReadOnly' {
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result.GetType().Name | Should -BeLike '*ReadOnly*'
        }

        It 'The result is read-only: calling Add() throws' {
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $cmd = Get-Command -Name Invoke-ADTTestCallback1
            { $result.Add($cmd) } | Should -Throw
        }
    }

    Context 'Contents After Adding' {
        It 'Returns the callback that was added' {
            $cmd = Get-Command -Name Invoke-ADTTestCallback1
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result | Should -Contain $cmd
        }

        It 'Count reflects the number of added callbacks' {
            $cmd1 = Get-Command -Name Invoke-ADTTestCallback1
            $cmd2 = Get-Command -Name Invoke-ADTTestCallback2
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback @($cmd1, $cmd2)
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result.Count | Should -Be 2
        }
    }

    Context 'Hookpoint Independence' {
        It 'Different hookpoints return independent lists' {
            $cmd1 = Get-Command -Name Invoke-ADTTestCallback1
            $cmd2 = Get-Command -Name Invoke-ADTTestCallback2
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd1
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cmd2

            $postOpenResult = Get-ADTModuleCallback -Hookpoint PostOpen
            $onExitResult = Get-ADTModuleCallback -Hookpoint OnExit

            $postOpenResult | Should -Contain $cmd1
            $postOpenResult | Should -Not -Contain $cmd2
            $onExitResult   | Should -Contain $cmd2
            $onExitResult   | Should -Not -Contain $cmd1
        }
    }
}
