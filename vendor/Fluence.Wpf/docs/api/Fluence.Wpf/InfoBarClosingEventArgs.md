# InfoBarClosingEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public class InfoBarClosingEventArgs : EventArgs
```

Provides data for the [Closing](../Fluence.Wpf.Controls/InfoBar.md#api-17ca0f3be614) event.

**Remarks:** Initializes a new instance of the [InfoBarClosingEventArgs](InfoBarClosingEventArgs.md) class.

**Parameter `reason`:** Why the info bar is closing.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/InfoBarClosingEventArgs.cs)

## Constructors

<a id="api-058e6fcd908e"></a>

### InfoBarClosingEventArgs

```csharp
public InfoBarClosingEventArgs(InfoBarCloseReason reason)
```

Provides data for the [Closing](../Fluence.Wpf.Controls/InfoBar.md#api-17ca0f3be614) event.

**Remarks:** Initializes a new instance of the [InfoBarClosingEventArgs](InfoBarClosingEventArgs.md) class.

**Parameter `reason`:** Why the info bar is closing.

## Properties

<a id="api-b3041dfb2e75"></a>

### Cancel

```csharp
public bool Cancel { get; set; }
```

Gets or sets a value indicating whether the close operation should be canceled. Set to `true` to prevent the [InfoBar](../Fluence.Wpf.Controls/InfoBar.md) from closing.

<a id="api-6800b1655444"></a>

### Reason

```csharp
public InfoBarCloseReason Reason { get; }
```

Gets the reason the info bar is closing. It matches the [Reason](InfoBarClosedEventArgs.md#api-b38abb2b708a) of the [Closed](../Fluence.Wpf.Controls/InfoBar.md#api-23b2f7b0587b) that follows when the close is not canceled.

## Related types

- [Fluence.Wpf.InfoBarCloseReason](InfoBarCloseReason.md)
