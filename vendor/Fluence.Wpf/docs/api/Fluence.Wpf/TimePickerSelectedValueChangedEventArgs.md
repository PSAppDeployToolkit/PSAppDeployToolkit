# TimePickerSelectedValueChangedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public sealed class TimePickerSelectedValueChangedEventArgs : EventArgs
```

Event data for [SelectedTimeChanged](../Fluence.Wpf.Controls/TimePicker.md#api-7cc23c0fb9cd).

**Remarks:** Initializes a new instance of the [TimePickerSelectedValueChangedEventArgs](TimePickerSelectedValueChangedEventArgs.md) class.

**Parameter `oldTime`:** The previously selected time, or `null` when no time was set.

**Parameter `newTime`:** The newly selected time, or `null` when the time was cleared.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/TimePickerSelectedValueChangedEventArgs.cs)

## Constructors

<a id="api-e89b541f4dcb"></a>

### TimePickerSelectedValueChangedEventArgs

```csharp
public TimePickerSelectedValueChangedEventArgs(TimeSpan? oldTime, TimeSpan? newTime)
```

Event data for [SelectedTimeChanged](../Fluence.Wpf.Controls/TimePicker.md#api-7cc23c0fb9cd).

**Remarks:** Initializes a new instance of the [TimePickerSelectedValueChangedEventArgs](TimePickerSelectedValueChangedEventArgs.md) class.

**Parameter `oldTime`:** The previously selected time, or `null` when no time was set.

**Parameter `newTime`:** The newly selected time, or `null` when the time was cleared.

## Properties

<a id="api-201388830832"></a>

### NewTime

```csharp
public TimeSpan? NewTime { get; }
```

Gets the newly selected time, or `null` when the time was cleared.

<a id="api-ccd4037ca779"></a>

### OldTime

```csharp
public TimeSpan? OldTime { get; }
```

Gets the previously selected time, or `null` when no time was set.
