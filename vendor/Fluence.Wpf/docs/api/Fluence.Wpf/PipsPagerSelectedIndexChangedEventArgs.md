# PipsPagerSelectedIndexChangedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public sealed class PipsPagerSelectedIndexChangedEventArgs : EventArgs
```

Event data for [SelectedIndexChanged](../Fluence.Wpf.Controls/PipsPager.md#api-fba6089c3ce3).

**Remarks:** Initializes a new instance of the [PipsPagerSelectedIndexChangedEventArgs](PipsPagerSelectedIndexChangedEventArgs.md) class.

**Parameter `oldIndex`:** The zero-based page index that was selected before the change.

**Parameter `newIndex`:** The zero-based page index that is selected after the change.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/PipsPagerSelectedIndexChangedEventArgs.cs)

## Constructors

<a id="api-723366d44377"></a>

### PipsPagerSelectedIndexChangedEventArgs

```csharp
public PipsPagerSelectedIndexChangedEventArgs(int oldIndex, int newIndex)
```

Event data for [SelectedIndexChanged](../Fluence.Wpf.Controls/PipsPager.md#api-fba6089c3ce3).

**Remarks:** Initializes a new instance of the [PipsPagerSelectedIndexChangedEventArgs](PipsPagerSelectedIndexChangedEventArgs.md) class.

**Parameter `oldIndex`:** The zero-based page index that was selected before the change.

**Parameter `newIndex`:** The zero-based page index that is selected after the change.

## Properties

<a id="api-ba246132a93b"></a>

### NewIndex

```csharp
public int NewIndex { get; }
```

Gets the zero-based page index that is selected after the change.

<a id="api-638f42492bfe"></a>

### OldIndex

```csharp
public int OldIndex { get; }
```

Gets the zero-based page index that was selected before the change.
