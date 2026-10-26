# Button

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class Button : System.Windows.Controls.Button
```

A Fluent Design styled button with multiple appearance modes.

**Base type:** [`System.Windows.Controls.Button`](https://learn.microsoft.com/dotnet/api/system.windows.controls.button) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/Button.cs)

## Constructors

<a id="api-77bdb7c93e5a"></a>

### Button

```csharp
public Button()
```

Creates a new `Button` instance.

## Properties

<a id="api-beed0ae42ce5"></a>

### Appearance

```csharp
public ControlAppearance Appearance { get; set; }
```

Gets or sets the visual appearance of the button.

<a id="api-b0f399b30346"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius of the button.

<a id="api-0324eab57026"></a>

### Icon

```csharp
public object Icon { get; set; }
```

Gets or sets the icon displayed in the button.

<a id="api-d40448014521"></a>

### IconPlacement

```csharp
public ElementPlacement IconPlacement { get; set; }
```

Gets or sets the placement of the icon relative to the content.

## Methods

<a id="api-0d3bfe0dca7a"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`Button` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.button).

<a id="api-8b613358b93a"></a>

### OnContentChanged

```csharp
protected override void OnContentChanged(object oldContent, object newContent)
```

Documentation inherited from the [`Button` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.button).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-804a9e8ce6b7"></a>

### AppearanceProperty

```csharp
public static readonly DependencyProperty AppearanceProperty
```

Identifies the [Appearance](Button.md#api-beed0ae42ce5) dependency property.

<a id="api-7e0015327c8f"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](Button.md#api-b0f399b30346) dependency property.

<a id="api-8569efe6c511"></a>

### IconPlacementProperty

```csharp
public static readonly DependencyProperty IconPlacementProperty
```

Identifies the [IconPlacement](Button.md#api-d40448014521) dependency property.

<a id="api-4cb65a2d265f"></a>

### IconProperty

```csharp
public static readonly DependencyProperty IconProperty
```

Identifies the [Icon](Button.md#api-0324eab57026) dependency property.

## Related types

- [Fluence.Wpf.ControlAppearance](../Fluence.Wpf/ControlAppearance.md)
- [Fluence.Wpf.ElementPlacement](../Fluence.Wpf/ElementPlacement.md)
