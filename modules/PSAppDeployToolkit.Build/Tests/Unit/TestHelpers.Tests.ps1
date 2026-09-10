BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"

    $script:HelperNames = @((Get-Module -Name PSAppDeployToolkit.TestHelpers).ExportedFunctions.Keys)
    $script:BuildModuleNames = @(Get-ChildItem -LiteralPath "$PSScriptRoot\..\..\Private", "$PSScriptRoot\..\..\Public" -Filter *.ps1 | Select-Object -ExpandProperty BaseName)
}

Describe 'PSAppDeployToolkit.TestHelpers' {
    Context 'The names it exports' {
        It 'Shares none of them with the build module' {
            # The build module marks every one of its own functions read-only as it loads, and the unit
            # tests run from inside its session state, so a helper sharing a name with one of them cannot
            # be written at all: the import fails, and with it the discovery of every test file that asks
            # for the helpers. That is a whole suite gone rather than one test, and it does not show up
            # when Pester is run directly, because nothing has loaded the build module then.
            $shared = @($script:HelperNames | & { process { if ($script:BuildModuleNames -contains $_) { return $_ } } })
            $shared | Should -BeNullOrEmpty -Because "the build module defines them too and locks them: [$($shared -join ', ')]"
        }

        It 'Exports something, so the comparison above is not vacuous' {
            $script:HelperNames | Should -Not -BeNullOrEmpty
            $script:BuildModuleNames | Should -Not -BeNullOrEmpty
        }
    }
}
