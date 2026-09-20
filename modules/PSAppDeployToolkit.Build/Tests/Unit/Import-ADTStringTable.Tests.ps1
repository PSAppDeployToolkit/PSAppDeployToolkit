BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Import-ADTStringTable' {
    Context 'The module defaults' {
        BeforeAll {
            $script:Strings = InModuleScope -ModuleName PSAppDeployToolkit { Import-ADTStringTable -BaseDirectory $null -UICulture ([System.Globalization.CultureInfo]::new('en-US')) -Config (Get-ADTConfig) }
        }

        It 'Returns a section for each dialog the toolkit can show' {
            $script:Strings.Keys | Should -Contain 'BalloonTip'
            $script:Strings.Keys | Should -Contain 'CloseAppsPrompt'
            $script:Strings.Keys | Should -Contain 'InstallationPrompt'
            $script:Strings.Keys | Should -Contain 'ProgressPrompt'
            $script:Strings.Keys | Should -Contain 'RestartPrompt'
        }

        It 'Keys the deployment-specific strings by deployment type' {
            $script:Strings.BalloonTip.Start.Keys | Should -Contain 'Install'
            $script:Strings.BalloonTip.Start.Keys | Should -Contain 'Uninstall'
            $script:Strings.BalloonTip.Start.Keys | Should -Contain 'Repair'
        }

        It 'Substitutes config values into the strings that reference them' {
            # The subtitles are authored as {Toolkit\CompanyName} so that a rebranded config carries into
            # every dialog without the strings having to be edited too.
            $script:Strings.InstallationPrompt.Subtitle.Install | Should -Not -Match '\{Toolkit'
            $script:Strings.InstallationPrompt.Subtitle.Install | Should -BeLike "$(InModuleScope -ModuleName PSAppDeployToolkit { (Get-ADTConfig).Toolkit.CompanyName })*"
        }
    }

    Context 'Cultures' {
        It 'Returns the requested language' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Import-ADTStringTable -BaseDirectory $null -UICulture ([System.Globalization.CultureInfo]::new('de-DE')) -Config (Get-ADTConfig)).BalloonTip.Start.Install | Should -BeExactly 'Installation wurde gestartet.'
            }
        }

        It 'Falls back to a parent culture for a regional variant it does not carry' {
            # The module ships one English table rather than one per region, so en-NZ has to walk up to it
            # instead of coming back empty.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $variant = Import-ADTStringTable -BaseDirectory $null -UICulture ([System.Globalization.CultureInfo]::new('en-NZ')) -Config (Get-ADTConfig)
                $variant.BalloonTip.Start.Install | Should -BeExactly (Import-ADTStringTable -BaseDirectory $null -UICulture ([System.Globalization.CultureInfo]::new('en-US')) -Config (Get-ADTConfig)).BalloonTip.Start.Install
            }
        }

        It 'Falls back to English for a language it does not ship' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Import-ADTStringTable -BaseDirectory $null -UICulture ([System.Globalization.CultureInfo]::new('mi-NZ')) -Config (Get-ADTConfig)).BalloonTip.Start.Install | Should -Not -BeNullOrEmpty
            }
        }
    }

    Context 'Supplemental strings' {
        BeforeAll {
            $script:Dir = "$TestDrive\Strings"
            $null = New-Item -Path $script:Dir -ItemType Directory -Force
            Set-Content -LiteralPath "$script:Dir\strings.psd1" -Value "@{ BalloonTip = @{ Start = @{ Install = 'Overridden install text.' } } }"
            $script:Merged = InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Dir = $script:Dir } { Import-ADTStringTable -BaseDirectory $Dir -UICulture ([System.Globalization.CultureInfo]::new('en-US')) -Config (Get-ADTConfig) }
        }

        It 'Takes the string the deployment supplied' {
            $script:Merged.BalloonTip.Start.Install | Should -BeExactly 'Overridden install text.'
        }

        It 'Leaves the siblings of an overridden string alone' {
            # Deployments override one line at a time, so a partial section must not blank out the rest.
            $script:Merged.BalloonTip.Start.Uninstall | Should -Not -BeNullOrEmpty
            $script:Merged.BalloonTip.Complete | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Refuses the same directory twice' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Import-ADTStringTable -BaseDirectory 'C:\Windows', 'C:\Windows' -Config (Get-ADTConfig) } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Import-ADTStringTable'
            }
        }

        It 'Refuses a blank directory' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Import-ADTStringTable -BaseDirectory '   ' -Config (Get-ADTConfig) } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Import-ADTStringTable'
            }
        }

        It 'Requires a directory to be nominated, even a null one' {
            # Null is meaningful here as it means "module defaults only", so it has to be stated rather
            # than defaulted into.
            Test-ADTMandatoryParameter -Command (InModuleScope PSAppDeployToolkit { Get-Command Import-ADTStringTable }) -Parameter BaseDirectory | Should -BeTrue
        }

        It 'Refuses a null culture' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Import-ADTStringTable -BaseDirectory $null -UICulture $null -Config (Get-ADTConfig) } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Import-ADTStringTable'
            }
        }

        It 'Refuses an empty config' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Import-ADTStringTable -BaseDirectory $null -Config @{} } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Import-ADTStringTable'
            }
        }
    }

    Context 'A supplied config' {
        It 'Substitutes from the config it was given rather than the seated one' {
            # Get-ADTDefaultStringTable hands it the module defaults so a string table can be built
            # before the module has been initialized at all.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $strings = Import-ADTStringTable -BaseDirectory $null -UICulture ([System.Globalization.CultureInfo]::new('en-US')) -Config @{ Toolkit = @{ CompanyName = 'Somewhere Else' } }
                $strings.InstallationPrompt.Subtitle.Install | Should -BeLike 'Somewhere Else*'
            }
        }

        It 'Has to be given one rather than reaching for the seated config' {
            # It builds a string table for a module that may not be initialized, so there is not always a
            # seated config to fall back on and the caller states which one the substitutions come from.
            Test-ADTMandatoryParameter -Command (InModuleScope PSAppDeployToolkit { Get-Command Import-ADTStringTable }) -Parameter Config | Should -BeTrue
        }
    }
}
