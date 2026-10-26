# ThemeResourceExtension

[C# API](../index.md) / [Fluence.Wpf.Markup](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Markup`

```csharp
public sealed class ThemeResourceExtension : DynamicResourceExtension
```

A theme-reactive resource reference equivalent to the WinUI 3 `{ThemeResource}` markup extension. Use it to reference a resource whose value must follow the active theme, such as any canonical Fluence color or brush token: `{fluence:ThemeResource TextFillColorPrimaryBrush}`.

**Remarks:** This extension derives from `DynamicResourceExtension` and shares its exact runtime behavior. Fluence republishes its computed color and brush dictionary on every theme or accent change, so any dynamic reference to a canonical token re-resolves automatically; this type exists so markup ported from WinUI 3 keeps its theme-versus-static intent readable. Unlike WinUI, WPF markup extensions always require an XML namespace prefix.



As with WinUI, a `StaticResource` reference to a theme-dependent value does not update when the theme changes; use this extension (or `DynamicResource`) for any value that must react to theme, accent, or high contrast at runtime.

**Base type:** [`DynamicResourceExtension`](https://learn.microsoft.com/dotnet/api/system.windows.dynamicresourceextension) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Markup/ThemeResourceExtension.cs)

## Constructors

<a id="api-87d33a9847a7"></a>

### ThemeResourceExtension

```csharp
public ThemeResourceExtension()
```

Initializes a new instance of the [ThemeResourceExtension](ThemeResourceExtension.md) class.

<a id="api-71ac5fabdf5e"></a>

### ThemeResourceExtension

```csharp
public ThemeResourceExtension(object resourceKey)
```

Initializes a new instance of the [ThemeResourceExtension](ThemeResourceExtension.md) class with the given resource key.

**Parameter `resourceKey`:** The key of the resource to reference.
