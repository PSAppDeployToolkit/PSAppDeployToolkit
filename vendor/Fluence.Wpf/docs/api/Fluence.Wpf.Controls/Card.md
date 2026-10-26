# Card

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class Card : ContentControl
```

Fluent-styled card container with optional header, footer, and icon.

**Base type:** [`ContentControl`](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/Card.cs)

## Constructors

<a id="api-4e71f627367b"></a>

### Card

```csharp
public Card()
```

Creates a new `Card` instance.

## Properties

<a id="api-cf0176ed3b99"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius of the card.

<a id="api-62c3b0a0b47c"></a>

### Footer

```csharp
public object Footer { get; set; }
```

Gets or sets the footer content of the card.

<a id="api-ba3fa2c876a3"></a>

### FooterTemplate

```csharp
public DataTemplate FooterTemplate { get; set; }
```

Gets or sets the data template for the footer content.

<a id="api-0ea2c2839c46"></a>

### Header

```csharp
public object Header { get; set; }
```

Gets or sets the header content of the card.

<a id="api-b8de9b02e0c0"></a>

### HeaderTemplate

```csharp
public DataTemplate HeaderTemplate { get; set; }
```

Gets or sets the data template for the header content.

<a id="api-4be9b4bac5b9"></a>

### Icon

```csharp
public object Icon { get; set; }
```

Gets or sets the icon displayed in the card header.

<a id="api-0b2074b0a959"></a>

### IsClickable

```csharp
public bool IsClickable { get; set; }
```

Gets or sets whether the card responds to mouse click interactions.

<a id="api-08ecb4e131f2"></a>

### IsPressed

```csharp
public bool IsPressed { get; }
```

Gets whether the card is currently pressed.

<a id="api-082eeb24faca"></a>

### Variant

```csharp
public CardVariant Variant { get; set; }
```

Gets or sets the visual variant of the card (Default, Outlined, Filled, Subtle).

## Methods

<a id="api-cd79f8452082"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-2248c240144e"></a>

### OnKeyDown

```csharp
protected override void OnKeyDown(KeyEventArgs e)
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-a351b223b7cb"></a>

### OnKeyUp

```csharp
protected override void OnKeyUp(KeyEventArgs e)
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-8409d4e6f51f"></a>

### OnLostMouseCapture

```csharp
protected override void OnLostMouseCapture(MouseEventArgs e)
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-ece59ace5768"></a>

### OnMouseLeave

```csharp
protected override void OnMouseLeave(MouseEventArgs e)
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-6c43b55d4c58"></a>

### OnMouseLeftButtonDown

```csharp
protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-9dab64f84c38"></a>

### OnMouseLeftButtonUp

```csharp
protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

## Events

<a id="api-c1d5457073ee"></a>

### Click

```csharp
public event RoutedEventHandler Click
```

Occurs when a clickable card is activated by a mouse left-button release that began with a press inside the card bounds.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-8c44ea92c376"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](Card.md#api-cf0176ed3b99) dependency property.

<a id="api-197eb7a2c630"></a>

### FooterProperty

```csharp
public static readonly DependencyProperty FooterProperty
```

Identifies the [Footer](Card.md#api-62c3b0a0b47c) dependency property.

<a id="api-45e959a5992d"></a>

### FooterTemplateProperty

```csharp
public static readonly DependencyProperty FooterTemplateProperty
```

Identifies the [FooterTemplate](Card.md#api-ba3fa2c876a3) dependency property.

<a id="api-8de0acd0824c"></a>

### HeaderProperty

```csharp
public static readonly DependencyProperty HeaderProperty
```

Identifies the [Header](Card.md#api-0ea2c2839c46) dependency property.

<a id="api-f8cf9a7a361c"></a>

### HeaderTemplateProperty

```csharp
public static readonly DependencyProperty HeaderTemplateProperty
```

Identifies the [HeaderTemplate](Card.md#api-b8de9b02e0c0) dependency property.

<a id="api-82ac173800df"></a>

### IconProperty

```csharp
public static readonly DependencyProperty IconProperty
```

Identifies the [Icon](Card.md#api-4be9b4bac5b9) dependency property.

<a id="api-df2a8d338728"></a>

### IsClickableProperty

```csharp
public static readonly DependencyProperty IsClickableProperty
```

Identifies the [IsClickable](Card.md#api-0b2074b0a959) dependency property.

<a id="api-2f3f95ebc449"></a>

### IsPressedProperty

```csharp
public static readonly DependencyProperty IsPressedProperty
```

Identifies the [IsPressed](Card.md#api-08ecb4e131f2) dependency property.

<a id="api-99035d50c5e4"></a>

### VariantProperty

```csharp
public static readonly DependencyProperty VariantProperty
```

Identifies the [Variant](Card.md#api-082eeb24faca) dependency property.

## Fields

<a id="api-c48f87ccf36e"></a>

### ClickEvent

```csharp
public static readonly RoutedEvent ClickEvent
```

Identifies the [Click](Card.md#api-c1d5457073ee) routed event.

## Related types

- [Fluence.Wpf.CardVariant](../Fluence.Wpf/CardVariant.md)
