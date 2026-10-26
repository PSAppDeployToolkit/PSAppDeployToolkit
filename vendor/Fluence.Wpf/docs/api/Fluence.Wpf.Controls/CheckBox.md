# CheckBox

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class CheckBox : System.Windows.Controls.CheckBox
```

Fluent-styled check box with optional description and rounded indicator.

**Base type:** [`System.Windows.Controls.CheckBox`](https://learn.microsoft.com/dotnet/api/system.windows.controls.checkbox) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/CheckBox.cs)

## Constructors

<a id="api-f57e175ed9db"></a>

### CheckBox

```csharp
public CheckBox()
```

Initializes the template-part motion lifetime.

## Properties

<a id="api-170629d7acad"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius of the check box indicator.

<a id="api-eee7a0923205"></a>

### Description

```csharp
public string? Description { get; set; }
```

Gets or sets the description text displayed below the check box content.

## Methods

<a id="api-2639a55fc853"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`CheckBox` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.checkbox).

<a id="api-3b0d76cbf02e"></a>

### OnPropertyChanged

```csharp
protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
```

Documentation inherited from the [`CheckBox` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.checkbox).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-995a0be6583c"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](CheckBox.md#api-170629d7acad) dependency property.

<a id="api-5dffa8a0947f"></a>

### DescriptionProperty

```csharp
public static readonly DependencyProperty DescriptionProperty
```

Identifies the [Description](CheckBox.md#api-eee7a0923205) dependency property.
