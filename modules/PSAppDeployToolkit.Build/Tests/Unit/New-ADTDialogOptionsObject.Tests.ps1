BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'New-ADTDialogOptionsObject' {
    Context 'Functionality' {
        It 'Builds the options object from the supplied data' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                $options = New-ADTDialogOptionsObject -Type ([PSADT.UserInterface.DialogOptions.BalloonTipOptions]) -Data @{ Title = 'A title'; Text = 'Some text'; Icon = [PSADT.UserInterface.BalloonTipIcon]::Info }
                $options | Should -BeOfType ([PSADT.UserInterface.DialogOptions.BalloonTipOptions])
                $options.Title | Should -BeExactly 'A title'
                $options.Text | Should -BeExactly 'Some text'
            }
        }

        It 'Uses the deployment type constructor when given a deployment type' {
            # The dialogs that vary their wording by deployment type take it as a separate argument rather
            # than as a key, so the right constructor has to be picked.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $config = Get-ADTConfig
                $data = @{
                    AppTitle = 'A title'
                    Subtitle = 'A subtitle'
                    AppIconImage = $config.Assets.Logo
                    AppBannerImage = $config.Assets.Banner
                    DialogTopMost = $true
                    Language = [System.Globalization.CultureInfo]::new('en-US')
                    Strings = (Get-ADTStringTable).RestartPrompt
                }
                New-ADTDialogOptionsObject -Type ([PSADT.UserInterface.DialogOptions.RestartDialogOptions]) -Data $data -DeploymentType Install | Should -BeOfType ([PSADT.UserInterface.DialogOptions.RestartDialogOptions])
            }
        }

        It 'Reports a key the dialog cannot do without' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { New-ADTDialogOptionsObject -Type ([PSADT.UserInterface.DialogOptions.BalloonTipOptions]) -Data @{ Title = 'A title' } } | Should -Throw -ErrorId 'ArgumentNullException,New-ADTDialogOptionsObject'
            }
        }
    }

    Context 'Assets that will not load' {
        It 'Substitutes the module default for a required asset' {
            # A deployment pointing its Logo at something that is not an image should still show its
            # dialogs, because falling back is better than failing the deployment over branding.
            $broken = "$TestDrive\Broken.png"
            [System.IO.File]::WriteAllText($broken, 'not an image')
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Broken = $broken } {
                $options = New-ADTDialogOptionsObject -Type ([PSADT.UserInterface.DialogOptions.NotifyIconOptions]) -Data @{ AppTitle = 'A title'; MessageText = 'Some text'; AppIconImage = $Broken }
                $options.AppIconImage | Should -Not -BeExactly $Broken
                [PSADT.Utilities.MiscUtilities]::GetBase64StringBytes($options.AppIconImage) | Should -Not -BeNullOrEmpty
            }
        }

        It 'Drops an optional asset that has no default to fall back on' {
            # TaskbarIcon ships unset, so there is nothing to substitute and the dialog simply goes without.
            $broken = "$TestDrive\Broken.png"
            [System.IO.File]::WriteAllText($broken, 'not an image')
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Broken = $broken } {
                $options = New-ADTDialogOptionsObject -Type ([PSADT.UserInterface.DialogOptions.NotifyIconOptions]) -Data @{ AppTitle = 'A title'; MessageText = 'Some text'; AppIconImage = (Get-ADTConfig).Assets.Logo; AppTaskbarIconImage = $Broken }
                $options.AppTaskbarIconImage | Should -BeNullOrEmpty
            }
        }

        It 'Warns about what it had to do' {
            $broken = "$TestDrive\Broken.png"
            [System.IO.File]::WriteAllText($broken, 'not an image')
            Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Broken = $broken } {
                $null = New-ADTDialogOptionsObject -Type ([PSADT.UserInterface.DialogOptions.NotifyIconOptions]) -Data @{ AppTitle = 'A title'; MessageText = 'Some text'; AppIconImage = $Broken }
            }
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Severity -eq 'Warning' }
        }
    }

    Context 'Input Validation' {
        It 'Refuses a type it has no idea how to construct' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { New-ADTDialogOptionsObject -Type ([System.String]) -Data @{ Anything = 1 } } | Should -Throw
            }
        }

        It 'Refuses empty data' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { New-ADTDialogOptionsObject -Type ([PSADT.UserInterface.DialogOptions.BalloonTipOptions]) -Data @{} } | Should -Throw -ErrorId 'ParameterArgumentValidationError,New-ADTDialogOptionsObject'
            }
        }

        It 'Refuses a null type' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { New-ADTDialogOptionsObject -Type $null -Data @{ Anything = 1 } } | Should -Throw -ErrorId 'ParameterArgumentValidationError,New-ADTDialogOptionsObject'
            }
        }
    }
}
