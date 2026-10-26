# WinUI 3 Parity and WPF differences

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
| Editable selection | `ComboBox.IsEditable=True` uses the native WPF text editor. Text, text search, selection, read-only mode, and keyboard behavior follow WPF ComboBox. |
| ComboBox pointer press | The closed ComboBox paints a transient pressed fill and border while its nested toggle is pressed. Focused presses keep the focused text and chevron roles, and editable focus keeps its underline. WPF retains click-on-release, typing, text search, and selection behavior. |
| Text input focus | `TextBox`, the styled native `PasswordBox`, `NumberBox`, editable `ComboBox`, the embedded TextBox in `AutoSuggestBox`, and the ColorPicker Hex, RGB, HSV, and alpha editors paint the focus accent inside their rounded border. Light and Dark use a 2-DIP bottom stroke; High Contrast uses a 2-DIP outline on all sides. Changing the painted focus stroke leaves content insets unchanged. The normal TextBox clear-button visibility still follows focus and text. TextBox validation keeps its status color and takes precedence over the accent. |
| Selected text | `TextBox`, `PasswordBox`, and `NumberBox`'s native text editor use WPF's default translucent selection overlay and WPF selected-text foreground behavior. The `net472` baseline cannot use the `SelectionTextBrush` property added in .NET Framework 4.8. Published selection color roles alone do not guarantee identical WinUI selected-text rendering; see [known issues](https://github.com/sintaxasn/Fluence.Wpf/blob/main/KNOWN_ISSUES.md#selected-text-rendering-in-native-wpf-editors). |

## Visual and platform boundaries

The control templates use Fluent colors, geometry, elevation, focus, and animation resources. Window chrome is translated through WPF `WindowChrome` and Windows interop. Windows 11 can supply Mica, Tabbed, Acrylic, and rounded-corner behavior; Windows 10 and high contrast use documented fallbacks. See [compatibility](reference/compatibility.md).

Backdrop materials are window-level in this WPF library; WPF does not provide WinUI's `SystemBackdropElement` for a per-element material. The Colors gallery's Layer on Mica Base Alt example makes its row and sample card transparent so the existing window backdrop shows through the canonical overlay brushes. This is a gallery-only approximation of the layered appearance, not an isolated Mica Alt material; the opaque Tertiary brush remains neutral, and the shared brushes are unchanged.

The Colors gallery also includes a Mica Background group after Solid Background. Its Base and Base Alt examples use the existing solid fallback brushes, `SolidBackgroundFillColorBaseBrush` and `SolidBackgroundFillColorBaseAltBrush`, because WPF applies Mica to the whole window rather than to individual tiles. The copy actions provide the valid window settings `SystemBackdropType="Mica"` and `SystemBackdropType="Tabbed"`; they do not identify brush resources or create a per-element backdrop. The examples link to Microsoft's [SystemBackdrop](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.systembackdrop?view=windows-app-sdk-2.0) and [SystemBackdropElement](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.systembackdropelement?view=windows-app-sdk-2.0) references.

Where WinUI uses a compositor feature that WPF does not expose, the library chooses a WPF behavior and records accepted limitations in [known issues](../KNOWN_ISSUES.md). For a specific control, inspect the [gallery example](../Fluence.Wpf.Demo/Pages/) beside the corresponding [control source](../Fluence.Wpf/Controls/) and [template](../Fluence.Wpf/Themes/Controls/). This is the most reliable way to assess parity for an interaction you plan to use.

## Color, alpha, and elevation fidelity

The theme color tables currently match all 81 Light and Dark color keys they share with WinUI 3 `Common_themeresources_any.xaml`. This includes `LayerFillColorDefault` (`#80FFFFFF` in Light and `#4C3A3A3A` in Dark) and the opaque `LayerOnMicaBaseAltFillColorTertiary` (`#FFF9F9F9` in Light and `#FF2C2C2C` in Dark). Accent hover and pressed fills also retain their WinUI alpha values: secondary is `#E6` and tertiary is `#CC`. Those values are composited over control-specific backing surfaces, so a screenshot pixel can differ from the resource color. The September Mica pre-blend is a separate window-composition correction and does not alter the accent ramp.

The system accent path uses the seven-shade palette supplied by Windows. A custom seed that matches the active Windows base can snapshot those same shades. Other seeds use the library's generated ramp. Windows may correct the requested seed before building its palette, so the fitted `Light*` and `Dark*` shades are not guaranteed to be lighter or darker than the raw `SystemAccentColor`, which remains the requested value. Judge ramp accuracy against raw Windows palette captures, separately from displayed control pixels that may include hover state and alpha compositing. Use `ApplyCustomAccentExact` when an application requires a configured color to be the visible primary fill.

WinUI documents these semantic elevation values, with a 1 px contour for each surface:

| Surface | WinUI elevation |
| --- | ---: |
| Control | 2 |
| Card | 8 |
| Tooltip | 16 |
| Flyout | 32 |
| Dialog or window | 128 |

A button stays at elevation 2 for rest and hover, then drops to 1 when pressed. Shadow size and softness increase with elevation, while the rendered intensity changes by theme. The control elevation border gradients in Fluence follow the WinUI CommonStyles definitions. Popup shadows are a WPF approximation: one fixed `DropShadowEffect` currently serves several transient surface types. WinUI's documentation does not provide a numeric conversion from those semantic elevation values to WPF blur, opacity, or offset. Changing those parameters without controlled Light and Dark reference captures would substitute guesswork for measured parity. See the [WinUI elevation guidance](https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/layering), [WinUI CommonStyles](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/CommonStyles/Common_themeresources_any.xaml), and [ThemeShadow API](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.themeshadow?view=winrt-28000).
