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
    [string]$RepositoryRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
    [string]$MapPath = (Join-Path $PSScriptRoot 'VisualCaptureMap.psd1'),
    [string]$WebsiteManifestPath,
    [string[]]$CaptureId
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'SourceDocsManifest.ps1')

function Get-Sha256Utf8
{
    param([AllowEmptyString()][string]$Content)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Content)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    return [System.Convert]::ToHexString($hash).ToLowerInvariant()
}

function Get-VisualInputContent
{
    param([Parameter(Mandatory)][string]$LiteralPath)
    $bytes = [System.IO.File]::ReadAllBytes($LiteralPath)
    $decoder = [System.Text.UTF8Encoding]::new($false, $true)
    $content = $decoder.GetString($bytes).TrimStart([char]0xFEFF).Replace("`r`n", "`n").Replace("`r", "`n")
    if (-not $LiteralPath.EndsWith('.xaml', [System.StringComparison]::OrdinalIgnoreCase))
    {
        return $content
    }

    try
    {
        $settings = [System.Xml.XmlReaderSettings]::new()
        $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
        $settings.XmlResolver = $null
        $settings.IgnoreWhitespace = $false
        $document = [System.Xml.XmlDocument]::new()
        $document.PreserveWhitespace = $true
        $document.XmlResolver = $null
        $reader = [System.Xml.XmlReader]::Create([System.IO.StringReader]::new($content), $settings)
        try { $document.Load($reader) }
        finally { $reader.Dispose() }
        $comments = @($document.SelectNodes('//comment()'))
        foreach ($comment in $comments)
        {
            $null = $comment.ParentNode.RemoveChild($comment)
        }
        return $document.OuterXml.Replace("`r`n", "`n").Replace("`r", "`n")
    }
    catch
    {
        # An uncertain parse is deliberately compared byte-for-byte after line-ending normalization.
        return $content
    }
}

$map = Import-PowerShellDataFile -LiteralPath $MapPath -SkipLimitCheck
if ($map.SchemaVersion -ne 1) { throw 'Unsupported visual capture map schema.' }
$known = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$familyById = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::Ordinal)
foreach ($capture in $map.Captures)
{
    if (-not $known.Add([string]$capture.Id)) { throw "Duplicate capture ID: $($capture.Id)" }
    if ([string]::IsNullOrWhiteSpace([string]$capture.Family)) { throw "Capture family is missing: $($capture.Id)" }
    $familyById[[string]$capture.Id] = [string]$capture.Family
}
$requestedFamilies = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
if ($CaptureId)
{
    foreach ($id in $CaptureId)
    {
        if (-not $known.Contains($id)) { throw "Unknown capture ID: $id" }
        [void]$requestedFamilies.Add($familyById[$id])
    }
}

$previous = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)
if ($WebsiteManifestPath -and [System.IO.File]::Exists($WebsiteManifestPath))
{
    $manifest = Get-Content -LiteralPath $WebsiteManifestPath -Raw | ConvertFrom-Json
    $records = [System.Collections.Generic.List[object]]::new()
    if ($manifest.PSObject.Properties['files']) { foreach ($record in $manifest.files) { $records.Add($record) } }
    if ($manifest.PSObject.Properties['captures']) { foreach ($record in $manifest.captures) { $records.Add($record) } }
    foreach ($record in $records)
    {
        if ($null -ne $record -and $record.PSObject.Properties['path'])
        {
            $previous[[string]$record.path] = $record
        }
    }
}

$examined = [System.Collections.Generic.List[object]]::new()
$changedFamilies = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($capture in $map.Captures)
{
    if ($CaptureId -and -not $requestedFamilies.Contains([string]$capture.Family)) { continue }
    $parts = [System.Collections.Generic.List[string]]::new()
    foreach ($path in @($capture.Inputs | Sort-Object -CaseSensitive -Unique))
    {
        $full = Get-DocsContainedPath -Root $RepositoryRoot -RelativePath $path
        if (-not [System.IO.File]::Exists($full)) { throw "Capture input is missing: $path" }
        $parts.Add("${path}`0$(Get-VisualInputContent -LiteralPath $full)")
    }
    $hash = Get-Sha256Utf8 ($parts -join "`0")
    $old = $null
    if ($previous.ContainsKey($capture.Path)) { $old = $previous[$capture.Path] }
    $checked = if ($null -ne $old -and $old.PSObject.Properties['checkedVisualInputSha256']) { $old.checkedVisualInputSha256 } else { $null }
    $reason = if ($null -eq $checked) { 'legacy-unverified' } elseif ($hash -cne $checked) { 'visual-input-changed' } else { 'family-companion' }
    if ($reason -cne 'family-companion')
    {
        [void]$changedFamilies.Add([string]$capture.Family)
    }
    $examined.Add([pscustomobject]@{
            id = $capture.Id
            path = $capture.Path
            sourceRelativePath = $capture.SourceRelativePath
            checkedVisualInputSha256 = $hash
            inputs = @($capture.Inputs)
            family = $capture.Family
            theme = $capture.Theme
            state = $capture.State
            mode = $capture.Mode
            reason = $reason
        })
}

# Result is data only. A candidate is never permission to capture or replace an image.
[pscustomobject]@{ schemaVersion = 1; candidates = @($examined | Where-Object { $changedFamilies.Contains([string]$_.family) }) }
