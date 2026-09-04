#Requires -Version 5.1
<#
.SYNOPSIS
    Loads the window UI from MainWindow.xaml on disk instead of an inline string.
    Teaches the more maintainable pattern of keeping XAML in its own file and wiring
    the named controls from PowerShell.
.NOTES
    Run with:  powershell.exe -STA -File .\04-LoadXamlFile.ps1
    WPF requires a single-threaded apartment (STA); the script relaunches itself if needed.
#>

# --- 1. WPF needs STA. Relaunch ourselves in STA if we are not already there. ---
if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA') {
    powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File $PSCommandPath @args
    return
}

$ErrorActionPreference = 'Stop'

# --- 2. Find the net472 build of Fluence.Wpf.dll; build it once if missing. ---
$dll = Join-Path $PSScriptRoot '..\Fluence.Wpf\bin\Release\net472\Fluence.Wpf.dll'
if (-not (Test-Path -LiteralPath $dll)) {
    Write-Host 'Building Fluence.Wpf (net472, Release) - first run only...'
    dotnet build (Join-Path $PSScriptRoot '..\Fluence.Wpf\Fluence.Wpf.csproj') -c Release -f net472 --nologo -v q
}

# --- 3. Load WPF + Fluence. ---
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Xaml
Add-Type -Path $dll

# --- 4. A WPF Application must exist BEFORE theming: ApplicationThemeManager.Apply publishes
#        brushes into Application.Current.Resources and silently no-ops if there is no app. ---
$app = New-Object System.Windows.Application

# --- 5. Turn the theme engine on. Auto = follow the Windows light/dark setting. ---
[Fluence.Wpf.ApplicationThemeManager]::Apply(
    [Fluence.Wpf.ApplicationTheme]::Auto,
    [Fluence.Wpf.BackdropType]::Mica,
    $true)
[Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()

# --- 6. Load XAML from a file instead of an inline string (XamlReader.Load over a file stream). ---
$xamlPath = Join-Path $PSScriptRoot 'MainWindow.xaml'
$window = $null
$stream = [System.IO.File]::OpenRead($xamlPath)
try {
    $reader = [System.Xml.XmlReader]::Create($stream)
    try { $window = [System.Windows.Markup.XamlReader]::Load($reader) }
    finally { $reader.Dispose() }
}
finally { $stream.Dispose() }

# --- 7. Wire the controls that MainWindow.xaml exposes by name. ---
$themeCombo = $window.FindName('ThemeComboBox')
if ($null -ne $themeCombo) {
    $themeCombo.add_SelectionChanged({
        param($s, $e)
        # The ComboBox items map (by index) to Auto / Light / Dark / HighContrast.
        $theme = switch ($s.SelectedIndex) {
            1       { [Fluence.Wpf.ApplicationTheme]::Light }
            2       { [Fluence.Wpf.ApplicationTheme]::Dark }
            3       { [Fluence.Wpf.ApplicationTheme]::HighContrast }
            default { [Fluence.Wpf.ApplicationTheme]::Auto }
        }
        [Fluence.Wpf.ApplicationThemeManager]::Apply($theme, [Fluence.Wpf.BackdropType]::Mica, $true)
    })
}
# The "Cycle accent" button steps through a small palette of custom accents.
$accents = @(
    [System.Windows.Media.Color]::FromRgb(0x00, 0x78, 0xD4),  # blue
    [System.Windows.Media.Color]::FromRgb(0x10, 0x89, 0x3E),  # green
    [System.Windows.Media.Color]::FromRgb(0xC4, 0x2B, 0x1C),  # red
    [System.Windows.Media.Color]::FromRgb(0x74, 0x37, 0xC9)   # purple
)
$script:accentIndex = 0
$accentBtn = $window.FindName('AccentButton')
if ($null -ne $accentBtn) {
    $accentBtn.add_Click({
        $color = $accents[$script:accentIndex % $accents.Count]
        $script:accentIndex++
        [Fluence.Wpf.ApplicationAccentColorManager]::ApplyCustomAccent($color)
    })
}
$sysAccentBtn = $window.FindName('SystemAccentButton')
if ($null -ne $sysAccentBtn) { $sysAccentBtn.add_Click({ [Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent() }) }

# Follow OS light/dark changes while open; stop watching on close.
[Fluence.Wpf.SystemThemeWatcher]::Watch($window)
$window.add_Closed({ [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window) })

# --- 8. Show the window and pump the WPF message loop until it closes. ---
[void]$app.Run($window)
