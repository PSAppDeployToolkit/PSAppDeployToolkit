# Repo-owned XAML formatter hook (PostToolUse on Edit/Write/MultiEdit).
# Formats each edited .xaml with the pinned XAML Styler tool and the committed reference
# style (Settings.XamlStyler), via eng/Format-Xaml.ps1 (single source of truth, which also
# enforces LF + single UTF-8 BOM and skips generated XAML). Runs BEFORE post-tool-util.ps1
# so the linter sees the already-formatted result. Non-blocking: formatting issues never
# fail the tool call; the CI check (eng/Format-Xaml.ps1 -Check) is the hard gate.
$ErrorActionPreference = "Stop"

function Get-ProjectRoot {
    if (-not [string]::IsNullOrWhiteSpace($env:CLAUDE_PROJECT_DIR)) {
        return [System.IO.Path]::GetFullPath($env:CLAUDE_PROJECT_DIR)
    }
    $scriptDirectory = Split-Path -Parent $PSCommandPath
    return [System.IO.Path]::GetFullPath((Join-Path $scriptDirectory "..\.."))
}

$projectRoot = Get-ProjectRoot
$stdin = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($stdin)) { exit 0 }

try { $payload = $stdin | ConvertFrom-Json } catch { exit 0 }
if ($null -eq $payload -or $null -eq $payload.tool_input) { exit 0 }

$paths = New-Object "System.Collections.Generic.List[string]"
if ($payload.tool_input.file_path) { $paths.Add([string]$payload.tool_input.file_path) }
if ($payload.tool_input.path) { $paths.Add([string]$payload.tool_input.path) }
if ($null -ne $payload.tool_input.edits) {
    foreach ($edit in $payload.tool_input.edits) {
        if ($edit.file_path) { $paths.Add([string]$edit.file_path) }
        if ($edit.path) { $paths.Add([string]$edit.path) }
    }
}

$formatter = Join-Path $projectRoot "eng/Format-Xaml.ps1"
if (-not (Test-Path -LiteralPath $formatter)) { exit 0 }

$seen = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($raw in $paths) {
    if ([string]::IsNullOrWhiteSpace($raw)) { continue }
    if (-not $raw.EndsWith(".xaml", [System.StringComparison]::OrdinalIgnoreCase)) { continue }

    $full = if ([System.IO.Path]::IsPathRooted($raw)) { [System.IO.Path]::GetFullPath($raw) }
            else { [System.IO.Path]::GetFullPath((Join-Path $projectRoot $raw)) }

    if (-not $seen.Add($full)) { continue }
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { continue }

    try {
        # Use the same host the hook runs under (powershell.exe per .claude/settings.json);
        # eng/Format-Xaml.ps1 is 5.1-compatible, so this avoids a hard dependency on pwsh 7.
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $formatter -Path $full *> $null
    }
    catch {
        # Formatting is best-effort in the hook; never block the edit on a formatter hiccup.
    }
}

exit 0
