# ListBox

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class ListBox : System.Windows.Controls.ListBox
```

A Fluent Design styled ListBox with rounded corners and selection indicator.

**Base type:** [`System.Windows.Controls.ListBox`](https://learn.microsoft.com/dotnet/api/system.windows.controls.listbox) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/ListBox.cs)

## Constructors

<a id="api-49265258c5c6"></a>

### ListBox

```csharp
public ListBox()
```

Creates a new `ListBox` instance.

## Properties

<a id="api-f49a756d8dd4"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius of the ListBox border.

## Methods

<a id="api-7a2cb74615ae"></a>

### GetContainerForItemOverride

```csharp
protected override DependencyObject GetContainerForItemOverride()
```

Documentation inherited from the [`ListBox` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.listbox).

<a id="api-030a1f4af76c"></a>

### IsItemItsOwnContainerOverride

```csharp
protected override bool IsItemItsOwnContainerOverride(object item)
```

Documentation inherited from the [`ListBox` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.listbox).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-5ee53c47cf22"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](ListBox.md#api-f49a756d8dd4) dependency property.
