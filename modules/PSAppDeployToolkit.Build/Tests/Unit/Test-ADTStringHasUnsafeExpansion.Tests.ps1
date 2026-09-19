BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    function Test-Probe
    {
        param
        (
            [Parameter(Mandatory = $true)]
            [AllowEmptyString()]
            [System.String]$InputString
        )

        InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Value = $InputString } {
            Test-ADTStringHasUnsafeExpansion -InputString $Value
        }
    }
}

Describe 'Test-ADTStringHasUnsafeExpansion' {
    Context 'What it refuses' {
        It 'Reports a subexpression: <Name>' -ForEach @(
            @{ Name = 'on its own'; Value = '$(2000+24)' }
            @{ Name = 'after a literal path'; Value = 'C:\Logs$(2000+24)' }
            @{ Name = 'calling a method'; Value = 'C:\Logs$([System.IO.File]::WriteAllText("C:\pwned.txt","x"))' }
            @{ Name = 'nested in text'; Value = 'before $(whatever) after' }
            @{ Name = 'more than one'; Value = '$env:Temp$(1)$(2)' }
        ) {
            # A subexpression evaluates whatever it holds, in whatever the toolkit is running as.
            Test-Probe -InputString $Value | Should -BeTrue
        }

        It 'Reports a braced provider path: <Name>' -ForEach @(
            @{ Name = 'a file path'; Value = '${C:\Windows\System32\drivers\etc\hosts}' }
            @{ Name = 'a file path in text'; Value = 'prefix ${C:\secret.txt} suffix' }
            @{ Name = 'a registry path'; Value = '${HKLM:\SOFTWARE\Policies}' }
            @{ Name = 'a UNC path'; Value = '${\server\share\file}' }
            @{ Name = 'a name holding a space'; Value = '${not a variable}' }
        ) {
            # A braced reference resolves through the provider, so this hands back the item's content
            # rather than a variable's value. It is a read of anything the caller can reach.
            Test-Probe -InputString $Value | Should -BeTrue
        }
    }

    Context 'What it allows' {
        It 'Allows a variable reference: <Name>' -ForEach @(
            @{ Name = 'an environment variable'; Value = '$env:ProgramData\SoftwareCache' }
            @{ Name = 'a plain variable'; Value = '$envProgramFiles' }
            @{ Name = 'a plain variable in text'; Value = 'before-$Probe-after' }
            @{ Name = 'a braced variable'; Value = '${Probe}' }
            @{ Name = 'a braced environment variable'; Value = '${env:ProgramData}' }
            @{ Name = 'a scoped variable'; Value = '${global:Probe}' }
            @{ Name = 'several at once'; Value = '$env:SystemRoot\Logs\$appName' }
        ) {
            # The toolkit has allowed values to name variables for a long time, and this must not change
            # which of them expand.
            Test-Probe -InputString $Value | Should -BeFalse
        }

        It 'Allows a string with nothing to expand: <Name>' -ForEach @(
            @{ Name = 'empty'; Value = '' }
            @{ Name = 'plain text'; Value = 'C:\Windows\Logs\Software' }
            @{ Name = 'a lone dollar'; Value = 'costs $ to run' }
            @{ Name = 'a dollar at the end'; Value = 'trailing $' }
            @{ Name = 'a brace that never closes'; Value = 'open ${ and no close' }
            @{ Name = 'a brace with no dollar'; Value = 'just {braces} here' }
        ) {
            Test-Probe -InputString $Value | Should -BeFalse
        }

        It 'Allows an escaped construct: <Name>' -ForEach @(
            @{ Name = 'an escaped subexpression'; Value = 'literally `$(not a subexpression)' }
            @{ Name = 'an escaped brace'; Value = 'literally `${C:\not-a-path}' }
            @{ Name = 'an escaped backtick before a real variable'; Value = '``$env:ProgramData' }
        ) {
            # A backtick escapes the dollar that follows, so the expander treats it as text and there is
            # nothing to refuse. The last case escapes the backtick itself, which leaves the variable live.
            Test-Probe -InputString $Value | Should -BeFalse
        }
    }

    Context 'Input Validation' {
        It 'Requires a string' {
            Test-ADTMandatoryParameter -Command (InModuleScope PSAppDeployToolkit { Get-Command Test-ADTStringHasUnsafeExpansion }) -Parameter InputString | Should -BeTrue
        }
    }
}
