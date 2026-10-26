# ContentDialogOpenedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public sealed class ContentDialogOpenedEventArgs : EventArgs
```

Provides data for the [Opened](../Fluence.Wpf.Controls/ContentDialog.md#api-413bbefe7c4a) event. WinUI's `ContentDialogOpenedEventArgs` carries no members either; the type exists so the event can gain data in a later minor release without a breaking signature change.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/ContentDialogOpenedEventArgs.cs)

## Constructors

<a id="api-0bf19f07c6f4"></a>

### ContentDialogOpenedEventArgs

```csharp
public ContentDialogOpenedEventArgs()
```

Creates a new `ContentDialogOpenedEventArgs` instance.
