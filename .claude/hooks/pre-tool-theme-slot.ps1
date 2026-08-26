$ErrorActionPreference = "Stop"

# PreToolUse advisory for edits to theme-slot-critical files.
# Emits a non-blocking reminder (additionalContext) when an edit targets a file
# that governs the three-slot MergedDictionaries invariant. See AGENTS.md sections 3 and 9.

function Add-FilePath {
    param(
        [Parameter(Mandatory = $false)]
        $Value,

        [Parameter(Mandatory = $false)]
        [System.Collections.Generic.List[string]]$Paths
    )

    if ($null -eq $Value) {
        return
    }

    if ($Value -is [string]) {
        if (-not [string]::IsNullOrWhiteSpace($Value)) {
            $Paths.Add($Value)
        }

        return
    }

    if ($Value -is [System.Array]) {
        foreach ($item in $Value) {
            Add-FilePath -Value $item -Paths $Paths
        }
    }
}

$stdin = [Console]::In.ReadToEnd()
$payload = $null

if (-not [string]::IsNullOrWhiteSpace($stdin)) {
    try {
        $payload = $stdin | ConvertFrom-Json
    }
    catch {
        $payload = $null
    }
}

$rawPaths = New-Object "System.Collections.Generic.List[string]"
if ($null -ne $payload -and $null -ne $payload.tool_input) {
    Add-FilePath -Value $payload.tool_input.file_path -Paths $rawPaths
    Add-FilePath -Value $payload.tool_input.path -Paths $rawPaths

    if ($null -ne $payload.tool_input.edits) {
        foreach ($edit in $payload.tool_input.edits) {
            Add-FilePath -Value $edit.file_path -Paths $rawPaths
            Add-FilePath -Value $edit.path -Paths $rawPaths
        }
    }
}

if ($rawPaths.Count -eq 0) {
    exit 0
}

$slotCritical = $false
foreach ($rawPath in $rawPaths) {
    $normalized = $rawPath.Replace('\', '/')

    if ($normalized -match 'ApplicationThemeManager\.cs$' -or
        $normalized -match 'Themes/Generic\.xaml$') {
        $slotCritical = $true
        break
    }
}

if (-not $slotCritical) {
    exit 0
}

$reminder = "Theme-slot-critical file. Keep the three-slot MergedDictionaries order intact: [0] computed colors + brushes (rebuilt and replaced every Apply by FluenceThemeEngine.BuildComputedDictionary), [1] Typography.xaml, [2] Generic.xaml. Any new color must be added to all three Theme.{Light|Dark|HighContrast}.xaml; BrushFactory auto-twins every color into a frozen SolidColorBrush (key + 'Brush'), so SpecialBrushes.cs is only for exceptions (non-standard twin names, gradients, high-contrast overrides). Route theme- and accent-bound values through DynamicResource, never StaticResource. Run DictionaryStabilityTests after the change. See AGENTS.md sections 3 and 9."

$output = @{
    hookSpecificOutput = @{
        hookEventName = "PreToolUse"
        additionalContext = $reminder
    }
}

[Console]::Out.WriteLine(($output | ConvertTo-Json -Compress))
exit 0
