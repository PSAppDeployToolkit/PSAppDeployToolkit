# RadioButton

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class RadioButton : System.Windows.Controls.RadioButton
```

A Fluent Design styled radio button with optional description text.

**Remarks:** Inspired by WInUI's RadioButton.

**Base type:** [`System.Windows.Controls.RadioButton`](https://learn.microsoft.com/dotnet/api/system.windows.controls.radiobutton) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/RadioButton.cs)

## Constructors

<a id="api-2fc33dee6dab"></a>

### RadioButton

```csharp
public RadioButton()
```

Initializes a radio button and its template-part motion lifetime.

## Properties

<a id="api-0cac938d383e"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius. Defaults to 10 for a round indicator.

<a id="api-fe759b4aaf32"></a>

### Description

```csharp
public string? Description { get; set; }
```

Gets or sets the description text displayed below the radio button content.

## Methods

<a id="api-30b723d166b7"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`RadioButton` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.radiobutton).

<a id="api-8f180480f04f"></a>

### OnPropertyChanged

```csharp
protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
```

Documentation inherited from the [`RadioButton` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.radiobutton).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-13c43d91967e"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](RadioButton.md#api-0cac938d383e) dependency property.

<a id="api-3edfc35f1234"></a>

### DescriptionProperty

```csharp
public static readonly DependencyProperty DescriptionProperty
```

Identifies the [Description](RadioButton.md#api-fe759b4aaf32) dependency property.
