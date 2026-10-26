# Image

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class Image : Control
```

A Fluent Design image presenter that frames a picture with a theme-aware 1px stroke and rounded-corner clipping while delegating natural sizing, stretch semantics, and DPI handling to a real inner `Image`. Authority: in-tree precedent (PersonPicture stroke tokens, FontIcon non-interactive shape); WinUI 3 ships no styled Image control, so the frame follows the Card stroke idiom.

**Base type:** [`Control`](https://learn.microsoft.com/dotnet/api/system.windows.controls.control) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/Image.cs)

## Constructors

<a id="api-077c2d49011c"></a>

### Image

```csharp
public Image()
```

Creates a new `Image` instance.

## Properties

<a id="api-2451d3bacc22"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius of the frame. The stroke border template-binds this value directly; the inner image is clipped in code using the top-left radius uniformly (the WPF Border corner-clip idiom). Set to 0 to disable clipping.

<a id="api-daf2d079d21d"></a>

### Source

```csharp
public ImageSource? Source { get; set; }
```

Gets or sets the image source to display. When `null` (default) nothing is drawn inside the frame.

<a id="api-4f3d9f252dbe"></a>

### Stretch

```csharp
public Stretch Stretch { get; set; }
```

Gets or sets how the image fills the available space. The default is `Uniform`, preserving the source aspect ratio.

## Methods

<a id="api-33a4ea48563b"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-caeeca1c38d1"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-8ff096023164"></a>

### OnRenderSizeChanged

```csharp
protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-cd7cd87255ce"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](Image.md#api-2451d3bacc22) dependency property.

<a id="api-9ac49bfaf9d6"></a>

### SourceProperty

```csharp
public static readonly DependencyProperty SourceProperty
```

Identifies the [Source](Image.md#api-daf2d079d21d) dependency property.

<a id="api-cd36698c707e"></a>

### StretchProperty

```csharp
public static readonly DependencyProperty StretchProperty
```

Identifies the [Stretch](Image.md#api-4f3d9f252dbe) dependency property.
