# ThemeDictionary

[C# API](../index.md) / [Fluence.Wpf.Markup](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Markup`

```csharp
public sealed class ThemeDictionary : ResourceDictionary
```

A resource dictionary with per-theme value tables, equivalent to WinUI 3 `ResourceDictionary.ThemeDictionaries`. Populate [ThemeDictionaries](ThemeDictionary.md#api-a183efe2ca7b) with [ThemeResourceDictionary](ThemeResourceDictionary.md) tables keyed `Light`, `Dark`, `HighContrast`, or `Default`; the matching table is selected automatically on every theme change and any `DynamicResource` (or `ThemeResource`) reference into it re-resolves.

**Remarks:** Selection picks the table whose [ThemeKey](ThemeResourceDictionary.md#api-375620cbd126) matches the resolved theme reported by [ResolvedTheme](../Fluence.Wpf/ApplicationThemeManager.md#api-5008bbd6b9b3) and falls back to the `Default` table when the exact theme key is absent. Under high contrast, the WinUI polarity keys `HighContrastBlack` (dark schemes) and `HighContrastWhite` (light schemes) are tried before the generic `HighContrast` key; polarity is judged from the live system window luminance, so custom schemes classify by how their background reads. Unlike WinUI, the tables carry their key on the `ThemeKey` property rather than `x:Key`: the WPF markup compiler cannot compile keyed children inside a dictionary-typed property of a `ResourceDictionary` subclass. In XAML:



~~~xaml
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
~~~



The type is equally usable from code, which suits values only known at runtime:



~~~csharp
ThemeDictionary icons = new()
{
    ThemeDictionaries =
    {
        new ThemeResourceDictionary { ThemeKey = "Light", ["AppIconImageSource"] = lightIcon },
        new ThemeResourceDictionary { ThemeKey = "Dark", ["AppIconImageSource"] = darkIcon },
    },
};
window.Resources.MergedDictionaries.Add(icons);
~~~



Use it in element, window, or application resources. It is not a replacement for the three application-level merged dictionaries owned by [ApplicationThemeManager](../Fluence.Wpf/ApplicationThemeManager.md), and its own `MergedDictionaries` collection is owned by the selection mechanism: entries added there by callers are discarded on the next swap. In a scope WPF seals read-only (`Style.Resources`, template resources) the dictionary keeps the selection made before sealing instead of swapping. As in WinUI, a `StaticResource` reference into a theme dictionary does not update after a theme change; reference its keys with `DynamicResource` or [ThemeResourceExtension](ThemeResourceExtension.md).



Instances are tracked with weak references, so a discarded [ThemeDictionary](ThemeDictionary.md) never leaks through the static theme-change subscription.

**Base type:** [`ResourceDictionary`](https://learn.microsoft.com/dotnet/api/system.windows.resourcedictionary) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Markup/ThemeDictionary.cs)

## Constructors

<a id="api-1553b9428d50"></a>

### ThemeDictionary

```csharp
public ThemeDictionary()
```

Initializes a new instance of the [ThemeDictionary](ThemeDictionary.md) class.

## Properties

<a id="api-a183efe2ca7b"></a>

### ThemeDictionaries

```csharp
public ThemeResourceDictionaryCollection ThemeDictionaries { get; }
```

Gets the per-theme tables. Each table's [ThemeKey](ThemeResourceDictionary.md#api-375620cbd126) names the theme it serves: `Light`, `Dark`, `HighContrast`, or `Default` (the fallback used when the resolved theme has no exact entry).

## Related types

- [Fluence.Wpf.Markup.ThemeResourceDictionaryCollection](ThemeResourceDictionaryCollection.md)
