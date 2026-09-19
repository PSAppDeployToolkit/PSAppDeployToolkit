BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Get-ADTModuleDirectory' {
    Context 'Functionality' {
        It 'Reports the directory the module itself was loaded from' {
            # Not to be confused with Get-ADTModuleDirectories, which reports the deployment's directories.
            # This one is the module's own, and is what Initialize-ADTModule falls back to when a caller
            # names no script directory - which is how the shipped config and strings are found.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Expected = (Get-Module -Name PSAppDeployToolkit).ModuleBase } {
                Get-ADTModuleDirectory | Should -BeExactly $Expected
            }
        }

        It 'Answers without the module having been initialized' {
            # It supplies the default for the parameter that initialization is driven by, so it has to
            # answer before there is any state to read.
            Test-ADTModuleInitialized | Should -BeFalse
            InModuleScope -ModuleName PSAppDeployToolkit {
                Get-ADTModuleDirectory | Should -Not -BeNullOrEmpty
            }
        }

        It 'Points at a directory holding the shipped config and strings' {
            # What the fallback is for. A path that resolved elsewhere would leave an uninitialized module
            # unable to find the defaults it ships with.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $directory = Get-ADTModuleDirectory
                Test-Path -LiteralPath $directory -PathType Container | Should -BeTrue
                Test-Path -LiteralPath "$directory\PSAppDeployToolkit.psd1" -PathType Leaf | Should -BeTrue
            }
        }
    }
}
