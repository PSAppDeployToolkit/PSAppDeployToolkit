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

Describe 'Clear-ADTModuleCallback' {
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

    Context 'Clearing an Empty Hookpoint' {
        It 'Does not throw when clearing an already-empty hookpoint' {
            { Clear-ADTModuleCallback -Hookpoint PostOpen } | Should -Not -Throw
        }

        It 'Count remains 0 after clearing an already-empty hookpoint' {
            Clear-ADTModuleCallback -Hookpoint PostOpen
            (Get-ADTModuleCallback -Hookpoint PostOpen).Count | Should -Be 0
        }
    }

    Context 'Clearing a Populated Hookpoint' {
        It 'Does not throw when clearing a hookpoint that has callbacks' {
            $cmd = Get-Command -Name Invoke-ADTTestCallback1
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd
            { Clear-ADTModuleCallback -Hookpoint PostOpen } | Should -Not -Throw
        }

        It 'Get-ADTModuleCallback returns an empty list after Clear' {
            $cmd = Get-Command -Name Invoke-ADTTestCallback1
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd
            Clear-ADTModuleCallback -Hookpoint PostOpen
            (Get-ADTModuleCallback -Hookpoint PostOpen).Count | Should -Be 0
        }

        It 'No callbacks remain after clearing multiple that were added' {
            $cmd1 = Get-Command -Name Invoke-ADTTestCallback1
            $cmd2 = Get-Command -Name Invoke-ADTTestCallback2
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback @($cmd1, $cmd2)
            Clear-ADTModuleCallback -Hookpoint PostOpen
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result | Should -Not -Contain $cmd1
            $result | Should -Not -Contain $cmd2
        }
    }

    Context 'Hookpoint Independence' {
        It 'Clearing one hookpoint does not affect another hookpoint' {
            $cmd1 = Get-Command -Name Invoke-ADTTestCallback1
            $cmd2 = Get-Command -Name Invoke-ADTTestCallback2
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd1
            Add-ADTModuleCallback -Hookpoint OnExit -Callback $cmd2
            Clear-ADTModuleCallback -Hookpoint PostOpen
            $onExitResult = Get-ADTModuleCallback -Hookpoint OnExit
            $onExitResult | Should -Contain $cmd2
            $onExitResult.Count | Should -Be 1
        }
    }

    Context 'Re-use After Clear' {
        It 'Can add callbacks again after clearing' {
            $cmd1 = Get-Command -Name Invoke-ADTTestCallback1
            $cmd2 = Get-Command -Name Invoke-ADTTestCallback2
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd1
            Clear-ADTModuleCallback -Hookpoint PostOpen
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd2
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result.Count | Should -Be 1
            $result | Should -Contain $cmd2
            $result | Should -Not -Contain $cmd1
        }
    }
}
