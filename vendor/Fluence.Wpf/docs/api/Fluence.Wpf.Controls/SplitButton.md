# SplitButton

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class SplitButton : ContentControl, ICommandSource
```

A button that combines a primary action (`Click` / [Command](SplitButton.md#api-c154ebd852eb)) on its left half with a secondary "chevron" half that opens a flyout popup containing arbitrary WPF content. The canonical WinUI 3 SplitButton pattern.

**Base type:** [`ContentControl`](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/SplitButton.cs)

## Constructors

<a id="api-94709378d073"></a>

### SplitButton

```csharp
public SplitButton()
```

Creates a new `SplitButton` instance.

## Properties

<a id="api-c5219fcdfb3e"></a>

### Appearance

```csharp
public ControlAppearance Appearance { get; set; }
```

Gets or sets the visual appearance of the split button. Set to [Accent](../Fluence.Wpf/ControlAppearance.md#api-17cedaaeda4c) to apply the accent-colored variant; the divider stroke will automatically switch to `ControlStrokeColorOnAccentSecondaryBrush` per WinUI 3 canonical styling.

<a id="api-c154ebd852eb"></a>

### Command

```csharp
public ICommand Command { get; set; }
```

Gets or sets the command to invoke when the primary half is clicked.

<a id="api-d39402b3eeda"></a>

### CommandParameter

```csharp
public object CommandParameter { get; set; }
```

Gets or sets the parameter passed to [Command](SplitButton.md#api-c154ebd852eb).

<a id="api-97a8cbb92ff3"></a>

### CommandTarget

```csharp
public IInputElement CommandTarget { get; set; }
```

Gets or sets the element that the command is raised on.

<a id="api-39a419cb746f"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the outer corner radius. Note that the inner halves are split by a 1 px divider; the left half rounds its left corners only and the right half rounds its right corners only using this radius.

<a id="api-4fdb77344fc2"></a>

### DropdownCornerRadius

```csharp
public CornerRadius DropdownCornerRadius { get; set; }
```

Gets or sets the corner radius of the flyout popup surface.

<a id="api-08e1999f6e41"></a>

### Flyout

```csharp
public object Flyout { get; set; }
```

Gets or sets the content displayed in the secondary-half flyout popup.

<a id="api-5f79e583f9bf"></a>

### FlyoutTemplate

```csharp
public DataTemplate FlyoutTemplate { get; set; }
```

Gets or sets the `DataTemplate` used to render [Flyout](SplitButton.md#api-08e1999f6e41).

<a id="api-8e29b1600c4f"></a>

### IsFlyoutOpen

```csharp
public bool IsFlyoutOpen { get; }
```

Gets a value indicating whether the secondary-half flyout popup is currently open.

## Methods

<a id="api-cd0fb7ea7b4f"></a>

### CloseFlyout

```csharp
public void CloseFlyout()
```

Closes the secondary-half flyout popup if it is open. WinUI's `SplitButton` hosts a `FlyoutBase` whose `Hide()` the application calls after handling a click inside arbitrary flyout content (a plain flyout never dismisses itself); this method is the equivalent close affordance for the [Flyout](SplitButton.md#api-08e1999f6e41) object content model, and the light-dismiss (click outside) path is unaffected.

<a id="api-6ae20b41204a"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-7f81e59d9f0b"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`ContentControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.contentcontrol).

<a id="api-ea22fe956b9f"></a>

### OnPrimaryButtonClick

```csharp
protected virtual void OnPrimaryButtonClick(object sender, RoutedEventArgs e)
```

Called when the primary half is clicked; raises [Click](SplitButton.md#api-8dce59357d6f) and invokes [Command](SplitButton.md#api-c154ebd852eb). Override to run logic before the click is raised, calling the base implementation to preserve the click and command behavior.

**Parameter `sender`:** The primary button template part that raised the click.

**Parameter `e`:** The routed event data from the primary button.

## Events

<a id="api-8dce59357d6f"></a>

### Click

```csharp
public event RoutedEventHandler Click
```

Raised when the primary half of the split button is clicked.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-ebd9863d205d"></a>

### AppearanceProperty

```csharp
public static readonly DependencyProperty AppearanceProperty
```

Identifies the [Appearance](SplitButton.md#api-c5219fcdfb3e) dependency property.

<a id="api-4e6980744ae5"></a>

### CommandParameterProperty

```csharp
public static readonly DependencyProperty CommandParameterProperty
```

Identifies the [CommandParameter](SplitButton.md#api-d39402b3eeda) dependency property.

<a id="api-ce8d511b4560"></a>

### CommandProperty

```csharp
public static readonly DependencyProperty CommandProperty
```

Identifies the [Command](SplitButton.md#api-c154ebd852eb) dependency property.

<a id="api-930fa4af70a7"></a>

### CommandTargetProperty

```csharp
public static readonly DependencyProperty CommandTargetProperty
```

Identifies the [CommandTarget](SplitButton.md#api-97a8cbb92ff3) dependency property.

<a id="api-2f15d8be4455"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](SplitButton.md#api-39a419cb746f) dependency property.

<a id="api-e090ed8192de"></a>

### DropdownCornerRadiusProperty

```csharp
public static readonly DependencyProperty DropdownCornerRadiusProperty
```

Identifies the [DropdownCornerRadius](SplitButton.md#api-4fdb77344fc2) dependency property.

<a id="api-17bd4d510fc7"></a>

### FlyoutProperty

```csharp
public static readonly DependencyProperty FlyoutProperty
```

Identifies the [Flyout](SplitButton.md#api-08e1999f6e41) dependency property.

<a id="api-5d2a0e1908fb"></a>

### FlyoutTemplateProperty

```csharp
public static readonly DependencyProperty FlyoutTemplateProperty
```

Identifies the [FlyoutTemplate](SplitButton.md#api-5f79e583f9bf) dependency property.

<a id="api-11ab884fb7b6"></a>

### IsFlyoutOpenProperty

```csharp
public static readonly DependencyProperty IsFlyoutOpenProperty
```

Identifies the [IsFlyoutOpen](SplitButton.md#api-8e29b1600c4f) dependency property.

## Fields

<a id="api-9e013921443c"></a>

### ClickEvent

```csharp
public static readonly RoutedEvent ClickEvent
```

Identifies the [Click](SplitButton.md#api-8dce59357d6f) routed event.

## Related types

- [Fluence.Wpf.ControlAppearance](../Fluence.Wpf/ControlAppearance.md)
