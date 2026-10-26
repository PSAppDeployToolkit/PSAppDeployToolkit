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

param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$WebsiteDocsPath = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path 'Fluence.Wpf.Website/docs')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$sourceDocs = Join-Path $RepositoryRoot 'docs'
if (-not (Test-Path -LiteralPath $WebsiteDocsPath -PathType Container)) {
    throw "Website docs directory does not exist: $WebsiteDocsPath"
}

function Copy-IfChanged([string]$source, [string]$target) {
    $targetParent = Split-Path -Parent $target
    New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
    if (Test-Path -LiteralPath $target) {
        $sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
        $targetHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
        if ($sourceHash -eq $targetHash) { return }
    }
    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host "Updated $target"
}

$apiSource = Join-Path $sourceDocs 'api'
if (-not (Test-Path -LiteralPath $apiSource -PathType Container)) {
    throw "Source API directory does not exist: $apiSource. Generate the C# API pages before syncing."
}
$apiPages = @(Get-ChildItem -LiteralPath $apiSource -Recurse -File -Filter '*.md')
if ($apiPages.Count -eq 0 -or -not (Test-Path -LiteralPath (Join-Path $apiSource 'index.md'))) {
    throw 'Source docs/api is empty or missing index.md. Generate the C# API pages before syncing.'
}

$functionSource = Join-Path $sourceDocs 'powershell/reference'
if (-not (Test-Path -LiteralPath $functionSource -PathType Container)) {
    throw "Source PowerShell reference directory does not exist: $functionSource. Generate the function pages before syncing."
}
$functionPages = @(Get-ChildItem -LiteralPath $functionSource -File -Filter '*-Fluence*.mdx')
$manifestPath = Join-Path $RepositoryRoot 'Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw "PowerShell module manifest does not exist: $manifestPath" }
$manifest = Import-PowerShellDataFile -Path $manifestPath
$functionNames = @($manifest.FunctionsToExport)
$missing = @($functionNames | Where-Object { -not (Test-Path -LiteralPath (Join-Path $functionSource "$_.mdx") -PathType Leaf) })
if ($functionNames.Count -eq 0 -or $functionPages.Count -ne $functionNames.Count -or $missing.Count -gt 0) {
    throw ('Source PowerShell function pages are incomplete. Generate them before syncing. Missing: ' + ($missing -join ', '))
}
foreach ($name in 'README.md', 'input-types.md', 'result-objects.md') {
    $source = Join-Path $functionSource $name
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing authored PowerShell reference page: $source" }
}
foreach ($name in 'controls.md', 'theming.md', 'winui-parity.md') {
    $source = Join-Path $sourceDocs $name
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing authored source document: $source" }
}

$apiTarget = Join-Path $WebsiteDocsPath 'api'
$expectedApi = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($page in $apiPages) {
    $relative = [System.IO.Path]::GetRelativePath($apiSource, $page.FullName)
    [void]$expectedApi.Add($relative)
    Copy-IfChanged $page.FullName (Join-Path $apiTarget $relative)
}
foreach ($page in @(Get-ChildItem -LiteralPath $apiTarget -Recurse -File -Filter '*.md' -ErrorAction SilentlyContinue)) {
    $relative = [System.IO.Path]::GetRelativePath($apiTarget, $page.FullName)
    if (-not $expectedApi.Contains($relative)) {
        Remove-Item -LiteralPath $page.FullName
        Write-Host "Removed stale API page $($page.FullName)"
    }
}

$functionTarget = Join-Path $WebsiteDocsPath 'powershell/reference'
New-Item -ItemType Directory -Path $functionTarget -Force | Out-Null
foreach ($page in @(Get-ChildItem -LiteralPath $functionTarget -File)) {
    if ($page.Name -match '^[A-Za-z]+-Fluence[A-Za-z0-9-]+\.(md|mdx)$' -and
        $page.BaseName -notin $functionNames) {
        Remove-Item -LiteralPath $page.FullName
        Write-Host "Removed stale function page $($page.FullName)"
    }
}
foreach ($name in $functionNames) {
    $source = Join-Path $functionSource "$name.mdx"
    $target = Join-Path $functionTarget "$name.mdx"
    Copy-IfChanged $source $target
    $legacy = Join-Path $functionTarget "$name.md"
    if (Test-Path -LiteralPath $legacy) { Remove-Item -LiteralPath $legacy }
}

foreach ($name in 'README.md', 'input-types.md', 'result-objects.md') {
    $source = Join-Path $functionSource $name
    Copy-IfChanged $source (Join-Path $functionTarget $name)
}

foreach ($name in 'controls.md', 'theming.md', 'winui-parity.md') {
    $source = Join-Path $sourceDocs $name
    Copy-IfChanged $source (Join-Path $WebsiteDocsPath $name)
}

Write-Host "Synced API, PowerShell function, and authored control/theme/parity pages to $WebsiteDocsPath."
