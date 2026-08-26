#Requires -Version 5.1
<#
.SYNOPSIS
    Demonstrates switching Light/Dark/Auto themes and cycling custom accent colours from
    PowerShell. Shows SystemThemeWatcher following the OS setting live.
.NOTES
    Run with:  powershell.exe -STA -File .\02-ThemeAndAccent.ps1
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

# --- 6. Build the window from inline XAML. ---
$xaml = @'
<ui:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"
    Title="Fluence.Wpf - Theme and Accent"
    Width="560" Height="360"
    SystemBackdropType="Mica"
    ExtendsContentIntoTitleBar="False">
    <StackPanel Margin="24" VerticalAlignment="Center">
        <TextBlock Text="Theme" ui:TextBlockExtensions.Typography="Subtitle"
                   Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
        <StackPanel Orientation="Horizontal" Margin="0,8,0,16">
            <ui:Button x:Name="LightBtn"  Content="Light"  Margin="0,0,8,0" />
            <ui:Button x:Name="DarkBtn"   Content="Dark"   Margin="0,0,8,0" />
            <ui:Button x:Name="AutoBtn"   Content="Auto (follow Windows)" />
        </StackPanel>
        <TextBlock Text="Accent" ui:TextBlockExtensions.Typography="Subtitle"
                   Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
        <StackPanel Orientation="Horizontal" Margin="0,8,0,16">
            <ui:Button x:Name="AccentBtn"       Content="Cycle custom accent" Appearance="Accent" Margin="0,0,8,0" />
            <ui:Button x:Name="SystemAccentBtn" Content="Use system accent" />
        </StackPanel>
        <ui:InfoBar x:Name="StatusBar" IsOpen="True" IsClosable="False"
                    Severity="Informational"
                    Title="Tip"
                    Message="Change the Windows theme while this is open - Auto follows it live." />
    </StackPanel>
</ui:FluenceWindow>
'@

$window = [System.Windows.Markup.XamlReader]::Parse($xaml)

# --- 7. Wire the theme buttons. ---
$window.FindName('LightBtn').add_Click({ [Fluence.Wpf.ApplicationThemeManager]::Apply([Fluence.Wpf.ApplicationTheme]::Light, [Fluence.Wpf.BackdropType]::Mica, $true) })
$window.FindName('DarkBtn').add_Click({  [Fluence.Wpf.ApplicationThemeManager]::Apply([Fluence.Wpf.ApplicationTheme]::Dark,  [Fluence.Wpf.BackdropType]::Mica, $true) })
$window.FindName('AutoBtn').add_Click({  [Fluence.Wpf.ApplicationThemeManager]::Apply([Fluence.Wpf.ApplicationTheme]::Auto,  [Fluence.Wpf.BackdropType]::Mica, $true) })

# A small palette to cycle through with ApplyCustomAccent(Color).
$accents = @(
    [System.Windows.Media.Color]::FromRgb(0x00, 0x78, 0xD4),  # blue
    [System.Windows.Media.Color]::FromRgb(0x10, 0x89, 0x3E),  # green
    [System.Windows.Media.Color]::FromRgb(0xC4, 0x2B, 0x1C),  # red
    [System.Windows.Media.Color]::FromRgb(0x74, 0x37, 0xC9)   # purple
)
$script:accentIndex = 0
$window.FindName('AccentBtn').add_Click({
    $color = $accents[$script:accentIndex % $accents.Count]
    $script:accentIndex++
    [Fluence.Wpf.ApplicationAccentColorManager]::ApplyCustomAccent($color)
})
$window.FindName('SystemAccentBtn').add_Click({ [Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent() })

# Follow OS light/dark changes while open; stop watching on close.
[Fluence.Wpf.SystemThemeWatcher]::Watch($window)
$window.add_Closed({ [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window) })

# --- 8. Show the window and pump the WPF message loop until it closes. ---
[void]$app.Run($window)
