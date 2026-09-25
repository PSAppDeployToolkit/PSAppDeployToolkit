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
            $null = New-ItemProperty -LiteralPath $script:Root -Name 'AWhitespace' -Value '   ' -PropertyType String
            $null = New-ItemProperty -LiteralPath $script:Root -Name 'AnEmptyMultiString' -Value ([System.String[]]@()) -PropertyType MultiString

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

        It 'Keeps a blank <Name> value as null' -ForEach @(
            @{ Name = 'AnEmpty' }
            @{ Name = 'AWhitespace' }
            @{ Name = 'AnEmptyMultiString' }
        ) {
            # Null is the form the module uses for an unset value, and the reader decides what that means for the setting it lands on.
            $section = (Convert-Probe -LiteralPath $script:Root)['ConvertProbe']
            $section.ContainsKey($Name) | Should -BeTrue
            ($null -eq $section[$Name]) | Should -BeTrue
        }

        It 'Leaves out the provider bookkeeping properties' {
            $section = (Convert-Probe -LiteralPath $script:Root)['ConvertProbe']
            foreach ($noise in 'PSPath', 'PSParentPath', 'PSChildName', 'PSProvider')
            {
                $section.ContainsKey($noise) | Should -BeFalse
            }
        }

        It 'Recurses into subkeys' {
            # The recursion has to reach the function itself. Written as $MyInvocation.MyCommand from inside
            # the anonymous scriptblock it lives in, it resolved to that block instead, so it never recursed
            # and was refused outright wherever code integrity is enforced.
            (Convert-Probe -LiteralPath $script:NestedRoot)['ConvertProbeNested']['Nested']['InnerValue'] | Should -BeExactly 'inner'
        }

        It 'Recurses more than one level deep' {
            $deep = (New-Item -Path 'TestRegistry:\ConvertProbeDeep\One\Two' -ItemType Directory -Force).PSPath
            $null = New-ItemProperty -LiteralPath $deep -Name 'DeepValue' -Value 'deep' -PropertyType String
            (Convert-Probe -LiteralPath 'TestRegistry:\ConvertProbeDeep')['ConvertProbeDeep']['One']['Two']['DeepValue'] | Should -BeExactly 'deep'
        }

        It 'Carries a key that has both values and subkeys' {
            $both = (New-Item -Path 'TestRegistry:\ConvertProbeBoth' -ItemType Directory -Force).PSPath
            $null = New-ItemProperty -LiteralPath $both -Name 'OwnValue' -Value 'own' -PropertyType String
            $null = New-ItemProperty -LiteralPath (New-Item -Path 'TestRegistry:\ConvertProbeBoth\Child' -ItemType Directory -Force).PSPath -Name 'ChildValue' -Value 'child' -PropertyType String

            $section = (Convert-Probe -LiteralPath $both)['ConvertProbeBoth']
            $section['OwnValue'] | Should -BeExactly 'own'
            $section['Child']['ChildValue'] | Should -BeExactly 'child'
        }

        It 'Returns nothing for a key with no values at all' {
            $empty = New-Item -Path 'TestRegistry:\ConvertProbeEmpty' -ItemType Directory
            Convert-Probe -LiteralPath $empty.PSPath | Should -BeNullOrEmpty
        }

        It 'Keeps a <Kind> value as the provider gave it' -ForEach @(
            @{ Kind = 'DWord'; Value = 600; TypeName = 'Int32' }
            @{ Kind = 'QWord'; Value = 600; TypeName = 'Int64' }
            @{ Kind = 'Binary'; Value = [System.Byte[]](1, 2, 3); TypeName = 'Byte[]' }
            @{ Kind = 'MultiString'; Value = [System.String[]]('one', 'two'); TypeName = 'String[]' }
        ) {
            # Only text is typed. The rest is the caller's to accept or refuse, and the collections used to fail
            # inside the function, which cost the caller every value alongside them.
            $key = (New-Item -Path "TestRegistry:\ConvertProbeRaw$Kind" -ItemType Directory -Force).PSPath
            $null = New-ItemProperty -LiteralPath $key -Name 'Raw' -Value $Value -PropertyType $Kind
            (Convert-Probe -LiteralPath $key)["ConvertProbeRaw$Kind"]['Raw'].GetType().Name | Should -BeExactly $TypeName
        }

        It 'Types a number with surrounding whitespace' {
            $key = (New-Item -Path 'TestRegistry:\ConvertProbePadded' -ItemType Directory -Force).PSPath
            $null = New-ItemProperty -LiteralPath $key -Name 'Padded' -Value ' 600 ' -PropertyType String
            (Convert-Probe -LiteralPath $key)['ConvertProbePadded']['Padded'] | Should -Be 600
        }

        It 'Keeps a number too large for an Int32 as text' {
            $key = (New-Item -Path 'TestRegistry:\ConvertProbeOverflow' -ItemType Directory -Force).PSPath
            $null = New-ItemProperty -LiteralPath $key -Name 'Big' -Value '99999999999' -PropertyType String
            (Convert-Probe -LiteralPath $key)['ConvertProbeOverflow']['Big'] | Should -BeExactly '99999999999'
        }

        It 'Reads a hex number with an upper-case prefix' {
            $key = (New-Item -Path 'TestRegistry:\ConvertProbeHex' -ItemType Directory -Force).PSPath
            $null = New-ItemProperty -LiteralPath $key -Name 'Hex' -Value '0XFF' -PropertyType String
            (Convert-Probe -LiteralPath $key)['ConvertProbeHex']['Hex'] | Should -Be 255
        }
    }
}
