## Scope

`Fluence.Wpf` targets WPF applications on .NET Framework 4.7.2 and .NET 10 for Windows. It mirrors the Windows 11 Fluent / WinUI 3 visual language using WPF primitives, with no dependency on the Windows App SDK.

## Basic Steps

1. Reference `Fluence.Wpf/Fluence.Wpf.csproj` or a local `Fluence.Wpf` package.
2. Add the XML namespace:

    ```xml
    xmlns:fluence="http://schemas.fluencewpf.com"
    ```

3. Initialize resources before showing the first window:

    ```csharp
    Fluence.Wpf.ApplicationThemeManager.Apply(
        Fluence.Wpf.ApplicationTheme.Auto,
        Fluence.Wpf.BackdropType.Mica,
        updateAccent: true);
    Fluence.Wpf.ApplicationAccentColorManager.ApplySystemAccent();
    ```

4. Replace shell windows with `fluence:FluenceWindow` where you need Fluent caption buttons, a DWM backdrop, rounded corners, or a title-bar content slot.
5. Replace controls incrementally. Start with leaf controls (`Button`, `TextBox`, `ComboBox`, `ListView`, `InfoBar`, `ProgressBar`), then move larger shell surfaces like `NavigationView` and `TabView`.

## Resource Rules

- Use `DynamicResource` for Fluence brushes, colors, typography, corner radii, and theme-bound values.
- Do not manually merge `Themes/Generic.xaml` when using `ApplicationThemeManager.Apply`; the manager owns the fixed resource dictionary slots.
- Bind to brush resources such as `TextFillColorPrimaryBrush` and `ControlFillColorDefaultBrush` from control templates and application XAML, not to raw color resources.

## Title bar and window controls

`FluenceWindow` owns DWM and caption-button behavior. Use its public properties: `SystemBackdropType`, `CornerStyle`, `ExtendsContentIntoTitleBar`, `TitleBar`, and the caption-button visibility properties. `CaptionButtonChrome` and `WindowPolicy` are internal helpers; do not reference them from application code.

## WinUI-canonical title-bar and caption metrics (visual-only change, no API impact)

The `FluenceWindow` and `TitleBar` controls were re-authored to WinUI-canonical metrics. **There are no public API, dependency property, event, or template-part changes** -- this is a drop-in update; existing XAML and code-behind compile and run unchanged.

Visual changes to be aware of:

- `FluenceWindow.TitleBarHeight` default changed from 68 to 48 px (the WinUI 3 canonical expanded title-bar height). Any explicit `TitleBarHeight="42"` (or other explicit value) in your XAML is unaffected.
- Minimize, maximize, and restore caption buttons are now 46 px wide (was approximately 64 px) and stretch to the full title-bar height instead of a fixed 32 px with top alignment. The close button is unchanged.
- Caption-button hover and press fills changed from a strong inverted fill (`ControlStrongFillColorDefaultBrush` background, `TextFillColorInverseBrush` glyph) to WinUI-canonical subtle fills (`SubtleFillColorSecondaryBrush` hover, `SubtleFillColorTertiaryBrush` press; glyph keeps its normal `TextFillColorPrimaryBrush` color). The close button hover/press colors are unchanged.
- `TitleBar` back and pane-toggle button slot width changed from 42 to 40 px, matching the WinUI 3 canonical hit-area width.

If your application relied on the exact 68 px default title-bar height or the previous caption-button sizing, set `TitleBarHeight` and caption-button widths explicitly in your `FluenceWindow` XAML.

## Verification

After migrating a page or shell surface, run the gallery and check Light, Dark, High Contrast, accent changes, and the target backdrop mode. For source builds, run:

```powershell
dotnet build Fluence.Wpf.sln -c Debug
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug
```
