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

[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$OutputRoot,
    [switch]$Check
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepositoryRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
if ([string]::IsNullOrWhiteSpace($OutputRoot)) { $OutputRoot = Join-Path $RepositoryRoot 'artifacts/docs/powershell/reference' }
$manifestPath = Join-Path $RepositoryRoot 'Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1'
if ([System.IO.Path]::DirectorySeparatorChar -eq '\')
{
    $outputPathRoot = [System.IO.Path]::GetPathRoot($OutputRoot)
    if ($outputPathRoot -eq '\' -or $outputPathRoot -eq '/' -or $outputPathRoot -match '^[A-Za-z]:$')
    {
        throw "OutputRoot '$OutputRoot' is an ambiguous Windows path. Use a fully qualified absolute path or a repository-relative path."
    }
}
if (-not [System.IO.Path]::IsPathRooted($OutputRoot)) { $OutputRoot = Join-Path $RepositoryRoot $OutputRoot }
$referencePath = [System.IO.Path]::GetFullPath($OutputRoot)
$manifest = Import-PowerShellDataFile -Path $manifestPath
$functionNames = @($manifest.FunctionsToExport)
if ($functionNames.Count -eq 0 -or @($functionNames | Where-Object { $_ -notmatch '^[A-Za-z]+-Fluence[A-Za-z0-9-]+$' }).Count -gt 0) {
    throw 'The module manifest has no supported exported function list.'
}

$generator = Get-Module -ListAvailable -Name Alt3.Docusaurus.Powershell |
    Where-Object { $_.Version -eq [version]'2.0.1' } |
    Select-Object -First 1
if ($null -eq $generator) {
    throw 'Alt3.Docusaurus.Powershell 2.0.1 is required. Install it with Install-Module -Name Alt3.Docusaurus.Powershell -RequiredVersion 2.0.1 -Scope CurrentUser.'
}
Import-Module $generator.Path -RequiredVersion 2.0.1 -ErrorAction Stop

$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('FluenceDocs-' + [guid]::NewGuid().ToString('N'))
$stagingRoot = [System.IO.Path]::GetFullPath($stagingRoot)
$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([char[]]'\/') + [System.IO.Path]::DirectorySeparatorChar
if (-not $stagingRoot.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $stagingRoot)) {
    throw "Generation staging must be a new directory inside '$tempRoot': $stagingRoot"
}
$stagingDocs = Join-Path $stagingRoot 'docs/powershell'
try {
    New-Item -ItemType Directory -Path $stagingDocs -Force | Out-Null
    New-DocusaurusHelp -Module $manifestPath -DocsFolder $stagingDocs -Sidebar 'reference'

    $generatedRoot = Join-Path $stagingDocs 'reference'
    $generated = @(Get-ChildItem -LiteralPath $generatedRoot -File -Filter '*.mdx' -ErrorAction SilentlyContinue)
    $generatedNames = @($generated | ForEach-Object { $_.BaseName })
    $missing = @($functionNames | Where-Object { $_ -notin $generatedNames })
    if ($missing.Count -gt 0 -or $generated.Count -ne $functionNames.Count) {
        throw ('PowerShell generator output is incomplete or unexpected. Missing: ' + ($missing -join ', ') + "; found $($generated.Count) pages for $($functionNames.Count) exported functions.")
    }

    # Share exact output bytes between Write and Check, including BOM and LF normalization.
    $pages = @{}
    $encoding = [System.Text.UTF8Encoding]::new($true)
    foreach ($page in $generated) {
        $content = [System.IO.File]::ReadAllText($page.FullName)
        $content = $content.TrimStart([char]0xFEFF).Replace("`r`n", "`n").Replace("`r", "`n")
        $pages[$page.Name] = [byte[]]($encoding.GetPreamble() + $encoding.GetBytes($content))
    }
    if ($Check) {
        $drift = [System.Collections.Generic.List[string]]::new()
        if (-not [System.IO.Directory]::Exists($referencePath)) { $drift.Add($referencePath) }
        foreach ($name in @($pages.Keys | Sort-Object)) {
            $path = Join-Path $referencePath $name
            if (-not [System.IO.File]::Exists($path) -or
                -not [System.Collections.StructuralComparisons]::StructuralEqualityComparer.Equals(
                    $pages[$name], [System.IO.File]::ReadAllBytes($path))) {
                $drift.Add($path)
            }
        }
        if (Test-Path -LiteralPath $referencePath) {
            foreach ($page in @(Get-ChildItem -LiteralPath $referencePath -File)) {
                if ($page.Name -match '^[A-Za-z]+-Fluence[A-Za-z0-9-]+\.(md|mdx)$' -and -not $pages.ContainsKey($page.Name)) {
                    $drift.Add($page.FullName)
                }
            }
        }
        if ($drift.Count -gt 0) {
            throw ("PowerShell reference drift:`n" + ($drift -join "`n"))
        }
        Write-Host "Checked $($pages.Count) PowerShell function pages in $referencePath."
        return
    }

    New-Item -ItemType Directory -Path $referencePath -Force | Out-Null
    # Old function Markdown and obsolete function MDX pages are replaced only after
    # every exported function has a fresh generated page. The three authored pages stay.
    foreach ($page in @(Get-ChildItem -LiteralPath $referencePath -File)) {
        if ($page.Name -match '^[A-Za-z]+-Fluence[A-Za-z0-9-]+\.(md|mdx)$' -and -not $pages.ContainsKey($page.Name)) {
            Remove-Item -LiteralPath $page.FullName
        }
    }
    foreach ($name in @($pages.Keys | Sort-Object)) {
        $destination = Join-Path $referencePath $name
        if (-not [System.IO.File]::Exists($destination) -or
            -not [System.Collections.StructuralComparisons]::StructuralEqualityComparer.Equals(
                $pages[$name], [System.IO.File]::ReadAllBytes($destination))) {
            [System.IO.File]::WriteAllBytes($destination, $pages[$name])
        }
    }
    Write-Host "Generated $($generated.Count) PowerShell function pages in $referencePath."
}
finally {
    if (Test-Path -LiteralPath $stagingRoot) {
        if (-not [System.IO.Path]::GetFullPath($stagingRoot).StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to clean generation staging outside '$tempRoot': $stagingRoot"
        }
        $entries = @(Get-Item -LiteralPath $stagingRoot -Force)
        $entries += @(Get-ChildItem -LiteralPath $stagingRoot -Recurse -Force)
        if (@($entries | Where-Object { $_.Attributes -band [System.IO.FileAttributes]::ReparsePoint }).Count -gt 0) {
            throw "Refusing to clean generation staging containing a reparse point: $stagingRoot"
        }
        [System.IO.Directory]::Delete($stagingRoot, $true)
    }
}
