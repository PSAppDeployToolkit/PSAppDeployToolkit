# ThemeChangedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public class ThemeChangedEventArgs : EventArgs
```

Provides data for the [Changed](ApplicationThemeManager.md#api-8da32bf87c06) event.

**Remarks:** Initializes a new instance of the [ThemeChangedEventArgs](ThemeChangedEventArgs.md) class.

**Parameter `theme`:** The resolved application theme after the change.

**Parameter `accentColor`:** The accent color applied with the theme.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/ThemeChangedEventArgs.cs)

## Constructors

<a id="api-6287e3c43d44"></a>

### ThemeChangedEventArgs

```csharp
public ThemeChangedEventArgs(ApplicationTheme theme, Color accentColor)
```

Provides data for the [Changed](ApplicationThemeManager.md#api-8da32bf87c06) event.

**Remarks:** Initializes a new instance of the [ThemeChangedEventArgs](ThemeChangedEventArgs.md) class.

**Parameter `theme`:** The resolved application theme after the change.

**Parameter `accentColor`:** The accent color applied with the theme.

## Properties

<a id="api-425f84f226a8"></a>

### AccentColor

```csharp
public Color AccentColor { get; }
```

Gets the accent color associated with this theme application.

<a id="api-8054dc04fe24"></a>

### Theme

```csharp
public ApplicationTheme Theme { get; }
```

Gets the resolved theme (Light, Dark, HighContrast, or logical Auto resolved to a concrete theme).

## Related types

- [Fluence.Wpf.ApplicationTheme](ApplicationTheme.md)
