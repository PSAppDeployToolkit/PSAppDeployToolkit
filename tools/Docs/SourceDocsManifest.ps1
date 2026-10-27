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

function Assert-DocsRelativePath {
    param([Parameter(Mandatory)][string]$Path)
    if ($Path -notmatch '^[A-Za-z0-9_.-]+(?:/[A-Za-z0-9_.-]+)*$' -or
        $Path -match '(^|/)(\.|\.\.)(/|$)' -or $Path.Contains(':') -or $Path.Contains('\')) {
        throw "Invalid documentation relative path: $Path"
    }
    return $Path
}

function Get-DocsContainedPath {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$RelativePath)
    [void](Assert-DocsRelativePath $RelativePath)
    $absoluteRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar)
    if (Test-Path -LiteralPath $absoluteRoot) {
        if ((Get-Item -LiteralPath $absoluteRoot -Force).Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
            throw "Documentation root is a reparse point: $absoluteRoot"
        }
    }
    $candidate = [System.IO.Path]::GetFullPath((Join-Path $absoluteRoot ($RelativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))))
    $comparison = if ($IsWindows) { [System.StringComparison]::OrdinalIgnoreCase } else { [System.StringComparison]::Ordinal }
    if (-not $candidate.StartsWith($absoluteRoot + [System.IO.Path]::DirectorySeparatorChar, $comparison)) {
        throw "Path escapes documentation root: $RelativePath"
    }
    $current = $absoluteRoot
    foreach ($part in $RelativePath.Split('/')) {
        $current = Join-Path $current $part
        if (Test-Path -LiteralPath $current) {
            $item = Get-Item -LiteralPath $current -Force
            if ($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
                throw "Reparse point in documentation path: $current"
            }
        }
    }
    return $candidate
}

function Get-DocsSha256 {
    param([Parameter(Mandatory)][byte[]]$Bytes)
    return [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($Bytes)).ToLowerInvariant()
}

function Assert-SourceDocsManifest {
    param([Parameter(Mandatory)]$Manifest, [string[]]$CapturePaths = @())
    if ($Manifest.schemaVersion -ne 1 -or $null -eq $Manifest.files) { throw 'Unsupported source docs manifest schema.' }
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $captures = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($capturePath in $CapturePaths) { [void]$captures.Add((Assert-DocsRelativePath $capturePath)) }
    foreach ($record in @($Manifest.files)) {
        $path = Assert-DocsRelativePath ([string]$record.path)
        if (-not $seen.Add($path)) { throw "Duplicate source docs manifest path: $path" }
        $reference = $path -match '^docs/api/(?:[A-Za-z0-9_.-]+/)*[A-Za-z0-9_.-]+\.md$' -or
            $path -match '^docs/powershell/reference/[A-Za-z0-9_.-]+\.mdx$' -or
            $path -match '^docs/powershell/reference/[A-Za-z]+-Fluence[A-Za-z0-9-]+\.md$' -or
            $path -cin @('docs/controls.md','docs/theming.md','docs/winui-parity.md',
                'docs/powershell/reference/README.md','docs/powershell/reference/input-types.md',
                'docs/powershell/reference/result-objects.md')
        $image = $captures.Contains($path)
        $galleryManifest = $path -ceq 'docs/screenshots/gallery/manifest.json'
        if (-not ($reference -or $image -or $galleryManifest)) { throw "Unmanaged source docs manifest path: $path" }
        if (($reference -and $record.kind -cne 'reference') -or
            ($image -and $record.kind -cne 'capture') -or
            ($galleryManifest -and $record.kind -cne 'gallery-manifest')) {
            throw "Source docs manifest kind does not match path: $path"
        }
        if ($record.kind -eq 'capture') {
            if (-not $image) { throw "Capture is absent from the explicit map: $path" }
            if ($record.decision -eq 'new-capture' -and -not $path.StartsWith('docs/screenshots/controls/', [StringComparison]::Ordinal)) {
                throw "New captures are supported only for controls: $path"
            }
            if ($record.sha256 -notmatch '^[a-f0-9]{64}$' -or $record.pngSha256 -cne $record.sha256 -or
                $record.inputs.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$record.theme) -or
                [string]::IsNullOrWhiteSpace([string]$record.state) -or [string]::IsNullOrWhiteSpace([string]$record.mode) -or
                $record.decision -cnotin @('legacy-unverified','material-change','visually-unchanged','new-capture') -or
                ($null -ne $record.checkedVisualInputSha256 -and $record.checkedVisualInputSha256 -notmatch '^[a-f0-9]{64}$')) {
                throw "Invalid capture record: $path"
            }
            foreach ($inputPath in @($record.inputs)) { [void](Assert-DocsRelativePath ([string]$inputPath)) }
            if ($record.decision -eq 'legacy-unverified' -and $null -ne $record.checkedVisualInputSha256) {
                throw "Legacy capture cannot claim checked inputs: $path"
            }
        } elseif ($record.kind -ne 'reference' -and $record.kind -ne 'gallery-manifest') {
            throw "Invalid source docs manifest kind: $path"
        }
        if ($record.sha256 -notmatch '^[a-f0-9]{64}$' -or [string]::IsNullOrWhiteSpace([string]$record.provenance)) {
            throw "Invalid source docs manifest hash or provenance: $path"
        }
    }
}

function ConvertTo-SourceDocsManifestByteArray {
    param([Parameter(Mandatory)]$Manifest, [string[]]$CapturePaths = @())
    Assert-SourceDocsManifest -Manifest $Manifest -CapturePaths $CapturePaths
    $records = [System.Collections.Generic.List[object]]::new()
    $sortedRecords = @($Manifest.files)
    [Array]::Sort($sortedRecords, [System.Comparison[object]] { param($a,$b) [StringComparer]::Ordinal.Compare([string]$a.path,[string]$b.path) })
    foreach ($record in $sortedRecords) {
        $ordered = [ordered]@{
            path = [string]$record.path
            kind = [string]$record.kind
            sha256 = [string]$record.sha256
            provenance = [string]$record.provenance
        }
        if ($record.kind -eq 'capture') {
            $ordered.theme = [string]$record.theme
            $ordered.state = [string]$record.state
            $ordered.mode = [string]$record.mode
            $sortedInputs = [string[]]@($record.inputs)
            [Array]::Sort($sortedInputs, [StringComparer]::Ordinal)
            $ordered.inputs = @($sortedInputs)
            $ordered.checkedVisualInputSha256 = $record.checkedVisualInputSha256
            $ordered.pngSha256 = [string]$record.pngSha256
            if ($record.PSObject.Properties.Name -contains 'decodedPixelSha256' -and $null -ne $record.decodedPixelSha256) {
                $ordered.decodedPixelSha256 = [string]$record.decodedPixelSha256
            }
            $ordered.decision = [string]$record.decision
        }
        $records.Add($ordered)
    }
    $json = ([ordered]@{ schemaVersion = 1; files = @($records) } | ConvertTo-Json -Depth 30)
    $json = $json.Replace("`r`n", "`n").TrimEnd("`n") + "`n"
    $bom = [byte[]](0xEF,0xBB,0xBF)
    return [byte[]]($bom + [System.Text.Encoding]::UTF8.GetBytes($json))
}
