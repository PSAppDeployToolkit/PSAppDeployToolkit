# ComboBox

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class ComboBox : System.Windows.Controls.ComboBox
```

Fluent-styled combo box with placeholder, icon, and rounded dropdown. Authority: WinUI 3 ComboBox_themeresources.xaml (FocusedStates / EditableFocusedStates VSM groups - WI-3 C18). Diverging from stock WPF, this control auto-selects index 0 when its items populate while `SelectedIndex` is still -1 and has never been explicitly set.

**Base type:** [`System.Windows.Controls.ComboBox`](https://learn.microsoft.com/dotnet/api/system.windows.controls.combobox) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/ComboBox.cs)

## Constructors

<a id="api-7070fa3bd3fb"></a>

### ComboBox

```csharp
public ComboBox()
```

Creates a new `ComboBox` instance.

## Properties

<a id="api-decc10379b26"></a>

### CornerRadius

```csharp
public CornerRadius CornerRadius { get; set; }
```

Gets or sets the corner radius of the combo box.

<a id="api-32919fecc6c8"></a>

### DropdownCornerRadius

```csharp
public CornerRadius DropdownCornerRadius { get; set; }
```

Gets or sets the corner radius of the dropdown popup.

<a id="api-e5c5d8b96cfa"></a>

### Icon

```csharp
public object Icon { get; set; }
```

Gets or sets the icon displayed in the combo box.

<a id="api-28ef72a53aa9"></a>

### IsDropDownOpenedUpward

```csharp
public bool IsDropDownOpenedUpward { get; }
```

Gets whether the dropdown is currently displayed above the control.

<a id="api-27741e89321f"></a>

### PlaceholderText

```csharp
public string PlaceholderText { get; set; }
```

Gets or sets the placeholder text displayed when no item is selected.

<a id="api-3249d5aa1bf5"></a>

### SelectedContent

```csharp
public object SelectedContent { get; }
```

Gets the content of the currently selected item.

<a id="api-0c35385c9734"></a>

### SelectedText

```csharp
public string SelectedText { get; }
```

Gets the text representation of the currently selected item.

## Methods

<a id="api-89b6acd68976"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`ComboBox` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.combobox).

<a id="api-67dae2f04b71"></a>

### OnDropDownClosed

```csharp
protected override void OnDropDownClosed(EventArgs e)
```

Documentation inherited from the [`ComboBox` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.combobox).

<a id="api-05199489d1aa"></a>

### OnDropDownOpened

```csharp
protected override void OnDropDownOpened(EventArgs e)
```

Documentation inherited from the [`ComboBox` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.combobox).

<a id="api-1712b54312fd"></a>

### OnGotFocus

```csharp
protected override void OnGotFocus(RoutedEventArgs e)
```

Documentation inherited from the [`ComboBox` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.combobox).

<a id="api-34e2186fe5b5"></a>

### OnItemsChanged

```csharp
protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
```

Documentation inherited from the [`ComboBox` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.combobox).

<a id="api-209f78b09824"></a>

### OnLostFocus

```csharp
protected override void OnLostFocus(RoutedEventArgs e)
```

Documentation inherited from the [`ComboBox` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.combobox).

<a id="api-df703704eaa3"></a>

### OnSelectionChanged

```csharp
protected override void OnSelectionChanged(SelectionChangedEventArgs e)
```

Documentation inherited from the [`ComboBox` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.combobox).

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-ec26a41fbdf7"></a>

### CornerRadiusProperty

```csharp
public static readonly DependencyProperty CornerRadiusProperty
```

Identifies the [CornerRadius](ComboBox.md#api-decc10379b26) dependency property.

<a id="api-9c107dfb485e"></a>

### DropdownCornerRadiusProperty

```csharp
public static readonly DependencyProperty DropdownCornerRadiusProperty
```

Identifies the [DropdownCornerRadius](ComboBox.md#api-32919fecc6c8) dependency property.

<a id="api-711949ee789e"></a>

### IconProperty

```csharp
public static readonly DependencyProperty IconProperty
```

Identifies the [Icon](ComboBox.md#api-e5c5d8b96cfa) dependency property.

<a id="api-e4ca57d06e01"></a>

### IsDropDownOpenedUpwardProperty

```csharp
public static readonly DependencyProperty IsDropDownOpenedUpwardProperty
```

Identifies the [IsDropDownOpenedUpward](ComboBox.md#api-28ef72a53aa9) dependency property.

<a id="api-3de4fca4d317"></a>

### PlaceholderTextProperty

```csharp
public static readonly DependencyProperty PlaceholderTextProperty
```

Identifies the [PlaceholderText](ComboBox.md#api-27741e89321f) dependency property.

<a id="api-49b07995c95e"></a>

### SelectedContentProperty

```csharp
public static readonly DependencyProperty SelectedContentProperty
```

Identifies the [SelectedContent](ComboBox.md#api-3249d5aa1bf5) dependency property.

<a id="api-94dd7f82b18e"></a>

### SelectedTextProperty

```csharp
public static readonly DependencyProperty SelectedTextProperty
```

Identifies the [SelectedText](ComboBox.md#api-0c35385c9734) dependency property.
