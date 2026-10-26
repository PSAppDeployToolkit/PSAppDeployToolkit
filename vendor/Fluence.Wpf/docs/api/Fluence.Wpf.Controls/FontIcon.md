# FontIcon

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class FontIcon : Control
```

Represents an icon that uses a glyph from a font.

**Base type:** [`Control`](https://learn.microsoft.com/dotnet/api/system.windows.controls.control) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/FontIcon.cs)

## Constructors

<a id="api-d7cacb6477c1"></a>

### FontIcon

```csharp
public FontIcon()
```

Initializes a new instance of the [FontIcon](FontIcon.md) class.

## Properties

<a id="api-e24048891a56"></a>

### EnableTransitions

```csharp
public bool EnableTransitions { get; set; }
```

Gets or sets whether high-quality bitmap scaling is used during transitions.

<a id="api-d3103c5c7209"></a>

### Glyph

```csharp
public string Glyph { get; set; }
```

Gets or sets the glyph character to display.

<a id="api-1b82b4e5d3f0"></a>

### IconFontFamily

```csharp
public FontFamily IconFontFamily { get; set; }
```

Gets or sets the font family used for the icon glyph.

<a id="api-72b1e3edc6c8"></a>

### IconFontSize

```csharp
public double IconFontSize { get; set; }
```

Gets or sets the font size of the icon glyph.

<a id="api-549ab9361146"></a>

### IsSpinning

```csharp
public bool IsSpinning { get; set; }
```

Gets or sets whether the icon continuously spins.

<a id="api-5d8a80ceb980"></a>

### MirroredWhenRightToLeft

```csharp
public bool MirroredWhenRightToLeft { get; set; }
```

Gets or sets whether the glyph is horizontally flipped when `FlowDirection` is `RightToLeft`.

<a id="api-ebc35a3d9794"></a>

### Rotation

```csharp
public double Rotation { get; set; }
```

Gets or sets the rotation angle in degrees.

## Methods

<a id="api-c17c19e6e3c2"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-af090c03d503"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-bbff21342311"></a>

### OnPropertyChanged

```csharp
protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-603838e3b066"></a>

### EnableTransitionsProperty

```csharp
public static readonly DependencyProperty EnableTransitionsProperty
```

Identifies the [EnableTransitions](FontIcon.md#api-e24048891a56) dependency property.

<a id="api-af8aca232edc"></a>

### GlyphProperty

```csharp
public static readonly DependencyProperty GlyphProperty
```

Identifies the [Glyph](FontIcon.md#api-d3103c5c7209) dependency property.

<a id="api-f642fbffbf50"></a>

### IconFontFamilyProperty

```csharp
public static readonly DependencyProperty IconFontFamilyProperty
```

Identifies the [IconFontFamily](FontIcon.md#api-1b82b4e5d3f0) dependency property.

<a id="api-b0b85c12c59a"></a>

### IconFontSizeProperty

```csharp
public static readonly DependencyProperty IconFontSizeProperty
```

Identifies the [IconFontSize](FontIcon.md#api-72b1e3edc6c8) dependency property.

<a id="api-2f3d3f982956"></a>

### IsSpinningProperty

```csharp
public static readonly DependencyProperty IsSpinningProperty
```

Identifies the [IsSpinning](FontIcon.md#api-549ab9361146) dependency property.

<a id="api-05f4d3690439"></a>

### MirroredWhenRightToLeftProperty

```csharp
public static readonly DependencyProperty MirroredWhenRightToLeftProperty
```

Identifies the [MirroredWhenRightToLeft](FontIcon.md#api-5d8a80ceb980) dependency property.

<a id="api-5eb2b15b1961"></a>

### RotationProperty

```csharp
public static readonly DependencyProperty RotationProperty
```

Identifies the [Rotation](FontIcon.md#api-ebc35a3d9794) dependency property.
