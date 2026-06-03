# Fluence.Wpf Library

This folder contains the reusable WPF control library. It targets `net472` and `net10.0-windows10.0.26100.0` and provides the Fluent/WinUI-style controls, theme resources, accent handling, window chrome, and native interop used by the demo applications.

## What Lives Here

- `Controls/` - public WPF controls such as `FluenceWindow`, `NavigationView`, `TabView`, `Card`, input controls, status controls, and layout helpers.
- `Themes/` - color, brush, typography, and control-template dictionaries loaded by `ApplicationThemeManager`.
- `Automation/` - UI Automation peers for custom controls.
- `Native/` and `Helpers/` - DWM, OS-version, registry, and rendering helpers.
- `ApplicationThemeManager`, `ApplicationAccentColorManager`, and `SystemThemeWatcher` - the theme/accent lifecycle surface consumers call at startup.

## Build

From the repository root:

```powershell
dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Debug
```

All targets use `LangVersion=latest`, but runtime API use must remain compatible with `net472` unless a file is explicitly target-specific. Keep public APIs documented with XML comments and keep `.cs`, `.xaml`, and `.csproj` files encoded as UTF-8 with BOM.

## Maintenance Notes

Use `ApplicationThemeManager.Apply(...)` to load the three managed resource-dictionary slots (`[0]` computed colors and brushes, `[1]` Typography, `[2]` Generic) instead of hand-merging `Themes/Generic.xaml`. When changing templates, prefer canonical WinUI-style theme keys and `DynamicResource` for theme/accent-bound brushes. See the root [AGENTS.md](../AGENTS.md), [docs/theming.md](../docs/theming.md), and [docs/controls.md](../docs/controls.md) for the full contract.
