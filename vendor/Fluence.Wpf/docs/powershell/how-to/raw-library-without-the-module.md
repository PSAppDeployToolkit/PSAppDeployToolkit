# How to use the library without the module

This guide shows how to load `Fluence.Wpf.dll` into a plain PowerShell script with `Add-Type`, create the application, apply the theme and show a window you wrote in XAML. Use it when you want the library and nothing else; for dialogs, prompts and progress use the module instead.

The four scripts under `Fluence.Wpf.Demo.PowerShell/` are runnable versions of everything below.

## Prerequisites

- Windows PowerShell 5.1 with the `net472` build of the library, or PowerShell 7.4 or later with the `net8.0-windows10.0.26100.0` build.
- The library built once: `dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Release`. The outputs are under `Fluence.Wpf/bin/Release/<tfm>/`.

WPF needs a single-threaded apartment. Windows PowerShell 5.1 starts in STA by default; an explicitly MTA process must relaunch:

```powershell
if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA')
{
    powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File $PSCommandPath @args
    return
}
```

`pwsh` starts in STA already.

## Load the assemblies

```powershell
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Xaml
$dll = Join-Path $PSScriptRoot '..\Fluence.Wpf\bin\Release\net472\Fluence.Wpf.dll'
Add-Type -Path $dll
```

On PowerShell 7 use the `net8.0-windows10.0.26100.0` folder and load the other DLLs in that folder first with `[System.Reflection.Assembly]::LoadFrom`; the library depends on them. The module's loader does exactly this, so on PowerShell 7 the module is the easier route.

## Create the application before theming

`ApplicationThemeManager.Apply` publishes its brushes into `Application.Current.Resources`. With no application object the call does nothing and the window renders with plain system controls.

```powershell
$app = New-Object System.Windows.Application
[Fluence.Wpf.ApplicationThemeManager]::Apply([Fluence.Wpf.ApplicationTheme]::Auto, 'Mica')
[Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()
```

Pass the backdrop as a string. PowerShell converts it to the library's backdrop enum, whichever name that enum has in the build you loaded (`BackdropType` in the 0.8 line, `WindowBackdropType` after the rename), so the script does not have to know.

## Write the window in XAML

Declare the Fluence namespace with its URI and use `fluence:FluenceWindow` as the root:

```powershell
$xaml = @'
<fluence:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fluence="http://schemas.fluencewpf.com"
    Title="My Window" Width="520" Height="340"
    SystemBackdropType="Mica" ExtendsContentIntoTitleBar="False">
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
        <TextBlock x:Name="HelloLabel" Text="Hello, World!" fluence:TextBlockExtensions.Typography="Title" Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
        <fluence:Button x:Name="CycleButton" Content="Next backdrop" Appearance="Accent" Margin="0,16,0,0" />
    </StackPanel>
</fluence:FluenceWindow>
'@
$window = [System.Windows.Markup.XamlReader]::Parse($xaml)
```

The older `clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf` form still works; the URI is shorter and survives a namespace move.

Use `DynamicResource` for every theme brush. `StaticResource` captures the value at parse time and does not follow theme or accent changes. `fluence:TextBlockExtensions.Typography` takes `Caption`, `Body`, `BodyStrong`, `Subtitle`, `Title`, `TitleLarge` or `Display`.

To load the XAML from a file instead:

```powershell
$stream = [System.IO.File]::OpenRead($xamlPath)
try
{
    $reader = [System.Xml.XmlReader]::Create($stream)
    try { $window = [System.Windows.Markup.XamlReader]::Load($reader) }
    finally { $reader.Dispose() }
}
finally { $stream.Dispose() }
```

Do not pass a path string to `XamlReader.Load`; it expects a stream or reader.

## Wire events

Find controls with `FindName` and subscribe with `add_<Event>`. Handlers run in a child scope, so a variable a handler assigns needs the `$script:` prefix; variables it only reads do not.

```powershell
$backdrops = @('Mica', 'Acrylic', 'Tabbed', 'None')
$script:tick = 0
$cycleButton = $window.FindName('CycleButton')
$cycleButton.add_Click({
    $script:tick++
    $window.SystemBackdropType = $backdrops[$script:tick % $backdrops.Count]
})
```

Add `param($s, $e)` when you need the sender or event arguments.

## Follow the Windows theme

```powershell
[Fluence.Wpf.SystemThemeWatcher]::Watch($window)
$window.add_Closed({ [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window) })
```

With `ApplicationTheme.Auto` applied, the window re-themes when the user switches Light and Dark in Windows Settings. Unwatch on close so the window is not kept alive.

## Change theme and accent later

`Apply` can be called at any time, including from a click handler; every `DynamicResource` updates.

```powershell
[Fluence.Wpf.ApplicationThemeManager]::Apply([Fluence.Wpf.ApplicationTheme]::Dark, 'Mica')
[Fluence.Wpf.ApplicationAccentColorManager]::ApplyCustomAccent([System.Windows.Media.Color]::FromRgb(0x10, 0x89, 0x3E))
[Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()
[Fluence.Wpf.ApplicationThemeManager]::CurrentTheme
[Fluence.Wpf.ApplicationThemeManager]::CurrentBackdrop
```

## Run the message loop

```powershell
[void]$app.Run($window)
```

`Run` shows the window and blocks until it closes. The `[void]` cast hides the integer it returns.

## The example scripts

Run any of them with `powershell.exe -STA -File .\01-HelloWorld.ps1`. Each relaunches itself in STA and builds the `net472` library on first run if `dotnet` is on the path.

| Script | Shows |
| --- | --- |
| `01-HelloWorld.ps1` | The whole bootstrap, a button that cycles the backdrop, `$script:` state, `SystemThemeWatcher`. |
| `02-ThemeAndAccent.ps1` | Light, Dark and Auto buttons; cycling a custom accent and returning to the system accent. |
| `03-ControlsTour.ps1` | Common controls in `Card` panels; a `ToggleSwitch` drives an `InfoBar` from PowerShell. |
| `04-LoadXamlFile.ps1` | The window in `MainWindow.xaml` on disk, loaded with `XamlReader.Load` and wired with `FindName`. |

## Fix common problems

**The window shows plain system controls.** No `Application` existed when `Apply` ran. Create it first.

**The background is black on first paint.** `Apply` ran after the XAML was parsed. Apply the theme, then parse.

**"The calling thread must be STA".** Add the relaunch block above or start with `powershell.exe -STA`.

**`Add-Type` fails on PowerShell 7.** You loaded the `net472` build. Use the `net8.0-windows10.0.26100.0` folder, or use the module.

**`$window` is `$null` after `XamlReader.Load`.** A path string was passed instead of a stream, or the parse threw and the error was not terminating. Set `$ErrorActionPreference = 'Stop'` and use the stream pattern above.

**Scripts are disabled on this system.** Run with `-ExecutionPolicy Bypass` (the relaunch block does) or set `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`.

## Related

- [theming.md](../../theming.md) for the token catalogue and dictionary layout
- [controls.md](../../controls.md) for every control with XAML snippets
- [Host a window from XAML](windows-from-xaml.md) for the same window through the module
