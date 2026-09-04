BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Import-ADTConfig' {
    Context 'The module defaults' {
        BeforeAll {
            $script:Config = InModuleScope -ModuleName PSAppDeployToolkit { Import-ADTConfig -BaseDirectory $null }
        }

        It 'Returns every section the module reads from' {
            # Every consumer of Get-ADTConfig indexes into one of these four, so a missing section is a
            # null reference somewhere further down rather than a diagnosable failure here.
            $script:Config.Keys | Should -Contain 'Toolkit'
            $script:Config.Keys | Should -Contain 'UI'
            $script:Config.Keys | Should -Contain 'MSI'
            $script:Config.Keys | Should -Contain 'Assets'
        }

        It 'Expands the environment variables the shipped paths are written with' {
            # The defaults are authored as $envWinDir\Logs\Software and friends, and nothing downstream
            # expands them a second time.
            $script:Config.Toolkit.LogPath | Should -Not -Match '\$env'
            [System.IO.Path]::IsPathRooted($script:Config.Toolkit.LogPath) | Should -BeTrue
        }

        It 'Appends the toolkit name to the temporary path' {
            # Keeps the toolkit's own scratch files together under whatever temp root was configured.
            $script:Config.Toolkit.TempPath | Should -BeLike "*\$($script:Config.Toolkit.CompanyName -replace '\s')*"
        }

        It 'Carries the mandatory assets inline' {
            # Logo and Banner ship as Base64 so a module copied anywhere still renders its dialogs.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Config = $script:Config } {
                [PSADT.Utilities.MiscUtilities]::GetBase64StringBytes($Config.Assets.Logo) | Should -Not -BeNullOrEmpty
                [PSADT.Utilities.MiscUtilities]::GetBase64StringBytes($Config.Assets.Banner) | Should -Not -BeNullOrEmpty
            }
        }

        It 'Leaves the optional assets unset' {
            # New-ADTDialogOptionsObject relies on these having no default so that it drops them rather
            # than substituting something, so they must not quietly gain a value.
            $script:Config.Assets.LogoDark | Should -BeNullOrEmpty
            $script:Config.Assets.TaskbarIcon | Should -BeNullOrEmpty
        }
    }

    Context 'Supplemental configuration' {
        It 'Takes a value from the supplied directory' {
            $dir = "$TestDrive\Merge"
            $null = New-Item -Path $dir -ItemType Directory -Force
            Set-Content -LiteralPath "$dir\config.psd1" -Value "@{ UI = @{ DefaultTimeout = 4242 } }"
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Dir = $dir } {
                (Import-ADTConfig -BaseDirectory $Dir).UI.DefaultTimeout | Should -Be 4242
            }
        }

        It 'Leaves everything the supplement did not mention alone' {
            # A deployment supplies a handful of overrides, not a whole config, so the rest has to survive.
            $dir = "$TestDrive\Partial"
            $null = New-Item -Path $dir -ItemType Directory -Force
            Set-Content -LiteralPath "$dir\config.psd1" -Value "@{ UI = @{ DefaultTimeout = 4242 } }"
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Dir = $dir } {
                $merged = Import-ADTConfig -BaseDirectory $Dir
                $merged.UI.DialogStyle | Should -BeExactly (Import-ADTConfig -BaseDirectory $null).UI.DialogStyle
            }
        }

        It 'Resolves an asset that sits beside the config' {
            $dir = "$TestDrive\Beside"
            $null = New-Item -Path $dir -ItemType Directory -Force
            Set-Content -LiteralPath "$dir\config.psd1" -Value "@{ Assets = @{ Logo = 'Local.png' } }"
            Set-Content -LiteralPath "$dir\Local.png" -Value 'placeholder'
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Dir = $dir } {
                (Import-ADTConfig -BaseDirectory $Dir).Assets.Logo | Should -BeExactly "$Dir\Local.png"
            }
        }

        It 'Resolves an asset written relative to the deployment folder' {
            # Templates reference ..\Assets\Logo.png so that the same config works from Files or SupportFiles.
            $dir = "$TestDrive\Relative"
            $null = New-Item -Path $dir -ItemType Directory -Force
            Set-Content -LiteralPath "$dir\config.psd1" -Value "@{ Assets = @{ Logo = '..\Elsewhere.png' } }"
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Dir = $dir } {
                (Import-ADTConfig -BaseDirectory $Dir).Assets.Logo | Should -BeExactly "$Dir\Elsewhere.png"
            }
        }

        It 'Renames the <Legacy> language identifier to <Modern>' -ForEach @(
            @{ Legacy = 'CZ'; Modern = 'cs' }
            @{ Legacy = 'ZH-Hans'; Modern = 'zh-CN' }
            @{ Legacy = 'ZH-Hant'; Modern = 'zh-HK' }
        ) {
            # Configs written against 4.1.0 and earlier still carry these, and the string table is keyed by
            # the modern name, so an untranslated override would silently fall back to English.
            $dir = "$TestDrive\Lang$Legacy"
            $null = New-Item -Path $dir -ItemType Directory -Force
            Set-Content -LiteralPath "$dir\config.psd1" -Value "@{ UI = @{ LanguageOverride = '$Legacy' } }"
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Dir = $dir; Modern = $Modern } {
                (Import-ADTConfig -BaseDirectory $Dir).UI.LanguageOverride | Should -BeExactly $Modern
            }
        }
    }

    Context 'Input Validation' {
        It 'Refuses an interval of <Value>' -ForEach @(
            @{ Value = 0 }
            @{ Value = -5 }
        ) {
            # A timeout of zero or less would have the dialogs expire the instant they appear.
            $dir = "$TestDrive\Int$([System.Math]::Abs($Value))"
            $null = New-Item -Path $dir -ItemType Directory -Force
            Set-Content -LiteralPath "$dir\config.psd1" -Value "@{ UI = @{ DefaultTimeout = $Value } }"
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Dir = $dir } {
                { Import-ADTConfig -BaseDirectory $Dir } | Should -Throw -ErrorId 'ConfigIntLessThanOrEqualToZero,Import-ADTConfig'
            }
        }

        It 'Allows the exit codes to be zero' {
            # Exit codes are exempt from the check above, since zero is the successful one.
            $dir = "$TestDrive\ExitCode"
            $null = New-Item -Path $dir -ItemType Directory -Force
            Set-Content -LiteralPath "$dir\config.psd1" -Value "@{ UI = @{ DefaultExitCode = 0; DeferExitCode = 0 } }"
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Dir = $dir } {
                (Import-ADTConfig -BaseDirectory $Dir).UI.DefaultExitCode | Should -Be 0
            }
        }

        It 'Refuses a dialog style it cannot render' {
            $dir = "$TestDrive\Style"
            $null = New-Item -Path $dir -ItemType Directory -Force
            Set-Content -LiteralPath "$dir\config.psd1" -Value "@{ UI = @{ DialogStyle = 'Nonsense' } }"
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Dir = $dir } {
                { Import-ADTConfig -BaseDirectory $Dir } | Should -Throw
            }
        }

        It 'Refuses the same directory twice' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Import-ADTConfig -BaseDirectory 'C:\Windows', 'C:\Windows' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Import-ADTConfig'
            }
        }

        It 'Refuses a blank directory' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Import-ADTConfig -BaseDirectory '   ' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Import-ADTConfig'
            }
        }

        It 'Reports a directory holding no config at all' {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Dir = "$TestDrive\Nothing" } {
                { Import-ADTConfig -BaseDirectory $Dir } | Should -Throw
            }
        }
    }
}
