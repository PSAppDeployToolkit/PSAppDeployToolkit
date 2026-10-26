# SelectorBarItem

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class SelectorBarItem : ListBoxItem
```

One selectable entry of a [SelectorBar](SelectorBar.md), mirroring the WinUI 3 `SelectorBarItem`: an optional icon, a text label, and an accent pill that grows out from the label centre while the item is selected.

**Remarks:** WinUI's SelectorBarItem carries [Text](SelectorBarItem.md#api-8a00a2b51d96) and [Icon](SelectorBarItem.md#api-f9f22c575f08) rather than arbitrary content, and this port keeps that surface. The type still inherits `Content` from its [ListBoxItem](ListBoxItem.md) base, but the default template does not present it; an item generated for a plain data item instead takes its label from [SelectorBar](SelectorBar.md), which fills [Text](SelectorBarItem.md#api-8a00a2b51d96) in when it prepares the container.



The selection pill is animated in code rather than from a template storyboard so the reveal can be skipped when motion is disabled, the pattern [PipsPager](PipsPager.md) already uses. WinUI plays the same reveal over its ComboBoxItemScaleAnimationDuration (167 ms) on the 0,0,0,1 key spline, scaling the pill from its 4 px rest width to four times that (SelectorBar.xaml SelectedNormal), and snaps straight back when the item is unselected.

**Base type:** [ListBoxItem](ListBoxItem.md)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/SelectorBarItem.cs)

## Constructors

<a id="api-a93db5d0d3b4"></a>

### SelectorBarItem

```csharp
public SelectorBarItem()
```

Creates a new `SelectorBarItem` instance.

## Properties

<a id="api-f9f22c575f08"></a>

### Icon

```csharp
public object? Icon { get; set; }
```

Gets or sets the icon shown before [Text](SelectorBarItem.md#api-8a00a2b51d96). The default template collapses the icon presenter while this is `null`, so a text-only item carries no leading gap.

<a id="api-8a00a2b51d96"></a>

### Text

```csharp
public string? Text { get; set; }
```

Gets or sets the label shown beside the optional [Icon](SelectorBarItem.md#api-f9f22c575f08).

## Methods

<a id="api-538afb8591b4"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [ListBoxItem API](../Fluence.Wpf.Controls/ListBoxItem.md).

<a id="api-dcb70caeb0f4"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [ListBoxItem API](../Fluence.Wpf.Controls/ListBoxItem.md).

<a id="api-301eae71b6e8"></a>

### OnPreviewKeyDown

```csharp
protected override void OnPreviewKeyDown(KeyEventArgs e)
```

Documentation inherited from the [ListBoxItem API](../Fluence.Wpf.Controls/ListBoxItem.md).

**Remarks:** The keyboard half of the same gesture: Ctrl+Space toggles selection on a WPF [ListBoxItem](ListBoxItem.md), and toggling off is what WinUI does not offer.

<a id="api-a0e23a2e920d"></a>

### OnPreviewMouseLeftButtonDown

```csharp
protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
```

Documentation inherited from the [ListBoxItem API](../Fluence.Wpf.Controls/ListBoxItem.md).

**Remarks:** Swallows the Ctrl+Click deselect gesture on the item that already carries the pill. WPF's [ListBox](ListBox.md) honours it even in `Single`, which would leave the bar with no selection at all; WinUI's SelectorBar has no deselect gesture, and the pill is the page's current destination. Ctrl+Click on any other item still moves the selection there.

<a id="api-dfa1967707ef"></a>

### OnSelected

```csharp
protected override void OnSelected(RoutedEventArgs e)
```

Documentation inherited from the [ListBoxItem API](../Fluence.Wpf.Controls/ListBoxItem.md).

<a id="api-6eebc5036af5"></a>

### OnUnselected

```csharp
protected override void OnUnselected(RoutedEventArgs e)
```

Documentation inherited from the [ListBoxItem API](../Fluence.Wpf.Controls/ListBoxItem.md).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-9b3627dd553a"></a>

### IconProperty

```csharp
public static readonly DependencyProperty IconProperty
```

Identifies the [Icon](SelectorBarItem.md#api-f9f22c575f08) dependency property.

<a id="api-be020fc173cd"></a>

### TextProperty

```csharp
public static readonly DependencyProperty TextProperty
```

Identifies the [Text](SelectorBarItem.md#api-8a00a2b51d96) dependency property.

## Related types

- [Fluence.Wpf.Controls.ListBoxItem](ListBoxItem.md)
