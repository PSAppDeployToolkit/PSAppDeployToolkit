BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'Convert-ADTValuesFromRemainingArguments' {
    Context 'Functionality' {
        # The token-by-token parsing is covered by PowerShellUtilitiesTests on the C# side. What is checked
        # here is the wrapper: that it hands the list over intact and returns something callers can splat.
        It 'Returns a dictionary that can still be added to' {
            # The source insists in capitals that this must not be read-only, because callers adjust it
            # before splatting it onward.
            $values = Convert-ADTValuesFromRemainingArguments -RemainingArguments @('-Name', 'Notepad')
            $values.IsReadOnly | Should -BeFalse
            $values.Add('Added', 'later')
            $values['Added'] | Should -BeExactly 'later'
        }

        It 'Pairs each parameter with the value that follows it' {
            $values = Convert-ADTValuesFromRemainingArguments -RemainingArguments @('-Name', 'Notepad', '-Count', 3)
            $values['Name'] | Should -BeExactly 'Notepad'
            $values['Count'] | Should -Be 3
        }

        It 'Turns a parameter with no value into a present switch' {
            $values = Convert-ADTValuesFromRemainingArguments -RemainingArguments @('-Force')
            $values['Force'] | Should -BeOfType ([System.Management.Automation.SwitchParameter])
            $values['Force'].IsPresent | Should -BeTrue
        }

        It 'Matches parameter names without regard to case' {
            $values = Convert-ADTValuesFromRemainingArguments -RemainingArguments @('-Name', 'Notepad')
            $values['NAME'] | Should -BeExactly 'Notepad'
        }

        It 'Treats <Case> as no arguments at all' -ForEach @(
            @{ Case = 'null'; Value = $null }
            @{ Case = 'an empty collection'; Value = @() }
        ) {
            # A ValueFromRemainingArguments parameter that bound nothing arrives as null, which the
            # parameter's [AllowNull()] promises to accept.
            $values = Convert-ADTValuesFromRemainingArguments -RemainingArguments $Value
            $values | Should -Not -BeNullOrEmpty
            $values.Count | Should -Be 0
        }

        It 'Round-trips through a ValueFromRemainingArguments parameter' {
            # How the function is actually reached, rather than with a hand-built list.
            function Test-RemainingArgumentsProbe
            {
                param
                (
                    [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true)]
                    [System.Collections.Generic.IReadOnlyList[System.Object]]$Parameters
                )

                return Convert-ADTValuesFromRemainingArguments -RemainingArguments $Parameters
            }

            (Test-RemainingArgumentsProbe -Name 'Notepad')['Name'] | Should -BeExactly 'Notepad'
            (Test-RemainingArgumentsProbe).Count | Should -Be 0
        }
    }
}
