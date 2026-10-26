# ToggleSplitButtonIsCheckedChangedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public sealed class ToggleSplitButtonIsCheckedChangedEventArgs : EventArgs
```

Event data for [IsCheckedChanged](../Fluence.Wpf.Controls/ToggleSplitButton.md#api-f1a492ed2bbd).

**Remarks:** Initializes a new instance of the [ToggleSplitButtonIsCheckedChangedEventArgs](ToggleSplitButtonIsCheckedChangedEventArgs.md) class.

**Parameter `isChecked`:** The checked state after the change.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/ToggleSplitButtonIsCheckedChangedEventArgs.cs)

## Constructors

<a id="api-7b5d83fd30eb"></a>

### ToggleSplitButtonIsCheckedChangedEventArgs

```csharp
public ToggleSplitButtonIsCheckedChangedEventArgs(bool isChecked)
```

Event data for [IsCheckedChanged](../Fluence.Wpf.Controls/ToggleSplitButton.md#api-f1a492ed2bbd).

**Remarks:** Initializes a new instance of the [ToggleSplitButtonIsCheckedChangedEventArgs](ToggleSplitButtonIsCheckedChangedEventArgs.md) class.

**Parameter `isChecked`:** The checked state after the change.

## Properties

<a id="api-6ffc0986df90"></a>

### IsChecked

```csharp
public bool IsChecked { get; }
```

Gets the checked state after the change. The state before the change is the inverse.
