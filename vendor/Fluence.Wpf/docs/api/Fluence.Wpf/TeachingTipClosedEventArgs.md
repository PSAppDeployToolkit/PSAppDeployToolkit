# TeachingTipClosedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public sealed class TeachingTipClosedEventArgs : EventArgs
```

Provides data for the [Closed](../Fluence.Wpf.Controls/TeachingTip.md#api-125b7dee370c) event.

**Remarks:** Initializes a new instance of the [TeachingTipClosedEventArgs](TeachingTipClosedEventArgs.md) class.

**Parameter `reason`:** Why the teaching tip closed.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/TeachingTipClosedEventArgs.cs)

## Constructors

<a id="api-2bca4addae40"></a>

### TeachingTipClosedEventArgs

```csharp
public TeachingTipClosedEventArgs(TeachingTipCloseReason reason)
```

Provides data for the [Closed](../Fluence.Wpf.Controls/TeachingTip.md#api-125b7dee370c) event.

**Remarks:** Initializes a new instance of the [TeachingTipClosedEventArgs](TeachingTipClosedEventArgs.md) class.

**Parameter `reason`:** Why the teaching tip closed.

## Properties

<a id="api-d38c229d5c34"></a>

### Reason

```csharp
public TeachingTipCloseReason Reason { get; }
```

Gets the reason the teaching tip closed.

## Related types

- [Fluence.Wpf.TeachingTipCloseReason](TeachingTipCloseReason.md)
