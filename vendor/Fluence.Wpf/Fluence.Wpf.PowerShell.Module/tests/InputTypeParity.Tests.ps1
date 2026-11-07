<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Enforces the New-FluencePrompt -InputType contract across the files that copy it by hand. The
# ValidateSet on New-FluencePrompt is the source of truth. Every value must build a control through
# New-FluenceInputControl (which throws for a type it does not map), have one row in the reference
# table docs/powershell/reference/input-types.md, and be counted in the reference README. Adding a
# type without all three fails here.
#
# Controls are built through Invoke-OnFluenceUi, as in New-FluenceInputControl.Tests.ps1, so the
# build runs on an STA thread on every host shape: inline on STA (powershell, pwsh) and on the
# module-owned STA runspace on an MTA host (pwsh -mta). Only strings cross the runspace boundary.

BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
    Import-Module $script:ModulePath -Force

    $script:ReferenceRoot = Join-Path $PSScriptRoot '..\..\docs\powershell\reference'
    $script:InputTypes = @((Get-Command New-FluencePrompt).Parameters['InputType'].Attributes.ValidValues)

    # Builds one control per input type on the UI thread. Returns one 'Type=OK ControlType' or
    # 'Type=FAILED message' entry per type. The entries travel as one joined string so the result
    # has the same shape on the inline and runspace routes.
    $script:BuildEach = {
        param([string[]]$types)
        $joined = & (Get-Module Fluence.Wpf.PowerShell) {
            param($typeList)
            Invoke-OnFluenceUi -Script {
                param($typeList2)
                $outcomes = foreach ($type in $typeList2.Split(','))
                {
                    $pp = @{ Name = 'Field'; Message = 'm'; InputType = $type }
                    if ($type -eq 'Choice' -or $type -eq 'List')
                    {
                        $pp.ValidateSet = @('A', 'B')
                    }
                    try
                    {
                        $prompt = New-FluencePrompt @pp
                        $control = New-FluenceInputControl -Prompt $prompt -State @{ Result = @{} }
                        '{0}=OK {1}' -f $type, $control.GetType().FullName
                    }
                    catch
                    {
                        '{0}=FAILED {1}' -f $type, $_.Exception.Message
                    }
                }
                $outcomes -join '|'
            } -ArgumentList @($typeList)
        } ($types -join ',')
        [string[]]([string]$joined).Split('|')
    }

    # Returns the input type names from the first column of the "| Input type |" table.
    $script:DocumentedTypes = {
        $lines = @(Get-Content -LiteralPath (Join-Path $script:ReferenceRoot 'input-types.md') -Encoding UTF8)
        $start = -1
        for ($i = 0; $i -lt $lines.Count; $i++)
        {
            if ($lines[$i] -match '^\|\s*Input type\s*\|')
            {
                $start = $i
                break
            }
        }
        if ($start -lt 0)
        {
            throw 'input-types.md has no "| Input type |" table.'
        }

        # Skip the header and its separator row; the table ends at the first line that is not a row.
        for ($i = $start + 2; $i -lt $lines.Count -and $lines[$i].StartsWith('|'); $i++)
        {
            if ($lines[$i] -match '^\|\s*`([^`]+)`\s*\|')
            {
                $Matches[1]
            }
        }
    }
}

Describe 'New-FluencePrompt input type parity' {

    It 'reads the input types from the New-FluencePrompt ValidateSet' {
        $script:InputTypes.Count | Should -BeGreaterThan 0
        $script:InputTypes | Should -Contain 'Text'
    }

    It 'builds a control for every input type through New-FluenceInputControl' {
        $outcomes = @(& $script:BuildEach $script:InputTypes)
        $failures = @($outcomes | Where-Object { $_ -notlike '*=OK *' })
        $failures -join '; ' | Should -BeNullOrEmpty -Because 'every New-FluencePrompt input type needs a New-FluenceInputControl branch'
        $outcomes.Count | Should -Be $script:InputTypes.Count
    }

    It 'throws for an input type with no branch instead of rendering a TextBox' {
        $message = & (Get-Module Fluence.Wpf.PowerShell) {
            Invoke-OnFluenceUi -Script {
                $prompt = [pscustomobject]@{ Name = 'Field'; Message = 'm'; InputType = 'Unmapped'; DefaultValue = $null }
                try
                {
                    $null = New-FluenceInputControl -Prompt $prompt -State @{ Result = @{} }
                    'no error'
                }
                catch
                {
                    $_.Exception.Message
                }
            }
        }
        $message | Should -BeLike "*no control for InputType 'Unmapped'*"
    }

    It 'documents every input type in one input-types.md table row' {
        $documented = @(& $script:DocumentedTypes)
        $missing = @($script:InputTypes | Where-Object { $documented -notcontains $_ })
        $extra = @($documented | Where-Object { $script:InputTypes -notcontains $_ })
        $missing -join ', ' | Should -BeNullOrEmpty -Because 'each New-FluencePrompt input type needs a row in input-types.md'
        $extra -join ', ' | Should -BeNullOrEmpty -Because 'input-types.md must not document a type New-FluencePrompt rejects'
        $documented.Count | Should -Be $script:InputTypes.Count -Because 'each input type has exactly one row'
    }

    It 'states the input type count in the reference README' {
        $readme = Get-Content -LiteralPath (Join-Path $script:ReferenceRoot 'README.md') -Raw -Encoding UTF8
        $match = [regex]::Match($readme, 'lists all (\d+) `New-FluencePrompt -InputType` values')
        $match.Success | Should -BeTrue -Because 'the reference README states how many input types there are'
        [int]$match.Groups[1].Value | Should -Be $script:InputTypes.Count
    }
}
