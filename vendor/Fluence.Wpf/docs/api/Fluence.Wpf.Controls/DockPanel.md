# DockPanel

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class DockPanel : Panel
```

Dock panel with uniform spacing between consecutive docked children.

**Base type:** [`Panel`](https://learn.microsoft.com/dotnet/api/system.windows.controls.panel) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/DockPanel.cs)

## Constructors

<a id="api-fbed11575184"></a>

### DockPanel

```csharp
public DockPanel()
```

Creates a new `DockPanel` instance.

## Properties

<a id="api-6c78de344821"></a>

### LastChildFill

```csharp
public bool LastChildFill { get; set; }
```

Gets or sets whether the last child element stretches to fill the remaining space.

<a id="api-4a8dc1fecc68"></a>

### Spacing

```csharp
public double Spacing { get; set; }
```

Gets or sets the uniform spacing between consecutive docked children.

## Methods

<a id="api-139ad58baa07"></a>

### ArrangeOverride

```csharp
protected override Size ArrangeOverride(Size finalSize)
```

Documentation inherited from the [`Panel` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.panel).

<a id="api-d645ccee7026"></a>

### MeasureOverride

```csharp
protected override Size MeasureOverride(Size availableSize)
```

Documentation inherited from the [`Panel` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.panel).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-3e17065a5296"></a>

### LastChildFillProperty

```csharp
public static readonly DependencyProperty LastChildFillProperty
```

Identifies the [LastChildFill](DockPanel.md#api-6c78de344821) dependency property.

<a id="api-7a7e6dc5a637"></a>

### SpacingProperty

```csharp
public static readonly DependencyProperty SpacingProperty
```

Identifies the [Spacing](DockPanel.md#api-4a8dc1fecc68) dependency property.
