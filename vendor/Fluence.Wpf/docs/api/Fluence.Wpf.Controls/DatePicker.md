# DatePicker

[C# API](../index.md) / [Fluence.Wpf.Controls](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf.Controls`

```csharp
public class DatePicker : Control
```

A control that lets the user pick a calendar date from day, month, and year selector columns hosted in a light-dismiss flyout, mirroring the WinUI 3 `DatePicker`. The always-visible field is a button-styled row showing the selected day, month name, and year ordered by the current culture's short date pattern; the flyout commits the pending column selection through its accept button and discards it through cancel.

**Base type:** [`Control`](https://learn.microsoft.com/dotnet/api/system.windows.controls.control) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/Controls/DatePicker.cs)

## Constructors

<a id="api-407681817b5f"></a>

### DatePicker

```csharp
public DatePicker()
```

Creates a new `DatePicker` instance.

## Properties

<a id="api-b96a0a7f952a"></a>

### DayVisible

```csharp
public bool DayVisible { get; set; }
```

Gets or sets whether the day segment and selector column are shown.

<a id="api-590cec883142"></a>

### Header

```csharp
public object? Header { get; set; }
```

Gets or sets the optional header content shown above the field.

<a id="api-8cc8f9bdb8a5"></a>

### MaxYear

```csharp
public int MaxYear { get; set; }
```

Gets or sets the last year offered by the year selector column.

<a id="api-3aab5619ad86"></a>

### MinYear

```csharp
public int MinYear { get; set; }
```

Gets or sets the first year offered by the year selector column.

<a id="api-3fbdc78ae102"></a>

### MonthVisible

```csharp
public bool MonthVisible { get; set; }
```

Gets or sets whether the month segment and selector column are shown.

<a id="api-10c873bde915"></a>

### PlaceholderText

```csharp
public string PlaceholderText { get; set; }
```

Gets or sets the placeholder text shown in the field while [SelectedDate](DatePicker.md#api-4d06d707829c) is `null` (for example "Pick a date").

<a id="api-4d06d707829c"></a>

### SelectedDate

```csharp
public DateTime? SelectedDate { get; set; }
```

Gets or sets the currently selected date, or `null` when no date has been picked yet. Changing this property raises [SelectedDateChanged](DatePicker.md#api-c79da806c4f8).

<a id="api-b5cddf665508"></a>

### YearVisible

```csharp
public bool YearVisible { get; set; }
```

Gets or sets whether the year segment and selector column are shown.

## Methods

<a id="api-8c058f749225"></a>

### OnApplyTemplate

```csharp
public override void OnApplyTemplate()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

<a id="api-53d28fd435ac"></a>

### OnCreateAutomationPeer

```csharp
protected override AutomationPeer OnCreateAutomationPeer()
```

Documentation inherited from the [`Control` API](https://learn.microsoft.com/dotnet/api/system.windows.controls.control).

## Events

<a id="api-c79da806c4f8"></a>

### SelectedDateChanged

```csharp
public event EventHandler<DatePickerSelectedValueChangedEventArgs>? SelectedDateChanged
```

Occurs after [SelectedDate](DatePicker.md#api-4d06d707829c) changes, whether through the flyout's accept button or a programmatic update.

## Dependency property identifiers

Use these identifiers with WPF property APIs. The matching CLR properties appear above when the control exposes them.

<a id="api-23b16cdef55d"></a>

### DayVisibleProperty

```csharp
public static readonly DependencyProperty DayVisibleProperty
```

Identifies the [DayVisible](DatePicker.md#api-b96a0a7f952a) dependency property.

<a id="api-358f148b65fd"></a>

### HeaderProperty

```csharp
public static readonly DependencyProperty HeaderProperty
```

Identifies the [Header](DatePicker.md#api-590cec883142) dependency property.

<a id="api-6a5133627c98"></a>

### MaxYearProperty

```csharp
public static readonly DependencyProperty MaxYearProperty
```

Identifies the [MaxYear](DatePicker.md#api-8cc8f9bdb8a5) dependency property.

<a id="api-e13777266889"></a>

### MinYearProperty

```csharp
public static readonly DependencyProperty MinYearProperty
```

Identifies the [MinYear](DatePicker.md#api-3aab5619ad86) dependency property.

<a id="api-303c5745e9e5"></a>

### MonthVisibleProperty

```csharp
public static readonly DependencyProperty MonthVisibleProperty
```

Identifies the [MonthVisible](DatePicker.md#api-3fbdc78ae102) dependency property.

<a id="api-123cb257dddd"></a>

### PlaceholderTextProperty

```csharp
public static readonly DependencyProperty PlaceholderTextProperty
```

Identifies the [PlaceholderText](DatePicker.md#api-10c873bde915) dependency property.

<a id="api-31897142fa86"></a>

### SelectedDateProperty

```csharp
public static readonly DependencyProperty SelectedDateProperty
```

Identifies the [SelectedDate](DatePicker.md#api-4d06d707829c) dependency property.

<a id="api-ce14460e12d9"></a>

### YearVisibleProperty

```csharp
public static readonly DependencyProperty YearVisibleProperty
```

Identifies the [YearVisible](DatePicker.md#api-b5cddf665508) dependency property.

## Related types

- [Fluence.Wpf.DatePickerSelectedValueChangedEventArgs](../Fluence.Wpf/DatePickerSelectedValueChangedEventArgs.md)
