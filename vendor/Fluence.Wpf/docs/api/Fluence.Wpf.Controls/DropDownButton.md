# DropDownButton

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class DropDownButton : ToggleButton
```

A toggle button that displays a flyout in a popup when checked.

**Base type:** [ToggleButton](ToggleButton.md)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/DropDownButton.cs)

## Constructors

<a id="api-e464c9e5dc12"></a>

### DropDownButton

```csharp
public DropDownButton()
```

Creates a new `DropDownButton` instance.

## Properties

<a id="api-73bdc5efb567"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius of the button border.

<a id="api-529e953f5f66"></a>

### DropdownCornerRadius

```csharp
public CornerRadius DropdownCornerRadius { get; set; }
```

Gets or sets the corner radius of the dropdown popup surface.

<a id="api-2da7d61370da"></a>

### Flyout

```csharp
public object Flyout { get; set; }
```

Gets or sets the content displayed in the dropdown popup.

<a id="api-9ca56549d0a2"></a>

### FlyoutTemplate

```csharp
public DataTemplate FlyoutTemplate { get; set; }
```

Gets or sets the `DataTemplate` used to render [Flyout](DropDownButton.md#api-2da7d61370da).

## Methods

<a id="api-96c07c450345"></a>

### CloseFlyout

```csharp
public void CloseFlyout()
```

Closes the dropdown flyout popup if it is open. WinUI's `DropDownButton` hosts a `FlyoutBase` whose `Hide()` the application calls after handling a click inside arbitrary flyout content (a plain flyout never dismisses itself); this method is the equivalent close affordance for the [Flyout](DropDownButton.md#api-2da7d61370da) object content model. Equivalent to setting `IsChecked` to `false`.

<a id="api-3f8bc4950253"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [ToggleButton API](../Fluence.Wpf.Controls/ToggleButton.md).

<a id="api-441d0e3d94c4"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [ToggleButton API](../Fluence.Wpf.Controls/ToggleButton.md).

<a id="api-3dd87e76b2bd"></a>

### OnPropertyChanged

```csharp
protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
```

Documentation inherited from the [ToggleButton API](../Fluence.Wpf.Controls/ToggleButton.md).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-d7fbe66a4abe"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](DropDownButton.md#api-73bdc5efb567) dependency property.

**Remarks:** Shadows the `Control.CornerRadiusProperty` introduced in net6+ so the property is also available on net472 where `Control` does not declare it.

<a id="api-9ecab52c0fdd"></a>

### DropdownCornerRadiusProperty

```csharp
public static readonly DependencyProperty DropdownCornerRadiusProperty
```

Identifies the [DropdownCornerRadius](DropDownButton.md#api-529e953f5f66) dependency property.

<a id="api-b5d8be7c1214"></a>

### FlyoutProperty

```csharp
public static readonly DependencyProperty FlyoutProperty
```

Identifies the [Flyout](DropDownButton.md#api-2da7d61370da) dependency property.

<a id="api-8ba14ce45487"></a>

### FlyoutTemplateProperty

```csharp
public static readonly DependencyProperty FlyoutTemplateProperty
```

Identifies the [FlyoutTemplate](DropDownButton.md#api-9ca56549d0a2) dependency property.

## Related types

- [Fluence.Wpf.Controls.ToggleButton](ToggleButton.md)
