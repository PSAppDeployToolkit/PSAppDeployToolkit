# ContentDialogClosedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public sealed class ContentDialogClosedEventArgs : EventArgs
```

Provides data for the [Closed](../Fluence.Wpf.Controls/ContentDialog.md#api-ce53999dec5b) event.

**Remarks:** Initializes a new instance of the [ContentDialogClosedEventArgs](ContentDialogClosedEventArgs.md) class.

**Parameter `result`:** The outcome that closed the dialog.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/ContentDialogClosedEventArgs.cs)

## Constructors

<a id="api-d845bd8e87b2"></a>

### ContentDialogClosedEventArgs

```csharp
public ContentDialogClosedEventArgs(ContentDialogResult result)
```

Provides data for the [Closed](../Fluence.Wpf.Controls/ContentDialog.md#api-ce53999dec5b) event.

**Remarks:** Initializes a new instance of the [ContentDialogClosedEventArgs](ContentDialogClosedEventArgs.md) class.

**Parameter `result`:** The outcome that closed the dialog.

## Properties

<a id="api-0e7b5a2e690b"></a>

### Result

```csharp
public ContentDialogResult Result { get; }
```

Gets the outcome that closed the dialog. This is the same value the task returned by [ShowAsync](../Fluence.Wpf.Controls/ContentDialog.md#api-8c20dda5a3cd) completes with.

## Related types

- [Fluence.Wpf.ContentDialogResult](ContentDialogResult.md)
