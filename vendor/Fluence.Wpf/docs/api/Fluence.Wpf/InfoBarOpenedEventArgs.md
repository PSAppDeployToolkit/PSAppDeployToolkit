# InfoBarOpenedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public sealed class InfoBarOpenedEventArgs : EventArgs
```

Provides data for the [Opened](../Fluence.Wpf.Controls/InfoBar.md#api-a2b39605dbb9) event. WinUI's `InfoBarOpenedEventArgs` carries no members either; the type exists so the event can gain data in a later minor release without a breaking signature change.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/InfoBarOpenedEventArgs.cs)

## Constructors

<a id="api-2e0b06da4007"></a>

### InfoBarOpenedEventArgs

```csharp
public InfoBarOpenedEventArgs()
```

Creates a new `InfoBarOpenedEventArgs` instance.
