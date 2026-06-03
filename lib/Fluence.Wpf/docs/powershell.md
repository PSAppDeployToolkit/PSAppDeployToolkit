---
title: Using Fluence.Wpf from Windows PowerShell 5.1
linkTitle: PowerShell
description: Build a themed Fluent WPF window from a PowerShell script with no project file and no compile step of your own.
weight: 50
---

Fluence.Wpf ships as a standard .NET Framework 4.7.2 assembly. Windows PowerShell 5.1
(built into every Windows installation) can load that assembly at runtime and create a
fully themed Fluent window from a single script file, with no project of your own to
compile.

The four scripts in `Fluence.Wpf.Demo.PowerShell/` are the canonical, runnable examples
this guide is based on. Every snippet below comes directly from those scripts.

---

## Overview

The PowerShell path trades the IDE for a single script:

- No project file, no solution, no `App.xaml`.
- `Add-Type` loads the WPF assemblies and the Fluence DLL.
- The window is defined in XAML, either as an inline string or a `.xaml` file on disk.
- Theme, accent, and OS-change tracking use the same static API you would call from
  C#, written with PowerShell syntax for static methods.

You get a Mica-backed, light/dark-aware Fluent window a few seconds after running the
script in a terminal.

---

## Prerequisites

### Windows PowerShell 5.1

Use `powershell.exe` (Windows PowerShell 5.1), not `pwsh` (PowerShell 7+). The Fluence
DLL targets `net472`, which is the CLR that `powershell.exe` hosts. PowerShell 7 uses
.NET (Core / 5+); loading the `net472` Fluence assembly there is not supported and not reliable (runtime and shim mismatches), so use `powershell.exe`.

Verify you have the right host:

```powershell
$PSVersionTable.PSVersion
# Major should be 5
```

### STA apartment mode

WPF requires the host thread to be in Single-Threaded Apartment (STA) mode. The default
console mode is MTA. The scripts detect this and relaunch themselves transparently:

```powershell
if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -ne 'STA') {
    powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File $PSCommandPath @args
    return
}
```

Place this block at the top of every script, before any WPF or Fluence code. The
simplest alternative is to always launch with the `-STA` flag:

```powershell
powershell.exe -STA -File .\MyScript.ps1
```

### .NET SDK on PATH

The scripts build the `net472` Fluence.Wpf DLL automatically on first run. For that to
work, `dotnet` must be on the system PATH. Install the .NET SDK from
<https://dot.net/download> if needed.

### The net472 DLL

The scripts locate the DLL relative to their own directory:

```powershell
$dll = Join-Path $PSScriptRoot '..\Fluence.Wpf\bin\Release\net472\Fluence.Wpf.dll'
if (-not (Test-Path -LiteralPath $dll)) {
    Write-Host 'Building Fluence.Wpf (net472, Release) - first run only...'
    dotnet build (Join-Path $PSScriptRoot '..\Fluence.Wpf\Fluence.Wpf.csproj') -c Release -f net472 --nologo -v q
}
```

To build the DLL manually at any time:

```powershell
dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Release -f net472
```

The resulting file is `Fluence.Wpf/bin/Release/net472/Fluence.Wpf.dll`.

---

## The canonical bootstrap

Every script follows the same sequence. Read through each step before writing your own.

### Step 1 - STA relaunch

(See the snippet above in the Prerequisites section.)

### Step 2 - Locate or build the DLL

(See the snippet above in the Prerequisites section.)

### Step 3 - Load WPF assemblies and the Fluence DLL

```powershell
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Xaml
Add-Type -Path $dll
```

`Add-Type` with `-AssemblyName` loads the four core WPF assemblies that ship with the
.NET Framework. `-Path` loads the Fluence.Wpf DLL from disk.

### Step 4 - Create a WPF Application

```powershell
$app = New-Object System.Windows.Application
```

