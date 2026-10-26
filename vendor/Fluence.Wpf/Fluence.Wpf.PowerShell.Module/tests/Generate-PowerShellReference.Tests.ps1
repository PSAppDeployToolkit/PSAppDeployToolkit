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


BeforeAll {
    $script:referenceScript = Join-Path $PSScriptRoot '../../tools/Docs/Generate-PowerShellReference.ps1'
    function New-DocusaurusHelp
    {
        [CmdletBinding()]
        [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
            Justification = 'Test-only command signature; every invocation is mocked into the Pester TestDrive.')]
        param([string]$Module, [string]$DocsFolder, [string]$Sidebar)
        throw "The fixture generator must be mocked: $Module, $DocsFolder, $Sidebar"
    }
    function Get-ReferenceSnapshot
    {
        param([string]$Path)
        return @(Get-ChildItem -LiteralPath $Path -File | Sort-Object Name | ForEach-Object {
            '{0}:{1}' -f $_.Name, (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }) -join '|'
    }
}

Describe 'PowerShell reference check mode' {
    BeforeEach {
        $script:docsFixture = Join-Path $TestDrive ([System.Guid]::NewGuid().ToString('N'))
        $manifestRoot = Join-Path $script:docsFixture 'Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell'
        $script:referenceRoot = Join-Path $script:docsFixture 'docs/powershell/reference'
        $null = New-Item -ItemType Directory -Path $manifestRoot, $script:referenceRoot -Force
        [System.IO.File]::WriteAllText((Join-Path $manifestRoot 'Fluence.Wpf.PowerShell.psd1'), "@{ FunctionsToExport = @('Get-FluenceAlpha', 'Show-FluenceBeta') }")
        foreach ($authoredPageName in @('README.md', 'input-types.md', 'result-objects.md'))
        {
            [System.IO.File]::WriteAllText((Join-Path $script:referenceRoot $authoredPageName), 'authored reference ' + $authoredPageName)
        }
        Mock Get-Module { [pscustomobject]@{ Version = [version]'2.0.1'; Path = 'fixture-generator.psm1' } } -ParameterFilter { $Name -eq 'Alt3.Docusaurus.Powershell' }
        Mock Import-Module { } -ParameterFilter { $Name -eq 'fixture-generator.psm1' }
        Mock New-DocusaurusHelp {
            param($DocsFolder)
            $root = Join-Path $DocsFolder 'reference'
            $null = New-Item -ItemType Directory -Path $root -Force
            foreach ($commandName in @('Get-FluenceAlpha', 'Show-FluenceBeta'))
            {
                [System.IO.File]::WriteAllText((Join-Path $root ($commandName + '.mdx')), "# $commandName`r`nFixture reference.`r`n", [System.Text.UTF8Encoding]::new($true))
            }
        }
        & $script:referenceScript -RepositoryRoot $script:docsFixture
    }

    It 'checks the exact normalized writer bytes without changing authored or generated pages' {
        $before = Get-ReferenceSnapshot $script:referenceRoot
        & $script:referenceScript -RepositoryRoot $script:docsFixture -Check
        Get-ReferenceSnapshot $script:referenceRoot | Should -BeExactly $before
        $bytes = [System.IO.File]::ReadAllBytes((Join-Path $script:referenceRoot 'Get-FluenceAlpha.mdx'))
        ($bytes[0..2] -join ',') | Should -BeExactly '239,187,191'
        $bytes | Should -Not -Contain 13
        @((Get-ChildItem -LiteralPath $script:referenceRoot -File)).Count | Should -Be 5
    }

    It 'reports <Problem> page drift and leaves every existing byte unchanged' -ForEach @(
        @{ Problem = 'changed'; Name = 'Get-FluenceAlpha.mdx' }
        @{ Problem = 'missing'; Name = 'Get-FluenceAlpha.mdx' }
        @{ Problem = 'extra'; Name = 'Get-FluenceObsolete.mdx' }
        @{ Problem = 'legacy'; Name = 'Get-FluenceAlpha.md' }
    ) {
        $page = Join-Path $script:referenceRoot $Name
        if ($Problem -eq 'missing') { Remove-Item -LiteralPath $page }
        else { [System.IO.File]::WriteAllText($page, 'drift marker') }
        $before = Get-ReferenceSnapshot $script:referenceRoot
        $failure = { & $script:referenceScript -RepositoryRoot $script:docsFixture -Check } | Should -Throw -PassThru
        $failure.Exception.Message | Should -Match ([regex]::Escape($Name))
        Get-ReferenceSnapshot $script:referenceRoot | Should -BeExactly $before
    }
}
