# TreeViewItem

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class TreeViewItem : System.Windows.Controls.TreeViewItem
```

A Fluent Design tree view item with full-row hover highlight, animated chevron, and WinUI 3-canonical background brush states. Authority: WinUI 3 TreeView_themeresources.xaml + TreeViewItem.xaml.

**Base type:** [`System.Windows.Controls.TreeViewItem`](https://learn.microsoft.com/dotnet/api/system.windows.controls.treeviewitem) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/TreeViewItem.cs)

## Constructors

<a id="api-3c7a5df9ea0b"></a>

### TreeViewItem

```csharp
public TreeViewItem()
```

Creates a new `TreeViewItem` instance.

## Properties

<a id="api-94d36e766197"></a>

### IsSelectionChecked

```csharp
public bool? IsSelectionChecked { get; set; }
```

Gets or sets whether this item is checked in a multiple-selection tree view. A `null` value represents an indeterminate parent state.

## Methods

<a id="api-c0acfc9e6d27"></a>

### GetContainerForItemOverride

```csharp
protected override DependencyObject GetContainerForItemOverride()
```

Documentation inherited from the [`TreeViewItem` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.treeviewitem).

<a id="api-c662fbc14590"></a>

### IsItemItsOwnContainerOverride

```csharp
protected override bool IsItemItsOwnContainerOverride(object item)
```

Documentation inherited from the [`TreeViewItem` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.treeviewitem).

<a id="api-6fcaadefbd9c"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`TreeViewItem` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.treeviewitem).

<a id="api-cfc38c029a05"></a>

### OnItemsChanged

```csharp
protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
```

Documentation inherited from the [`TreeViewItem` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.treeviewitem).

<a id="api-2696a67d4bd2"></a>

### OnKeyDown

```csharp
protected override void OnKeyDown(KeyEventArgs e)
```

Documentation inherited from the [`TreeViewItem` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.treeviewitem).

<a id="api-2de098dd3239"></a>

### OnPreviewKeyDown

```csharp
protected override void OnPreviewKeyDown(KeyEventArgs e)
```

Documentation inherited from the [`TreeViewItem` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.treeviewitem).

<a id="api-dd47d2de6833"></a>

### OnSelected

```csharp
protected override void OnSelected(RoutedEventArgs e)
```

Documentation inherited from the [`TreeViewItem` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.treeviewitem).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-7c21ff994466"></a>

### IsSelectionCheckedProperty

```csharp
public static readonly DependencyProperty IsSelectionCheckedProperty
```

Identifies the [IsSelectionChecked](TreeViewItem.md#api-94d36e766197) dependency property.
