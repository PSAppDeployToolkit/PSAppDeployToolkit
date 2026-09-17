BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"

    $script:HelperNames = @((Get-Module -Name PSAppDeployToolkit.TestHelpers).ExportedFunctions.Keys)
    $script:BuildModuleNames = @(Get-ChildItem -LiteralPath "$PSScriptRoot\..\..\Private", "$PSScriptRoot\..\..\Public" -Filter *.ps1 | Select-Object -ExpandProperty BaseName)
}

Describe 'PSAppDeployToolkit.TestHelpers' {
    Context 'The names it exports' {
        It 'Shares none of them with the build module' {
            # The build module marks its own functions read-only as it loads and the unit tests run inside
            # its session state, so a helper sharing a name cannot be written: the import fails and takes
            # the discovery of every test file asking for the helpers with it.
            $shared = @($script:HelperNames | & { process { if ($script:BuildModuleNames -contains $_) { return $_ } } })
            $shared | Should -BeNullOrEmpty -Because "the build module defines them too and locks them: [$($shared -join ', ')]"
        }

        It 'Exports something, so the comparison above is not vacuous' {
            $script:HelperNames | Should -Not -BeNullOrEmpty
            $script:BuildModuleNames | Should -Not -BeNullOrEmpty
        }
    }
}
