# InfoBadge

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class InfoBadge : ContentControl
```

A small badge overlay that displays a numeric value, icon, or dot indicator. Typically attached to a [NavigationViewItem](NavigationViewItem.md) or other control.

**Remarks:** `Content` is driven by the badge itself: it carries the value's text, the icon element, or nothing at all, depending on which display kind [Value](InfoBadge.md#api-e4cf98f0c93e) and [IconSource](InfoBadge.md#api-98fedc97dab7) resolve to. WinUI's InfoBadge has no content surface at all (`InfoBadge.idl` declares only `Value`, `IconSource` and `TemplateSettings`); here it is an artifact of the WPF base class, so a value assigned to it directly is overwritten the next time either property changes. Set [Value](InfoBadge.md#api-e4cf98f0c93e) or [IconSource](InfoBadge.md#api-98fedc97dab7) instead.

**Base type:** [`ContentControl`](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/InfoBadge.cs)

## Constructors

<a id="api-25fe0a509e31"></a>

### InfoBadge

```csharp
public InfoBadge()
```

Creates a new `InfoBadge` instance.

## Properties

<a id="api-a595d8474f3e"></a>

### BadgeStyle

```csharp
public InfoBadgeStyle BadgeStyle { get; set; }
```

Gets or sets the severity style of the badge.

<a id="api-7924d6de849f"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius of the badge. Left alone it follows the badge's own height, staying a capsule at any size; set locally it is honoured as given, which is what WinUI's own `InfoBadge.cpp` does with a local value.

**Remarks:** This preserves parity rather than adding to it. WinUI's InfoBadge inherits `CornerRadius` from its own `Control` base and reads it to honour a local value; WPF's `Control` has no such property, so the badge declares it.

<a id="api-98fedc97dab7"></a>

### IconSource

```csharp
public object IconSource { get; set; }
```

Gets or sets an icon element to display inside the badge (overrides Value text).

<a id="api-e4cf98f0c93e"></a>

### Value

```csharp
public int Value { get; set; }
```

Gets or sets the numeric value displayed. Set to -1, the default, to show a dot instead. Anything below -1 is rejected: WinUI throws for it (`InfoBadge.cpp`), because a negative count has no badge to render and folding it into the dot would hide the bug instead of reporting it.

## Methods

<a id="api-e90c062809d3"></a>

### GetStyleGlyph

```csharp
public static string GetStyleGlyph(InfoBadgeStyle badgeStyle)
```

Returns the canonical Segoe Fluent glyph for a badge severity, so a consumer can build the icon form of a badge without hardcoding codepoints, in the same shape as [GetSeverityGlyph](InfoBar.md#api-e33a7c6c2c79).

**Remarks:** WinUI ships a dot, a value and an icon style per severity, and only the icon styles carry a glyph (`InfoBadge_themeresources.xaml`). Fluence expresses severity as one [BadgeStyle](InfoBadge.md#api-a595d8474f3e) property, so the badge cannot pick the icon form for you without taking the dot form away: assign the glyph to [IconSource](InfoBadge.md#api-98fedc97dab7) when you want it. The values are WinUI's own, and the icon styles for Attention and Informational also inset the glyph by 0,4,0,2.

**Parameter `badgeStyle`:** The severity to return the glyph for.

**Returns:** The glyph character for `badgeStyle`.

**Exception `System.ArgumentOutOfRangeException`:** `badgeStyle` is not a defined [InfoBadgeStyle](../Fluence.Wpf/InfoBadgeStyle.md) value.

<a id="api-1434ee0b8633"></a>

### MeasureOverride

```csharp
protected override Size MeasureOverride(Size constraint)
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

**Remarks:** A badge is never narrower than it is tall: WinUI squares it up when the natural width comes out under the height (`InfoBadge.cpp``MeasureOverride`), which is what turns a single digit value into a circle rather than a squashed oval. With the 4 dip minimum width WinUI's own metrics carry, nothing else would hold that shape.

<a id="api-38fc7af2cfba"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-208e0b580d16"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-69b92a9f9944"></a>

### OnRenderSizeChanged

```csharp
protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-5ac184dc921d"></a>

### BadgeStyleProperty

```csharp
public static readonly DependencyProperty BadgeStyleProperty
```

Identifies the [BadgeStyle](InfoBadge.md#api-a595d8474f3e) dependency property.

<a id="api-da7a994e3dc6"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](InfoBadge.md#api-7924d6de849f) dependency property.

<a id="api-891a91b7dc6f"></a>

### IconSourceProperty

```csharp
public static readonly DependencyProperty IconSourceProperty
```

Identifies the [IconSource](InfoBadge.md#api-98fedc97dab7) dependency property.

<a id="api-4de79fc86057"></a>

### ValueProperty

```csharp
public static readonly DependencyProperty ValueProperty
```

Identifies the [Value](InfoBadge.md#api-e4cf98f0c93e) dependency property.

## Related types

- [Fluence.Wpf.InfoBadgeStyle](../Fluence.Wpf/InfoBadgeStyle.md)
