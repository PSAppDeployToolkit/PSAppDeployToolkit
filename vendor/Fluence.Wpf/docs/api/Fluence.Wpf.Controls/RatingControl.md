# RatingControl

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class RatingControl : Control
```

A Fluent Design star-based rating control. Renders up to [MaxRating](RatingControl.md#api-4d1b9e3d0bdb) star glyphs using Segoe Fluent Icons (U+E734 StarEmpty / U+E735 StarFilled). Authority: WinUI 3 RatingControl_themeresources.xaml + RatingControl.xaml. Brush states: `AccentFillColorDefaultBrush` (set, and while hovering to preview a higher rating, still-set stars at or below the current value), `ControlAltFillColorTertiaryBrush` (the hover-preview stars above the current value), `TextFillColorSecondaryBrush` (unset), `TextFillColorDisabledBrush` (disabled, set stars only - disabled unset stars stay TextFillColorSecondaryBrush).

**Base type:** [`Control`](https://learn.microsoft.com/dotnet/api/system.windows.controls.control) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/RatingControl.cs)

## Constructors

<a id="api-7ea73ce4fec4"></a>

### RatingControl

```csharp
public RatingControl()
```

Creates a new `RatingControl` instance.

## Properties

<a id="api-8b9b085ff8c9"></a>

### Caption

```csharp
public string Caption { get; set; }
```

Gets or sets the optional caption text shown after the stars.

<a id="api-98659596d55b"></a>

### IsReadOnly

```csharp
public bool IsReadOnly { get; set; }
```

Gets or sets whether the user can change the rating. When `true`, the control is display-only.

<a id="api-4d1b9e3d0bdb"></a>

### MaxRating

```csharp
public int MaxRating { get; set; }
```

Gets or sets the maximum number of stars displayed. Default is 5.

<a id="api-07ebe67f5399"></a>

### Value

```csharp
public double Value { get; set; }
```

Gets or sets the current rating value (0 to [MaxRating](RatingControl.md#api-4d1b9e3d0bdb)).

## Methods

<a id="api-6cfe7dc647ad"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-8e6bdfa58e0f"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-857fea2f82c6"></a>

### OnKeyDown

```csharp
protected override void OnKeyDown(KeyEventArgs e)
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-780d4a3e5cbb"></a>

### CaptionProperty

```csharp
public static readonly DependencyProperty CaptionProperty
```

Identifies the [Caption](RatingControl.md#api-8b9b085ff8c9) dependency property.

<a id="api-e35480c795d8"></a>

### IsReadOnlyProperty

```csharp
public static readonly DependencyProperty IsReadOnlyProperty
```

Identifies the [IsReadOnly](RatingControl.md#api-98659596d55b) dependency property.

<a id="api-22158d459905"></a>

### MaxRatingProperty

```csharp
public static readonly DependencyProperty MaxRatingProperty
```

Identifies the [MaxRating](RatingControl.md#api-4d1b9e3d0bdb) dependency property.

<a id="api-f2d91f7f4175"></a>

### ValueProperty

```csharp
public static readonly DependencyProperty ValueProperty
```

Identifies the [Value](RatingControl.md#api-07ebe67f5399) dependency property.
