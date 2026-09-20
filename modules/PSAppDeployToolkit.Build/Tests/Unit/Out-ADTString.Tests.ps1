BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Private, so every call has to be made from inside the module. The value goes in by name here; the
    # pipeline form gets its own probe below because the two bind differently for collections.
    function Out-Probe
    {
        param
        (
            [Parameter(Mandatory = $true)]
            [AllowNull()][AllowEmptyString()][AllowEmptyCollection()]
            [System.Object]$Value,

            [Parameter(Mandatory = $false)]
            [System.Management.Automation.SwitchParameter]$Stream
        )

        InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Value = $Value; Stream = $Stream } {
            Out-ADTString -InputObject $Value -Stream:$Stream
        }
    }

    function Out-PipelineProbe
    {
        param
        (
            [Parameter(Mandatory = $true)]
            [AllowNull()][AllowEmptyString()][AllowEmptyCollection()]
            [System.Object]$Value,

            [Parameter(Mandatory = $false)]
            [System.Management.Automation.SwitchParameter]$Stream
        )

        InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Value = $Value; Stream = $Stream } {
            $Value | Out-ADTString -Stream:$Stream
        }
    }

    function Get-LineLength
    {
        param
        (
            [Parameter(Mandatory = $true)]
            [AllowEmptyString()]
            [System.String]$Text
        )

        return $Text.Split([System.String[]]("`r`n", "`n"), [System.StringSplitOptions]::None).Length
    }
}

