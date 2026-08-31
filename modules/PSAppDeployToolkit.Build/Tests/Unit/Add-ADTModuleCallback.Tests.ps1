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
Describe 'Add-ADTModuleCallback' {
    Context 'Functionality' {
        AfterEach {
            foreach ($name in [System.Enum]::GetNames([PSAppDeployToolkit.Foundation.CallbackType]))
            {
                Clear-ADTModuleCallback -Hookpoint $name
            }
        }

        It 'Registers a callback against <Hookpoint>' -ForEach $script:Hookpoints {
            Add-ADTModuleCallback -Hookpoint $Hookpoint -Callback $script:ProbeOne
            $registered = Get-ADTModuleCallback -Hookpoint $Hookpoint
            $registered.Contains($script:ProbeOne) | Should -BeTrue
        }

        It 'Registers several callbacks at once, in the order given' {
            Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne, $script:ProbeTwo
            $registered = Get-ADTModuleCallback -Hookpoint OnInit
            $registered.Count | Should -Be 2
            $registered.Name | Should -Be @('Test-ProbeCallbackOne', 'Test-ProbeCallbackTwo')
        }

        It 'Puts a later registration ahead of an earlier one' {
            # Each call inserts at the front, so the most recently registered callback runs first. An
            # extension registering over the top of another is relying on that.
            Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne
            Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeTwo
            (Get-ADTModuleCallback -Hookpoint OnInit).Name | Should -Be @('Test-ProbeCallbackTwo', 'Test-ProbeCallbackOne')
        }

        It 'Does not register the same callback twice' {
            # Registering on every run of a script is normal, so a repeat has to be idempotent rather than
            # queueing the callback up again.
            Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne
            Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne
            (Get-ADTModuleCallback -Hookpoint OnInit).Count | Should -Be 1
        }

        It 'Keeps the hookpoints apart' {
            Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne
            Get-ADTModuleCallback -Hookpoint PreClose | Should -BeNullOrEmpty
        }

        It 'Rejects the same callback listed twice in one call' {
            { Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne, $script:ProbeOne } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Rejects a hookpoint it does not know' {
            { Add-ADTModuleCallback -Hookpoint 'NotAHookpoint' -Callback $script:ProbeOne } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
