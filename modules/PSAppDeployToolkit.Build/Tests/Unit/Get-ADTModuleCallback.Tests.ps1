BeforeDiscovery {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Read from the enumeration so a hookpoint added later is covered without this file being touched.
    $script:Hookpoints = foreach ($name in [System.Enum]::GetNames([PSAppDeployToolkit.Foundation.CallbackType]))
    {
        @{ Hookpoint = $name }
    }
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    function Test-ProbeCallbackOne { }
    function Test-ProbeCallbackTwo { }
    $script:ProbeOne = Get-Command Test-ProbeCallbackOne
    $script:ProbeTwo = Get-Command Test-ProbeCallbackTwo
}
Describe 'Get-ADTModuleCallback' {
    Context 'Functionality' {
        AfterEach {
            foreach ($name in [System.Enum]::GetNames([PSAppDeployToolkit.Foundation.CallbackType]))
            {
                Clear-ADTModuleCallback -Hookpoint $name
            }
        }

        It 'Returns nothing for <Hookpoint> when none are registered' -ForEach $script:Hookpoints {
            Get-ADTModuleCallback -Hookpoint $Hookpoint | Should -BeNullOrEmpty
        }

        It 'Returns what was registered' {
            Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne
            $callbacks = Get-ADTModuleCallback -Hookpoint OnInit
            $callbacks.Count | Should -Be 1
            $callbacks[0] | Should -BeOfType ([System.Management.Automation.CommandInfo])
        }

        It 'Returns the collection whole rather than one item at a time' {
            # Written with WriteObject($false) deliberately, so the caller receives a single read-only
            # collection. Wrapping the call in @() would otherwise yield one element, not the callbacks.
            Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne, $script:ProbeTwo
            $callbacks = Get-ADTModuleCallback -Hookpoint OnInit
            # Tested without a pipeline, because piping it is exactly what enumerates it away.
            $callbacks -is [System.Collections.ObjectModel.ReadOnlyCollection[System.Management.Automation.CommandInfo]] | Should -BeTrue
            { $callbacks.Add($script:ProbeOne) } | Should -Throw
        }

        It 'Lists the most recent registration first' {
            Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne
            Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeTwo
            (Get-ADTModuleCallback -Hookpoint OnInit).Name | Should -Be @('Test-ProbeCallbackTwo', 'Test-ProbeCallbackOne')
        }

        It 'Rejects a hookpoint it does not know' {
            { Get-ADTModuleCallback -Hookpoint 'NotAHookpoint' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
