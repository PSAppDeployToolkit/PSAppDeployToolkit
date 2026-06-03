#Requires -Version 5.1
<#
.SYNOPSIS
    Formats (or checks) the repository's authored XAML with XAML Styler using the committed
    reference style (Settings.XamlStyler) and the version pinned in .config/dotnet-tools.json.

.DESCRIPTION
    Single source of truth for XAML formatting in this repo. Used three ways:
      1. The Claude edit hook (.claude/hooks/post-tool-format-xaml.ps1) formats each edited file.
      2. Developers run it with no arguments to format all authored XAML.
      3. CI runs it with -Check to fail the build if any authored XAML is not conformant.

    Generated XAML is excluded (it must not be reformatted):
      * Fluence.Wpf/Properties/DesignTime.*.xaml  - emitted byte-for-byte by DesignTimeResourceWriter;
        reformatting would break the DesignTimeResources_AreCurrent drift guard.
      * **/Resources/fluence-wpf-banner-*.xaml     - generated vector geometry (excluded from Page compile).

    After styling, LF line endings and a UTF-8 BOM are enforced to satisfy the repo's
    .gitattributes (eol=lf) and .editorconfig (charset=utf-8-bom).

.PARAMETER Check
    Verify formatting without modifying files. Exits 1 if any authored XAML is not conformant.

.PARAMETER Path
    One or more specific .xaml files to process. When omitted, all git-tracked .xaml are processed.

.EXAMPLE
    pwsh eng/Format-Xaml.ps1            # format all authored XAML
.EXAMPLE
    pwsh eng/Format-Xaml.ps1 -Check     # CI: fail if any authored XAML is unformatted
.EXAMPLE
    pwsh eng/Format-Xaml.ps1 -Path Fluence.Wpf.Demo/MainWindow.xaml
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [string[]]$Path
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$config = Join-Path $repoRoot 'Settings.XamlStyler'

if (-not (Test-Path -LiteralPath $config)) {
    Write-Error "Reference style not found: $config"
    exit 1
}

# Generated XAML that must never be reformatted (matched on repo-relative, forward-slash path).
function Test-Excluded {
    param([string]$RelativePath)
    if ($RelativePath -match '(^|/)Fluence\.Wpf/Properties/DesignTime\.[^/]+\.xaml$') { return $true }
    if ($RelativePath -match '(^|/)Resources/fluence-wpf-banner-[^/]*\.xaml$') { return $true }
    return $false
}

function Get-RelativePath {
    param([string]$FullPath)
    $rootWithSep = $repoRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $full = [System.IO.Path]::GetFullPath($FullPath)
    if ($full.StartsWith($rootWithSep, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $full.Substring($rootWithSep.Length).Replace('\', '/')
    }
    return $full.Replace('\', '/')
}

# Formats a single file in place: XAML Styler with the reference config, then a forced
# LF + single UTF-8 BOM. UTF8.GetString keeps any leading BOM in the string, so strip it
# before re-emitting (otherwise the file gets a doubled BOM).
function Format-OneFile {
    param([string]$File)
    & dotnet tool run xstyler -- -f $File -c $config -l Minimal *> $null
    if ($LASTEXITCODE -ne 0) { throw "xstyler failed on $File" }
    $bytes = [System.IO.File]::ReadAllBytes($File)
    $text = [System.Text.Encoding]::UTF8.GetString($bytes).TrimStart([char]0xFEFF).Replace("`r`n", "`n").Replace("`r", "`n")
    [System.IO.File]::WriteAllText($File, $text, (New-Object System.Text.UTF8Encoding($true)))
}

Push-Location $repoRoot
try {
    # Build the target list.
    if ($Path) {
        $candidates = $Path | ForEach-Object { [System.IO.Path]::GetFullPath((Join-Path $repoRoot $_)) }
    }
    else {
        # -co --exclude-standard: tracked (cached) AND untracked-but-not-ignored, so a new
        # .xaml that has not been committed yet is still formatted/checked (avoids a false pass).
        $candidates = (& git -C $repoRoot ls-files -co --exclude-standard '*.xaml') | ForEach-Object { Join-Path $repoRoot $_ }
    }

    $targets = New-Object System.Collections.Generic.List[string]
    foreach ($candidate in $candidates) {
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { continue }
        $relative = Get-RelativePath -FullPath $candidate
        if (Test-Excluded -RelativePath $relative) { continue }
        $targets.Add($candidate)
    }

    if ($targets.Count -eq 0) {
        Write-Host 'No authored XAML files to process.'
        exit 0
    }

    $failed = New-Object System.Collections.Generic.List[string]

    foreach ($target in $targets) {
        if ($Check) {
            # Non-destructive check: format a temp copy through the identical pipeline and compare.
            # (XAML Styler's own -p passive mode reports false positives, so we compare results.)
            $tmp = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName() + ".xaml")
            Copy-Item -LiteralPath $target -Destination $tmp -Force
            try {
                Format-OneFile -File $tmp
                $current = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
                $formatted = (Get-FileHash -LiteralPath $tmp -Algorithm SHA256).Hash
                if ($current -ne $formatted) { $failed.Add((Get-RelativePath -FullPath $target)) }
            }
            finally {
                Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
            }
        }
        else {
            Format-OneFile -File $target
        }
    }

    if ($Check) {
        if ($failed.Count -gt 0) {
            Write-Error ("XAML format check failed for $($failed.Count) file(s). Run 'pwsh eng/Format-Xaml.ps1' to fix:`n  " + ($failed -join "`n  "))
            exit 1
        }
        Write-Host "XAML format check passed ($($targets.Count) files)."
    }
    else {
        Write-Host "Formatted $($targets.Count) XAML files."
    }
}
finally {
    Pop-Location
}
