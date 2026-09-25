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

    Context 'Machine policy' {
        BeforeAll {
            # Read from a key under the current user's hive so nothing needs elevation. The versioned keys are
            # named from the module's own version, so the tests hold for whichever version is under test.
            $script:PolicyRoot = (New-Item -Path 'TestRegistry:\PolicyRoot' -ItemType Directory).PSPath
            $script:ModuleVersion = (Get-Module -Name PSAppDeployToolkit).Version
            $script:ThisLayer = "$($script:ModuleVersion.Major).$($script:ModuleVersion.Minor)"
            $script:NextLayer = "$($script:ModuleVersion.Major).$($script:ModuleVersion.Minor + 1)"
            $script:PriorMajor = $script:ModuleVersion.Major - 1

            # The reader's policy root is fixed to HKLM, so calls for it are redirected to the key above and every other
            # call, such as the converter walking subkeys, goes to the real cmdlet. The body runs in the module's scope,
            # so the key's path is baked into it rather than read from a variable of this file.
            Mock -ModuleName PSAppDeployToolkit Get-ChildItem ([System.Management.Automation.ScriptBlock]::Create(@"
                `$arguments = @{} + `$PesterBoundParameters
                if (`$arguments.ContainsKey('LiteralPath'))
                {
                    `$arguments.LiteralPath = @(`$arguments.LiteralPath | ForEach-Object { `$_.Replace('Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\PSAppDeployToolkit', '$($script:PolicyRoot)') })
                }
                Microsoft.PowerShell.Management\Get-ChildItem @arguments
"@))

            function Set-PolicyValue
            {
                param
                (
                    [Parameter(Mandatory = $true)]
                    [System.String]$Key,

                    [Parameter(Mandatory = $true)]
                    [System.String]$Name,

                    [Parameter(Mandatory = $true)]
                    [AllowEmptyString()]
                    [System.Object]$Value
                )

                # New-Item -Force on a registry key that already exists empties it, so only create what is missing.
                if (!(Test-Path -LiteralPath "$script:PolicyRoot\$Key"))
                {
                    $null = New-Item -Path "$script:PolicyRoot\$Key" -ItemType Directory -Force
                }
                $null = New-ItemProperty -LiteralPath "$script:PolicyRoot\$Key" -Name $Name -Value $Value -PropertyType String -Force
            }

            function Import-PolicyProbe
            {
                param
                (
                    [Parameter(Mandatory = $false)]
                    [System.Collections.Hashtable]$Splat = @{}
                )

                $arguments = @{ BaseDirectory = $null; FileName = 'config.psd1' }
                foreach ($entry in $Splat.GetEnumerator())
                {
                    $arguments[$entry.Key] = $entry.Value
                }
                Import-Probe -Splat $arguments
            }
        }

        AfterEach {
            Get-ChildItem -LiteralPath $script:PolicyRoot | Remove-Item -Recurse -Force
        }

        It 'Reads a value from the shared key' {
            Set-PolicyValue -Key 'Config\Toolkit' -Name CompanyName -Value 'Shared'
            (Import-PolicyProbe).Toolkit.CompanyName | Should -BeExactly 'Shared'
        }

        It 'Lets a key versioned for this module override the shared key' {
            Set-PolicyValue -Key 'Config\Toolkit' -Name CompanyName -Value 'Shared'
            Set-PolicyValue -Key "$script:ThisLayer\Config\Toolkit" -Name CompanyName -Value 'Versioned'
            (Import-PolicyProbe).Toolkit.CompanyName | Should -BeExactly 'Versioned'
        }

        It 'Ignores a key versioned beyond this module' {
            Set-PolicyValue -Key 'Config\Toolkit' -Name CompanyName -Value 'Shared'
            Set-PolicyValue -Key "$script:NextLayer\Config\Toolkit" -Name CompanyName -Value 'Future'
            (Import-PolicyProbe).Toolkit.CompanyName | Should -BeExactly 'Shared'
        }

        It 'Ignores a subkey not named for a version' {
            Set-PolicyValue -Key 'Extras\Config\Toolkit' -Name CompanyName -Value 'Extras'
            (Import-PolicyProbe).Toolkit.CompanyName | Should -BeExactly 'PSAppDeployToolkit'
        }

        It 'Applies versioned keys in version order rather than name order' {
            # 3.10 sorts before 3.9 as text, so the later version can only win if the order is by version.
            Set-PolicyValue -Key "$($script:PriorMajor).9\Config\Toolkit" -Name CompanyName -Value 'Older'
            Set-PolicyValue -Key "$($script:PriorMajor).10\Config\Toolkit" -Name CompanyName -Value 'Newer'
            (Import-PolicyProbe).Toolkit.CompanyName | Should -BeExactly 'Newer'
        }

        It 'Falls back to a parent culture under a versioned key' {
            Set-PolicyValue -Key "$script:ThisLayer\Config\en\Toolkit" -Name CompanyName -Value 'English'
            (Import-PolicyProbe -Splat @{ UICulture = [System.Globalization.CultureInfo]::new('en-AU') }).Toolkit.CompanyName | Should -BeExactly 'English'
        }

        It 'Overlays culture-specific values on the un-cultured ones rather than replacing them' {
            Set-PolicyValue -Key 'Config\Toolkit' -Name CompanyName -Value 'Anywhere'
            Set-PolicyValue -Key 'Config\Toolkit' -Name LogPath -Value 'C:\AnywhereLogs'
            Set-PolicyValue -Key 'Config\en-AU\Toolkit' -Name CompanyName -Value 'Australia'
            $data = Import-PolicyProbe -Splat @{ UICulture = [System.Globalization.CultureInfo]::new('en-AU') }
            $data.Toolkit.CompanyName | Should -BeExactly 'Australia'
            $data.Toolkit.LogPath | Should -BeExactly 'C:\AnywhereLogs'
        }

        It 'Lets the more specific culture win over its parent' {
            Set-PolicyValue -Key 'Config\en\Toolkit' -Name CompanyName -Value 'English'
            Set-PolicyValue -Key 'Config\en-AU\Toolkit' -Name CompanyName -Value 'Australia'
            (Import-PolicyProbe -Splat @{ UICulture = [System.Globalization.CultureInfo]::new('en-AU') }).Toolkit.CompanyName | Should -BeExactly 'Australia'
        }

        It 'Keeps a value the defaults do not have' {
            Set-PolicyValue -Key 'Config\UI' -Name DialogStyleCompatMode -Value 'Classic'
            (Import-PolicyProbe).UI.DialogStyleCompatMode | Should -BeExactly 'Classic'
        }

        It 'Takes a Base64 asset from the shared key' {
            Set-PolicyValue -Key 'Config\Assets' -Name Logo -Value 'AAAA'
            (Import-PolicyProbe).Assets.Logo | Should -BeExactly 'AAAA'
        }

        It 'Takes an asset the defaults leave unset' {
            # TaskbarIcon ships as null, so there is no type to hold the policy value to.
            Set-PolicyValue -Key 'Config\Assets' -Name TaskbarIcon -Value 'C:\Brand\Tray.ico'
            (Import-PolicyProbe).Assets.TaskbarIcon | Should -BeExactly 'C:\Brand\Tray.ico'
        }

        It 'Takes a Base64 asset from a versioned key' {
            Set-PolicyValue -Key "$script:ThisLayer\Config\Assets" -Name Logo -Value 'AAAA'
            Set-PolicyValue -Key "$script:ThisLayer\Config\Assets" -Name TaskbarIcon -Value 'AAAA'
            $data = Import-PolicyProbe
            $data.Assets.Logo | Should -BeExactly 'AAAA'
            $data.Assets.TaskbarIcon | Should -BeExactly 'AAAA'
        }

        It 'Clears a setting that ships as null when the policy leaves it blank' {
            # The deployment sets a dark logo. A blank policy value takes it away rather than being passed over.
            $deployment = "$TestDrive\blank"
            $null = New-Item -Path "$deployment\$((Get-UICulture).Name)" -ItemType Directory -Force
            Set-Content -LiteralPath "$deployment\$((Get-UICulture).Name)\config.psd1" -Encoding UTF8 -Value "@{ Assets = @{ LogoDark = 'C:\Brand\Dark.png' } }"
            Set-PolicyValue -Key 'Config\Assets' -Name LogoDark -Value ''
            $data = Import-PolicyProbe -Splat @{ BaseDirectory = $deployment }
            $data.Assets.ContainsKey('LogoDark') | Should -BeTrue
            ($null -eq $data.Assets.LogoDark) | Should -BeTrue
        }

        It 'Clears a shared-key value when the versioned key leaves it blank' {
            Set-PolicyValue -Key 'Config\Assets' -Name LogoDark -Value 'C:\Brand\Dark.png'
            Set-PolicyValue -Key "$script:ThisLayer\Config\Assets" -Name LogoDark -Value ''
            ($null -eq (Import-PolicyProbe).Assets.LogoDark) | Should -BeTrue
        }

        It 'Ignores a blank policy value for a setting that cannot be blank' {
            Set-PolicyValue -Key 'Config\Toolkit' -Name LogPath -Value ''
            (Import-PolicyProbe).Toolkit.LogPath | Should -Not -BeNullOrEmpty
        }
    }
}
