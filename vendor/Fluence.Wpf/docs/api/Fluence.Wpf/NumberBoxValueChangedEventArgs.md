# NumberBoxValueChangedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public sealed class NumberBoxValueChangedEventArgs : EventArgs
```

Event data for [ValueChanged](../Fluence.Wpf.Controls/NumberBox.md#api-339ec366940c).

**Remarks:** Initializes a new instance of the [NumberBoxValueChangedEventArgs](NumberBoxValueChangedEventArgs.md) class.

**Parameter `oldValue`:** The previous value.

**Parameter `newValue`:** The new value.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/NumberBoxValueChangedEventArgs.cs)

## Constructors

<a id="api-40f154fac925"></a>

### NumberBoxValueChangedEventArgs

```csharp
public NumberBoxValueChangedEventArgs(double oldValue, double newValue)
```

Event data for [ValueChanged](../Fluence.Wpf.Controls/NumberBox.md#api-339ec366940c).

**Remarks:** Initializes a new instance of the [NumberBoxValueChangedEventArgs](NumberBoxValueChangedEventArgs.md) class.

**Parameter `oldValue`:** The previous value.

**Parameter `newValue`:** The new value.

## Properties

<a id="api-ab17ad940e42"></a>

### NewValue

```csharp
public double NewValue { get; }
```

Gets the new value.

<a id="api-5076850c0da1"></a>

### OldValue

```csharp
public double OldValue { get; }
```

Gets the previous value.
