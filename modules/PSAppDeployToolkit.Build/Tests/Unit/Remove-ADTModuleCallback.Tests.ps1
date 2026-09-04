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
Describe 'Remove-ADTModuleCallback' {
    Context 'Functionality' {
        AfterEach {
            foreach ($name in [System.Enum]::GetNames([PSAppDeployToolkit.Foundation.CallbackType]))
            {
                Clear-ADTModuleCallback -Hookpoint $name
            }
        }

        It 'Removes the callback it is given' {
            Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne
            Remove-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne
            Get-ADTModuleCallback -Hookpoint OnInit | Should -BeNullOrEmpty
        }

        It 'Leaves the others in place' {
            Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne, $script:ProbeTwo
            Remove-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne
            (Get-ADTModuleCallback -Hookpoint OnInit).Name | Should -Be @('Test-ProbeCallbackTwo')
        }

        It 'Leaves the other hookpoints alone' {
            Add-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne
            Add-ADTModuleCallback -Hookpoint PreClose -Callback $script:ProbeOne
            Remove-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne
            (Get-ADTModuleCallback -Hookpoint PreClose).Contains($script:ProbeOne) | Should -BeTrue
        }

        It 'Says nothing when the callback was never registered' {
            # Removing on the way out is normal even where registration failed, so this has to be safe.
            { Remove-ADTModuleCallback -Hookpoint OnInit -Callback $script:ProbeOne } | Should -Not -Throw
        }
    }
}
