BeforeDiscovery {
    # The names its callers ask for are read out of the source rather than listed here, so a call site
    # added later is covered the moment it is written. Only the parser is needed, not the module itself.
    $ModuleFunctionRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, '..', '..', '..', 'PSAppDeployToolkit'))
    $SeenCallSites = [System.Collections.Generic.HashSet[System.String]]::new([System.StringComparer]::Ordinal)
    $CallSites = foreach ($file in (Get-ChildItem -Path ([System.IO.Path]::Combine($ModuleFunctionRoot, 'Private')), ([System.IO.Path]::Combine($ModuleFunctionRoot, 'Public')) -Filter *.ps1 -File))
    {
        $fileAst = [System.Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$null, [ref]$null)
        foreach ($call in $fileAst.FindAll({ ($args[0] -is [System.Management.Automation.Language.CommandAst]) -and ($args[0].GetCommandName() -eq 'Get-ADTCommand') }, $true))
        {
            # Only a literal can be checked from here. The callers naming themselves through $MyInvocation
            # resolve to their own name, which the table carries by construction.
            foreach ($element in ($call.CommandElements | Select-Object -Skip 1))
            {
                if (($element -is [System.Management.Automation.Language.StringConstantExpressionAst]) -and $SeenCallSites.Add("$($file.BaseName)/$($element.Value)"))
                {
                    @{ Caller = $file.BaseName; Name = $element.Value }
                }
            }
        }
    }

    # A sweep that found nothing would generate no tests at all and report as a clean pass.
    if (!$CallSites)
    {
        throw "Unable to find any call to [Get-ADTCommand] naming a literal command within the module's source."
    }
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'Get-ADTCommand' {
    Context 'Lookups' {
        It 'Hands back the entry the module''s own table is holding' {
            # Read out of the table rather than resolved on the spot. A cmdlet is what shows the difference:
            # Get-Command builds a fresh CmdletInfo each time, where the table holds the one it captured as
            # the module imported. A function would prove nothing, being the same instance either way.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $command = Get-ADTCommand -Name Remove-Item
                $command | Should -BeOfType ([System.Management.Automation.CommandInfo])
                [System.Object]::ReferenceEquals($command, $Script:CommandTable['Remove-Item']) | Should -BeTrue
            }
        }

        It 'Reaches the private functions the public command table filters out' {
            # The difference between the two. Get-ADTCommandTable hands extending modules a table with the
            # private functions stripped; this is the module's own view, and carries everything it can call.
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Get-ADTCommandTable).ContainsKey('Get-ADTModuleState') | Should -BeFalse
                (Get-ADTCommand -Name Get-ADTModuleState).CommandType | Should -Be ([System.Management.Automation.CommandTypes]::Function)
            }
        }

        It 'Reaches <Name>, imported by the module for its own use' -ForEach @(
            @{ Name = 'Remove-Item'; Source = 'Microsoft.PowerShell.Management' }
            @{ Name = 'New-Variable'; Source = 'Microsoft.PowerShell.Utility' }
            @{ Name = 'Dismount-WindowsImage'; Source = 'Dism' }
        ) {
            # All three are asked for by name at a call site, and none of them is a PSADT function.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Name = $Name; Source = $Source } {
                $command = Get-ADTCommand -Name $Name
                $command.CommandType | Should -Be ([System.Management.Automation.CommandTypes]::Cmdlet)
                $command.Source | Should -BeExactly $Source
            }
        }

        It 'Hands back something the caller can invoke' {
            # A CommandInfo that cannot be called through is no use to the call sites, every one of which
            # either invokes it or passes it to something that will.
            InModuleScope -ModuleName PSAppDeployToolkit {
                & (Get-ADTCommand -Name Out-ADTPowerShellEncodedCommand) -Command 'Get-Process' | Should -BeExactly 'RwBlAHQALQBQAHIAbwBjAGUAcwBzAA=='
            }
        }

        It 'Ignores a command of the same name defined outside the module' {
            # Why the name is looked up in the table instead of resolved. A function defined in the caller's
            # session shadows the real command for Get-Command, and must not change what the module calls.
            # Dism's cmdlet stands in for the shadowed command because nothing else in the suite goes near it.
            $null = New-Item -Path Function:\global:Dismount-WindowsImage -Value { 'impostor' }
            try
            {
                InModuleScope -ModuleName PSAppDeployToolkit {
                    (Get-Command -Name Dismount-WindowsImage).CommandType | Should -Be ([System.Management.Automation.CommandTypes]::Function)
                    (Get-ADTCommand -Name Dismount-WindowsImage).CommandType | Should -Be ([System.Management.Automation.CommandTypes]::Cmdlet)
                }
            }
            finally
            {
                Remove-Item -LiteralPath Function:\Dismount-WindowsImage -Force
            }
        }
    }

    Context 'Refusal' {
        It 'Refuses a name the table does not carry' {
            # A miss is a defect in the module rather than bad input from a user, which is why it terminates
            # instead of returning nothing.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Get-ADTCommand -Name Get-NotARealCommand } | Should -Throw -ExceptionType ([System.Management.Automation.CommandNotFoundException]) -ErrorId 'CommandNotFoundException,Get-ADTCommand' -ExpectedMessage "The term 'Get-NotARealCommand' is not recognized*"
            }
        }

        It 'Names the command it could not find as the target' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                $record = { Get-ADTCommand -Name Get-NotARealCommand } | Should -Throw -PassThru
                $record.TargetObject | Should -BeExactly 'Get-NotARealCommand'
            }
        }

        It 'Refuses regardless of the error preference it was called with' {
            # -ErrorAction cannot suppress a ThrowTerminatingError. Worth pinning, because a caller able to
            # turn a miss into a $null would push the failure somewhere with no connection to the name.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Get-ADTCommand -Name Get-NotARealCommand -ErrorAction SilentlyContinue } | Should -Throw -ErrorId 'CommandNotFoundException,Get-ADTCommand'
                { Get-ADTCommand -Name Get-NotARealCommand -ErrorAction Ignore } | Should -Throw -ErrorId 'CommandNotFoundException,Get-ADTCommand'
            }
        }

        It 'Refuses a name that differs only in case' {
            # The table is an ordinal dictionary, so a lookup is an exact match. Every caller names its
            # command as declared, and one that does not is told so rather than quietly missing.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Get-ADTCommand -Name get-adtcommandtable } | Should -Throw -ErrorId 'CommandNotFoundException,Get-ADTCommand'
            }
        }
    }

    Context 'Input Validation' {
        It 'Requires a Name' {
            Test-ADTMandatoryParameter -Command (InModuleScope PSAppDeployToolkit { Get-Command Get-ADTCommand }) -Parameter Name | Should -BeTrue
        }

        It 'Refuses a name that is <Description>' -ForEach @(
            @{ Description = 'empty'; Name = '' }
            @{ Description = 'only spaces'; Name = '   ' }
            @{ Description = 'only a tab'; Name = "`t" }
        ) {
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Name = $Name } {
                { Get-ADTCommand -Name $Name } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentValidationError,Get-ADTCommand'
            }
        }
    }

    Context 'Callers' {
        It 'Resolves [<Name>], which <Caller> asks it for' -ForEach $CallSites {
            # Each name here is a literal in the module's source. A typo in one would otherwise surface only
            # when that code path runs on somebody's machine.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Name = $Name } {
                (Get-ADTCommand -Name $Name).Name | Should -BeExactly $Name
            }
        }
    }
}
