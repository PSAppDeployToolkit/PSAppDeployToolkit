# AutoSuggestBoxTextChangedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public class AutoSuggestBoxTextChangedEventArgs : EventArgs
```

Provides data for the [TextChanged](../Fluence.Wpf.Controls/AutoSuggestBox.md#api-e7db91ba4290) event.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/AutoSuggestBoxTextChangedEventArgs.cs)

## Constructors

<a id="api-f52958ef9745"></a>

### AutoSuggestBoxTextChangedEventArgs

```csharp
public AutoSuggestBoxTextChangedEventArgs()
```

Creates a new `AutoSuggestBoxTextChangedEventArgs` instance.

## Properties

<a id="api-e4026a76e522"></a>

### Reason

```csharp
public AutoSuggestionBoxTextChangeReason Reason { get; set; }
```

Gets or sets the reason the text changed.

## Methods

<a id="api-9308ec5ec268"></a>

### CheckCurrent

```csharp
public bool CheckCurrent()
```

Returns whether the text of the [AutoSuggestBox](../Fluence.Wpf.Controls/AutoSuggestBox.md) that raised this event is unchanged since the event was raised. Use this WinUI parity helper to discard stale asynchronous filtering results. Returns `true` for instances not raised by an [AutoSuggestBox](../Fluence.Wpf.Controls/AutoSuggestBox.md).

**Returns:** `true` when the owning box text still matches the text captured when the event was raised; otherwise `false`.

## Related types

- [Fluence.Wpf.AutoSuggestionBoxTextChangeReason](AutoSuggestionBoxTextChangeReason.md)