Create the `Application` before calling `Apply`. `ApplicationThemeManager.Apply`
publishes brushes into `Application.Current.Resources`. With no `Application` object in
place, the call silently no-ops and the window renders unstyled: plain system controls,
no Fluent brushes.

### Step 5 - Apply the theme

```powershell
[Fluence.Wpf.ApplicationThemeManager]::Apply(
    [Fluence.Wpf.ApplicationTheme]::Auto,
    [Fluence.Wpf.BackdropType]::Mica,
    $true)
[Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()
```

The first `Apply` call seeds the resource stack: brushes, typography, and control
templates. Later calls swap only the computed color and brush dictionary, so every
`DynamicResource` binding in the control templates re-resolves on its own.

### Step 6 - Parse XAML and build the window

For inline XAML:

```powershell
$xaml = @'
<ui:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"
    Title="My Window"
    Width="520" Height="340"
    SystemBackdropType="Mica"
    ExtendsContentIntoTitleBar="False">
    <!-- content here -->
</ui:FluenceWindow>
'@

$window = [System.Windows.Markup.XamlReader]::Parse($xaml)
```

For XAML in a file on disk:

```powershell
$stream = [System.IO.File]::OpenRead($xamlPath)
try {
    $reader = [System.Xml.XmlReader]::Create($stream)
    try { $window = [System.Windows.Markup.XamlReader]::Load($reader) }
    finally { $reader.Dispose() }
}
finally { $stream.Dispose() }
```

### Step 7 - Wire event handlers and watch for OS theme changes

```powershell
$button = $window.FindName('MyButton')
$button.add_Click({ Write-Host 'Clicked' })

[Fluence.Wpf.SystemThemeWatcher]::Watch($window)
$window.add_Closed({ [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window) })
```

### Step 8 - Run the message loop

```powershell
[void]$app.Run($window)
```

`$app.Run($window)` shows the window and blocks until it is closed. The `[void]` cast
suppresses the integer return value that PowerShell would otherwise print to the console.

---

## Theming API from PowerShell

### Applying a theme

Static methods on .NET types are called with `[Namespace.ClassName]::MethodName(...)`.

```powershell
# Follow the Windows light/dark setting (default for scripts)
[Fluence.Wpf.ApplicationThemeManager]::Apply(
    [Fluence.Wpf.ApplicationTheme]::Auto,
    [Fluence.Wpf.BackdropType]::Mica,
    $true)

# Force light
[Fluence.Wpf.ApplicationThemeManager]::Apply(
    [Fluence.Wpf.ApplicationTheme]::Light,
    [Fluence.Wpf.BackdropType]::Mica,
    $true)

# Force dark
[Fluence.Wpf.ApplicationThemeManager]::Apply(
    [Fluence.Wpf.ApplicationTheme]::Dark,
    [Fluence.Wpf.BackdropType]::Mica,
    $true)

# High contrast
[Fluence.Wpf.ApplicationThemeManager]::Apply(
    [Fluence.Wpf.ApplicationTheme]::HighContrast,
    [Fluence.Wpf.BackdropType]::None,
    $true)
```

`Apply` can be called at any time - including from button click handlers - to change the
theme of a running window. All `DynamicResource` bindings update immediately.

### ApplicationTheme enum values

| Value | Behavior |
|-------|----------|
| `Auto` | Follows the Windows light/dark system setting |
| `Light` | Forces light theme |
| `Dark` | Forces dark theme |
| `HighContrast` | Forces high contrast mode |

### BackdropType enum values

| Value | Behavior |
|-------|----------|
| `None` | Solid window background, no system material |
| `Auto` | Let Fluence choose the best available backdrop |
| `Mica` | Mica (Windows 11 only; falls back on Windows 10) |
| `Acrylic` | Acrylic (Windows 10 1809+) |
| `Tabbed` | Tabbed / Mica Alt (Windows 11 only) |

### Reading the current state