Describe 'Out-ADTString' {
    Context 'Whitespace handling' {
        It 'Leaves no trailing whitespace on any line' {
            # The whole reason the function exists. On Windows PowerShell every line comes back padded out
            # to the requested width, so a hashtable rendered at 16383 carries ~16KB of padding per line.
            $result = Out-Probe -Value @{ Key1 = 1 }
            $result.Split([System.String[]]("`r`n", "`n"), [System.StringSplitOptions]::None) | Should -Not -Match '\s$'
        }

        It 'Leaves no trailing whitespace on any streamed line' {
            Out-Probe -Value @{ Key1 = 1 } -Stream | Should -Not -Match '\s$'
        }

        It 'Is shorter than the raw Out-String it wraps' -Skip:($PSVersionTable.PSVersion.Major -ge 6) {
            # Guarded because only Windows PowerShell pads. On PowerShell 7 the two are already the same
            # length, so the comparison would assert nothing rather than fail.
            $padded = InModuleScope -ModuleName PSAppDeployToolkit { Out-String -InputObject @{ Key1 = 1 } -Width 16383 }
            (Out-Probe -Value @{ Key1 = 1 }).Length | Should -BeLessThan $padded.Length
        }

        It 'Keeps the blank lines that separate the rendered output' {
            # Callers append the result to a log message and .Trim() it themselves, so the surrounding
            # blank lines have to survive rather than be collapsed along with the padding.
            Get-LineLength -Text (Out-Probe -Value @{ Key1 = 1 }) | Should -BeGreaterThan 3
        }
    }

    Context 'Return shape' {
        It 'Returns one string' {
            $result = Out-Probe -Value @{ Key1 = 1 }
            $result | Should -BeOfType ([System.String])
            @($result).Count | Should -Be 1
        }

        It 'Returns one string per line with -Stream' {
            $lines = @(Out-Probe -Value @{ Key1 = 1 } -Stream)
            $lines.Count | Should -Be (Get-LineLength -Text (Out-Probe -Value @{ Key1 = 1 }))
            $lines | ForEach-Object { $_ | Should -BeOfType ([System.String]) }
        }

        It 'Renders the same text either way' {
            [System.String]::Join([System.Environment]::NewLine, @(Out-Probe -Value @{ Key1 = 1 } -Stream)) | Should -BeExactly (Out-Probe -Value @{ Key1 = 1 })
        }

        It 'Does not wrap or truncate a long line' {
            # 16383 is the width asked of Out-String. A value well past the console width has to come back
            # whole, because the callers log it rather than display it.
            $long = 'x' * 500
            Out-Probe -Value ([pscustomobject]@{ Long = $long }) | Should -Match ([System.Text.RegularExpressions.Regex]::Escape($long))
        }
    }

    Context 'Formatting data' {
        It 'Renders a Format-List piped in' {
            # Format-* emits a stream of records that only renders as a set. Handing them to Out-String one
            # at a time throws a NullReferenceException from inside the formatter.
            $result = InModuleScope -ModuleName PSAppDeployToolkit { [pscustomobject]@{ A = 'x'; B = 'y' } | Format-List | Out-ADTString }
            $result | Should -Match 'A : x'
            $result | Should -Match 'B : y'
        }

        It 'Renders a Format-Table piped in' {
            $result = InModuleScope -ModuleName PSAppDeployToolkit { [pscustomobject]@{ A = 'x'; B = 'y' } | Format-Table | Out-ADTString }
            $result | Should -Match 'A\s+B'
            $result | Should -Match 'x\s+y'
        }

        It 'Renders a Format-List handed over as a single value' {
            # Resolve-ADTErrorRecord passes the whole record set as one -InputObject rather than piping it.
            $result = InModuleScope -ModuleName PSAppDeployToolkit { Out-ADTString -InputObject (Format-List -InputObject ([pscustomobject]@{ A = 'x' })) }
            $result | Should -Match 'A : x'
        }
    }

    Context 'Pipeline input' {
        It 'Renders everything piped in as one string' {
            $result = Out-PipelineProbe -Value @(1, 2, 3)
            @($result).Count | Should -Be 1
            $result.Split([System.String[]]("`r`n", "`n"), [System.StringSplitOptions]::None) | Should -Be @('1', '2', '3')
        }

        It 'Renders a value handed over by name the same as one piped in' {
            Out-PipelineProbe -Value @{ Key1 = 1 } | Should -BeExactly (Out-Probe -Value @{ Key1 = 1 })
        }
    }

    Context 'Emptiness' {
        # Every caller uses the result as a truth test in place of the [String]::IsNullOrWhiteSpace((Out-String ...))
        # it replaced, so anything that renders as nothing has to come back as nothing rather than throw.
        It 'Returns nothing for <Name> handed over by name' -ForEach @(
            @{ Name = 'null'; Value = $null }
            @{ Name = 'an empty string'; Value = '' }
            @{ Name = 'whitespace'; Value = '   ' }
            @{ Name = 'an empty collection'; Value = @() }
            @{ Name = 'an empty hashtable'; Value = @{} }
        ) {
            # An empty hashtable is in here because Remove-ADTHashtableNullOrEmptyValues leans on it: a
            # nested section left with no keys after its own pass has to be dropped along with the rest.
            $result = Out-Probe -Value $Value
            $result | Should -BeNullOrEmpty
            [System.Boolean]$result | Should -BeFalse
        }

        It 'Returns nothing for <Name> piped in' -ForEach @(
            @{ Name = 'null'; Value = $null }
            @{ Name = 'an empty string'; Value = '' }
            @{ Name = 'whitespace'; Value = '   ' }
            @{ Name = 'an empty collection'; Value = @() }
        ) {
            $result = Out-PipelineProbe -Value $Value
            $result | Should -BeNullOrEmpty
            [System.Boolean]$result | Should -BeFalse
        }

        It 'Returns nothing for null with -Stream' {
            Out-Probe -Value $null -Stream | Should -BeNullOrEmpty
        }

        It 'Returns <Expected> for <Name>, which is not empty' -ForEach @(
            @{ Name = 'zero'; Value = 0; Expected = '0' }
            @{ Name = 'false'; Value = $false; Expected = 'False' }
        ) {
            # A value that renders to something has to stay, no matter how false-looking it is. Dropping
            # either of these would silently discard a bound parameter in Open-ADTSession.
            $result = Out-Probe -Value $Value
            [System.Boolean]$result | Should -BeTrue
            $result.Trim() | Should -BeExactly $Expected
        }
    }

    Context 'Parameters' {
        It 'Requires InputObject' {
            # Asked of the declaration rather than by leaving the argument out, because a
            # host able to prompt does exactly that and hangs the run waiting on input.
            Test-ADTMandatoryParameter -Command (InModuleScope PSAppDeployToolkit { Get-Command Out-ADTString }) -Parameter InputObject | Should -BeTrue
        }

        It 'Rethrows a failure against itself' {
            # The catch calls ThrowTerminatingError so the error is attributed to this function rather than
            # to the Out-String buried inside it. Nothing a caller can pass reaches it, hence the mock.
            InModuleScope -ModuleName PSAppDeployToolkit {
                Mock Out-String { throw 'render failed' }
                { Out-ADTString -InputObject 'anything' } | Should -Throw -ExceptionType ([System.Management.Automation.RuntimeException])
            }
        }
    }
}
