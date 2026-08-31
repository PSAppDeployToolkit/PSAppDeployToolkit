BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    function Convert-Probe
    {
        param
        (
            [Parameter(Mandatory = $true)]
            [System.String]$LiteralPath
        )

        # Pipeline-only and private, so it is reached with the key piped in from inside the module.
        InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Path = $LiteralPath } {
            Get-Item -LiteralPath $Path | Convert-ADTRegistryKeyToHashtable
        }
    }
}

Describe 'Convert-ADTRegistryKeyToHashtable' {
    Context 'Functionality' {
        BeforeAll {
            $script:Root = (New-Item -Path 'TestRegistry:\ConvertProbe' -ItemType Directory).PSPath
            $null = New-ItemProperty -LiteralPath $script:Root -Name 'AString' -Value 'text' -PropertyType String
            $null = New-ItemProperty -LiteralPath $script:Root -Name 'ANumber' -Value '123' -PropertyType String
            $null = New-ItemProperty -LiteralPath $script:Root -Name 'ANegative' -Value '-5' -PropertyType String
            $null = New-ItemProperty -LiteralPath $script:Root -Name 'AHexNumber' -Value '0x1F' -PropertyType String
            $null = New-ItemProperty -LiteralPath $script:Root -Name 'ATrue' -Value 'True' -PropertyType String
            $null = New-ItemProperty -LiteralPath $script:Root -Name 'AFalse' -Value 'False' -PropertyType String
            $null = New-ItemProperty -LiteralPath $script:Root -Name 'AnEmpty' -Value '' -PropertyType String

            # Kept under a separate root, because a subkey anywhere below the key being converted sends it
            # down the recursion path that currently fails outright. See the skipped test below.
            $script:NestedRoot = (New-Item -Path 'TestRegistry:\ConvertProbeNested' -ItemType Directory).PSPath
            $child = New-Item -Path "$script:NestedRoot\Nested" -ItemType Directory
            $null = New-ItemProperty -LiteralPath $child.PSPath -Name 'InnerValue' -Value 'inner' -PropertyType String
        }

        It 'Keys the result by the leaf name of the registry key' {
            (Convert-Probe -LiteralPath $script:Root).Keys | Should -Contain 'ConvertProbe'
        }

        It 'Converts <Name> to a <TypeName> of <Expected>' -ForEach @(
            @{ Name = 'AString'; TypeName = 'String'; Expected = 'text' }
            @{ Name = 'ANumber'; TypeName = 'Int32'; Expected = 123 }
            @{ Name = 'ANegative'; TypeName = 'Int32'; Expected = -5 }
            @{ Name = 'AHexNumber'; TypeName = 'Int32'; Expected = 31 }
            @{ Name = 'ATrue'; TypeName = 'Boolean'; Expected = $true }
            @{ Name = 'AFalse'; TypeName = 'Boolean'; Expected = $false }
        ) {
            # Registry values are all strings here, so the typing is the function's own doing.
            $section = (Convert-Probe -LiteralPath $script:Root)['ConvertProbe']
            $section[$Name] | Should -Be $Expected
            $section[$Name] | Should -BeOfType ([System.Type]"System.$TypeName")
        }

        It 'Drops a value that renders as nothing' {
            (Convert-Probe -LiteralPath $script:Root)['ConvertProbe'].ContainsKey('AnEmpty') | Should -BeFalse
        }

        It 'Leaves out the provider bookkeeping properties' {
            $section = (Convert-Probe -LiteralPath $script:Root)['ConvertProbe']
            foreach ($noise in 'PSPath', 'PSParentPath', 'PSChildName', 'PSProvider')
            {
                $section.ContainsKey($noise) | Should -BeFalse
            }
        }

        It 'Recurses into subkeys' -Skip {
            # Skipped: the subkey recursion is `$registryKeys | & $MyInvocation.MyCommand` written inside an
            # anonymous `& { end { ... } }` block. There, $MyInvocation.MyCommand is a ScriptInfo wrapping
            # that anonymous block rather than the enclosing function, so it does not re-enter
            # Convert-ADTRegistryKeyToHashtable at all. Invoking that ScriptInfo also fails outright with
            # "AuthorizationManager check failed" wherever a code integrity policy is enforced.
            #
            # Reachable in production: Import-ADTModuleDataFile pipes the HKLM policy key for Config or
            # Strings into this function, and those are nested, so any customer using the ADMX policy
            # overrides on a locked-down machine takes this path. Unskip with the fix.
            (Convert-Probe -LiteralPath $script:NestedRoot)['ConvertProbeNested']['Nested']['InnerValue'] | Should -BeExactly 'inner'
        }

        It 'Returns nothing for a key with no values at all' {
            $empty = New-Item -Path 'TestRegistry:\ConvertProbeEmpty' -ItemType Directory
            Convert-Probe -LiteralPath $empty.PSPath | Should -BeNullOrEmpty
        }
    }
}
