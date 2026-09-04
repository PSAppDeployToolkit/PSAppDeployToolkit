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
Describe 'Clear-ADTModuleCallback' {
    Context 'Functionality' {
        AfterEach {
            foreach ($name in [System.Enum]::GetNames([PSAppDeployToolkit.Foundation.CallbackType]))
            {
                Clear-ADTModuleCallback -Hookpoint $name
            }
        }

        It 'Empties the hookpoint' {
            Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne, $script:ProbeTwo
            Clear-ADTModuleCallback -Hookpoint OnInit
            Get-ADTModuleCallback -Hookpoint OnInit | Should -BeNullOrEmpty
        }

        It 'Leaves the other hookpoints alone' {
            Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne
            Add-ADTModuleCallback -Hookpoint PostClose -Callback $script:ProbeOne
            Clear-ADTModuleCallback -Hookpoint OnInit
            (Get-ADTModuleCallback -Hookpoint PostClose).Contains($script:ProbeOne) | Should -BeTrue
        }

        It 'Says nothing when the hookpoint is already empty' {
            { Clear-ADTModuleCallback -Hookpoint OnInit } | Should -Not -Throw
        }

        It 'Can be called for <Hookpoint>' -ForEach $script:Hookpoints {
            # Every declared hookpoint has a table entry behind it, so none of them may fail to clear.
            { Clear-ADTModuleCallback -Hookpoint $Hookpoint } | Should -Not -Throw
        }
    }
}
