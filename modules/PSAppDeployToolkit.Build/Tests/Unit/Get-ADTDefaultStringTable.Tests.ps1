BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Get-ADTDefaultStringTable' {
    Context 'Without an initialized module' {
        It 'Returns a section for each dialog the toolkit can show' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                $strings = Get-ADTDefaultStringTable
                $strings | Should -BeOfType ([System.Collections.Hashtable])
                $strings.Keys | Should -Contain 'BalloonTip'
                $strings.Keys | Should -Contain 'CloseAppsPrompt'
                $strings.Keys | Should -Contain 'InstallationPrompt'
                $strings.Keys | Should -Contain 'ProgressPrompt'
                $strings.Keys | Should -Contain 'RestartPrompt'
            }
        }

        It 'Leaves the module uninitialized' {
            # The dialogs reach for this precisely so they can render without seating the module first.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $null = Get-ADTDefaultStringTable
                Test-ADTModuleInitialized | Should -BeFalse
            }
        }

        It 'Substitutes the default config into the strings that reference it' {
            # The subtitles are authored as {Toolkit\CompanyName}, and a dialog showing that verbatim
            # would be worse than showing nothing.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $subtitle = (Get-ADTDefaultStringTable).InstallationPrompt.Subtitle.Install
                $subtitle | Should -Not -Match '\{Toolkit'
                $subtitle | Should -BeLike "$((Get-ADTDefaultConfig).Toolkit.CompanyName)*"
            }
        }

        It 'Leaves the numeric format placeholders alone' {
            # DiskSpaceText is handed to [System.String]::Format at the call site, so {0} and friends
            # have to survive the config substitution.
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Get-ADTDefaultStringTable).DiskSpaceText.Message.Install | Should -Match '\{0\}'
            }
        }
    }

    Context 'Cultures' {
        It 'Returns the requested language' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Get-ADTDefaultStringTable -UICulture ([System.Globalization.CultureInfo]::new('de-DE'))).BalloonTip.Start.Install | Should -BeExactly 'Installation wurde gestartet.'
            }
        }

        It 'Falls back to a parent culture for a regional variant it does not carry' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Get-ADTDefaultStringTable -UICulture ([System.Globalization.CultureInfo]::new('en-NZ'))).BalloonTip.Start.Install | Should -BeExactly (Get-ADTDefaultStringTable -UICulture ([System.Globalization.CultureInfo]::new('en-US'))).BalloonTip.Start.Install
            }
        }

        It 'Falls back to English for a language it does not ship' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Get-ADTDefaultStringTable -UICulture ([System.Globalization.CultureInfo]::new('mi-NZ'))).BalloonTip.Start.Install | Should -Not -BeNullOrEmpty
            }
        }

        It 'Resolves the language itself when none is nominated' {
            # Falling through to Get-ADTStringLanguage is what honours a LanguageOverride and, failing
            # that, the logged-on user's locale rather than the calling process's.
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock Get-ADTStringLanguage { [System.Globalization.CultureInfo]::new('de-DE') }
                (Get-ADTDefaultStringTable).BalloonTip.Start.Install | Should -BeExactly 'Installation wurde gestartet.'
                Should -Invoke Get-ADTStringLanguage -Times 1 -Exactly
            }
        }
    }

    Context 'Variable expansion' {
        It 'Expands the caller variables when a session state is supplied' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock Import-ADTStringTable { @{ Greeting = 'Hello $TestSubject' } }

                [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestSubject', Justification = "Referenced from the string under expansion, which PSScriptAnalyzer has no visibility of.")]
                $TestSubject = 'world'
                (Get-ADTDefaultStringTable -SessionState $ExecutionContext.SessionState).Greeting | Should -BeExactly 'Hello world'
            }
        }

        It 'Leaves them alone when no session state is supplied' {
            # Without a session state there is nothing to expand against, so the placeholder has to stay
            # put rather than being flattened to an empty string.
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock Import-ADTStringTable { @{ Greeting = 'Hello $TestSubject' } }
                (Get-ADTDefaultStringTable).Greeting | Should -BeExactly 'Hello $TestSubject'
            }
        }
    }

    Context 'Input Validation' {
        It 'Refuses a null <Parameter>' -ForEach @(
            @{ Parameter = 'UICulture' }
            @{ Parameter = 'SessionState' }
        ) {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Parameter = $Parameter } {
                { Get-ADTDefaultStringTable @{ $Parameter = $null } } | Should -Throw
            }
        }
    }

    Context 'Against an initialized module' {
        BeforeAll {
            # This reads the default config, which answers only for an uninitialized module, so the default
            # table is taken first and compared with the seated one afterwards. Each resolves its own
            # language, which is part of what has to agree rather than something to arrange away.
            $script:DefaultStrings = InModuleScope -ModuleName PSAppDeployToolkit { Get-ADTDefaultStringTable }
            InModuleScope -ModuleName PSAppDeployToolkit { Initialize-ADTModule -InformationAction SilentlyContinue }
            $script:SeatedStrings = InModuleScope -ModuleName PSAppDeployToolkit { Get-ADTStringTable }
        }

        It 'Agrees with the initialized string table' {
            # Callers pick one or the other depending on whether the module is seated, so the two have to
            # land on the same text for anything a deployment has not overridden.
            $script:DefaultStrings.InstallationPrompt.Subtitle.Install | Should -BeExactly $script:SeatedStrings.InstallationPrompt.Subtitle.Install
            $script:DefaultStrings.BalloonTip.Start.Install | Should -BeExactly $script:SeatedStrings.BalloonTip.Start.Install
        }
    }
}
