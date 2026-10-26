# BreadcrumbBar

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class BreadcrumbBar : ItemsControl
```

A horizontal trail of crumbs mirroring the WinUI 3 `BreadcrumbBar`: each item renders as a clickable [BreadcrumbBarItem](BreadcrumbBarItem.md) followed by a chevron separator, with the last crumb shown as the current location (no trailing chevron, primary text, SemiBold).

**Remarks:** Items come from `ItemsSource` (or the inline items collection) and render through the normal ItemsControl mechanics, so `DisplayMemberPath` and `ItemTemplate` are respected. Subscribe to [ItemClicked](BreadcrumbBar.md#api-8c3b462d4081) to navigate; the event is raised for every crumb including the last one, matching WinUI.



WinUI collapses leading crumbs into an ellipsis crumb when the bar is width-constrained; that overflow collapse is a deliberate v1 omission here. The strip simply extends to its natural width and clips when constrained.

**Base type:** [`ItemsControl`](https://learn.microsoft.com/dotnet/api/system.windows.controls.itemscontrol) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/BreadcrumbBar.cs)

## Constructors

<a id="api-2e950493eab8"></a>

### BreadcrumbBar

```csharp
public BreadcrumbBar()
```

Initializes a new instance of the [BreadcrumbBar](BreadcrumbBar.md) class and subscribes to child [Click](BreadcrumbBarItem.md#api-09d30b9d9bd2) events for aggregation into [ItemClicked](BreadcrumbBar.md#api-8c3b462d4081).

## Methods

<a id="api-bd2652db3217"></a>

### GetContainerForItemOverride

```csharp
protected override DependencyObject GetContainerForItemOverride()
```

Documentation inherited from the [`ItemsControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.itemscontrol).

<a id="api-1205d6692688"></a>

### IsItemItsOwnContainerOverride

```csharp
protected override bool IsItemItsOwnContainerOverride(object item)
```

Documentation inherited from the [`ItemsControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.itemscontrol).

<a id="api-8865928ff55f"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`ItemsControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.itemscontrol).

<a id="api-be05c849aa76"></a>

### OnItemsChanged

```csharp
protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
```

Documentation inherited from the [`ItemsControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.itemscontrol).

<a id="api-1561c2f94dc4"></a>

### PrepareContainerForItemOverride

```csharp
protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
```

Documentation inherited from the [`ItemsControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.itemscontrol).

## Events

<a id="api-8c3b462d4081"></a>

### ItemClicked

```csharp
public event EventHandler<BreadcrumbBarItemClickedEventArgs>? ItemClicked
```

Occurs when any crumb is clicked, including the last (current) one, matching the WinUI 3 BreadcrumbBar contract. The event args carry the clicked data item and its zero-based index in the items collection.

## Related types

- [Fluence.Wpf.BreadcrumbBarItemClickedEventArgs](../Fluence.Wpf/BreadcrumbBarItemClickedEventArgs.md)
