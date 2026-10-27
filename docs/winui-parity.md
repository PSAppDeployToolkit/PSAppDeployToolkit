# WinUI 3 parity and WPF differences

Fluence.Wpf follows the Windows 11 Fluent visual language and many WinUI 3 control names. It remains a WPF library. This page helps when moving a WinUI example into a WPF application or deciding whether a familiar WinUI pattern is available.

## What maps directly

- Core control names and roles include `Button`, `NavigationView`, `TabView`, `ContentDialog`, `InfoBar`, `TeachingTip`, `AutoSuggestBox`, `NumberBox`, `DatePicker`, `TimePicker`, and `ColorPicker`.
- Common color and brush names follow WinUI-style families such as `TextFillColor*`, `ControlFillColor*`, `AccentFillColor*`, and `CardBackgroundFillColor*`.
- Light, Dark, and High Contrast are supported, with live theme and accent updates through published WPF resources.
- `ThemeResourceExtension` and `ThemeDictionary` provide WPF equivalents of theme-aware markup and per-theme values.

The [control catalog](controls.md) and [theme reference](theming.md) define the actual shipped surface. The public API and resource-key baselines in the repository are the exact inventories.

## Differences to account for

| Concern | Fluence.Wpf behavior |
| --- | --- |
| Runtime | Controls are WPF types and templates. A consuming app does not need the Windows App SDK. |
| XAML namespace | Use `xmlns:fluence="http://schemas.fluencewpf.com"`; write `{fluence:ThemeResource Key}` with the prefix. |
| App resources | `ApplicationThemeManager.Apply` publishes a WPF merged-dictionary stack. Use `DynamicResource` or `ThemeResource` for live values. |
| Per-control keys | The computed dictionary publishes control state roles such as `AccentButtonBackgroundPressedBrush` and `CheckBoxCheckBackgroundFillCheckedBrush`. They update every consumer of that key; replace a template to restyle one control instance. |
| Acrylic surfaces | WPF has no equivalent per-element backdrop brush, so acrylic resource roles are painted fallback colors. Window backdrop requests use the available Windows DWM or legacy path. |
| Navigation | `NavigationView` derives from WPF `Selector`; its page host belongs in `NavigationView.Content`, and the app owns navigation history. |
| Date picker | The Fluence picker has its own selected-date API and is not a subclass of WPF `DatePicker`. |
| Text and images | Fluence `TextBlock` and `Image` are templated controls rather than subclasses of the native WPF elements. |
| Password entry | Native WPF `PasswordBox` is styled implicitly and extended with attached properties because it is sealed. |

## Visual and platform boundaries

The control templates use Fluent colors, geometry, elevation, focus, and animation resources. Window chrome is translated through WPF `WindowChrome` and Windows interop. Windows 11 can supply Mica, Tabbed, Acrylic, and rounded-corner behavior; Windows 10 and high contrast use documented fallbacks. See [Windows and backdrop support](csharp/requirements.md#windows-and-backdrop-support).

Where WinUI uses a compositor feature that WPF does not expose, the library chooses a WPF behavior and records accepted limitations in [known issues](../KNOWN_ISSUES.md). For a specific control, inspect the [gallery example](../Fluence.Wpf.Demo/Pages/) beside the corresponding [control source](../Fluence.Wpf/Controls/) and [template](../Fluence.Wpf/Themes/Controls/). This is the most reliable way to assess parity for an interaction you plan to use.

## Color, alpha, and elevation fidelity

The theme color tables follow shared Light and Dark color keys from WinUI 3 `Common_themeresources_any.xaml`. This includes `LayerFillColorDefault` (`#80FFFFFF` in Light and `#4C3A3A3A` in Dark) and the opaque `LayerOnMicaBaseAltFillColorTertiary` (`#FFF9F9F9` in Light and `#FF2C2C2C` in Dark). Accent hover and pressed fills also retain their WinUI alpha values: secondary is `#E6` and tertiary is `#CC`. Those values are composited over control-specific backing surfaces, so a screenshot pixel can differ from the resource color. Window backdrop compensation does not alter the accent ramp.

The system accent path uses the seven-shade palette supplied by Windows. A custom seed that matches the active Windows base can snapshot those same shades. Other seeds use the library's generated ramp. Windows may correct the requested seed before building its palette, so the fitted `Light*` and `Dark*` shades are not guaranteed to be lighter or darker than the raw `SystemAccentColor`, which remains the requested value. Judge ramp accuracy against raw Windows palette captures, separately from displayed control pixels that may include hover state and alpha compositing. See the [accent guidance](theming.md#accent-backdrop-and-design-time) to set an exact visible fill.

WinUI documents these semantic elevation values, with a 1 px contour for each surface:

| Surface | WinUI elevation |
| --- | ---: |
| Control | 2 |
| Card | 8 |
| Tooltip | 16 |
| Flyout | 32 |
| Dialog or window | 128 |

A button stays at elevation 2 for rest and hover, then drops to 1 when pressed. Shadow size and softness increase with elevation, while the rendered intensity changes by theme. The control elevation border gradients in Fluence follow the WinUI CommonStyles definitions. Popup shadows are a WPF approximation: one fixed `DropShadowEffect` currently serves several transient surface types. WinUI's documentation does not provide a numeric conversion from those semantic elevation values to WPF blur, opacity, or offset. Changing those parameters without controlled Light and Dark reference captures would substitute guesswork for measured parity. See the [WinUI elevation guidance](https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/layering), [WinUI CommonStyles](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/CommonStyles/Common_themeresources_any.xaml), and [ThemeShadow API](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.themeshadow?view=winrt-28000).
