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

Describe 'Add-ADTModuleCallback' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
    }

    AfterEach {
        Clear-ADTModuleCallback -Hookpoint PostOpen
    }

    Context 'Basic Addition' {
        It 'Does not throw when adding a valid callback to a valid hookpoint' {
            $cmd = Get-Command -Name Invoke-ADTTestCallback1
            { Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd } | Should -Not -Throw
        }

        It 'Added callback appears in Get-ADTModuleCallback result' {
            $cmd = Get-Command -Name Invoke-ADTTestCallback1
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result | Should -Contain $cmd
        }

        It 'Count increases by one after adding a single callback' {
            $cmd = Get-Command -Name Invoke-ADTTestCallback1
            $before = (Get-ADTModuleCallback -Hookpoint PostOpen).Count
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd
            $after = (Get-ADTModuleCallback -Hookpoint PostOpen).Count
            $after | Should -Be ($before + 1)
        }
    }

    Context 'Duplicate Prevention' {
        It 'Adding the same callback twice does not create a duplicate entry' {
            $cmd = Get-Command -Name Invoke-ADTTestCallback1
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result.Count | Should -Be 1
        }

        It 'Adding the same callback twice does not throw' {
            $cmd = Get-Command -Name Invoke-ADTTestCallback1
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd
            { Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd } | Should -Not -Throw
        }
    }

    Context 'Multiple Callbacks' {
        It 'Adding multiple callbacks at once: all appear in the list' {
            $cmd1 = Get-Command -Name Invoke-ADTTestCallback1
            $cmd2 = Get-Command -Name Invoke-ADTTestCallback2
            $cmd3 = Get-Command -Name Invoke-ADTTestCallback3
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback @($cmd1, $cmd2, $cmd3)
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result | Should -Contain $cmd1
            $result | Should -Contain $cmd2
            $result | Should -Contain $cmd3
        }

        It 'Adding multiple callbacks at once preserves natural order' {
            $cmd1 = Get-Command -Name Invoke-ADTTestCallback1
            $cmd2 = Get-Command -Name Invoke-ADTTestCallback2
            $cmd3 = Get-Command -Name Invoke-ADTTestCallback3
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback @($cmd1, $cmd2, $cmd3)
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result[0] | Should -Be $cmd1
            $result[1] | Should -Be $cmd2
            $result[2] | Should -Be $cmd3
        }

        It 'Count equals the number of distinct callbacks added' {
            $cmd1 = Get-Command -Name Invoke-ADTTestCallback1
            $cmd2 = Get-Command -Name Invoke-ADTTestCallback2
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback @($cmd1, $cmd2)
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $result.Count | Should -Be 2
        }
    }

    Context 'Read-Only Result' {
        It 'The list returned by Get-ADTModuleCallback is read-only and throws on Add()' {
            $cmd = Get-Command -Name Invoke-ADTTestCallback1
            Add-ADTModuleCallback -Hookpoint PostOpen -Callback $cmd
            $result = Get-ADTModuleCallback -Hookpoint PostOpen
            $cmd2 = Get-Command -Name Invoke-ADTTestCallback2
            { $result.Add($cmd2) } | Should -Throw
        }
    }
}