```powershell
[Fluence.Wpf.ApplicationThemeManager]::CurrentTheme
[Fluence.Wpf.ApplicationThemeManager]::CurrentBackdrop
```

### Accent color

```powershell
# Use the accent color configured in Windows Settings
[Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()

# Set a custom accent color (here: Windows blue)
[Fluence.Wpf.ApplicationAccentColorManager]::ApplyCustomAccent(
    [System.Windows.Media.Color]::FromRgb(0, 120, 212))

# Cycle a palette of custom accents (pattern from 02-ThemeAndAccent.ps1)
$accents = @(
    [System.Windows.Media.Color]::FromRgb(0x00, 0x78, 0xD4),  # blue
    [System.Windows.Media.Color]::FromRgb(0x10, 0x89, 0x3E),  # green
    [System.Windows.Media.Color]::FromRgb(0xC4, 0x2B, 0x1C),  # red
    [System.Windows.Media.Color]::FromRgb(0x74, 0x37, 0xC9)   # purple
)
$script:accentIndex = 0
$cycleButton.add_Click({
    $color = $accents[$script:accentIndex % $accents.Count]
    $script:accentIndex++
    [Fluence.Wpf.ApplicationAccentColorManager]::ApplyCustomAccent($color)
})
```

### Following OS theme changes live

Register a window with `SystemThemeWatcher` to receive OS light/dark change notifications
automatically. Unregister when the window closes to avoid memory leaks.

```powershell
[Fluence.Wpf.SystemThemeWatcher]::Watch($window)
$window.add_Closed({ [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window) })
```

With `ApplicationTheme::Auto` and `SystemThemeWatcher` active together, the window
re-themes itself when the user switches between Light and Dark in Windows Settings,
without any additional code.

---

## FluenceWindow from XAML

### Namespace declaration

Fluence controls live in `Fluence.Wpf.Controls`. Declare the namespace as `ui` (or any
prefix you prefer) on the root element:

```xml
xmlns:ui="clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"
```

Use `ui:FluenceWindow` as the root element instead of the standard WPF `Window`.

### Key dependency properties

| Property | Type | Default | Notes |
|----------|------|---------|-------|
| `SystemBackdropType` | `BackdropType` | `Auto` | Sets the DWM system material |
| `ExtendsContentIntoTitleBar` | `bool` | `false` | When `true`, content fills the title bar area |
| `TitleBar` | content slot | - | Custom content placed in the title bar |
| `TitleBarHeight` | `double` | - | Override the title bar height |
| `CornerStyle` | enum | `Round` | Controls window corner rounding |

### Switching the backdrop at runtime

Setting `SystemBackdropType` on the window object re-applies the DWM backdrop live:

```powershell
# Cycle backdrop on button click (from 01-HelloWorld.ps1)
$backdrops = @('Mica', 'Acrylic', 'Tabbed', 'None')
$script:tick = 0

$cycleButton.add_Click({
    $script:tick++
    $name = $backdrops[$script:tick % $backdrops.Count]
    $window.SystemBackdropType = [Enum]::Parse([Fluence.Wpf.BackdropType], $name)
    $backdropLabel.Text = "Backdrop: $name"
})
```

### Theme color brushes

Control templates and any custom content reference theme brushes with `DynamicResource`.
The brush updates automatically when the theme or accent changes:

```xml
<TextBlock Foreground="{DynamicResource TextFillColorPrimaryBrush}"
           Text="Primary text" />

<TextBlock Foreground="{DynamicResource TextFillColorSecondaryBrush}"
           Text="Secondary text" />
```

Do not use `StaticResource` for theme brushes - it captures the value at parse time and
will not follow theme or accent changes.

For the complete list of available color and brush tokens, see [theming.md](theming.md).

### Typography

`ui:TextBlockExtensions.Typography` maps to the named Fluent type-ramp styles:

