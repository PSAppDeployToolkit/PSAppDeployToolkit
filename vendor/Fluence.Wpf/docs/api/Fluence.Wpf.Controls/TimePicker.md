# TimePicker

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class TimePicker : Control
```

A control that lets the user pick a time of day from hour, minute, and (in 12-hour mode) AM/PM selector columns hosted in a light-dismiss flyout, mirroring the WinUI 3 `TimePicker`. The always-visible field is a button-styled row showing the selected hour, two-digit minute, and culture AM/PM designator; the flyout commits the pending column selection through its accept button and discards it through cancel.

**Base type:** [`Control`](https://learn.microsoft.com/dotnet/api/system.windows.controls.control) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/TimePicker.cs)

## Constructors

<a id="api-4f6c5143e753"></a>

### TimePicker

```csharp
public TimePicker()
```

Creates a new `TimePicker` instance.

## Properties

<a id="api-556a7f79619a"></a>

### ClockIdentifier

```csharp
public string ClockIdentifier { get; set; }
```

Gets or sets the clock system used by the field and the hour column: "12HourClock" (hours 1..12 plus an AM/PM designator column) or "24HourClock" (hours 0..23, no designator column). Any other value is coerced back to "12HourClock". The default follows the user's regional clock: "24HourClock" when the current culture's short time pattern uses the 24-hour 'H' specifier, otherwise "12HourClock". An explicitly set value always wins over the regional default.

<a id="api-c93dd8a78ada"></a>

### Header

```csharp
public object? Header { get; set; }
```

Gets or sets the optional header content shown above the field.

<a id="api-6b4f3a1685ef"></a>

### MinuteIncrement

```csharp
public int MinuteIncrement { get; set; }
```

Gets or sets the step between the offered minute values (for example 15 offers 00, 15, 30, and 45). Values are clamped into 1..59.

<a id="api-02164da3aede"></a>

### PlaceholderText

```csharp
public string PlaceholderText { get; set; }
```

Gets or sets the placeholder text shown in the field while [SelectedTime](TimePicker.md#api-c44ee1688947) is `null` (for example "Pick a time").

<a id="api-c44ee1688947"></a>

### SelectedTime

```csharp
public TimeSpan? SelectedTime { get; set; }
```

Gets or sets the currently selected time of day, or `null` when no time has been picked yet. Changing this property raises [SelectedTimeChanged](TimePicker.md#api-7cc23c0fb9cd).

## Methods

<a id="api-94c1b23a975d"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-15247621f4a3"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

## Events

<a id="api-7cc23c0fb9cd"></a>

### SelectedTimeChanged

```csharp
public event EventHandler<TimePickerSelectedValueChangedEventArgs>? SelectedTimeChanged
```

Occurs after [SelectedTime](TimePicker.md#api-c44ee1688947) changes, whether through the flyout's accept button or a programmatic update.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-354513d42584"></a>

### ClockIdentifierProperty

```csharp
public static readonly DependencyProperty ClockIdentifierProperty
```

Identifies the [ClockIdentifier](TimePicker.md#api-556a7f79619a) dependency property.

<a id="api-a8f70e30eb7d"></a>

### HeaderProperty

```csharp
public static readonly DependencyProperty HeaderProperty
```

Identifies the [Header](TimePicker.md#api-c93dd8a78ada) dependency property.

<a id="api-8a043e0c1c51"></a>

### MinuteIncrementProperty

```csharp
public static readonly DependencyProperty MinuteIncrementProperty
```

Identifies the [MinuteIncrement](TimePicker.md#api-6b4f3a1685ef) dependency property.

<a id="api-947ed6b7d0a2"></a>

### PlaceholderTextProperty

```csharp
public static readonly DependencyProperty PlaceholderTextProperty
```

Identifies the [PlaceholderText](TimePicker.md#api-02164da3aede) dependency property.

<a id="api-8bac0697189b"></a>

### SelectedTimeProperty

```csharp
public static readonly DependencyProperty SelectedTimeProperty
```

Identifies the [SelectedTime](TimePicker.md#api-c44ee1688947) dependency property.

## Related types

- [Fluence.Wpf.TimePickerSelectedValueChangedEventArgs](../Fluence.Wpf/TimePickerSelectedValueChangedEventArgs.md)
