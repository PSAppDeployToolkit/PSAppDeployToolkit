# TabViewTabCloseRequestedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public class TabViewTabCloseRequestedEventArgs : RoutedEventArgs
```

Event data for [TabCloseRequested](../Fluence.Wpf.Controls/TabView.md#api-a9b5ddee74ed) and [CloseRequested](../Fluence.Wpf.Controls/TabViewItem.md#api-be21ac4f070a).

**Remarks:** Initializes a new instance of the [TabViewTabCloseRequestedEventArgs](TabViewTabCloseRequestedEventArgs.md) class.

**Parameter `routedEvent`:** The routed event being raised.

**Parameter `source`:** The element raising the event.

**Parameter `tab`:** The [TabViewItem](../Fluence.Wpf.Controls/TabViewItem.md) the user has asked to close.

**Parameter `item`:** The bound data item, or the [TabViewItem](../Fluence.Wpf.Controls/TabViewItem.md) itself if no data was bound.

**Base type:** [`RoutedEventArgs`](https://learn.microsoft.com/dotnet/api/system.windows.routedeventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/TabViewTabCloseRequestedEventArgs.cs)

## Constructors

<a id="api-26efb20a2e92"></a>

### TabViewTabCloseRequestedEventArgs

```csharp
public TabViewTabCloseRequestedEventArgs(RoutedEvent routedEvent, object source, TabViewItem tab, object item)
```

Event data for [TabCloseRequested](../Fluence.Wpf.Controls/TabView.md#api-a9b5ddee74ed) and [CloseRequested](../Fluence.Wpf.Controls/TabViewItem.md#api-be21ac4f070a).

**Remarks:** Initializes a new instance of the [TabViewTabCloseRequestedEventArgs](TabViewTabCloseRequestedEventArgs.md) class.

**Parameter `routedEvent`:** The routed event being raised.

**Parameter `source`:** The element raising the event.

**Parameter `tab`:** The [TabViewItem](../Fluence.Wpf.Controls/TabViewItem.md) the user has asked to close.

**Parameter `item`:** The bound data item, or the [TabViewItem](../Fluence.Wpf.Controls/TabViewItem.md) itself if no data was bound.

## Properties

<a id="api-8f1b75e09c15"></a>

### Item

```csharp
public object Item { get; }
```

Gets the data item bound to [Tab](TabViewTabCloseRequestedEventArgs.md#api-dbbf76dd85b0), or the tab itself when items are declared inline.

<a id="api-dbbf76dd85b0"></a>

### Tab

```csharp
public TabViewItem Tab { get; }
```

Gets the tab container the user asked to close.

## Methods

<a id="api-2e2751e39257"></a>

### InvokeEventHandler

```csharp
protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
```

Documentation inherited from the [`RoutedEventArgs` API](https://learn.microsoft.com/dotnet/api/system.windows.routedeventargs).

**Remarks:** The routed event declares `EventHandler`1` rather than `RoutedEventHandler`, so the base implementation would fall back to `DynamicInvoke`. Casting here keeps dispatch a direct call.

## Related types

- [Fluence.Wpf.Controls.TabViewItem](../Fluence.Wpf.Controls/TabViewItem.md)
