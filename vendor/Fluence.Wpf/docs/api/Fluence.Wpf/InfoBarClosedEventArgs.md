# InfoBarClosedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public sealed class InfoBarClosedEventArgs : EventArgs
```

Provides data for the [Closed](../Fluence.Wpf.Controls/InfoBar.md#api-23b2f7b0587b) event.

**Remarks:** Initializes a new instance of the [InfoBarClosedEventArgs](InfoBarClosedEventArgs.md) class.

**Parameter `reason`:** Why the info bar closed.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/InfoBarClosedEventArgs.cs)

## Constructors

<a id="api-b8143ba94a34"></a>

### InfoBarClosedEventArgs

```csharp
public InfoBarClosedEventArgs(InfoBarCloseReason reason)
```

Provides data for the [Closed](../Fluence.Wpf.Controls/InfoBar.md#api-23b2f7b0587b) event.

**Remarks:** Initializes a new instance of the [InfoBarClosedEventArgs](InfoBarClosedEventArgs.md) class.

**Parameter `reason`:** Why the info bar closed.

## Properties

<a id="api-b38abb2b708a"></a>

### Reason

```csharp
public InfoBarCloseReason Reason { get; }
```

Gets the reason the info bar closed.

## Related types

- [Fluence.Wpf.InfoBarCloseReason](InfoBarCloseReason.md)
