BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    function Import-Probe
    {
        param
        (
            [Parameter(Mandatory = $true)]
            [System.Collections.Hashtable]$Splat
        )

        InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ S = $Splat } {
            Import-ADTModuleDataFile @S
        }
    }
}

Describe 'Import-ADTModuleDataFile' {
    Context 'Functionality' {
        BeforeAll {
            # Import-LocalizedData looks for the file under a directory named for the current UI culture,
            # which is not necessarily the current culture: this machine is en-AU with an en-GB UI.
            $script:OverrideDir = "$TestDrive\override"
            $null = New-Item -Path "$script:OverrideDir\$((Get-UICulture).Name)" -ItemType Directory -Force
            Set-Content -LiteralPath "$script:OverrideDir\$((Get-UICulture).Name)\config.psd1" -Encoding UTF8 -Value @'
@{
    Toolkit = @{
        LogPath = 'C:\OverriddenLogPath'
    }
    AddedByOverride = @{
        AddedKey = 'added value'
    }
}
'@
        }

        It 'Returns the module defaults when no override directory is given' {
            # -BaseDirectory is mandatory but takes null, which is how the caller says there is no override.
            $data = Import-Probe -Splat @{ BaseDirectory = $null; FileName = 'config.psd1'; IgnorePolicy = $true }
            $data | Should -BeOfType ([System.Collections.Hashtable])
            $data.Keys | Should -Contain 'Toolkit'
            $data.Toolkit.ContainsKey('LogPath') | Should -BeTrue
        }

        It 'Reads the section from the file name' {
            # 'config.psd1' selects the Config defaults, 'strings.psd1' the Strings ones.
            $strings = Import-Probe -Splat @{ BaseDirectory = $null; FileName = 'strings.psd1'; IgnorePolicy = $true }
            $strings.Keys | Should -Contain 'CloseAppsPrompt'
            $strings.Keys | Should -Not -Contain 'Toolkit'
        }

        It 'Lets an override replace a default value' {
            $data = Import-Probe -Splat @{ BaseDirectory = $script:OverrideDir; FileName = 'config.psd1'; IgnorePolicy = $true }
            $data.Toolkit.LogPath | Should -BeExactly 'C:\OverriddenLogPath'
        }

        It 'Keeps defaults the override does not mention' {
            $data = Import-Probe -Splat @{ BaseDirectory = $script:OverrideDir; FileName = 'config.psd1'; IgnorePolicy = $true }
            $data.Toolkit.ContainsKey('LogStyle') | Should -BeTrue
            $data.Keys | Should -Contain 'MSI'
        }

        It 'Lets an override add a section of its own' {
            (Import-Probe -Splat @{ BaseDirectory = $script:OverrideDir; FileName = 'config.psd1'; IgnorePolicy = $true }).AddedByOverride.AddedKey | Should -BeExactly 'added value'
        }

        It 'Applies overrides in the order the directories are given' {
            # Later directories win, which is what lets a customer layer their own config over a template.
            $second = "$TestDrive\second"
            $null = New-Item -Path "$second\$((Get-UICulture).Name)" -ItemType Directory -Force
            Set-Content -LiteralPath "$second\$((Get-UICulture).Name)\config.psd1" -Encoding UTF8 -Value "@{ Toolkit = @{ LogPath = 'C:\SecondWins' } }"

            (Import-Probe -Splat @{ BaseDirectory = @($script:OverrideDir, $second); FileName = 'config.psd1'; IgnorePolicy = $true }).Toolkit.LogPath | Should -BeExactly 'C:\SecondWins'
        }

        It 'Falls back to a parent culture when the exact one is missing' {
            # The module ships en-US only, so any en-* caller has to resolve to it rather than fail.
            $data = Import-Probe -Splat @{ BaseDirectory = $null; FileName = 'config.psd1'; UICulture = [System.Globalization.CultureInfo]::new('en-AU'); IgnorePolicy = $true }
            $data.Keys | Should -Contain 'Toolkit'
        }

        It 'Rejects a duplicate directory' {
            { Import-Probe -Splat @{ BaseDirectory = @($script:OverrideDir, $script:OverrideDir); FileName = 'config.psd1'; IgnorePolicy = $true } } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}
