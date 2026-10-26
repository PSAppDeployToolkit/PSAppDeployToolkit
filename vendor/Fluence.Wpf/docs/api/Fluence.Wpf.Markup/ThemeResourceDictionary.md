# ThemeResourceDictionary

[C# API](../index.md) / [Fluence.Wpf.Markup](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Markup`

```csharp
public sealed class ThemeResourceDictionary : ResourceDictionary
```

A per-theme value table inside [ThemeDictionaries](ThemeDictionary.md#api-a183efe2ca7b). The [ThemeKey](ThemeResourceDictionary.md#api-375620cbd126) names the theme the table serves: `Light`, `Dark`, `HighContrast`, the high-contrast polarity keys `HighContrastBlack` and `HighContrastWhite`, or `Default` (the fallback for themes without an exact table).

**Remarks:** The key lives on this property rather than `x:Key` because the WPF markup compiler cannot compile keyed children inside a dictionary-typed property of a `ResourceDictionary` subclass. Set [ThemeKey](ThemeResourceDictionary.md#api-375620cbd126) before adding the table to a [ThemeDictionary](ThemeDictionary.md); changing it afterwards takes effect on the next theme change or collection mutation.

**Base type:** [`ResourceDictionary`](https://learn.microsoft.com/dotnet/api/system.windows.resourcedictionary) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Markup/ThemeResourceDictionary.cs)

## Constructors

<a id="api-7ef6227e1c42"></a>

### ThemeResourceDictionary

```csharp
public ThemeResourceDictionary()
```

Creates a new `ThemeResourceDictionary` instance.

## Properties

<a id="api-375620cbd126"></a>

### ThemeKey

```csharp
public string ThemeKey { get; set; }
```

Gets or sets the theme this table serves: `Light`, `Dark`, `HighContrast`, `HighContrastBlack`, `HighContrastWhite`, or `Default`.
