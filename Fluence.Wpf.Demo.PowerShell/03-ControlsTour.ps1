#Requires -Version 5.1
<#
.SYNOPSIS
    Showcases common Fluence.Wpf controls (buttons, toggle, checkbox, radio, text box, number
    box) inside scrolling cards. The toggle switch drives an InfoBar message from PowerShell.
.NOTES
    Run with:  powershell.exe -STA -File .\03-ControlsTour.ps1
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
    Title="Fluence.Wpf - Controls tour"
    Width="620" Height="560"
    SystemBackdropType="Mica"
    ExtendsContentIntoTitleBar="False">
    <ui:SmoothScrollViewer>
        <StackPanel Margin="24">
            <ui:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Buttons" ui:TextBlockExtensions.Typography="Subtitle"
                               Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <StackPanel Orientation="Horizontal">
                        <ui:Button Content="Standard" Margin="0,0,8,0" />
                        <ui:Button Content="Accent" Appearance="Accent" Margin="0,0,8,0" />
                        <ui:Button Content="Disabled" IsEnabled="False" />
                    </StackPanel>
                </StackPanel>
            </ui:Card>
            <ui:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Selection" ui:TextBlockExtensions.Typography="Subtitle"
                               Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <ui:ToggleSwitch x:Name="DemoToggle" OnContent="On" OffContent="Off" Margin="0,0,0,8" />
                    <ui:CheckBox Content="I am a checkbox" Margin="0,0,0,8" />
                    <ui:RadioButton Content="Option A" GroupName="Demo" Margin="0,0,0,4" />
                    <ui:RadioButton Content="Option B" GroupName="Demo" />
                </StackPanel>
            </ui:Card>
            <ui:Card Margin="0,0,0,16">
                <StackPanel>
                    <TextBlock Text="Text input" ui:TextBlockExtensions.Typography="Subtitle"
                               Foreground="{DynamicResource TextFillColorPrimaryBrush}" Margin="0,0,0,8" />
                    <ui:TextBox PlaceholderText="Type here" Margin="0,0,0,8" />
                    <ui:NumberBox Header="A number" Minimum="0" Maximum="100" SpinButtonPlacementMode="Compact" />
                </StackPanel>
            </ui:Card>
            <ui:InfoBar x:Name="StatusBar" IsOpen="True" IsClosable="False"
                        Severity="Informational" Title="Toggle state"
                        Message="Flip the switch above to update this message from PowerShell." />
        </StackPanel>
    </ui:SmoothScrollViewer>
</ui:FluenceWindow>
'@

$window = [System.Windows.Markup.XamlReader]::Parse($xaml)

# --- 7. One live interaction: the toggle drives the InfoBar text via a PowerShell handler. ---
$bar    = $window.FindName('StatusBar')
$toggle = $window.FindName('DemoToggle')
$toggle.add_Checked({   $bar.Message = 'The switch is ON (handled in PowerShell).' })
$toggle.add_Unchecked({ $bar.Message = 'The switch is OFF (handled in PowerShell).' })

# Follow OS light/dark changes while open; stop watching on close.
[Fluence.Wpf.SystemThemeWatcher]::Watch($window)
$window.add_Closed({ [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window) })

# --- 8. Show the window and pump the WPF message loop until it closes. ---
[void]$app.Run($window)
