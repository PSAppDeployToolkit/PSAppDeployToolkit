# Border

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class Border : System.Windows.Controls.Border
```

Border with optional Fluent visual presets applied via theme resources.

**Base type:** [`System.Windows.Controls.Border`](https://learn.microsoft.com/dotnet/api/system.windows.controls.border) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/Border.cs)

## Constructors

<a id="api-e6f992616769"></a>

### Border

```csharp
public Border()
```

Creates a new `Border` instance.

## Properties

<a id="api-bc5007b85a8e"></a>

### Variant

```csharp
public BorderVariant Variant { get; set; }
```

Gets or sets the visual preset variant (None, Card, Subtle, Divider).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-148ad2bbdff1"></a>

### VariantProperty

```csharp
public static readonly DependencyProperty VariantProperty
```

Identifies the [Variant](Border.md#api-bc5007b85a8e) dependency property.

## Related types

- [Fluence.Wpf.BorderVariant](../Fluence.Wpf/BorderVariant.md)
