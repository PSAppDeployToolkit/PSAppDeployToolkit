# Fluence.Wpf - PowerShell examples

Self-contained Windows PowerShell 5.1 scripts that build a themed Fluent window with no
project, no compilation step of your own - just the Fluence.Wpf DLL loaded at runtime.

## Requirements

- Windows PowerShell 5.1 (built into Windows) run in STA mode. Each script relaunches itself
  in STA automatically.
- The .NET SDK on PATH (`dotnet`). On first run a script builds the `net472` Fluence.Wpf.dll
  if it is not already present.

## Run

```powershell
powershell.exe -STA -File .\01-HelloWorld.ps1
```

| Script | Shows |
| --- | --- |
| `01-HelloWorld.ps1` | The smallest example: a Mica window, a button that cycles Mica/Acrylic/Tabbed/None, and a rotating "Hello, World!" label. |
| `02-ThemeAndAccent.ps1` | Switching Light/Dark/Auto, cycling a custom accent, returning to the system accent, and following OS theme changes with `SystemThemeWatcher`. |
| `03-ControlsTour.ps1` | Common controls (buttons, toggle, checkbox, radio, text box, number box) in cards, with a toggle that updates an `InfoBar` from PowerShell. |
| `04-LoadXamlFile.ps1` | Loading the UI from `MainWindow.xaml` on disk instead of an inline string, then wiring its named controls. |

## The pattern every script uses

1. Relaunch in STA (WPF requirement).
2. Locate `..\Fluence.Wpf\bin\Release\net472\Fluence.Wpf.dll`; `dotnet build` it once if missing.
3. `Add-Type` the WPF assemblies + the Fluence DLL.
4. Create a `System.Windows.Application` **before** theming (otherwise the theme brushes have
   nowhere to publish).
5. `ApplicationThemeManager.Apply(theme, backdrop, updateAccent)`.
6. Parse XAML (`XamlReader.Parse` for a string, `XamlReader.Load` for a file), wire handlers
   with `$control.add_Click({ ... })`.
7. `$app.Run($window)` to show the window and run the message loop.

See `../docs/powershell.md` for the full guide.
