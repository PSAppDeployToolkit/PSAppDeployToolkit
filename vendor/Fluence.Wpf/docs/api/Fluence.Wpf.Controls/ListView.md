# ListView

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class ListView : System.Windows.Controls.ListView
```

A Fluent Design styled list view with animated item states.

**Base type:** [`System.Windows.Controls.ListView`](https://learn.microsoft.com/dotnet/api/system.windows.controls.listview) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/ListView.cs)

## Constructors

<a id="api-cf5163f361db"></a>

### ListView

```csharp
public ListView()
```

Initializes a new instance of the [ListView](ListView.md) class and wires the loaded event for default group styling.

## Properties

<a id="api-f2e263830ab2"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius of the list view.

<a id="api-852f60099875"></a>

### EmptyContent

```csharp
public object EmptyContent { get; set; }
```

Content displayed when the list has no items.

<a id="api-253524ed840d"></a>

### HoverHighlightEnabled

```csharp
public bool HoverHighlightEnabled { get; set; }
```

Gets or sets whether hover highlighting is enabled.

<a id="api-6292233d5bda"></a>

### IsItemSelectable

```csharp
public bool IsItemSelectable { get; set; }
```

Gets or sets whether items can be selected and show hover/selection visuals. When false, rows are display-only; scrolling and item animations are unchanged.

<a id="api-c7ccc170bc64"></a>

### ItemAnimationsEnabled

```csharp
public bool ItemAnimationsEnabled { get; set; }
```

Gets or sets whether item animations are enabled.

<a id="api-6ce981f4e81b"></a>

### ItemsLayout

```csharp
public ListViewItemsLayout ItemsLayout { get; set; }
```

Gets or sets whether items run down the list one per row, or wrap across it as a grid.

**Remarks:** [Grid](../Fluence.Wpf/ListViewItemsLayout.md#api-d0013adee797) swaps the items panel for a wrapping one, which is what WinUI's own `GridView` does with its `ItemsWrapGrid`. WPF ships no virtualizing wrap panel, so the grid layout gives up the virtualization the default vertical panel has; a very long grid is better served by a consumer-supplied panel through `ItemsPanel`, which this property leaves alone once set. The WPF `View` property is untouched: that one takes a `GridView` of columns, a different thing.

## Methods

<a id="api-e25f650b9c93"></a>

### AnimateRemove

```csharp
public void AnimateRemove(object item, Action? onCompleted)
```

Animates out the item and then calls the provided callback.

**Parameter `item`:** The item to remove from the list or bound `IList` after the exit animation.

**Parameter `onCompleted`:** An optional callback invoked after removal completes.

<a id="api-0045529ad31b"></a>

### GetContainerForItemOverride

```csharp
protected override DependencyObject GetContainerForItemOverride()
```

Documentation inherited from the [`ListView` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.listview).

<a id="api-f36e72298e79"></a>

### GetParentIsItemSelectable

```csharp
public static bool GetParentIsItemSelectable(DependencyObject element)
```

Gets whether the parent list allows item selection (for template triggers).

**Parameter `element`:** The item container that stores the mirrored selection state.

**Returns:** `true` when the parent list allows item selection; otherwise `false`.

<a id="api-1beedbe4c6a2"></a>

### IsItemItsOwnContainerOverride

```csharp
protected override bool IsItemItsOwnContainerOverride(object item)
```

Documentation inherited from the [`ListView` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.listview).

<a id="api-e457b55cdd06"></a>

### OnSelectionChanged

```csharp
protected override void OnSelectionChanged(SelectionChangedEventArgs e)
```

Documentation inherited from the [`ListView` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.listview).

<a id="api-49e6f3d2cdc4"></a>

### PrepareContainerForItemOverride

```csharp
protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
```

Documentation inherited from the [`ListView` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.listview).

<a id="api-81489e065fba"></a>

### SetParentIsItemSelectable

```csharp
public static void SetParentIsItemSelectable(DependencyObject element, bool value)
```

Sets the parent list's [IsItemSelectable](ListView.md#api-6292233d5bda) value on an item container for template triggers.

**Parameter `element`:** The item container that receives the mirrored selection state.

**Parameter `value`:** `true` when the parent list allows item selection; otherwise `false`.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-328e627b4fd4"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](ListView.md#api-f2e263830ab2) dependency property.

<a id="api-b0770cd46d6f"></a>

### EmptyContentProperty

```csharp
public static readonly DependencyProperty EmptyContentProperty
```

Identifies the [EmptyContent](ListView.md#api-852f60099875) dependency property.

<a id="api-d0358bdb4316"></a>

### HoverHighlightEnabledProperty

```csharp
public static readonly DependencyProperty HoverHighlightEnabledProperty
```

Identifies the [HoverHighlightEnabled](ListView.md#api-253524ed840d) dependency property.

<a id="api-bc346b8a77c3"></a>

### IsItemSelectableProperty

```csharp
public static readonly DependencyProperty IsItemSelectableProperty
```

Identifies the [IsItemSelectable](ListView.md#api-6292233d5bda) dependency property.

<a id="api-10bbd051847f"></a>

### ItemAnimationsEnabledProperty

```csharp
public static readonly DependencyProperty ItemAnimationsEnabledProperty
```

Identifies the [ItemAnimationsEnabled](ListView.md#api-c7ccc170bc64) dependency property.

<a id="api-8d5a1cbe9dd7"></a>

### ItemsLayoutProperty

```csharp
public static readonly DependencyProperty ItemsLayoutProperty
```

Identifies the [ItemsLayout](ListView.md#api-6ce981f4e81b) dependency property.

<a id="api-3a88c9217daa"></a>

### ParentIsItemSelectableProperty

```csharp
public static readonly DependencyProperty ParentIsItemSelectableProperty
```

Attached property mirrored from the parent [ListView](ListView.md) so item templates can use `MultiDataTrigger` (each condition must use a `Binding`, not `Property`, in WPF).

## Related types

- [Fluence.Wpf.ListViewItemsLayout](../Fluence.Wpf/ListViewItemsLayout.md)
