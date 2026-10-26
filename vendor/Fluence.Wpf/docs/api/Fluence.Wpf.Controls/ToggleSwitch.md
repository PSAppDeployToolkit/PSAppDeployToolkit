# ToggleSwitch

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class ToggleSwitch : ToggleButton
```

A toggle switch control with On/Off content.

**Remarks:** Inspired by WinUI's ToggleSwitch.

**Base type:** [ToggleButton](ToggleButton.md)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/ToggleSwitch.cs)

## Constructors

<a id="api-2cad3dc7b908"></a>

### ToggleSwitch

```csharp
public ToggleSwitch()
```

Creates a new `ToggleSwitch` instance.

## Properties

<a id="api-5f566980fe26"></a>

### HeaderContent

```csharp
public object HeaderContent { get; set; }
```

Gets or sets the header content displayed above the switch.

<a id="api-15cc3c7c3368"></a>

### OffContent

```csharp
public object OffContent { get; set; }
```

Gets or sets the content displayed when the switch is off.

<a id="api-bed56d8abb21"></a>

### OffContentTemplate

```csharp
public DataTemplate OffContentTemplate { get; set; }
```

Gets or sets the data template for the off-state content.

<a id="api-4e6304b36106"></a>

### OnContent

```csharp
public object OnContent { get; set; }
```

Gets or sets the content displayed when the switch is on.

<a id="api-68cb34a045be"></a>

### OnContentTemplate

```csharp
public DataTemplate OnContentTemplate { get; set; }
```

Gets or sets the data template for the on-state content.

## Methods

<a id="api-2e375e6d40b3"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [ToggleButton API](../Fluence.Wpf.Controls/ToggleButton.md).

<a id="api-6b46379a8e8f"></a>

### OnChecked

```csharp
protected override void OnChecked(RoutedEventArgs e)
```

Documentation inherited from the [ToggleButton API](../Fluence.Wpf.Controls/ToggleButton.md).

<a id="api-812c7fbd6d24"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [ToggleButton API](../Fluence.Wpf.Controls/ToggleButton.md).

<a id="api-56187002c0db"></a>

### OnIndeterminate

```csharp
protected override void OnIndeterminate(RoutedEventArgs e)
```

Documentation inherited from the [ToggleButton API](../Fluence.Wpf.Controls/ToggleButton.md).

<a id="api-c198bfa5d38b"></a>

### OnUnchecked

```csharp
protected override void OnUnchecked(RoutedEventArgs e)
```

Documentation inherited from the [ToggleButton API](../Fluence.Wpf.Controls/ToggleButton.md).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-89246e3eb0a9"></a>

### HeaderContentProperty

```csharp
public static readonly DependencyProperty HeaderContentProperty
```

Identifies the [HeaderContent](ToggleSwitch.md#api-5f566980fe26) dependency property.

<a id="api-d2cf8a0acf4f"></a>

### OffContentProperty

```csharp
public static readonly DependencyProperty OffContentProperty
```

Identifies the [OffContent](ToggleSwitch.md#api-15cc3c7c3368) dependency property.

<a id="api-ac5e2344a22e"></a>

### OffContentTemplateProperty

```csharp
public static readonly DependencyProperty OffContentTemplateProperty
```

Identifies the [OffContentTemplate](ToggleSwitch.md#api-bed56d8abb21) dependency property.

<a id="api-93f412db2327"></a>

### OnContentProperty

```csharp
public static readonly DependencyProperty OnContentProperty
```

Identifies the [OnContent](ToggleSwitch.md#api-4e6304b36106) dependency property.

<a id="api-03166ff1bb07"></a>

### OnContentTemplateProperty

```csharp
public static readonly DependencyProperty OnContentTemplateProperty
```

Identifies the [OnContentTemplate](ToggleSwitch.md#api-68cb34a045be) dependency property.

## Related types

- [Fluence.Wpf.Controls.ToggleButton](ToggleButton.md)
