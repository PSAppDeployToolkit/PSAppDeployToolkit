Fluence.Wpf uses WinUI 3 naming and state behavior for theme resources, implemented as plain WPF. If you already work with WinUI keys, most token names and control roles will look familiar.

See [docs/winui-parity.md](winui-parity.md) for a token-by-token and template-by-template comparison against WinUI 3 CommonStyles, including where and why Fluence deliberately diverges.

## Merge order (application resources)

`Application.Current.Resources.MergedDictionaries` uses a **stable 3-slot layout** after the first `ApplicationThemeManager.Apply`:

| Index | Content | On theme or accent change |
| ----- | ------- | ------------------------- |
| 0 | Computed colors and brushes | **Replaced** with a freshly built dictionary on every change |
| 1 | Typography (`Typography.xaml`) | Loaded once; never replaced |
| 2 | Control templates (`Generic.xaml`) | Loaded once; never replaced |

Slot 0 holds every canonical Color token and its frozen `SolidColorBrush` twin. It is built entirely in C# by `FluenceThemeEngine` each time `Apply` is called; replacing it causes all `DynamicResource` bindings to re-resolve with no promotion step. `Brushes.xaml` and `Accent.xaml` no longer exist; brushes are produced by `BrushFactory` (auto Color-to-Brush twins) and `SpecialBrushes` (gradient elevation borders, High Contrast SystemColors overrides, and brush-only exceptions). The per-theme XAML files (`Themes/Colors/Theme.*.xaml`) are Color-only tables read at runtime by `BaseColorTables` inside `ColorMap.Build`; they contain no brushes.

Repeated `Apply` calls must not accumulate extra theme dictionaries (`DictionaryStabilityTests` enforces this). Seeding the slots again, which happens when an application clears `Application.Resources` and applies a theme afresh, removes the dictionaries Fluence published before: `Typography.xaml` and `Generic.xaml` by their pack URI, and the computed dictionary by a marker key it carries, since a dictionary built in code has no `Source` to match on. This matters because WPF resolves merged dictionaries last-wins: a computed dictionary left behind past slot 0 would answer lookups that the freshly published one owns.

`Typography.xaml` defines the Fluent type ramp as named `TextBlock` styles: `BodyTextBlockStyle`, `BodyStrongTextBlockStyle`, `TitleLargeTextBlockStyle`, and so on. `TextBlockExtensions.Typography` is the compatibility API; it resolves those styles rather than duplicating font metrics in code.

## Rules for XAML and code

- Consume theme and accent brushes with **`DynamicResource`**, not `StaticResource`, so they track live updates.
- Do not hard-code theme colors in control templates; bind to shared keys.
- **High contrast**: brushes are built from live `SystemColors` snapshots in `SpecialBrushes.AddHighContrastBrushes` and published in slot 0 like any other theme. There is no promotion or `_promotedHighContrastBrushKeys` list. A `WM_SETTINGCHANGE` via `SystemThemeWatcher` triggers a re-Apply that refreshes the snapshot.

## Canonical token families

Fluence.Wpf defines the full WinUI 3 token ramp. These are the keys you will reference most often in custom templates:

- **Text**: `TextFillColorPrimary`, `TextFillColorSecondary`, `TextFillColorTertiary`, `TextFillColorDisabled`, `TextOnAccentFillColorPrimary` / `Secondary` / `Disabled`.
- **Fill**: `ControlFillColorDefault`, `ControlFillColorSecondary`, `ControlFillColorTertiary`, `ControlFillColorInputActive`, `ControlFillColorDisabled`, `ControlAltFillColorSecondary` / `Tertiary` / `Quarternary` (CheckBox / RadioButton / ToggleSwitch tracks, `Card` `Filled` variant), `AccentFillColorDefault` / `Secondary` / `Tertiary` / `Disabled`, `SubtleFillColorSecondary` / `Tertiary`, `LayerFillColorDefault`, `CardBackgroundFillColorDefault`.
- **Stroke**: `ControlStrokeColorDefault` / `Secondary`, **`ControlStrongStrokeColorDefault`** (radio / check-box rings), **`ControlStrongStrokeColorDisabled`**, `CardStrokeColorDefault`, `SurfaceStrokeColorDefault` (the `FluenceWindow` outer border on Windows 10; Windows 11 uses the DWM border), `DividerStrokeColorDefault`, `FocusStrokeColorOuter` / `Inner`.
- **Background**: `SolidBackgroundFillColorBase`, `ApplicationBackgroundColor`.
- **Window controls**: `WindowCloseButtonBackgroundPointerOver`, `WindowCloseButtonBackgroundPressed`, `WindowCloseButtonForegroundPointerOver`.
- **High contrast aliases**: `SystemColorWindowTextColorBrush`, `SystemColorWindowColorBrush`, `SystemColorButtonFaceColorBrush`, `SystemColorButtonTextColorBrush`, `SystemColorHighlightColorBrush`, `SystemColorHighlightTextColorBrush`, `SystemColorHotlightColorBrush`, `SystemColorGrayTextColorBrush`. These brush-only aliases map directly to WPF `SystemColors`, so you can preview or bind Windows high contrast roles without hard-coding platform resources.

