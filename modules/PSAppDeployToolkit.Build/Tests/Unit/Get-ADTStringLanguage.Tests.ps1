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
                (Import-ADTStringTable -BaseDirectory $null -UICulture (Get-ADTStringLanguage)).BalloonTip.Start.Install | Should -Not -BeNullOrEmpty
            }
        }

        It 'Honours a configured override' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                $config = Get-ADTConfig
                $original = $config.UI.LanguageOverride
                try
                {
                    $config.UI.LanguageOverride = 'de-DE'
                    (Get-ADTStringLanguage).Name | Should -BeExactly 'de-DE'
                }
                finally
                {
                    $config.UI.LanguageOverride = $original
                }
            }
        }

        It 'Ignores an override of <Description>' -ForEach @(
            @{ Description = 'nothing at all'; Value = $null }
            @{ Description = 'only whitespace'; Value = '   ' }
        ) {
            # The shipped config carries an empty LanguageOverride, so an unset value has to mean "work it
            # out from the logged-on user" rather than being treated as a culture name.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Value = $Value } {
                $config = Get-ADTConfig
                $original = $config.UI.LanguageOverride
                try
                {
                    $config.UI.LanguageOverride = $Value
                    (Get-ADTStringLanguage).Name | Should -Not -BeNullOrEmpty
                }
                finally
                {
                    $config.UI.LanguageOverride = $original
                }
            }
        }

        It 'Refuses an override that is not a culture at all' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                $config = Get-ADTConfig
                $original = $config.UI.LanguageOverride
                try
                {
                    $config.UI.LanguageOverride = 'not a culture'
                    { Get-ADTStringLanguage } | Should -Throw
                }
                finally
                {
                    $config.UI.LanguageOverride = $original
                }
            }
        }
    }
}
