# TextBlock

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class TextBlock : ContentControl
```

A Fluent Design enhanced TextBlock that supports the FluentTypography type ramp.

**Base type:** [`ContentControl`](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/TextBlock.cs)

## Constructors

<a id="api-629717bcfa76"></a>

### TextBlock

```csharp
public TextBlock()
```

Creates a new `TextBlock` instance.

## Properties

<a id="api-55f86c94798b"></a>

### Text

```csharp
public string Text { get; set; }
```

Gets or sets the text displayed by the inner `TextBlock`.

<a id="api-f75052914b28"></a>

### TextTrimming

```csharp
public TextTrimming TextTrimming { get; set; }
```

Gets or sets how text is trimmed when it overflows the layout area.

<a id="api-2d596b82f154"></a>

### TextWrapping

```csharp
public TextWrapping TextWrapping { get; set; }
```

Gets or sets how text wraps within the inner `TextBlock`.

<a id="api-9546042f092f"></a>

### Typography

```csharp
public FluentTypography Typography { get; set; }
```

Gets or sets the Fluent typography style applied to the inner text.

## Methods

<a id="api-1f043d3bc84e"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-4eb93b579b45"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-569c9822b651"></a>

### TextProperty

```csharp
public static readonly DependencyProperty TextProperty
```

Identifies the [Text](TextBlock.md#api-55f86c94798b) dependency property.

<a id="api-20651272d5d2"></a>

### TextTrimmingProperty

```csharp
public static readonly DependencyProperty TextTrimmingProperty
```

Identifies the [TextTrimming](TextBlock.md#api-f75052914b28) dependency property.

<a id="api-9d8ac04046ca"></a>

### TextWrappingProperty

```csharp
public static readonly DependencyProperty TextWrappingProperty
```

Identifies the [TextWrapping](TextBlock.md#api-2d596b82f154) dependency property.

<a id="api-3bb0548321b8"></a>

### TypographyProperty

```csharp
public static readonly DependencyProperty TypographyProperty
```

Identifies the [Typography](TextBlock.md#api-9546042f092f) dependency property.

## Related types

- [Fluence.Wpf.FluentTypography](../Fluence.Wpf/FluentTypography.md)