Each color token has a matching `*Brush` frozen `SolidColorBrush` - for example `ControlStrongStrokeColorDefaultBrush` - produced by `BrushFactory`. Reference the brush keys from XAML, not the raw color keys.

### Published families the list above does not spell out

- **System fill roles**: `SystemFillColorSuccess`, `SystemFillColorCaution`, `SystemFillColorCritical`, `SystemFillColorAttention`, `SystemFillColorNeutral`, `SystemFillColorSolidNeutral`, and their `*Background` and `SolidAttentionBackground` companions. These are the `InfoBar` severity roles and the status colours a consumer should reuse rather than hard-coding.
- **Background, layer and card, beyond Default**: `SolidBackgroundFillColorSecondary` / `Tertiary` / `Quarternary` / `Quinary` / `Senary`, `LayerFillColorAlt`, `CardBackgroundFillColorSecondary` / `Tertiary`, and the `ControlOnImageFillColor*` family. WinUI spells the fourth member `Quarternary`; so does Fluence, deliberately.
- **Motion**: `ControlFasterAnimationDuration` (83 ms), `ControlFastAnimationDuration` (167 ms), `ControlNormalAnimationDuration` (250 ms), and the easing curve `ControlFastOutSlowInKeySpline` (`0,0,0,1`). `ControlPressAnimationDuration` (100 ms) and `ControlSlowAnimationDuration` (333 ms) are **Fluence only**: they extend the WinUI naming pattern, so they read like WinUI keys and are not.
- **Corner radii**: `ControlCornerRadius` (4), `OverlayCornerRadius` (8), `ComboBoxItemCornerRadius` (3, WinUI's own key for the drop-down item, which it deliberately keeps distinct from `ControlCornerRadius`), and `PopupCornerRadius`, which is WPF only because WinUI has no `Popup` of this shape.
- **Typography styles**: `CaptionTextBlockStyle`, `BodyTextBlockStyle`, `BodyStrongTextBlockStyle`, `BodyLargeTextBlockStyle`, `SubtitleTextBlockStyle`, `TitleTextBlockStyle`, `TitleLargeTextBlockStyle`, `DisplayTextBlockStyle`, and the family `ContentControlThemeFontFamily`. `FluentFontFamily` is a supported alias of that family and ships for the life of 1.x; prefer the WinUI name in new code.
- **Window chrome**: `TitleBarActiveColor`, `TitleBarInactiveColor` and `WindowBorderColor`, with their `*Brush` twins. WinUI's own 47-key `TitleBar*` family describes the layout of its `TitleBar` control, not the colour of a window frame, so these are WPF-specific and have no WinUI counterpart.
- **Accent ramp**: `SystemAccentColor` plus `SystemAccentColorLight1` to `Light3` and `Dark1` to `Dark3` are the Windows ramp, the values WinRT supplies to a WinUI app and a WPF app has to materialise itself. `SystemAccentColorPrimary`, `Secondary` and `Tertiary` are a different, theme-resolved family that comes from the .NET WPF Fluent theme: the `Primary` Color resolves to `Dark1` in Light and `Light2` in Dark (`Fluence.Wpf/Theming/ColorMap.cs:73`), so it is the one to bind for accent text and fills that must stay legible in both themes. Its brush twin is not the same value: `SystemAccentColorPrimaryBrush` publishes `Dark2` in Light and `Dark3` in Dark, and `SpecialBrushes.Add` applies the same kind of Color-to-brush divergence to `SystemAccentColorSecondaryBrush` and `SystemAccentColorTertiaryBrush` (`Fluence.Wpf/Theming/SpecialBrushes.cs:61-68`, under the comment "Divergent accent brushes whose Color twin differs from the brush value"). Both families ship.
- **Acrylic and mica surfaces**: `AcrylicBackgroundFillColorDefault` and `AcrylicBackgroundFillColorBase` are the acrylic plate fallbacks every flyout, menu, picker and tooltip paints, and `AccentAcrylicBackgroundFillColorDefault` and `AccentAcrylicBackgroundFillColorBase` are their accent-tinted counterparts, built from the accent ramp the way WinUI's `AcrylicBrush_themeresources.xaml` builds them (`Dark1` and `Dark2` in dark, `Light3` for both in light). `LayerOnAcrylicFillColorDefault`, `LayerOnAccentAcrylicFillColorDefault` and the `LayerOnMicaBaseAltFillColor*` family are the layers that sit on top of those surfaces. WPF has no per-element backdrop, so these are flat fallback colours rather than live blurs. In high contrast every one of their `*Brush` twins except `LayerOnMicaBaseAltFillColorTransparentBrush`, which stays transparent, resolves to a `SystemColors` value (the two accent members to the highlight), while the Colors themselves come from the high contrast table as fixed literals, the two accent members included: they are not read from the accent ramp there.
- **InfoBadge foregrounds**: `InfoBadgeAttentionForegroundBrush`, `InfoBadgeInformationalForegroundBrush`, `InfoBadgeSuccessForegroundBrush`, `InfoBadgeCautionForegroundBrush` and `InfoBadgeCriticalForegroundBrush`, one per `InfoBadgeStyle`. WinUI keys the badge's foreground for the same reason (`InfoBadgeForeground`): the numeral sits on a filled capsule, so it has to pair with whatever that plate is. Outside high contrast all five resolve to `TextOnAccentFillColorPrimary`; in high contrast they split, because the Attention plate is the system highlight and the other four are the window text colour. Brush-only, with no Color twin.
- **Fluence-only plate**: `AccentFillBackdrop`, the opaque plate painted under a sub-1.0-alpha accent fill so the translucent fill composites over a known ground rather than over the DWM backdrop, and `TeachingTipTopHighlight`, the 1 px top highlight WinUI's teaching tip draws.
- **Aliases**: `ApplicationPageBackgroundThemeBrush` is WinUI's name for the brush `ApplicationBackgroundBrush` publishes; both ship as the same frozen instance and both are supported for the life of 1.x. `ApplicationBackgroundColor` is the Color behind them.

One divergence to know about: `AccentFillColorSecondary` and `AccentFillColorTertiary` bake their alpha into the Color (`0xE6` and `0xCC`) where WinUI puts `Opacity` on a brush over the full accent colour. Over an opaque ground the two are identical. Over a translucent one they are not, because WPF composites brush `Opacity` at full precision while a premultiplied-alpha layer over Mica goes through the DWM alpha path measured in [KNOWN_ISSUES.md](../KNOWN_ISSUES.md).

## What is supported, and what is not

`Themes/Generic.xaml` merges 53 dictionaries into `Application.Resources`, so every `x:Key` under `Themes/Controls/**` and `Themes/Icons/**` is technically resolvable from an application. **Those keys are not public API.** They are template internals: styles, control templates, storyboards, geometries and glyph resources that exist to build one control, and they may be renamed, retargeted or removed in any release, including a patch. Do not bind them, do not override them, and do not rely on their values.

Two exceptions are supported and are meant to be reused: `DefaultControlFocusVisualStyle` and `DefaultCollectionFocusVisualStyle`, the focus visuals every Fluence control and collection item applies.

What is supported is everything published into the computed dictionary at slot `[0]`, which is every canonical Color token and its `*Brush` twin plus the special brushes, together with the keys in `Themes/Typography/Typography.xaml`. `Fluence.Wpf.Tests/Theming/golden/PublicKeys.txt` is the frozen inventory of that set, and `ThemeParityTests.PublicKeyInventory_MatchesFrozenSetAsync` fails the build on an unrecorded addition or removal, which is the XAML counterpart of `PublicApiAnalyzers` on the CLR surface.

There are no per-control override keys. WinUI publishes roughly 3,100 of them, for example `ButtonBackgroundPointerOver` and `CheckBoxCheckBackgroundFillChecked`; Fluence publishes none, because its templates bind the global palette directly. The consequence is concrete: **restyling one control means replacing that control's template**, not setting a key. Per-control override keys are additive, so they can arrive in any minor release; none is scheduled.

## Elevation

Transient surfaces cast a shadow; persistent ones do not. `FlyoutShadowEffect` is the single elevation token: a frozen `DropShadowEffect` (blur radius 18, direction 270, shadow depth 4, 22% black) built by `SpecialBrushes`. It is theme independent, so it does not change with theme or accent.

Thirteen templates use it: `FlyoutPresenter`, `ContextMenu` (root menu and submenu), `ComboBox`, `DatePicker`, `TimePicker`, `DropDownButton`, `SplitButton`, `ToggleSplitButton`, `AutoSuggestBox`, `CommandBarFlyout`, `TeachingTip`, `ContentDialog`, and `ToolTip`. `ToolTip` now casts the same effect as the rest instead of relying on the Win32 popup shadow (`HasDropShadow`): that property only triggers the native drop shadow when the popup's own theme template applies `SystemDropShadowChrome`, which the Fluent `ToolTip` template does not, so the earlier claim that `ToolTip` elevated through `HasDropShadow` was never accurate. Persistent surfaces such as `Card` stay flat: background, a 1 px stroke, and a corner radius.

WPF disables ClearType for every text run beneath an `Effect`, so no template puts the effect on the surface that hosts the text. Each one paints an empty sibling `Border` named `ShadowCaster` behind the surface, matched in size and corner radius, and carries the effect there:

```xml
<Grid>
    <Border x:Name="ShadowCaster"
            Background="{TemplateBinding Background}"
            CornerRadius="{DynamicResource OverlayCornerRadius}"
            Effect="{DynamicResource FlyoutShadowEffect}" />
    <Border x:Name="PresenterSurface"
            CornerRadius="{DynamicResource OverlayCornerRadius}"
            RenderOptions.ClearTypeHint="Enabled">
        <!--  content  -->
    </Border>
</Grid>
```

Use the same pattern in a custom template. A `Popup` needs no layout gutter and no placement compensation: `PopupRoot` already sizes the popup window roughly 15 px larger per side than its child, and the effect bleeds at most 13 px.

## WinUI markup parity: ThemeResource and ThemeDictionary

The `Fluence.Wpf.Markup` namespace (same `http://schemas.fluencewpf.com` xmlns) ships WPF equivalents of the WinUI 3 theme XAML extensions:

- **`{fluence:ThemeResource Key}`** - a theme-reactive resource reference. It derives from `DynamicResourceExtension` and behaves identically (Fluence republishes its computed dictionary on every theme or accent change, so dynamic references re-resolve); use it to keep the theme-versus-static intent readable in markup ported from WinUI. Unlike WinUI, the xmlns prefix is mandatory - WPF has no prefix-free markup extensions. Works everywhere `DynamicResource` works, including `Setter.Value`.
- **`ThemeDictionary`** - the `ResourceDictionary.ThemeDictionaries` equivalent: app-author per-theme value tables (brushes, strings, image sources) that swap automatically on theme changes, with `DynamicResource` / `ThemeResource` consumers re-resolving.

```xaml
<Grid.Resources>
    <fluence:ThemeDictionary>
        <fluence:ThemeDictionary.ThemeDictionaries>
            <fluence:ThemeResourceDictionary ThemeKey="Light">
                <SolidColorBrush x:Key="HeroBrush" Color="#EEEEEE" />
            </fluence:ThemeResourceDictionary>
            <fluence:ThemeResourceDictionary ThemeKey="Dark">
                <SolidColorBrush x:Key="HeroBrush" Color="#333333" />
            </fluence:ThemeResourceDictionary>
        </fluence:ThemeDictionary.ThemeDictionaries>
    </fluence:ThemeDictionary>
</Grid.Resources>
```

Selection matches `ThemeKey` (`Light`, `Dark`, `HighContrast`) against `ApplicationThemeManager.ResolvedTheme` and falls back to a `Default` table when the exact key is absent. Under high contrast the WinUI polarity keys are tried first: `HighContrastBlack` for dark schemes (Aquatic-style white-on-black) and `HighContrastWhite` for light schemes (Desert-style black-on-white), judged from the live system window luminance so custom schemes classify by how their background reads; the generic `HighContrast` key is the next candidate, then `Default`. The gallery home page hero uses exactly this to pick the lockup variant per scheme with no code-behind. Matching is ordinal and case-sensitive; a table whose key matches nothing silently loses to the `Default` fallback. Tables carry their key on `ThemeKey` rather than `x:Key` because the WPF markup compiler cannot compile keyed children inside a dictionary-typed property of a `ResourceDictionary` subclass; the shipped collection shape compiles and loads on every TFM.

The type is equally usable from code, which suits values only known at runtime:

```csharp
ThemeDictionary icons = new()
{
    ThemeDictionaries =
    {
        new ThemeResourceDictionary { ThemeKey = "Light", ["AppIconImageSource"] = lightIcon },
        new ThemeResourceDictionary { ThemeKey = "Dark", ["AppIconImageSource"] = darkIcon },
    },
};
window.Resources.MergedDictionaries.Add(icons);
```

Caveats:

- Use `ThemeDictionary` in element, window, or `App.Resources` scope. It is not a replacement for the three application-level merged slots owned by `ApplicationThemeManager`.
- The `ThemeDictionary`'s own `MergedDictionaries` collection belongs to the selection mechanism: it is cleared and repopulated on every swap, so never add entries to it directly - put shared values in the `ThemeDictionary` itself or in a sibling dictionary.
- In a scope WPF seals read-only (`Style.Resources`, template resources) the dictionary keeps the selection made before sealing instead of swapping - prefer element or window scope for values that must track the live theme.
- As in WinUI, `StaticResource` references into a theme dictionary stay stale after a theme change; always use `DynamicResource` or `{fluence:ThemeResource}`.
- Mirror of the WinUI guideline: do not use `{fluence:ThemeResource}` or `DynamicResource` for values defined *inside* a theme table - a `Freezable` there has no inheritance context, so dynamic references resolve unreliably. Use literals or `StaticResource` inside tables; the per-theme swap itself provides the theme reactivity.
- Instances are tracked weakly; discarded dictionaries are garbage-collectable despite the static theme-change subscription.

## Accent

- `ApplicationAccentColorManager.ApplySystemAccent()` - sets the accent intent to System and re-runs the full pipeline, resolving the OS registry palette.
- `ApplicationAccentColorManager.ApplyCustomAccent(Color)` - sets the accent intent to a fixed color; the ramp is generated to WinUI-style keys (`SystemAccentColorPrimary` / `Secondary` / `Tertiary` plus the `AccentFillColor*` role tokens).
- `ApplicationAccentColorManager.ApplyCustomAccent(Color light, Color dark)` - per-theme accent seeds: the light seed drives the ramp on the light theme, the dark seed on dark and high-contrast themes. The intent is sticky - every later theme change regenerates the ramp from the matching seed with no re-apply call and no `Changed` subscription needed.
- `ApplicationThemeManager.Apply(theme)` alone uses the OS palette by default - no separate `ApplySystemAccent()` call is needed on startup.
- Accent changes re-run the full pipeline and replace slot [0]; `DynamicResource` consumers refresh automatically.

## Backdrop (`FluenceWindow`)

`WindowBackdropType`: `None`, `Auto`, `Mica`, `Acrylic`, `Tabbed`.

Which backdrops work depends on OS support, and unsupported combinations fall back silently per the `WindowPolicy` resolution rules.

| Requested | Windows 11 22H2+ (build 22621) | Windows 11 21H2 (22000 to 22620) | Windows 10 17063+ | Windows 10 below 17063 |
| --------- | ------------------------------ | -------------------------------- | ----------------- | ---------------------- |
| `Auto`, `Mica` | Mica (`DWMSBT_MAINWINDOW`) | Mica (legacy `DWMWA_MICA_EFFECT`) | None | None |
| `Acrylic` | Acrylic (`DWMSBT_TRANSIENTWINDOW`) | Mica | Legacy acrylic | None |
| `Tabbed` | Tabbed (`DWMSBT_TABBEDWINDOW`) | Mica | None | None |
| `None` | None | None | None | None |

The table above assumes a Light or Dark resolved theme; under `ApplicationTheme.HighContrast`, every request resolves to `None` on every Windows version, because `WindowPolicy.ResolveEffectiveBackdrop` suppresses all materials before any OS capability check runs (Microsoft Learn: "High contrast mode: all materials are suppressed").

With a backdrop active the window's own `Background` is transparent and every translucent layer above it (the `NavigationView` content canvas, `Card` fills) is blended by DWM, not by WPF. On a desktop whose display path runs at 10 bits per colour channel with advanced color off that blend carries only two bits of alpha: the layer's premultiplied colour keeps its 50% weight while its alpha rounds up to 67%, so less backdrop is admitted underneath and the result lands short (209 instead of 249 for `#80FFFFFF` over Light Mica), reading darker than the token specifies. See `KNOWN_ISSUES.md` for the measurement. Under `None` the window paints `ApplicationBackgroundBrush` (`SolidBackgroundFillColorBase`, the WinUI `ApplicationPageBackgroundThemeBrush` value) and WPF blends the layers itself at full precision. `FluenceWindow` detects a display reporting more than 8 bits per colour channel with advanced color off and, while the effective backdrop is Mica or Tabbed, sets the window scoped `NavigationViewContentBackgroundBrush` resource to WinUI's own pre-blended `LayerOnMicaBaseAltFillColorTertiary` token (falling back to a computed opaque pre-blend only when that token is undefined). A consumer who sets that same key on their own window before it is shown is never overridden: `FluenceWindow` only ever changes a value it set itself.

On Windows 10 build 17063 and later, `Acrylic` is applied through the undocumented `SetWindowCompositionAttribute` accent policy (`ACCENT_ENABLE_ACRYLICBLURBEHIND`) rather than through DWM, tinted with the `AcrylicBackgroundFillColorDefault` theme token including its alpha. That path is disabled, and the window falls back to an opaque `None`, when the Windows "Transparency effects" setting is off (high contrast already resolves to `None` before this path is ever reached, per the general rule above). While the window is being moved or resized it drops to the cheaper Aero blur (`ACCENT_ENABLE_BLURBEHIND`) and restores the acrylic on release, because per-frame acrylic recomposition makes a Windows 10 drag visibly lag the cursor.

## System theme watcher

`SystemThemeWatcher.Watch(window)` registers debounced Win32 settings hooks and notifies `ApplicationThemeManager` when the OS theme changes. One watched window per process is the normal setup; `ApplicationThemeManager.Changed` is the event to subscribe to.

## Design-time

`FluenceThemeEngine` computes the full Fluence color and brush set in C# at runtime and publishes it at `MergedDictionaries[0]`. None of those brushes exist as authored XAML, so the XAML designer and Blend cannot resolve `*Brush` keys on their own. To fix the preview, Fluence ships two generated, design-time-only dictionaries that hold the computed palette for the default `#0078D4` accent:

- `Properties/DesignTime.Light.xaml`
- `Properties/DesignTime.Dark.xaml`

The project-wide preview file `Properties/DesignTimeResources.xaml` merges the Light one (plus Typography and Generic), mirroring the runtime 3-slot model so the whole library previews correctly in Light. These files are compiled into the assembly (`Page` build action) and are referenceable at design time by pack URI, for example `pack://application:,,,/Fluence.Wpf;component/Properties/DesignTime.Dark.xaml`. Nothing merges them at runtime.

To preview **Dark**, add a design-time-only merge of `DesignTime.Dark.xaml` (under `mc:Ignorable="d"` / the `d:` namespace) to the specific window or page you are previewing.

These files are a serialized snapshot of the engine output, kept honest by a unit test: `DesignTimeResources_AreCurrent` regenerates each file in memory and fails CI if the committed file drifts. After an intentional engine change that affects colors or brushes, run the (normally `[Ignore]`d) `RegenerateDesignTimeResources` test to rewrite both files, then re-commit. The snapshot is deterministic and machine-independent: it forces the default accent through the HSV ramp generator (no registry / DWM read), uses the default theme title-bar colors for the window-chrome tokens, and omits the live-`SystemColors` `SystemColor*` aliases, the runtime-only `AcrylicNoiseBrush`, the flyout shadow effect, and the focus-visual styles. High contrast is out of scope for design-time previews. The XAML designer and runtime merge stacks are not identical - always check the result in the demo app.

## Testing

The test suite runs a full theme cycle, Light then Dark then High Contrast then Light, and asserts that critical brushes resolve at each step. See `ThemeTestHelpers.ApplyStandardThemeCycle` and `AssertKeyThemeBrushesResolve` in `Fluence.Wpf.Tests`. The `ControlStrongStrokeColor*` contract is covered by `Fluence.Wpf.Tests/Control/Rules/FluentStrokeTests.cs`.
