# TabViewItem

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class TabViewItem : TabItem
```

A `TabItem` container used by [TabView](TabView.md) that renders an icon, a header, and an optional close button aligned with the WinUI 3 TabView visual language.

**Base type:** [`TabItem`](https://learn.microsoft.com/dotnet/api/system.windows.controls.tabitem) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/TabViewItem.cs)

## Constructors

<a id="api-fde16d8423d9"></a>

### TabViewItem

```csharp
public TabViewItem()
```

Creates a new `TabViewItem` instance.

## Properties

<a id="api-80f7054dd230"></a>

### Icon

```csharp
public object Icon { get; set; }
```

Gets or sets the icon shown at the leading edge of the tab header. Accepts any element (typically a [FontIcon](FontIcon.md) or [Image](Image.md)).

<a id="api-48fc9ddf1ba3"></a>

### IsClosable

```csharp
public bool IsClosable { get; set; }
```

Gets or sets whether the per-tab close button is shown for this item. Note the effective visibility still follows the owning [CloseButtonOverlayMode](TabView.md#api-08cfe6ed2b35).

## Methods

<a id="api-560bfda901b2"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`TabItem` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.tabitem).

## Events

<a id="api-be21ac4f070a"></a>

### CloseRequested

```csharp
public event EventHandler<TabViewTabCloseRequestedEventArgs> CloseRequested
```

Raised when the user clicks the per-tab close button. The parent [TabView](TabView.md) aggregates this into [TabCloseRequested](TabView.md#api-a9b5ddee74ed) for convenience.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-f3a451faf071"></a>

### IconProperty

```csharp
public static readonly DependencyProperty IconProperty
```

Identifies the [Icon](TabViewItem.md#api-80f7054dd230) dependency property.

<a id="api-edb5a5817e4a"></a>

### IsClosableProperty

```csharp
public static readonly DependencyProperty IsClosableProperty
```

Identifies the [IsClosable](TabViewItem.md#api-48fc9ddf1ba3) dependency property.

## Fields

<a id="api-5f1faa9f3267"></a>

### CloseRequestedEvent

```csharp
public static readonly RoutedEvent CloseRequestedEvent
```

Identifies the [CloseRequested](TabViewItem.md#api-be21ac4f070a) routed event.

## Related types

- [Fluence.Wpf.TabViewTabCloseRequestedEventArgs](../Fluence.Wpf/TabViewTabCloseRequestedEventArgs.md)
