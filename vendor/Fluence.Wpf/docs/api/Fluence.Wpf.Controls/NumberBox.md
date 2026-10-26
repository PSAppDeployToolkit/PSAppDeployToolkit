# NumberBox

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class NumberBox : Control
```

A numeric input control with optional spin buttons and min/max clamping.

**Base type:** [`Control`](https://learn.microsoft.com/dotnet/api/system.windows.controls.control) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/NumberBox.cs)

## Constructors

<a id="api-1edfaca322c0"></a>

### NumberBox

```csharp
public NumberBox()
```

Creates a new `NumberBox` instance.

## Properties

<a id="api-e35cf033b8e6"></a>

### AcceptsExpression

```csharp
public bool AcceptsExpression { get; set; }
```

Gets or sets whether the control may parse simple expressions (reserved).

<a id="api-12e3f44040e7"></a>

### Description

```csharp
public string Description { get; set; }
```

Gets or sets helper text displayed below the control.

<a id="api-a32584807d96"></a>

### Header

```csharp
public object Header { get; set; }
```

Gets or sets an optional header displayed above the input.

<a id="api-88b2e56b88a5"></a>

### LargeChange

```csharp
public double LargeChange { get; set; }
```

Gets or sets the large increment (reserved for keyboard/page navigation).

<a id="api-6abeffcd06e4"></a>

### Maximum

```csharp
public double Maximum { get; set; }
```

Gets or sets the maximum allowed value.

<a id="api-37d0fca38a8f"></a>

### Minimum

```csharp
public double Minimum { get; set; }
```

Gets or sets the minimum allowed value.

<a id="api-07263fe83c83"></a>

### PlaceholderText

```csharp
public string PlaceholderText { get; set; }
```

Gets or sets watermark text shown when the text box is empty.

<a id="api-2da0414cf176"></a>

### SmallChange

```csharp
public double SmallChange { get; set; }
```

Gets or sets the increment used by spin buttons.

<a id="api-0b7f47514969"></a>

### SpinButtonPlacementMode

```csharp
public NumberBoxSpinButtonPlacementMode SpinButtonPlacementMode { get; set; }
```

Gets or sets where spin buttons are shown.

<a id="api-3194cd27ccdc"></a>

### Text

```csharp
public string Text { get; set; }
```

Gets or sets the text representation of the value.

<a id="api-0b25f14ff0d1"></a>

### Value

```csharp
public double Value { get; set; }
```

Gets or sets the numeric value.

## Methods

<a id="api-ec756953b9a5"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-ddbdb9b8d06d"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-e5a38e4352e2"></a>

### OnDownClick

```csharp
protected virtual void OnDownClick()
```

Decrements [Value](NumberBox.md#api-0b25f14ff0d1) by [SmallChange](NumberBox.md#api-2da0414cf176) with clamping. A cleared value has nothing to step from, so the call does nothing.

<a id="api-72bc3d343356"></a>

### OnGotKeyboardFocus

```csharp
protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-baa80325cceb"></a>

### OnPreviewMouseLeftButtonDown

```csharp
protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-ec6f53bd5070"></a>

### OnUpClick

```csharp
protected virtual void OnUpClick()
```

Increments [Value](NumberBox.md#api-0b25f14ff0d1) by [SmallChange](NumberBox.md#api-2da0414cf176) with clamping. A cleared value has nothing to step from, so the call does nothing.

<a id="api-820dc3076f9a"></a>

### OnValueChanged

```csharp
protected virtual void OnValueChanged(double oldValue, double newValue)
```

Raises the [ValueChanged](NumberBox.md#api-339ec366940c) event.

**Parameter `oldValue`:** The previous value.

**Parameter `newValue`:** The new value.

<a id="api-21efa6780190"></a>

### TryParseText

```csharp
public bool TryParseText()
```

Updates [Value](NumberBox.md#api-0b25f14ff0d1) from [Text](NumberBox.md#api-3194cd27ccdc) if parsing succeeds, and clears [Value](NumberBox.md#api-0b25f14ff0d1) to `NaN` when the field is empty.

**Returns:** `true` if a number was parsed and applied; otherwise `false`.

## Events

<a id="api-339ec366940c"></a>

### ValueChanged

```csharp
public event EventHandler<NumberBoxValueChangedEventArgs>? ValueChanged
```

Occurs when [Value](NumberBox.md#api-0b25f14ff0d1) changes after coercion.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-62ad70a8c9ab"></a>

### AcceptsExpressionProperty

```csharp
public static readonly DependencyProperty AcceptsExpressionProperty
```

Identifies the [AcceptsExpression](NumberBox.md#api-e35cf033b8e6) dependency property.

<a id="api-4f13da81ff93"></a>

### DescriptionProperty

```csharp
public static readonly DependencyProperty DescriptionProperty
```

Identifies the [Description](NumberBox.md#api-12e3f44040e7) dependency property.

<a id="api-6b3cc52540d5"></a>

### HeaderProperty

```csharp
public static readonly DependencyProperty HeaderProperty
```

Identifies the [Header](NumberBox.md#api-a32584807d96) dependency property.

<a id="api-d0478f0c0b72"></a>

### LargeChangeProperty

```csharp
public static readonly DependencyProperty LargeChangeProperty
```

Identifies the [LargeChange](NumberBox.md#api-88b2e56b88a5) dependency property.

<a id="api-25db3d074992"></a>

### MaximumProperty

```csharp
public static readonly DependencyProperty MaximumProperty
```

Identifies the [Maximum](NumberBox.md#api-6abeffcd06e4) dependency property.

<a id="api-3932b8e4426d"></a>

### MinimumProperty

```csharp
public static readonly DependencyProperty MinimumProperty
```

Identifies the [Minimum](NumberBox.md#api-37d0fca38a8f) dependency property.

<a id="api-3ef7ae443010"></a>

### PlaceholderTextProperty

```csharp
public static readonly DependencyProperty PlaceholderTextProperty
```

Identifies the [PlaceholderText](NumberBox.md#api-07263fe83c83) dependency property.

<a id="api-cb14531d7f7c"></a>

### SmallChangeProperty

```csharp
public static readonly DependencyProperty SmallChangeProperty
```

Identifies the [SmallChange](NumberBox.md#api-2da0414cf176) dependency property.

<a id="api-10b100f25c13"></a>

### SpinButtonPlacementModeProperty

```csharp
public static readonly DependencyProperty SpinButtonPlacementModeProperty
```

Identifies the [SpinButtonPlacementMode](NumberBox.md#api-0b7f47514969) dependency property.

<a id="api-ffcf0c5b55b9"></a>

### TextProperty

```csharp
public static readonly DependencyProperty TextProperty
```

Identifies the [Text](NumberBox.md#api-3194cd27ccdc) dependency property.

<a id="api-487a76f815ad"></a>

### ValueProperty

```csharp
public static readonly DependencyProperty ValueProperty
```

Identifies the [Value](NumberBox.md#api-0b25f14ff0d1) dependency property.

## Related types

- [Fluence.Wpf.NumberBoxSpinButtonPlacementMode](../Fluence.Wpf/NumberBoxSpinButtonPlacementMode.md)
- [Fluence.Wpf.NumberBoxValueChangedEventArgs](../Fluence.Wpf/NumberBoxValueChangedEventArgs.md)
