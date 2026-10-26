# SmoothScrollViewer

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class SmoothScrollViewer : ScrollViewer
```

A scroll viewer that animates scrolling with easing for a smooth experience.

**Base type:** [`ScrollViewer`](https://learn.microsoft.com/dotnet/api/system.windows.controls.scrollviewer) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/SmoothScrollViewer.cs)

## Constructors

<a id="api-83113c103db9"></a>

### SmoothScrollViewer

```csharp
public SmoothScrollViewer()
```

Creates a new `SmoothScrollViewer` instance.

## Properties

<a id="api-a91ec872c19a"></a>

### ScrollDuration

```csharp
public Duration ScrollDuration { get; set; }
```

Gets or sets the duration of the smooth scroll animation.

## Methods

<a id="api-2a1f0af8c5a9"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`ScrollViewer` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.scrollviewer).

<a id="api-345906bc932e"></a>

### OnMouseWheel

```csharp
protected override void OnMouseWheel(MouseWheelEventArgs e)
```

Documentation inherited from the [`ScrollViewer` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.scrollviewer).

<a id="api-ab15e355f913"></a>

### OnScrollChanged

```csharp
protected override void OnScrollChanged(ScrollChangedEventArgs e)
```

Documentation inherited from the [`ScrollViewer` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.scrollviewer).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-ddd9cbb400b8"></a>

### ScrollDurationProperty

```csharp
public static readonly DependencyProperty ScrollDurationProperty
```

Identifies the [ScrollDuration](SmoothScrollViewer.md#api-a91ec872c19a) dependency property.
