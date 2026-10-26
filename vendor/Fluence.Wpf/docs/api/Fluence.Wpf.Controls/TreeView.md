# TreeView

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class TreeView : System.Windows.Controls.TreeView
```

A Fluent Design tree view with Fluent hover, selection, and expand/collapse visuals. Items are represented by [TreeViewItem](TreeViewItem.md) containers. Authority: WinUI 3 TreeView_themeresources.xaml.

**Base type:** [`System.Windows.Controls.TreeView`](https://learn.microsoft.com/dotnet/api/system.windows.controls.treeview) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/TreeView.cs)

## Constructors

<a id="api-b558a7fda2bc"></a>

### TreeView

```csharp
public TreeView()
```

Initializes a new instance of the [TreeView](TreeView.md) class.

## Properties

<a id="api-42457807b594"></a>

### SelectedItems

```csharp
public IList SelectedItems { get; }
```

Gets the live list of currently selected items.

<a id="api-8f030bfa4eb8"></a>

### SelectionMode

```csharp
public TreeViewSelectionMode SelectionMode { get; set; }
```

Gets or sets the selection mode used by the tree view.

## Methods

<a id="api-8994d81beb72"></a>

### GetContainerForItemOverride

```csharp
protected override DependencyObject GetContainerForItemOverride()
```

Documentation inherited from the [`TreeView` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.treeview).

<a id="api-9d8717a249bd"></a>

### IsItemItsOwnContainerOverride

```csharp
protected override bool IsItemItsOwnContainerOverride(object item)
```

Documentation inherited from the [`TreeView` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.treeview).

<a id="api-253cd54a35d8"></a>

### OnItemsChanged

```csharp
protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
```

Documentation inherited from the [`TreeView` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.treeview).

<a id="api-d226caab2086"></a>

### OnPreviewKeyDown

```csharp
protected override void OnPreviewKeyDown(KeyEventArgs e)
```

Documentation inherited from the [`TreeView` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.treeview).

<a id="api-b83a1d7a93c0"></a>

### OnSelectedItemChanged

```csharp
protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
```

Documentation inherited from the [`TreeView` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.treeview).

<a id="api-1df0433e0446"></a>

### PrepareContainerForItemOverride

```csharp
protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
```

Documentation inherited from the [`TreeView` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.treeview).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-9239c9d40c13"></a>

### SelectionModeProperty

```csharp
public static readonly DependencyProperty SelectionModeProperty
```

Identifies the [SelectionMode](TreeView.md#api-8f030bfa4eb8) dependency property.

## Related types

- [Fluence.Wpf.TreeViewSelectionMode](../Fluence.Wpf/TreeViewSelectionMode.md)
