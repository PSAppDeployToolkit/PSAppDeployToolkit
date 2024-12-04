BeforeAll {
    Set-Location -Path $PSScriptRoot
    $ModuleName = 'PSAppDeployToolkit'
    $PathToManifest = [System.IO.Path]::Combine('..', '..', $ModuleName, "$ModuleName.psd1")
    Get-Module $ModuleName -ErrorAction SilentlyContinue | Remove-Module -Force
    Import-Module $PathToManifest -Force
    $manifestContent = Test-ModuleManifest -Path $PathToManifest
    $moduleExported = Get-Command -Module $ModuleName | Select-Object -ExpandProperty Name
    $manifestExported = ($manifestContent.ExportedFunctions).Keys
}
BeforeDiscovery {
    Set-Location -Path $PSScriptRoot
    $ModuleName = 'PSAppDeployToolkit'
    $PathToManifest = [System.IO.Path]::Combine('..', '..', $ModuleName, "$ModuleName.psd1")
    $manifestContent = Test-ModuleManifest -Path $PathToManifest
    $moduleExported = Get-Command -Module $ModuleName | Select-Object -ExpandProperty Name
    $manifestExported = ($manifestContent.ExportedFunctions).Keys
}
Describe $ModuleName {

    Context 'Exported Commands' -Fixture {

        Context 'Number of commands' -Fixture {

            It 'Exports the same number of public functions as what is listed in the Module Manifest' {
                ($manifestExported | Measure-Object).Count | Should -BeExactly ($moduleExported | Measure-Object).Count
            }

        }

        Context 'Explicitly exported commands' {

            It 'Includes <_> in the Module Manifest ExportedFunctions' -ForEach $moduleExported {
                $manifestExported -contains $_ | Should -BeTrue
            }

        }
    } #context_ExportedCommands

    Context 'Command Help' -Fixture {
        Context '<_>' -Foreach $moduleExported {

            BeforeEach {
                $help = Get-Help -Name $_ -Full
            }

            It -Name 'Includes a Synopsis' -Test {
                $help.Synopsis | Should -Not -BeNullOrEmpty
            }

            It -Name 'Includes a Description' -Test {
                $help.description.Text | Should -Not -BeNullOrEmpty
            }

            It -Name 'Includes an Example' -Test {
                $help.examples.example | Should -Not -BeNullOrEmpty
            }
        }
    } #context_CommandHelp
}

