# ApplicationThemeManager

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public static class ApplicationThemeManager
```

Manages Fluence.Wpf theme resource dictionaries, accent coordination, and runtime theme changes.

**Remarks:** [Apply](ApplicationThemeManager.md#api-317c377bb209) delegates to the internal theme engine, which publishes one computed colors-plus-brushes dictionary at slot [0] and replaces it on every change.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/ApplicationThemeManager.cs)

## Properties

<a id="api-f023c934ee13"></a>

### CurrentBackdrop

```csharp
public static WindowBackdropType CurrentBackdrop { get; }
```

Gets the currently requested backdrop type.

<a id="api-43cf64c7b1e1"></a>

### CurrentTheme

```csharp
public static ApplicationTheme CurrentTheme { get; }
```

Gets the currently requested theme (may be [Auto](ApplicationTheme.md#api-192b7cd432f0)).

<a id="api-14d14121fa3a"></a>

### IsAppInDarkMode

```csharp
public static bool IsAppInDarkMode { get; }
```

Gets a value indicating whether the Windows app color mode is currently Dark. Reflects the live registry value; independent of [CurrentTheme](ApplicationThemeManager.md#api-43cf64c7b1e1).

<a id="api-050a0e4f611f"></a>

### IsSystemInDarkMode

```csharp
public static bool IsSystemInDarkMode { get; }
```

Gets a value indicating whether the Windows system (window-chrome) color mode is currently Dark. Reflects the live registry value; independent of [CurrentTheme](ApplicationThemeManager.md#api-43cf64c7b1e1).

<a id="api-5008bbd6b9b3"></a>

### ResolvedTheme

```csharp
public static ApplicationTheme ResolvedTheme { get; }
```

Gets the concrete theme (Light, Dark, or HighContrast) that was resolved and applied during the most recent theme pipeline run (that is, the last call to [Apply](ApplicationThemeManager.md#api-317c377bb209) or to any [ApplicationAccentColorManager](ApplicationAccentColorManager.md) apply method ([ApplySystemAccent](ApplicationAccentColorManager.md#api-194f672c8db4) or [ApplyCustomAccent](ApplicationAccentColorManager.md#api-e3ee20c8a77b)), whichever ran last. When [CurrentTheme](ApplicationThemeManager.md#api-43cf64c7b1e1) is [Auto](ApplicationTheme.md#api-192b7cd432f0), this reflects the OS theme at the time of that last pipeline run; it does not update automatically when the OS theme changes without a subsequent pipeline run. Before the first pipeline run, this property returns [Light](ApplicationTheme.md#api-5e33d24b2851) as the pre-initialization default.

## Methods

<a id="api-317c377bb209"></a>

### Apply

```csharp
public static void Apply(ApplicationTheme theme, WindowBackdropType backdrop = WindowBackdropType.Auto)
```

Initializes the theme resource stack or applies a later theme change.

**Remarks:** The first call seeds the three resource slots ([0] computed colors and brushes, [1] Typography, [2] Generic). Later calls replace the computed slot so `DynamicResource` consumers re-resolve. Visible [FluenceWindow](../Fluence.Wpf.Controls/FluenceWindow.md) instances fade their previous WPF appearance over the newly published resources when client-area motion is enabled; the resources themselves still change immediately.



A call whose computed output is identical to the last published one does not rebuild or replace the computed slot; the theme engine gates that on a fingerprint of every input to the pipeline. [CurrentTheme](ApplicationThemeManager.md#api-43cf64c7b1e1) and [CurrentBackdrop](ApplicationThemeManager.md#api-f023c934ee13) are still assigned, because they record the caller's request and remain observable even when the resolved colors do not move (for example [Auto](ApplicationTheme.md#api-192b7cd432f0) resolving to the same concrete theme as an explicit request, or a backdrop change that no computed brush depends on). [Changed](ApplicationThemeManager.md#api-8da32bf87c06) therefore fires when either the computed dictionary was republished or the requested theme or backdrop actually changed, and is suppressed only when the call was a true no-op in both respects. A publish that fails because `Current` is null counts as no publish, so an early-startup call that also repeats the current request raises nothing; the state assignments still happen and the next call retries the publish.

**Parameter `theme`:** The requested application theme. Use [Auto](ApplicationTheme.md#api-192b7cd432f0) to follow Windows app theme settings.

**Parameter `backdrop`:** The requested window backdrop policy retained for [CurrentBackdrop](ApplicationThemeManager.md#api-f023c934ee13) consumers.

<a id="api-c357b33c1146"></a>

### ApplySystemTheme

```csharp
public static void ApplySystemTheme()
```

Re-applies with [Auto](ApplicationTheme.md#api-192b7cd432f0) to pick up system changes.

## Events

<a id="api-8da32bf87c06"></a>

### Changed

```csharp
public static event EventHandler<ThemeChangedEventArgs>? Changed
```

Raised when [Apply](ApplicationThemeManager.md#api-317c377bb209) publishes resources or changes the requested theme or backdrop. Accent-only apply methods do not raise this event.

## Related types

- [Fluence.Wpf.ApplicationTheme](ApplicationTheme.md)
- [Fluence.Wpf.ThemeChangedEventArgs](ThemeChangedEventArgs.md)
- [Fluence.Wpf.WindowBackdropType](WindowBackdropType.md)
