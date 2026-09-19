BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Get-ADTStringLanguage' {
    Context 'Functionality' {
        It 'Returns a culture' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                Get-ADTStringLanguage | Should -BeOfType ([System.Globalization.CultureInfo])
            }
        }

        It 'Returns a culture the string table can serve' {
            # Whatever this resolves to is handed straight to Import-ADTStringTable, so it has to be
            # something that resolves rather than throwing.
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Import-ADTStringTable -BaseDirectory $null -UICulture (Get-ADTStringLanguage) -Config (Get-ADTConfig)).BalloonTip.Start.Install | Should -Not -BeNullOrEmpty
            }
        }

        It 'Hands back the language the module was initialized with' {
            # The language is worked out once, during initialization, and held on the state from then on.
            # A config edited afterwards is not consulted again, so this is a read rather than a decision.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $config = Get-ADTConfig
                $original = $config.UI.LanguageOverride
                try
                {
                    $config.UI.LanguageOverride = 'de-DE'
                    Get-ADTStringLanguage | Should -Be (Get-ADTModuleState).Language
                }
                finally
                {
                    $config.UI.LanguageOverride = $original
                }
            }
        }

        It 'Refuses a config once the module is initialized' {
            # There is nothing for a parameter to change once the answer is seated,
            # so being handed onemeans the caller believes it is still deciding.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Get-ADTStringLanguage -Config @{ UI = @{ LanguageOverride = 'de-DE' } } } | Should -Throw -ErrorId 'GetStringLanguageInvalidOperation,Get-ADTStringLanguage'
            }
        }

        It 'Refuses an environment table once the module is initialized' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Get-ADTStringLanguage -Environment (Get-ADTEnvironmentTable) } | Should -Throw -ErrorId 'GetStringLanguageInvalidOperation,Get-ADTStringLanguage'
            }
        }
    }

    Context 'A supplied config' {
        BeforeAll {
            # Parameters are only accepted before initialization, so these run against
            # a fresh import. The file's own AfterAll puts the module back either way.
            Import-ADTModuleUnderTest -Force
        }

        It 'Reads the override from the config it was given' {
            # Get-ADTDefaultStringTable hands it the module defaults, so the seated config must not win.
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Get-ADTStringLanguage -Config @{ UI = @{ LanguageOverride = 'de-DE' } }).Name | Should -BeExactly 'de-DE'
            }
        }

        It 'Ignores an override of <Description>' -ForEach @(
            @{ Description = 'nothing at all'; Value = $null }
            @{ Description = 'only whitespace'; Value = '   ' }
        ) {
            # The shipped config carries an empty LanguageOverride, so an unset value has to mean
            # "work it out from the logged-on user" rather than being treated as a culture name.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Value = $Value } {
                (Get-ADTStringLanguage -Config @{ UI = @{ LanguageOverride = $Value } }).Name | Should -Not -BeNullOrEmpty
            }
        }

        It 'Refuses an override that is not a culture at all' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Get-ADTStringLanguage -Config @{ UI = @{ LanguageOverride = 'not a culture' } } } | Should -Throw
            }
        }

        It 'Works without an environment table to read' {
            # The uninitialized path has none, so the caller SID and logged-on user have to come
            # from the account utilities rather than from an environment table it was handed.
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Get-ADTStringLanguage -Config (Get-ADTDefaultConfig)).Name | Should -Not -BeNullOrEmpty
            }
        }

        It 'Refuses an empty config' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Get-ADTStringLanguage -Config @{} } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Get-ADTStringLanguage'
            }
        }
    }
}
