# FlyoutBaseClosingEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public class FlyoutBaseClosingEventArgs : EventArgs
```

Provides data for the [Closing](../Fluence.Wpf.Controls/FlyoutBase.md#api-b19856a13e79) event.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/FlyoutBaseClosingEventArgs.cs)

## Constructors

<a id="api-22cd22207889"></a>

### FlyoutBaseClosingEventArgs

```csharp
public FlyoutBaseClosingEventArgs()
```

Creates a new `FlyoutBaseClosingEventArgs` instance.

## Properties

<a id="api-057e7c6f12d1"></a>

### Cancel

```csharp
public bool Cancel { get; set; }
```

Gets or sets a value indicating whether the close operation should be canceled. Set to `true` to keep the flyout open.
