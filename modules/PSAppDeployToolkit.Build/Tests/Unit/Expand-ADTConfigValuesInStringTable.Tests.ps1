BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    $script:TestConfig = @{
        Toolkit = @{ CompanyName = 'Contoso'; LogPath = 'C:\Logs' }
        UI = @{ DefaultTimeout = 3300 }
    }
}

Describe 'Expand-ADTConfigValuesInStringTable' {
    Context 'Substitution' {
        It 'Replaces a placeholder with the value the config carries' {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Config = $script:TestConfig } {
                $table = @{ Subtitle = '{Toolkit\CompanyName} - App Installation' }
                Expand-ADTConfigValuesInStringTable -Hashtable $table -Config $Config
                $table.Subtitle | Should -BeExactly 'Contoso - App Installation'
            }
        }

        It 'Walks the whole table rather than just its top level' {
            # The shipped string table nests three deep in places, so a top-level-only pass would leave
            # most of the placeholders standing.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Config = $script:TestConfig } {
                $table = @{ Prompt = @{ Subtitle = @{ Install = '{Toolkit\CompanyName} installing' } } }
                Expand-ADTConfigValuesInStringTable -Hashtable $table -Config $Config
                $table.Prompt.Subtitle.Install | Should -BeExactly 'Contoso installing'
            }
        }

        It 'Replaces every placeholder in the one string' {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Config = $script:TestConfig } {
                $table = @{ Line = '{Toolkit\CompanyName} logs to {Toolkit\LogPath}' }
                Expand-ADTConfigValuesInStringTable -Hashtable $table -Config $Config
                $table.Line | Should -BeExactly 'Contoso logs to C:\Logs'
            }
        }

        It 'Leaves a numeric format placeholder alone' {
            # The strings are handed to [System.String]::Format later on, so {0} and friends have to
            # survive this pass intact.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Config = $script:TestConfig } {
                $table = @{ Message = 'Space required: {1}MB for {0} from {Toolkit\CompanyName}' }
                Expand-ADTConfigValuesInStringTable -Hashtable $table -Config $Config
                $table.Message | Should -BeExactly 'Space required: {1}MB for {0} from Contoso'
            }
        }

        It 'Leaves a string carrying no placeholder untouched' {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Config = $script:TestConfig } {
                $table = @{ Plain = 'Installation started.' }
                Expand-ADTConfigValuesInStringTable -Hashtable $table -Config $Config
                $table.Plain | Should -BeExactly 'Installation started.'
            }
        }

        It 'Leaves a non-string value as the type it was' {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Config = $script:TestConfig } {
                $table = @{ Timeout = 120; Enabled = $true }
                Expand-ADTConfigValuesInStringTable -Hashtable $table -Config $Config
                $table.Timeout | Should -BeOfType ([System.Int32])
                $table.Enabled | Should -BeOfType ([System.Boolean])
            }
        }
    }

    Context 'Behaviour' {
        It 'Substitutes from the config it was given rather than the seated one' {
            # This is what lets the uninitialized path build a string table against the module defaults.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $table = @{ Subtitle = '{Toolkit\CompanyName}' }
                Expand-ADTConfigValuesInStringTable -Hashtable $table -Config @{ Toolkit = @{ CompanyName = 'Somewhere Else' } }
                $table.Subtitle | Should -BeExactly 'Somewhere Else'
            }
        }

        It 'Updates the supplied table in place and returns nothing' {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Config = $script:TestConfig } {
                $table = @{ Subtitle = '{Toolkit\CompanyName}' }
                Expand-ADTConfigValuesInStringTable -Hashtable $table -Config $Config | Should -BeNullOrEmpty
                $table.Subtitle | Should -BeExactly 'Contoso'
            }
        }
    }

    Context 'Input Validation' {
        It 'Requires a <Parameter>' -ForEach @(
            @{ Parameter = 'Hashtable' }
            @{ Parameter = 'Config' }
        ) {
            Test-ADTMandatoryParameter -Command (InModuleScope PSAppDeployToolkit { Get-Command Expand-ADTConfigValuesInStringTable }) -Parameter $Parameter | Should -BeTrue
        }

        It 'Refuses an empty <Parameter>' -ForEach @(
            @{ Parameter = 'Hashtable' }
            @{ Parameter = 'Config' }
        ) {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Parameter = $Parameter } {
                $splat = @{ Hashtable = @{ Any = 'thing' }; Config = @{ Any = 'thing' } }
                $splat.$Parameter = @{}
                { Expand-ADTConfigValuesInStringTable @splat } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Expand-ADTConfigValuesInStringTable'
            }
        }
    }
}
