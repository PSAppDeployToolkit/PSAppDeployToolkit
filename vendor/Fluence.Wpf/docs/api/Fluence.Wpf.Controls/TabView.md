# TabView

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class TabView : TabControl
```

A `TabControl` aligned with the WinUI 3 TabView: per-tab close buttons, a trailing "add" (+) button, horizontally scrollable tab strip with scroll navigation buttons, and WinUI-styled selection indicator.

**Remarks:** [TabView](TabView.md) produces [TabViewItem](TabViewItem.md) containers by default. Consumers can bind to `ItemsSource` and optionally supply an `ItemTemplate` to render header content for each item; the icon and close-button chrome are always supplied by the container template.



Listen to [AddTabButtonClick](TabView.md#api-1644932d5df8) to create new tabs and to [TabCloseRequested](TabView.md#api-a9b5ddee74ed) to remove a tab; this control does not itself mutate the items collection so applications remain in full control of their data model.



`PART_ScrollBackButton` and `PART_ScrollForwardButton` are automatically shown or hidden based on whether the tab strip overflows the available width.

**Base type:** [`TabControl`](https://learn.microsoft.com/dotnet/api/system.windows.controls.tabcontrol) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/TabView.cs)

## Constructors

<a id="api-9e42ee561130"></a>

### TabView

```csharp
public TabView()
```

Initializes a new instance of the [TabView](TabView.md) class and subscribes to child [CloseRequested](TabViewItem.md#api-be21ac4f070a) events for aggregation.

## Properties

<a id="api-08cfe6ed2b35"></a>

### CloseButtonOverlayMode

```csharp
public TabViewCloseButtonOverlayMode CloseButtonOverlayMode { get; set; }
```

Gets or sets when per-tab close buttons are shown on this control's items.

<a id="api-26b2ee098d3a"></a>

### IsAddTabButtonVisible

```csharp
public bool IsAddTabButtonVisible { get; set; }
```

Gets or sets whether the trailing add-tab (+) button is shown.

<a id="api-41cf7e1e51f1"></a>

### TabWidthMode

```csharp
public TabViewWidthMode TabWidthMode { get; set; }
```

Gets or sets how tab widths are distributed in the tab strip.

## Methods

<a id="api-ca6551c4b4ec"></a>

### GetContainerForItemOverride

```csharp
protected override DependencyObject GetContainerForItemOverride()
```

Documentation inherited from the [`TabControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.tabcontrol).

<a id="api-6b0ca7d48269"></a>

### IsItemItsOwnContainerOverride

```csharp
protected override bool IsItemItsOwnContainerOverride(object item)
```

Documentation inherited from the [`TabControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.tabcontrol).

<a id="api-ce271ed82386"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`TabControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.tabcontrol).

<a id="api-6ecf2dcc0628"></a>

### OnItemsChanged

```csharp
protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
```

Documentation inherited from the [`TabControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.tabcontrol).

<a id="api-a78b85c9c133"></a>

### OnSelectionChanged

```csharp
protected override void OnSelectionChanged(SelectionChangedEventArgs e)
```

Documentation inherited from the [`TabControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.tabcontrol).

<a id="api-436d434caf74"></a>

### PrepareContainerForItemOverride

```csharp
protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
```

Documentation inherited from the [`TabControl` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.tabcontrol).

## Events

<a id="api-1644932d5df8"></a>

### AddTabButtonClick

```csharp
public event RoutedEventHandler AddTabButtonClick
```

Raised when the user clicks the add-tab (+) button. Consumers typically insert a new item into their items collection and optionally select it.

<a id="api-a9b5ddee74ed"></a>

### TabCloseRequested

```csharp
public event EventHandler<TabViewTabCloseRequestedEventArgs> TabCloseRequested
```

Raised when the user clicks the close (×) button of a [TabViewItem](TabViewItem.md). The event args include the container and the bound item; consumers decide whether to remove it.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-dcf02f97949f"></a>

### CloseButtonOverlayModeProperty

```csharp
public static readonly DependencyProperty CloseButtonOverlayModeProperty
```

Identifies the [CloseButtonOverlayMode](TabView.md#api-08cfe6ed2b35) dependency property.

<a id="api-5b23bd342e59"></a>

### IsAddTabButtonVisibleProperty

```csharp
public static readonly DependencyProperty IsAddTabButtonVisibleProperty
```

Identifies the [IsAddTabButtonVisible](TabView.md#api-26b2ee098d3a) dependency property.

<a id="api-730155fdcd2f"></a>

### TabWidthModeProperty

```csharp
public static readonly DependencyProperty TabWidthModeProperty
```

Identifies the [TabWidthMode](TabView.md#api-41cf7e1e51f1) dependency property.

## Fields

<a id="api-777b2ab85113"></a>

### AddTabButtonClickEvent

```csharp
public static readonly RoutedEvent AddTabButtonClickEvent
```

Identifies the [AddTabButtonClick](TabView.md#api-1644932d5df8) routed event.

<a id="api-5cf93a585e5b"></a>

### TabCloseRequestedEvent

```csharp
public static readonly RoutedEvent TabCloseRequestedEvent
```

Identifies the [TabCloseRequested](TabView.md#api-a9b5ddee74ed) routed event.

## Related types

- [Fluence.Wpf.TabViewCloseButtonOverlayMode](../Fluence.Wpf/TabViewCloseButtonOverlayMode.md)
- [Fluence.Wpf.TabViewTabCloseRequestedEventArgs](../Fluence.Wpf/TabViewTabCloseRequestedEventArgs.md)
- [Fluence.Wpf.TabViewWidthMode](../Fluence.Wpf/TabViewWidthMode.md)
