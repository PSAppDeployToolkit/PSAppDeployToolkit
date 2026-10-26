# ApplicationAccentColorManager

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public static class ApplicationAccentColorManager
```

Manages system and custom accent colors and publishes them as `DynamicResource` brush keys aligned with Windows 11.

**Remarks:** [Apply](ApplicationThemeManager.md#api-317c377bb209) uses the Windows accent palette by default. Call [ApplyCustomAccent](ApplicationAccentColorManager.md#api-e3ee20c8a77b) to pin a custom ramp seed, [ApplyCustomAccent](ApplicationAccentColorManager.md#api-7846e9ba41b8) with `exact: true` for an exact visible light-theme primary, or [ApplySystemAccent](ApplicationAccentColorManager.md#api-194f672c8db4) to return to the Windows palette.

**Example**

~~~csharp
ApplicationThemeManager.Apply(ApplicationTheme.Auto, WindowBackdropType.Mica);
ApplicationAccentColorManager.ApplyCustomAccent(System.Windows.Media.Colors.CornflowerBlue);
~~~

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/ApplicationAccentColorManager.cs)

## Properties

<a id="api-b0642aa92deb"></a>

### IsAccentColorOnTitleBarsEnabled

```csharp
public static bool IsAccentColorOnTitleBarsEnabled { get; }
```

Gets a value indicating whether Windows is configured to show accent color on title bars and window borders.

<a id="api-a0be6a791d95"></a>

### SystemAccentColor

```csharp
public static Color SystemAccentColor { get; }
```

Gets the current base accent color (ARGB). A theme apply loads the Windows accent palette by default; before the first apply, this returns the fallback blue.

<a id="api-3c3c6709e654"></a>

### SystemAccentColorDark1

```csharp
public static Color SystemAccentColorDark1 { get; }
```

Gets the first dark shade on the resolved accent ramp.

<a id="api-33761434f7ff"></a>

### SystemAccentColorDark2

```csharp
public static Color SystemAccentColorDark2 { get; }
```

Gets the second dark shade on the resolved accent ramp.

<a id="api-65ccdb75272a"></a>

### SystemAccentColorDark3

```csharp
public static Color SystemAccentColorDark3 { get; }
```

Gets the darkest shade on the resolved accent ramp.

<a id="api-8132c3dcc448"></a>

### SystemAccentColorLight1

```csharp
public static Color SystemAccentColorLight1 { get; }
```

Gets the first light tint on the resolved accent ramp. Default matches [SystemAccentColor](ApplicationAccentColorManager.md#api-a0be6a791d95) until the ramp is loaded.

<a id="api-80394f6b3e4b"></a>

### SystemAccentColorLight2

```csharp
public static Color SystemAccentColorLight2 { get; }
```

Gets the second light tint on the resolved accent ramp.

<a id="api-b9568a468733"></a>

### SystemAccentColorLight3

```csharp
public static Color SystemAccentColorLight3 { get; }
```

Gets the lightest tint on the resolved accent ramp.

<a id="api-0770d0d64b6f"></a>

### SystemAccentColorPrimary

```csharp
public static Color SystemAccentColorPrimary { get; }
```

Gets the primary accent color used for emphasis surfaces.

<a id="api-bc0f46f48d5c"></a>

### SystemAccentColorSecondary

```csharp
public static Color SystemAccentColorSecondary { get; }
```

Gets the secondary accent color used for layered emphasis.

<a id="api-d49740177230"></a>

### SystemAccentColorTertiary

```csharp
public static Color SystemAccentColorTertiary { get; }
```

Gets the tertiary accent color used for subtle accent fills.

<a id="api-7f8b4589007d"></a>

### TitleBarActiveColor

```csharp
public static Color TitleBarActiveColor { get; }
```

Gets the active titlebar color (from DWM AccentColor or default gray).

<a id="api-3744dd7f3b71"></a>

### TitleBarInactiveColor

```csharp
public static Color TitleBarInactiveColor { get; }
```

Gets the inactive titlebar color (from DWM AccentColorInactive or default gray).

<a id="api-0b227219be49"></a>

### WindowBorderColor

```csharp
public static Color WindowBorderColor { get; }
```

Gets the window border color (titlebar active on Win11, blended on Win10).

## Methods

<a id="api-7846e9ba41b8"></a>

### ApplyCustomAccent

```csharp
public static void ApplyCustomAccent(Color color, bool exact = false)
```

Applies a custom accent and republishes theme resources. By default the color is a ramp seed. With `exact` enabled, it is the visible light-theme primary; the dark-theme primary still comes from the resolved ramp's Light2 shade.

**Parameter `color`:** The accent seed, or the exact light-theme primary.

**Parameter `exact`:** Whether to use `color` as the visible light-theme primary.

<a id="api-a3c097e5f5c0"></a>

### ApplyCustomAccent

```csharp
public static void ApplyCustomAccent(Color lightThemeAccent, Color darkThemeAccent, bool exact = false)
```

Applies per-theme custom accents and republishes theme resources. By default both colors are ramp seeds. With `exact` enabled, they are the visible primary colors in light and dark themes respectively.

**Parameter `lightThemeAccent`:** The light-theme seed or exact primary.

**Parameter `darkThemeAccent`:** The dark-theme seed or exact primary, and the high-contrast ramp seed.

**Parameter `exact`:** Whether to use the supplied colors as visible light- and dark-theme primaries.

<a id="api-ca1111232abe"></a>

### ApplyCustomAccent

```csharp
public static void ApplyCustomAccent(Color lightThemeAccent, Color darkThemeAccent)
```

Applies per-theme custom accent seeds and republishes theme resources. A seed matching the active Windows accent captures its seven-rung palette; other seeds use generated ramps. The selected palette stays pinned across later theme changes.

**Parameter `lightThemeAccent`:** The ramp seed used on the light theme.

**Parameter `darkThemeAccent`:** The ramp seed used on dark and high-contrast themes.

<a id="api-e3ee20c8a77b"></a>

### ApplyCustomAccent

```csharp
public static void ApplyCustomAccent(Color color)
```

Applies a custom accent seed and republishes theme resources. When the seed matches the active Windows accent, its seven-rung palette is captured; otherwise a ramp is generated.

**Parameter `color`:** The accent color to use as the ramp base.

<a id="api-3006181574b3"></a>

### ApplyCustomAccentExact

```csharp
public static void ApplyCustomAccentExact(Color lightThemeAccent, Color darkThemeAccent)
```

Applies exact visible primary accents on light and dark themes. The intent follows subsequent theme changes. High contrast retains its system state colors and uses the dark-theme color as its raw ramp seed.

**Parameter `lightThemeAccent`:** The exact primary accent color on the light theme.

**Parameter `darkThemeAccent`:** The exact primary accent color on the dark theme.

<a id="api-ca99c2ce264f"></a>

### ApplyCustomAccentExact

```csharp
public static void ApplyCustomAccentExact(Color lightThemeAccent)
```

Applies an exact visible primary accent on the light theme. The dark-theme primary is derived from the resolved ramp, and the intent follows subsequent theme changes. High contrast retains its system state colors and uses this color as its raw ramp seed.

**Parameter `lightThemeAccent`:** The exact primary accent color on the light theme.

<a id="api-194f672c8db4"></a>

### ApplySystemAccent

```csharp
public static void ApplySystemAccent()
```

Sets the accent intent to the live Windows accent palette and re-applies the current theme.

## Events

<a id="api-c6f8c5e304bd"></a>

### AccentColorChanged

```csharp
public static event EventHandler<EventArgs>? AccentColorChanged
```

Occurs after the theme engine publishes application resources, whether the publish follows a theme apply or an accent apply. A redundant apply raises no event.
