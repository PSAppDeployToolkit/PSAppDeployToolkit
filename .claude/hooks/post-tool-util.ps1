param([switch]$CheckAll)

$ErrorActionPreference = "Stop"

function Get-ProjectRoot {
    if (-not [string]::IsNullOrWhiteSpace($env:CLAUDE_PROJECT_DIR)) {
        return [System.IO.Path]::GetFullPath($env:CLAUDE_PROJECT_DIR)
    }

    $scriptDirectory = Split-Path -Parent $PSCommandPath
    return [System.IO.Path]::GetFullPath((Join-Path $scriptDirectory "..\.."))
}

function Resolve-EditedPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$ProjectRoot
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $ProjectRoot $Path))
}

function Test-IsUnderRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    $rootWithSeparator = $Root.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    return $Path.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    $rootWithSeparator = $Root.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    return $Path.Substring($rootWithSeparator.Length).Replace('\', '/')
}

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

$projectRoot = Get-ProjectRoot

# Extensions whose content this check inspects (line endings, banned APIs, dashes, hex).
$extensions = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
@(".cs", ".xaml", ".csproj", ".props", ".targets", ".sln", ".slnx", ".md", ".yml", ".yaml", ".json", ".ps1", ".psm1", ".psd1") | ForEach-Object { [void]$extensions.Add($_) }

# UTF-8 BOM is mandated only for these source types (AGENTS.md section 2). Other tracked types
# (.yml/.yaml/.json/.ps1/.psm1/.psd1/.sln/.slnx) are still checked for line endings and content,
# but not for a BOM.
$bomExtensions = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
@(".cs", ".xaml", ".csproj", ".props", ".targets", ".md") | ForEach-Object { [void]$bomExtensions.Add($_) }

$fullPaths = New-Object "System.Collections.Generic.List[string]"
$relativePaths = New-Object "System.Collections.Generic.List[string]"

if ($CheckAll) {
    # Repo-wide gate (CI). Sweep every tracked file of a checked type and apply the same
    # text-policy checks the edit hook applies per file. Generated XAML is emitted byte-for-byte
    # by DesignTimeResourceWriter and is not hand-authored, so it is excluded.
    $tracked = & git -C $projectRoot ls-files
    foreach ($rel in $tracked) {
        if ([string]::IsNullOrWhiteSpace($rel)) { continue }

        $extension = [System.IO.Path]::GetExtension($rel)
        if (-not $extensions.Contains($extension)) { continue }
        if ($rel -match 'Properties/DesignTime.*\.xaml$') { continue }

        $fullPath = [System.IO.Path]::GetFullPath((Join-Path $projectRoot $rel))
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { continue }

        $fullPaths.Add($fullPath)
        $relativePaths.Add($rel.Replace('\', '/'))
    }
}
else {
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

    $seen = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($rawPath in $rawPaths) {
        $fullPath = Resolve-EditedPath -Path $rawPath -ProjectRoot $projectRoot

        if (-not (Test-IsUnderRoot -Path $fullPath -Root $projectRoot)) {
            continue
        }

        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            continue
        }

        $extension = [System.IO.Path]::GetExtension($fullPath)
        if (-not $extensions.Contains($extension)) {
            continue
        }

        if ($seen.Add($fullPath)) {
            $fullPaths.Add($fullPath)
            $relativePaths.Add((Get-RelativePath -Path $fullPath -Root $projectRoot))
        }
    }
}

if ($fullPaths.Count -eq 0) {
    exit 0
}

$violations = New-Object "System.Collections.Generic.List[string]"

for ($i = 0; $i -lt $fullPaths.Count; $i++) {
    $path = $fullPaths[$i]
    $relativePath = $relativePaths[$i]
    $bytes = [System.IO.File]::ReadAllBytes($path)
    $extension = [System.IO.Path]::GetExtension($path)

    if ($bytes.Length -ge 3) {
        $hasBom = $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    }
    else {
        $hasBom = $false
    }

    if (-not $hasBom -and $bomExtensions.Contains($extension)) {
        $violations.Add("$relativePath must be UTF-8 with BOM.")
    }

    for ($byteIndex = 0; $byteIndex -lt $bytes.Length; $byteIndex++) {
        if ($bytes[$byteIndex] -eq 0x0D) {
            $violations.Add("$relativePath must use LF line endings, not CRLF or CR.")
            break
        }
    }

    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    $isCSharp = [string]::Equals($extension, ".cs", [System.StringComparison]::OrdinalIgnoreCase)
    $isXaml = [string]::Equals($extension, ".xaml", [System.StringComparison]::OrdinalIgnoreCase)
    $isMarkdown = [string]::Equals($extension, ".md", [System.StringComparison]::OrdinalIgnoreCase)

    if ($isCSharp) {
        if ($text -match 'IsNullOrEmpty\s*\(') {
            $violations.Add("$relativePath uses string.IsNullOrEmpty (banned, RS0030). Use string.IsNullOrWhiteSpace.")
        }

        # The text-rendering policy test legitimately references TextOptions to enforce the ban.
        if ($text -match 'TextOptions\.' -and $relativePath -notmatch 'TextRenderingPolicyTests') {
            $violations.Add("$relativePath references TextOptions.*; text-rendering options are banned (central inheritance is the design).")
        }
    }

    if ($isXaml) {
        if ($text -match 'TextOptions\.') {
            $violations.Add("$relativePath references TextOptions.*; text-rendering options are banned (SnapsToDevicePixels belongs only on FluenceWindow.xaml).")
        }

        if ($relativePath -match 'Themes/Controls/' -and $text -match '"#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?"') {
            $violations.Add("$relativePath inlines a hard-coded hex color in a control template. Bind a canonical WinUI brush key via DynamicResource instead.")
        }
    }

    if ($isCSharp -or $isMarkdown) {
        if ($text.Contains([char]0x2014) -or $text.Contains([char]0x2013)) {
            $violations.Add("$relativePath contains an em dash or en dash. Use hyphens in comments and documentation.")
        }
    }
}

if ($CheckAll) {
    $diffOutput = & git -C $projectRoot diff --check 2>&1
}
else {
    $diffOutput = & git -C $projectRoot diff --check -- $relativePaths 2>&1
}
if ($LASTEXITCODE -ne 0) {
    $violations.Add("git diff --check failed: $($diffOutput -join '; ')")
}

if ($violations.Count -gt 0) {
    if ($CheckAll) {
        [Console]::Error.WriteLine("Text-policy check failed with $($violations.Count) issue(s):")
        foreach ($violation in $violations) {
            [Console]::Error.WriteLine("  - $violation")
        }

        exit 1
    }

    $message = "PostToolUtil found text-policy issues. " + ($violations -join " ")
    [Console]::Error.WriteLine($message)
    [Console]::Out.WriteLine((@{
        decision = "block"
        reason = $message
    } | ConvertTo-Json -Compress))
    exit 0
}

if ($CheckAll) {
    [Console]::Out.WriteLine("Text-policy check passed: $($fullPaths.Count) tracked file(s) checked.")
}