```xml
<TextBlock ui:TextBlockExtensions.Typography="Title"
           Foreground="{DynamicResource TextFillColorPrimaryBrush}"
           Text="Title text" />

<TextBlock ui:TextBlockExtensions.Typography="Subtitle"
           Foreground="{DynamicResource TextFillColorPrimaryBrush}"
           Text="Subtitle text" />
```

Available values: `Caption`, `Body`, `BodyStrong`, `Subtitle`, `Title`, `TitleLarge`,
`Display`.

---

## Wiring events

### Finding named elements

`FindName` returns the element with the given `x:Name` attribute, or `$null` if not found:

```powershell
$button  = $window.FindName('MyButton')
$label   = $window.FindName('MyLabel')
$toggle  = $window.FindName('DemoToggle')
```

### Subscribing to events

WPF events are subscribed with `add_<EventName>`:

```powershell
$button.add_Click({ Write-Host 'Button clicked' })

$toggle.add_Checked({   $bar.Message = 'The switch is ON (handled in PowerShell).' })
$toggle.add_Unchecked({ $bar.Message = 'The switch is OFF (handled in PowerShell).' })
```

### Handler state and $script: scope

Script blocks used as event handlers run in a child scope. Variables defined in the
enclosing script are not automatically writable from inside a handler. Use the
`$script:` scope prefix for any variable that handlers need to read or update:

```powershell
$script:tick = 0

$cycleButton.add_Click({
    $script:tick++
    $name = $backdrops[$script:tick % $backdrops.Count]
    $window.SystemBackdropType = [Enum]::Parse([Fluence.Wpf.BackdropType], $name)
})
```

Variables that are only read (not reassigned) inside the handler, such as `$window` or
`$bar`, do not need the `$script:` prefix - PowerShell walks up the scope chain for reads.

### Accessing event arguments

Add `param($s, $e)` inside the script block to access the sender and event args:

```powershell
$themeCombo.add_SelectionChanged({
    param($s, $e)
    $theme = switch ($s.SelectedIndex) {
        1       { [Fluence.Wpf.ApplicationTheme]::Light }
        2       { [Fluence.Wpf.ApplicationTheme]::Dark }
        3       { [Fluence.Wpf.ApplicationTheme]::HighContrast }
        default { [Fluence.Wpf.ApplicationTheme]::Auto }
    }
    [Fluence.Wpf.ApplicationThemeManager]::Apply($theme, [Fluence.Wpf.BackdropType]::Mica, $true)
})
```

---

## The four example scripts

The `Fluence.Wpf.Demo.PowerShell/` directory holds four ready-to-run scripts. Run any of
them from a terminal:

```powershell
powershell.exe -STA -File .\01-HelloWorld.ps1
```

### 01-HelloWorld.ps1

The smallest complete example. Opens a Mica-backed window with a greeting label and a
single button. Each click cycles the backdrop through Mica, Acrylic, Tabbed, and None,
and advances the label through a short list of international greetings. Covers the full
bootstrap sequence, `FindName`, `$script:` state, and `SystemThemeWatcher`.

[View script](../Fluence.Wpf.Demo.PowerShell/01-HelloWorld.ps1)

### 02-ThemeAndAccent.ps1

Runtime theme and accent control. Three buttons switch between Light, Dark, and Auto;
two more cycle a palette of custom accent colors and restore the system accent. With
`Auto` selected and `SystemThemeWatcher` registered, the window re-themes itself when the
Windows setting changes while the script is running.

[View script](../Fluence.Wpf.Demo.PowerShell/02-ThemeAndAccent.ps1)

### 03-ControlsTour.ps1

Common Fluence controls inside scrolling `Card` panels: standard and accent `Button`,
`ToggleSwitch`, `CheckBox`, `RadioButton`, `TextBox`, and `NumberBox`. The `ToggleSwitch`
drives an `InfoBar` message entirely from PowerShell, with no XAML binding.

[View script](../Fluence.Wpf.Demo.PowerShell/03-ControlsTour.ps1)

### 04-LoadXamlFile.ps1

