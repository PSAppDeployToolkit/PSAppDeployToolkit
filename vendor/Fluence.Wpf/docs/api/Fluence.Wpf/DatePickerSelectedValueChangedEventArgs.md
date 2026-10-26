# DatePickerSelectedValueChangedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public sealed class DatePickerSelectedValueChangedEventArgs : EventArgs
```

Event data for [SelectedDateChanged](../Fluence.Wpf.Controls/DatePicker.md#api-c79da806c4f8).

**Remarks:** Initializes a new instance of the [DatePickerSelectedValueChangedEventArgs](DatePickerSelectedValueChangedEventArgs.md) class.

**Parameter `oldDate`:** The previously selected date, or `null` when no date was set.

**Parameter `newDate`:** The newly selected date, or `null` when the date was cleared.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/DatePickerSelectedValueChangedEventArgs.cs)

## Constructors

<a id="api-439cf856d81d"></a>

### DatePickerSelectedValueChangedEventArgs

```csharp
public DatePickerSelectedValueChangedEventArgs(DateTime? oldDate, DateTime? newDate)
```

Event data for [SelectedDateChanged](../Fluence.Wpf.Controls/DatePicker.md#api-c79da806c4f8).

**Remarks:** Initializes a new instance of the [DatePickerSelectedValueChangedEventArgs](DatePickerSelectedValueChangedEventArgs.md) class.

**Parameter `oldDate`:** The previously selected date, or `null` when no date was set.

**Parameter `newDate`:** The newly selected date, or `null` when the date was cleared.

## Properties

<a id="api-c464ee01cf73"></a>

### NewDate

```csharp
public DateTime? NewDate { get; }
```

Gets the newly selected date, or `null` when the date was cleared.

<a id="api-01739adc1e3a"></a>

### OldDate

```csharp
public DateTime? OldDate { get; }
```

Gets the previously selected date, or `null` when no date was set.
