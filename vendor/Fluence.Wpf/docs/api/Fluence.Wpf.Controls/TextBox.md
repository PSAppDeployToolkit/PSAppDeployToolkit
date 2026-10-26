# TextBox

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class TextBox : System.Windows.Controls.TextBox
```

A Fluent Design styled text box with placeholder, clear button, and icon support.

**Base type:** [`System.Windows.Controls.TextBox`](https://learn.microsoft.com/dotnet/api/system.windows.controls.textbox) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/TextBox.cs)

## Constructors

<a id="api-95b29fabc965"></a>

### TextBox

```csharp
public TextBox()
```

Initializes a new instance of the [TextBox](TextBox.md) class and wires text-changed handling.

## Properties

<a id="api-71b9cae3f6ec"></a>

### ClearButtonEnabled

```csharp
public bool ClearButtonEnabled { get; set; }
```

Gets or sets whether the clear button is shown when the text box has content and focus.

<a id="api-9da68d7baddb"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius of the text box.

<a id="api-b41dfc208822"></a>

### HelperText

```csharp
public string HelperText { get; set; }
```

Gets or sets the helper text displayed below the text box.

<a id="api-cc55970d9a1a"></a>

### Icon

```csharp
public object Icon { get; set; }
```

Gets or sets the icon displayed in the text box.

<a id="api-4cc43d1245f0"></a>

### IconPlacement

```csharp
public ElementPlacement IconPlacement { get; set; }
```

Gets or sets the placement of the icon relative to the text.

<a id="api-5f8ad7c87266"></a>

### PlaceholderEnabled

```csharp
public bool PlaceholderEnabled { get; set; }
```

Gets or sets whether the placeholder text is enabled.

<a id="api-1ac0dd676e75"></a>

### PlaceholderText

```csharp
public string PlaceholderText { get; set; }
```

Gets or sets the placeholder text displayed when the text box is empty.

<a id="api-074ba9892610"></a>

### ValidationMessage

```csharp
public string ValidationMessage { get; set; }
```

Gets or sets the validation message displayed when a validation state is active.

<a id="api-6869eb8f4c9b"></a>

### ValidationState

```csharp
public ValidationState ValidationState { get; set; }
```

Gets or sets the current validation state of the text box.

## Methods

<a id="api-674f42535a56"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`TextBox` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.textbox).

<a id="api-ea02fc6c911a"></a>

### OnPropertyChanged

```csharp
protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
```

Documentation inherited from the [`TextBox` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.textbox).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-b2b0d2174c3c"></a>

### ClearButtonEnabledProperty

```csharp
public static readonly DependencyProperty ClearButtonEnabledProperty
```

Identifies the [ClearButtonEnabled](TextBox.md#api-71b9cae3f6ec) dependency property.

<a id="api-0a844e920ff0"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](TextBox.md#api-9da68d7baddb) dependency property.

<a id="api-fd2951297aeb"></a>

### HelperTextProperty

```csharp
public static readonly DependencyProperty HelperTextProperty
```

Identifies the [HelperText](TextBox.md#api-b41dfc208822) dependency property.

<a id="api-e84e37184330"></a>

### IconPlacementProperty

```csharp
public static readonly DependencyProperty IconPlacementProperty
```

Identifies the [IconPlacement](TextBox.md#api-4cc43d1245f0) dependency property.

<a id="api-4de922a80fb0"></a>

### IconProperty

```csharp
public static readonly DependencyProperty IconProperty
```

Identifies the [Icon](TextBox.md#api-cc55970d9a1a) dependency property.

<a id="api-afe35254b5ad"></a>

### PlaceholderEnabledProperty

```csharp
public static readonly DependencyProperty PlaceholderEnabledProperty
```

Identifies the [PlaceholderEnabled](TextBox.md#api-5f8ad7c87266) dependency property.

<a id="api-6313aa172728"></a>

### PlaceholderTextProperty

```csharp
public static readonly DependencyProperty PlaceholderTextProperty
```

Identifies the [PlaceholderText](TextBox.md#api-1ac0dd676e75) dependency property.

<a id="api-5e53a55e40a7"></a>

### ValidationMessageProperty

```csharp
public static readonly DependencyProperty ValidationMessageProperty
```

Identifies the [ValidationMessage](TextBox.md#api-074ba9892610) dependency property.

<a id="api-455b6b1e5fd0"></a>

### ValidationStateProperty

```csharp
public static readonly DependencyProperty ValidationStateProperty
```

Identifies the [ValidationState](TextBox.md#api-6869eb8f4c9b) dependency property.

## Related types

- [Fluence.Wpf.ElementPlacement](../Fluence.Wpf/ElementPlacement.md)
- [Fluence.Wpf.ValidationState](../Fluence.Wpf/ValidationState.md)
