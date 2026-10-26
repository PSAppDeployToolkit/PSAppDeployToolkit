# StackPanel

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class StackPanel : System.Windows.Controls.StackPanel
```

Stack panel with uniform spacing between children.

**Base type:** [`System.Windows.Controls.StackPanel`](https://learn.microsoft.com/dotnet/api/system.windows.controls.stackpanel) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/StackPanel.cs)

## Constructors

<a id="api-98b97540c655"></a>

### StackPanel

```csharp
public StackPanel()
```

Creates a new `StackPanel` instance.

## Properties

<a id="api-0b338d684ffc"></a>

### Spacing

```csharp
public double Spacing { get; set; }
```

Gets or sets the uniform spacing between children.

## Methods

<a id="api-e352f999aa31"></a>

### ArrangeOverride

```csharp
protected override Size ArrangeOverride(Size arrangeSize)
```

Documentation inherited from the [`StackPanel` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.stackpanel).

<a id="api-01c1258e60e5"></a>

### MeasureOverride

```csharp
protected override Size MeasureOverride(Size constraint)
```

Documentation inherited from the [`StackPanel` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.stackpanel).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-83fcf9d03fc9"></a>

### SpacingProperty

```csharp
public static readonly DependencyProperty SpacingProperty
```

Identifies the [Spacing](StackPanel.md#api-0b338d684ffc) dependency property.
