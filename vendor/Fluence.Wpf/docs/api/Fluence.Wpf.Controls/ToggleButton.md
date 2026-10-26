# ToggleButton

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class ToggleButton : System.Windows.Controls.Primitives.ToggleButton
```

A Fluent Design styled ToggleButton with accent checked state.

**Base type:** [`System.Windows.Controls.Primitives.ToggleButton`](https://learn.microsoft.com/dotnet/api/system.windows.controls.primitives.togglebutton) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/ToggleButton.cs)

## Constructors

<a id="api-b1153c44cad6"></a>

### ToggleButton

```csharp
public ToggleButton()
```

Creates a new `ToggleButton` instance.

## Properties

<a id="api-93d4f2fb8236"></a>

### Appearance

```csharp
public ControlAppearance Appearance { get; set; }
```

Gets or sets the visual appearance of the toggle button.

**Remarks:** The default template renders the single canonical WinUI toggle visual for every [ControlAppearance](../Fluence.Wpf/ControlAppearance.md) value: the checked state is the accent state, so a separate accent rest variant would make checked and unchecked indistinguishable. The property exists primarily for derived controls such as [DropDownButton](DropDownButton.md), which consume it in their own templates.

<a id="api-b4313262acd5"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius of the toggle button.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-f28d3fe44152"></a>

### AppearanceProperty

```csharp
public static readonly DependencyProperty AppearanceProperty
```

Identifies the [Appearance](ToggleButton.md#api-93d4f2fb8236) dependency property.

<a id="api-a8884ed6cbda"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](ToggleButton.md#api-b4313262acd5) dependency property.

## Related types

- [Fluence.Wpf.ControlAppearance](../Fluence.Wpf/ControlAppearance.md)
