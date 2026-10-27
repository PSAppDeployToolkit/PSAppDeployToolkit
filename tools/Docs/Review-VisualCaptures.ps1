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
    [Parameter(Mandatory)][string]$CandidatesPath,
    [Parameter(Mandatory)][string]$WebsiteRoot,
    [string]$CaptureOutputRoot = (Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'artifacts\docs'),
    [string]$MapPath = (Join-Path $PSScriptRoot 'VisualCaptureMap.psd1'),
    [string]$WebsiteManifestPath = (Join-Path $WebsiteRoot 'source-docs-manifest.json'),
    [string]$DecisionsPath,
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName PresentationCore
. (Join-Path $PSScriptRoot 'SourceDocsManifest.ps1')

function Get-FileHashLower
{
    param([string]$LiteralPath)
    return (Get-FileHash -LiteralPath $LiteralPath -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-DecodedPng
{
    param([string]$LiteralPath)
    $stream = [System.IO.File]::OpenRead($LiteralPath)
    try
    {
        $decoder = [System.Windows.Media.Imaging.BitmapDecoder]::Create($stream,
            [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,
            [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
        $frame = $decoder.Frames[0]
        $converted = [System.Windows.Media.Imaging.FormatConvertedBitmap]::new(
            $frame, [System.Windows.Media.PixelFormats]::Bgra32, $null, 0)
        $stride = $converted.PixelWidth * 4
        $pixels = [byte[]]::new($stride * $converted.PixelHeight)
        $converted.CopyPixels($pixels, $stride, 0)
        $hash = [System.Security.Cryptography.SHA256]::HashData($pixels)
        return [pscustomobject]@{
            width = $converted.PixelWidth
            height = $converted.PixelHeight
            pixelSha256 = [System.Convert]::ToHexString($hash).ToLowerInvariant()
        }
    }
    finally { $stream.Dispose() }
}

$candidateData = Get-Content -LiteralPath $CandidatesPath -Raw | ConvertFrom-Json
if ($candidateData.schemaVersion -ne 1) { throw 'Unsupported candidate schema.' }
$map = Import-PowerShellDataFile -LiteralPath $MapPath -SkipLimitCheck
if ($map.SchemaVersion -ne 1) { throw 'Unsupported visual capture map schema.' }
$mapById = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)
foreach ($spec in $map.Captures) { $mapById[[string]$spec.Id] = $spec }
$submitted = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($candidate in $candidateData.candidates)
{
    if (-not $submitted.Add([string]$candidate.id) -or -not $mapById.ContainsKey([string]$candidate.id))
    {
        throw "Unknown or duplicate capture candidate: $($candidate.id)"
    }
    $spec = $mapById[[string]$candidate.id]
    if ($candidate.path -cne $spec.Path -or $candidate.sourceRelativePath -cne $spec.SourceRelativePath -or
        $candidate.mode -cne $spec.Mode -or $candidate.theme -cne $spec.Theme -or $candidate.state -cne $spec.State)
    {
        throw "Capture candidate differs from authored map: $($candidate.id)"
    }
}
foreach ($candidate in $candidateData.candidates)
{
    $family = [string]$mapById[[string]$candidate.id].Family
    foreach ($spec in $map.Captures)
    {
        if ($spec.Family -ceq $family -and -not $submitted.Contains([string]$spec.Id))
        {
            throw "Incomplete capture family $family; missing $($spec.Id)."
        }
    }
}
$baseline = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)
if ([System.IO.File]::Exists($WebsiteManifestPath))
{
    $manifest = Get-Content -LiteralPath $WebsiteManifestPath -Raw | ConvertFrom-Json
    if ($manifest.PSObject.Properties['files'])
    {
        foreach ($file in $manifest.files)
        {
            if ($file.PSObject.Properties['path']) { $baseline[[string]$file.path] = $file }
        }
    }
    if ($manifest.PSObject.Properties['captures'])
    {
        foreach ($file in $manifest.captures) { $baseline[[string]$file.path] = $file }
    }
}
$decisions = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)
if ($DecisionsPath)
{
    $decisionData = Get-Content -LiteralPath $DecisionsPath -Raw | ConvertFrom-Json
    foreach ($decision in $decisionData.decisions)
    {
        if (-not $decisions.TryAdd([string]$decision.id, $decision)) { throw "Duplicate decision: $($decision.id)" }
    }
}
$approved = [System.Collections.Generic.List[object]]::new()
$pending = [System.Collections.Generic.List[object]]::new()
$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($candidate in $candidateData.candidates)
{
    if (-not $seen.Add([string]$candidate.id)) { throw "Duplicate candidate: $($candidate.id)" }
    $publishedPath = Get-DocsContainedPath -Root $WebsiteRoot -RelativePath $candidate.path
    $capturedPath = Get-DocsContainedPath -Root $CaptureOutputRoot -RelativePath $candidate.sourceRelativePath
    $old = $null
    if ($baseline.ContainsKey([string]$candidate.path)) { $old = $baseline[[string]$candidate.path] }
    if (-not [System.IO.File]::Exists($capturedPath))
    {
        $pending.Add([pscustomobject]@{ id = $candidate.id; reason = 'missing-capture-or-baseline'; before = $publishedPath; after = $capturedPath })
        continue
    }
    if (-not [System.IO.File]::Exists($publishedPath))
    {
        if (-not $candidate.id.StartsWith('controls/', [System.StringComparison]::Ordinal))
        {
            $pending.Add([pscustomobject]@{ id = $candidate.id; reason = 'missing-capture-or-baseline'; before = $publishedPath; after = $capturedPath })
            continue
        }
        # A recorded baseline missing from disk is not a new capture.
        if ($null -ne $old)
        {
            $pending.Add([pscustomobject]@{ id = $candidate.id; reason = 'missing-capture-or-baseline'; before = $publishedPath; after = $capturedPath })
            continue
        }
        $afterHash = Get-FileHashLower $capturedPath
        $after = Get-DecodedPng $capturedPath
        $decision = $null
        if ($decisions.ContainsKey([string]$candidate.id)) { $decision = $decisions[[string]$candidate.id] }
        if ($null -eq $decision -or $decision.decision -cne 'new-capture' -or
            -not $decision.PSObject.Properties['candidatePngSha256'] -or $decision.candidatePngSha256 -cne $afterHash -or
            -not $decision.PSObject.Properties['provenance'] -or [string]::IsNullOrWhiteSpace($decision.provenance))
        {
            $pending.Add([pscustomobject]@{ id = $candidate.id; reason = 'new-capture-review-required'; before = $publishedPath; after = $capturedPath })
            continue
        }
        $approved.Add([pscustomobject]@{
            id = $candidate.id; path = $candidate.path; sourceRelativePath = $candidate.sourceRelativePath
            sha256 = $afterHash; pngSha256 = $afterHash; decodedPixelSha256 = $after.pixelSha256
            checkedVisualInputSha256 = $candidate.checkedVisualInputSha256; inputs = @($candidate.inputs)
            theme = $candidate.theme; state = $candidate.state; mode = $candidate.mode
            decision = 'new-capture'; provenance = [string]$decision.provenance
        })
        continue
    }
    if ($candidate.id.StartsWith('powershell/images/', [System.StringComparison]::Ordinal) -and
        ($null -eq $old -or -not $old.PSObject.Properties['mode'] -or -not $old.mode))
    {
        $pending.Add([pscustomobject]@{ id = $candidate.id; reason = 'baseline-capture-mode-unverified'; before = $publishedPath; after = $capturedPath })
        continue
    }
    if ($null -ne $old -and $old.PSObject.Properties['mode'] -and $old.mode -and $old.mode -cne $candidate.mode)
    {
        $pending.Add([pscustomobject]@{ id = $candidate.id; reason = 'capture-mode-mismatch'; before = $publishedPath; after = $capturedPath })
        continue
    }
    $beforeHash = Get-FileHashLower $publishedPath
    if ($null -ne $old -and $old.PSObject.Properties['pngSha256'] -and $old.pngSha256 -and $old.pngSha256 -cne $beforeHash)
    {
        throw "Published baseline hash changed for $($candidate.id)."
    }
    $afterHash = Get-FileHashLower $capturedPath
    $before = Get-DecodedPng $publishedPath
    $after = Get-DecodedPng $capturedPath
    $samePixels = $before.width -eq $after.width -and $before.height -eq $after.height -and $before.pixelSha256 -ceq $after.pixelSha256
    $decision = $null
    if ($decisions.ContainsKey([string]$candidate.id)) { $decision = $decisions[[string]$candidate.id] }
    if ($samePixels)
    {
        $resultDecision = 'visually-unchanged'
        $provenance = 'decoded-pixel-identical'
        $pngHash = $beforeHash
        $pixelHash = $before.pixelSha256
    }
    elseif ($null -ne $decision -and $decision.decision -cin @('material-change','visually-unchanged') -and
        $decision.PSObject.Properties['candidatePngSha256'] -and $decision.candidatePngSha256 -ceq $afterHash -and
        $decision.PSObject.Properties['provenance'] -and -not [string]::IsNullOrWhiteSpace($decision.provenance))
    {
        $resultDecision = [string]$decision.decision
        $provenance = [string]$decision.provenance
        $pngHash = if ($resultDecision -eq 'material-change') { $afterHash } else { $beforeHash }
        $pixelHash = if ($resultDecision -eq 'material-change') { $after.pixelSha256 } else { $before.pixelSha256 }
    }
    else
    {
        $pending.Add([pscustomobject]@{
            id = $candidate.id; reason = 'material-review-required'; before = $publishedPath; after = $capturedPath
            beforeWidth = $before.width; beforeHeight = $before.height; beforePixelSha256 = $before.pixelSha256
            afterWidth = $after.width; afterHeight = $after.height; afterPixelSha256 = $after.pixelSha256
        })
        continue
    }
    $approved.Add([pscustomobject]@{
        id = $candidate.id; path = $candidate.path; sourceRelativePath = $candidate.sourceRelativePath
        sha256 = $pngHash; pngSha256 = $pngHash; decodedPixelSha256 = $pixelHash
        checkedVisualInputSha256 = $candidate.checkedVisualInputSha256; inputs = @($candidate.inputs)
        theme = $candidate.theme; state = $candidate.state; mode = $candidate.mode
        decision = $resultDecision; provenance = $provenance
    })
}
$result = [pscustomobject]@{ schemaVersion = 1; captures = @($approved.ToArray()); pending = @($pending.ToArray()) }
if ($OutputPath)
{
    if ($pending.Count -gt 0) { throw "Visual review is incomplete for $($pending.Count) capture(s); approved output was not written." }
    $json = [pscustomobject]@{ schemaVersion = 1; captures = @($approved.ToArray()) } | ConvertTo-Json -Depth 12
    $json = $json.Replace("`r`n", "`n") + "`n"
    [System.IO.File]::WriteAllText($OutputPath, $json, [System.Text.UTF8Encoding]::new($true))
}
$result
