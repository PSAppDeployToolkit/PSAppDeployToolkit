#Requires -Version 5.1
<#
.SYNOPSIS
    Smallest possible Fluence.Wpf window from PowerShell: a Mica window with a button that
    cycles the backdrop (Mica -> Acrylic -> Tabbed -> None) and a label that rotates through
    "Hello, World!" greetings.
.NOTES
    Run with:  powershell.exe -STA -File .\01-HelloWorld.ps1
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

# --- 6. Build the window from inline XAML. ui: = the Fluence.Wpf.Controls namespace. ---
$xaml = @'
<ui:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"
    Title="Fluence.Wpf - PowerShell Hello World"
    Width="520" Height="340"
    SystemBackdropType="Mica"
    ExtendsContentIntoTitleBar="False">
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
        <TextBlock x:Name="HelloLabel"
                   Text="Hello, World!"
                   HorizontalAlignment="Center"
                   ui:TextBlockExtensions.Typography="Title"
                   Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
        <TextBlock x:Name="BackdropLabel"
                   Text="Backdrop: Mica"
                   Margin="0,8,0,20"
                   HorizontalAlignment="Center"
                   Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
        <ui:Button x:Name="CycleButton"
                   Content="Next backdrop + greeting"
                   Appearance="Accent"
                   HorizontalAlignment="Center" />
    </StackPanel>
</ui:FluenceWindow>
'@

$window = [System.Windows.Markup.XamlReader]::Parse($xaml)

# --- 7. Wire the button. Each click advances the backdrop and the greeting. ---
$backdrops = @('Mica', 'Acrylic', 'Tabbed', 'None')
$greetings = @('Hello, World!', 'Hej, varlden!', 'Hola, mundo!', 'Bonjour, le monde!', 'Ola, mundo!', 'Ciao, mondo!')
$script:tick = 0

$helloLabel    = $window.FindName('HelloLabel')
$backdropLabel = $window.FindName('BackdropLabel')
$cycleButton   = $window.FindName('CycleButton')

$cycleButton.add_Click({
    $script:tick++
    $name = $backdrops[$script:tick % $backdrops.Count]
    # Setting the window's SystemBackdropType re-applies the DWM backdrop live.
    $window.SystemBackdropType = [Enum]::Parse([Fluence.Wpf.BackdropType], $name)
    $backdropLabel.Text = "Backdrop: $name"
    $helloLabel.Text    = $greetings[$script:tick % $greetings.Count]
})

# Follow OS light/dark changes while open; stop watching on close.
[Fluence.Wpf.SystemThemeWatcher]::Watch($window)
$window.add_Closed({ [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window) })

# --- 8. Show the window and pump the WPF message loop until it closes. ---
[void]$app.Run($window)
