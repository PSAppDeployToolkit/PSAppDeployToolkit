# BreadcrumbBarItemClickedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public sealed class BreadcrumbBarItemClickedEventArgs : EventArgs
```

Event data for [ItemClicked](../Fluence.Wpf.Controls/BreadcrumbBar.md#api-8c3b462d4081).

**Remarks:** Initializes a new instance of the [BreadcrumbBarItemClickedEventArgs](BreadcrumbBarItemClickedEventArgs.md) class.

**Parameter `item`:** The data item of the clicked crumb, or the [BreadcrumbBarItem](../Fluence.Wpf.Controls/BreadcrumbBarItem.md) itself when it was added directly.

**Parameter `index`:** The zero-based position of the clicked crumb in the bar's items collection.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/BreadcrumbBarItemClickedEventArgs.cs)

## Constructors

<a id="api-81aa1aeca210"></a>

### BreadcrumbBarItemClickedEventArgs

```csharp
public BreadcrumbBarItemClickedEventArgs(object? item, int index)
```

Event data for [ItemClicked](../Fluence.Wpf.Controls/BreadcrumbBar.md#api-8c3b462d4081).

**Remarks:** Initializes a new instance of the [BreadcrumbBarItemClickedEventArgs](BreadcrumbBarItemClickedEventArgs.md) class.

**Parameter `item`:** The data item of the clicked crumb, or the [BreadcrumbBarItem](../Fluence.Wpf.Controls/BreadcrumbBarItem.md) itself when it was added directly.

**Parameter `index`:** The zero-based position of the clicked crumb in the bar's items collection.

## Properties

<a id="api-1d2762c032f9"></a>

### Index

```csharp
public int Index { get; }
```

Gets the zero-based position of the clicked crumb in the bar's items collection.

<a id="api-845f111d7ff3"></a>

### Item

```csharp
public object? Item { get; }
```

Gets the data item of the clicked crumb, or the [BreadcrumbBarItem](../Fluence.Wpf.Controls/BreadcrumbBarItem.md) itself when it was added directly to the items collection.