The more maintainable pattern: the window UI lives in a separate `MainWindow.xaml` file
and loads with `XamlReader.Load` over a file stream instead of an inline here-string. The
script then calls `FindName` to locate named controls and wires them exactly as the other
scripts do. Reach for this once the XAML outgrows a comfortable string literal.

[View script](../Fluence.Wpf.Demo.PowerShell/04-LoadXamlFile.ps1)

---

## Troubleshooting

### Window opens unstyled or with plain system controls

**Cause.** No `System.Windows.Application` object existed when `ApplicationThemeManager.Apply`
was called. Without an application object, `Apply` cannot publish brushes to
`Application.Current.Resources` and silently returns without doing anything. Control
templates load but find none of the Fluence brush resources, so the window renders with
default system styling.

**Fix.** Ensure `New-Object System.Windows.Application` runs before `Apply`:

```powershell
$app = New-Object System.Windows.Application   # BEFORE Apply
[Fluence.Wpf.ApplicationThemeManager]::Apply(...)
```

### Window background is black on first paint

**Cause.** A `DynamicResource` brush key was referenced before the theme manager had a
chance to publish. Most commonly caused by moving `Apply` after XAML parsing.

**Fix.** Call `Apply` before parsing any XAML that references Fluence brush keys:

```powershell
$app = New-Object System.Windows.Application
[Fluence.Wpf.ApplicationThemeManager]::Apply(...)   # before XamlReader.Parse
$window = [System.Windows.Markup.XamlReader]::Parse($xaml)
```

### STA / apartment state error

**Symptom.** An exception mentioning `InvalidOperationException` and "apartment" or
"STA" when trying to create WPF objects.

**Cause.** The script is running on an MTA thread (the default for `powershell.exe`
launched without `-STA`).

**Fix.** Add the STA relaunch block at the top of the script (shown in Prerequisites), or
always launch with:

```powershell
powershell.exe -STA -File .\MyScript.ps1
```

### Add-Type or type-not-found errors with pwsh (PowerShell 7+)

**Symptom.** `Add-Type -Path $dll` reports that the assembly could not be loaded, or
Fluence type names are not recognized after loading.

**Cause.** `pwsh` (PowerShell 7+) runs on .NET (Core / 5+). The Fluence DLL targets
`net472` and is only loadable by the .NET Framework 4.7.2 runtime that `powershell.exe`
(Windows PowerShell 5.1) hosts.

**Fix.** Use `powershell.exe`, not `pwsh`.

### Execution policy prevents the script from running

**Symptom.** `File ... cannot be loaded because running scripts is disabled on this system.`

**Fix.** Run with an explicit execution policy override (the STA relaunch block already
includes `-ExecutionPolicy Bypass`), or adjust the machine policy with administrator
rights:

```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

### Building the DLL manually

If the automatic first-run build fails (for example because the .NET SDK is not on PATH),
build the DLL manually from the repository root:

```powershell
dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Release -f net472
```

The expected output path is `Fluence.Wpf/bin/Release/net472/Fluence.Wpf.dll`.

### $window is null after XamlReader.Load

**Symptom.** `FindName` calls throw null-reference errors; `$window` is `$null`.

**Cause.** `XamlReader.Load` threw an exception that was silently swallowed, or the
wrong overload was used (passing a string path instead of an `XmlReader`).

**Fix.** Set `$ErrorActionPreference = 'Stop'` at the top of the script so exceptions
surface immediately. Use the stream-plus-`XmlReader` pattern shown in the bootstrap
section; do not pass a file path string directly to `XamlReader.Load`.

---

## Next steps

- [theming.md](theming.md) - the full token catalog, dictionary slot layout, accent
  ramp, and high contrast behavior.
- [controls.md](controls.md) - the complete control inventory with XAML snippets for
  each control.
- [getting-started.md](getting-started.md) - C# project setup, `App.OnStartup`
  initialization, and `FluenceWindow` composition.
