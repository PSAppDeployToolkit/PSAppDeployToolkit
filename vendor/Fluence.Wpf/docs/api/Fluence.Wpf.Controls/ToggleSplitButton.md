# ToggleSplitButton

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class ToggleSplitButton : SplitButton
```

A [SplitButton](SplitButton.md) whose primary half toggles a checked state instead of firing a plain action: clicking it flips [IsChecked](ToggleSplitButton.md#api-6ec9ad522011) and then raises [Click](SplitButton.md#api-8dce59357d6f), while the secondary "chevron" half still opens the flyout. The canonical WinUI 3 ToggleSplitButton pattern, used when the primary half switches a mode on or off and the flyout chooses which variant of that mode applies.

**Base type:** [SplitButton](SplitButton.md)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/ToggleSplitButton.cs)

## Constructors

<a id="api-3667963d47fa"></a>

### ToggleSplitButton

```csharp
public ToggleSplitButton()
```

Creates a new `ToggleSplitButton` instance.

## Properties

<a id="api-6ec9ad522011"></a>

### IsChecked

```csharp
public bool IsChecked { get; set; }
```

Gets or sets a value indicating whether the toggle split button is checked. A primary-half click flips this value before [Click](SplitButton.md#api-8dce59357d6f) is raised.

## Methods

<a id="api-8eb4fc9598f4"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [SplitButton API](../Fluence.Wpf.Controls/SplitButton.md).

<a id="api-96508486331d"></a>

### OnPrimaryButtonClick

```csharp
protected override void OnPrimaryButtonClick(object sender, RoutedEventArgs e)
```

Toggles [IsChecked](ToggleSplitButton.md#api-6ec9ad522011) and then runs the inherited click behavior, mirroring the WinUI ToggleSplitButton primary-click contract: a [Click](SplitButton.md#api-8dce59357d6f) handler observes the already-flipped state.

**Parameter `sender`:** The primary button template part that raised the click.

**Parameter `e`:** The routed event data from the primary button.

## Events

<a id="api-f1a492ed2bbd"></a>

### IsCheckedChanged

```csharp
public event EventHandler<ToggleSplitButtonIsCheckedChangedEventArgs>? IsCheckedChanged
```

Raised when [IsChecked](ToggleSplitButton.md#api-6ec9ad522011) changes, whether from a primary-half click, the Toggle automation pattern, a binding, or a direct property set.

**Remarks:** Unlike WinUI, the event also fires for values applied before the control is loaded (for example markup-set initial values), matching how the WPF `ToggleButton` raises its Checked and Unchecked events.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-f0147a97752f"></a>

### IsCheckedProperty

```csharp
public static readonly DependencyProperty IsCheckedProperty
```

Identifies the [IsChecked](ToggleSplitButton.md#api-6ec9ad522011) dependency property.

## Related types

- [Fluence.Wpf.Controls.SplitButton](SplitButton.md)
- [Fluence.Wpf.ToggleSplitButtonIsCheckedChangedEventArgs](../Fluence.Wpf/ToggleSplitButtonIsCheckedChangedEventArgs.md)
