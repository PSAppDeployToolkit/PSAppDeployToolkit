BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    function global:Invoke-ADTTestCallback1 { [CmdletBinding()] param() }
    function global:Invoke-ADTTestCallback2 { [CmdletBinding()] param() }
    function global:Invoke-ADTTestCallback3 { [CmdletBinding()] param() }
}

AfterAll {
    Remove-Item -Path 'Function:\Invoke-ADTTestCallback1' -ErrorAction SilentlyContinue
    Remove-Item -Path 'Function:\Invoke-ADTTestCallback2' -ErrorAction SilentlyContinue
    Remove-Item -Path 'Function:\Invoke-ADTTestCallback3' -ErrorAction SilentlyContinue
}

Describe 'Remove-ADTModuleCallback' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
    }

    AfterEach {
        Clear-ADTModuleCallback -Hookpoint PostOpen
    }

    Context 'Removing a Present Callback' {
        It 'Does not throw when removing a callback that was added' {
            $cmd = Get-Command -Name Invoke-ADTTestCallback1
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd
            { Remove-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd } | Should -Not -Throw
        }

        It 'Removed callback no longer appears in the list' {
            $cmd = Get-Command -Name Invoke-ADTTestCallback1
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd
            Remove-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result | Should -Not -Contain $cmd
        }

        It 'Count decreases by one after removing a single callback' {
            $cmd1 = Get-Command -Name Invoke-ADTTestCallback1
            $cmd2 = Get-Command -Name Invoke-ADTTestCallback2
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback @($cmd1, $cmd2)
            $before = (Get-ADTModuleCallback -Hookpoint PostOpen).Count
            Remove-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd1
            $after = (Get-ADTModuleCallback -Hookpoint PostOpen).Count
            $after | Should -Be ($before - 1)
        }
    }

    Context 'Removing a Non-Present Callback' {
        It 'Does not throw when removing a callback that was never added' {
            $cmd = Get-Command -Name Invoke-ADTTestCallback1
            { Remove-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd } | Should -Not -Throw
        }

        It 'List remains unchanged when removing a callback that was never added' {
            $cmd1 = Get-Command -Name Invoke-ADTTestCallback1
            $cmd2 = Get-Command -Name Invoke-ADTTestCallback2
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd1
            Remove-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd2
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result.Count | Should -Be 1
            $result | Should -Contain $cmd1
        }
    }

    Context 'Selective Removal' {
        It 'Removing one callback leaves the others intact' {
            $cmd1 = Get-Command -Name Invoke-ADTTestCallback1
            $cmd2 = Get-Command -Name Invoke-ADTTestCallback2
            $cmd3 = Get-Command -Name Invoke-ADTTestCallback3
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback @($cmd1, $cmd2, $cmd3)
            Remove-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd2
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result | Should -Contain $cmd1
            $result | Should -Not -Contain $cmd2
            $result | Should -Contain $cmd3
        }

        It 'Removing multiple callbacks at once removes all of them' {
            $cmd1 = Get-Command -Name Invoke-ADTTestCallback1
            $cmd2 = Get-Command -Name Invoke-ADTTestCallback2
            $cmd3 = Get-Command -Name Invoke-ADTTestCallback3
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback @($cmd1, $cmd2, $cmd3)
            Remove-ADTModuleCallback -Hookpoint PostOpen -Callback @($cmd1, $cmd3)
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result | Should -Not -Contain $cmd1
            $result | Should -Contain $cmd2
            $result | Should -Not -Contain $cmd3
        }

        It 'Count is correct after removing multiple callbacks' {
            $cmd1 = Get-Command -Name Invoke-ADTTestCallback1
            $cmd2 = Get-Command -Name Invoke-ADTTestCallback2
            $cmd3 = Get-Command -Name Invoke-ADTTestCallback3
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback @($cmd1, $cmd2, $cmd3)
            Remove-ADTModuleCallback -Hookpoint PostOpen -Callback @($cmd1, $cmd2)
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result.Count | Should -Be 1
        }
    }
}
