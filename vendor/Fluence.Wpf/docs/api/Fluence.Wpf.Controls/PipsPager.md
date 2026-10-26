# PipsPager

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class PipsPager : Control
```

A page indicator mirroring the WinUI 3 `PipsPager`: a horizontal or vertical run of round pip dots, one per visible page, with the selected pip rendered larger in the accent fill. Clicking a pip selects its page, optional previous/next chevron buttons step the selection, and arrow keys move the selection while keyboard focus is inside the pager.

**Remarks:** One pip per page is generated in code into the `PART_PipsHost` panel (the same approach as [RatingControl](RatingControl.md)) and the whole run is hosted in the `PART_PipsScrollViewer` viewport. When [NumberOfPages](PipsPager.md#api-c6dd66ea485e) exceeds [MaxVisiblePips](PipsPager.md#api-bde6c259b7ec) the viewport is clamped to [MaxVisiblePips](PipsPager.md#api-bde6c259b7ec) pip boxes along the orientation axis and stays put while the selection moves inside it, scrolling only far enough to bring a selection that has left the viewport back to the nearest edge. Subscribe to [SelectedIndexChanged](PipsPager.md#api-fba6089c3ce3) to react to selection moves from any input path.



Pips are realized eagerly rather than virtualized, so a pager is meant for the page counts a page indicator is readable at, not for thousands of pages. One WinUI behavior remains a deliberate omission: the scale-down of the pips at the viewport edges. Navigation buttons in [VisibleOnPointerOver](../Fluence.Wpf/PipsPagerButtonVisibility.md#api-3708244955ac) mode collapse when the pointer leaves, so the pager's desired size changes with hover.

**Base type:** [`Control`](https://learn.microsoft.com/dotnet/api/system.windows.controls.control) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/PipsPager.cs)

## Constructors

<a id="api-90681dac567e"></a>

### PipsPager

```csharp
public PipsPager()
```

Creates a new `PipsPager` instance.

## Properties

<a id="api-bde6c259b7ec"></a>

### MaxVisiblePips

```csharp
public int MaxVisiblePips { get; set; }
```

Gets or sets the maximum number of pips visible at once. Every page still gets a pip; when [NumberOfPages](PipsPager.md#api-c6dd66ea485e) exceeds this count the pip run scrolls inside a viewport this many pips long. Values below 1 coerce to 1. Default is 5, matching WinUI.

<a id="api-be0f12c672fc"></a>

### NextButtonVisibility

```csharp
public PipsPagerButtonVisibility NextButtonVisibility { get; set; }
```

Gets or sets when the next-page chevron button is shown. The button is disabled while the last page is selected. Default is [Collapsed](../Fluence.Wpf/PipsPagerButtonVisibility.md#api-f8b11852cb9f), matching WinUI.

<a id="api-c6dd66ea485e"></a>

### NumberOfPages

```csharp
public int NumberOfPages { get; set; }
```

Gets or sets the total number of pages represented by the pager. Negative values coerce to 0. Default is 0 (no pips).

<a id="api-1f2c61eff30d"></a>

### Orientation

```csharp
public Orientation Orientation { get; set; }
```

Gets or sets whether the pips flow horizontally or vertically. The default template also swaps the navigation chevrons between left/right and up/down to match. Default is `Horizontal`.

<a id="api-42180a1a07a1"></a>

### PreviousButtonVisibility

```csharp
public PipsPagerButtonVisibility PreviousButtonVisibility { get; set; }
```

Gets or sets when the previous-page chevron button is shown. The button is disabled while the first page is selected. Default is [Collapsed](../Fluence.Wpf/PipsPagerButtonVisibility.md#api-f8b11852cb9f), matching WinUI.

<a id="api-7f88b65e5895"></a>

### SelectedPageIndex

```csharp
public int SelectedPageIndex { get; set; }
```

Gets or sets the zero-based index of the selected page. Values coerce into [0, [NumberOfPages](PipsPager.md#api-c6dd66ea485e) - 1], and to 0 while the pager has no pages. Binds two-way by default.

## Methods

<a id="api-f2dfd58075f0"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-7bafb6a40e68"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-4219d8591614"></a>

### OnKeyDown

```csharp
protected override void OnKeyDown(KeyEventArgs e)
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

## Events

<a id="api-fba6089c3ce3"></a>

### SelectedIndexChanged

```csharp
public event EventHandler<PipsPagerSelectedIndexChangedEventArgs>? SelectedIndexChanged
```

Occurs after [SelectedPageIndex](PipsPager.md#api-7f88b65e5895) has changed from any input path (pip click, navigation buttons, arrow keys, or a programmatic set). The event args carry the previous and the new zero-based page index.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-2a992a0bfa72"></a>

### MaxVisiblePipsProperty

```csharp
public static readonly DependencyProperty MaxVisiblePipsProperty
```

Identifies the [MaxVisiblePips](PipsPager.md#api-bde6c259b7ec) dependency property.

<a id="api-4683ad76dba1"></a>

### NextButtonVisibilityProperty

```csharp
public static readonly DependencyProperty NextButtonVisibilityProperty
```

Identifies the [NextButtonVisibility](PipsPager.md#api-be0f12c672fc) dependency property.

<a id="api-04e1c7004ec9"></a>

### NumberOfPagesProperty

```csharp
public static readonly DependencyProperty NumberOfPagesProperty
```

Identifies the [NumberOfPages](PipsPager.md#api-c6dd66ea485e) dependency property.

<a id="api-833e7808b048"></a>

### OrientationProperty

```csharp
public static readonly DependencyProperty OrientationProperty
```

Identifies the [Orientation](PipsPager.md#api-1f2c61eff30d) dependency property.

<a id="api-128ccf47d509"></a>

### PreviousButtonVisibilityProperty

```csharp
public static readonly DependencyProperty PreviousButtonVisibilityProperty
```

Identifies the [PreviousButtonVisibility](PipsPager.md#api-42180a1a07a1) dependency property.

<a id="api-a2a29864de0c"></a>

### SelectedPageIndexProperty

```csharp
public static readonly DependencyProperty SelectedPageIndexProperty
```

Identifies the [SelectedPageIndex](PipsPager.md#api-7f88b65e5895) dependency property.

## Related types

- [Fluence.Wpf.PipsPagerButtonVisibility](../Fluence.Wpf/PipsPagerButtonVisibility.md)
- [Fluence.Wpf.PipsPagerSelectedIndexChangedEventArgs](../Fluence.Wpf/PipsPagerSelectedIndexChangedEventArgs.md)
