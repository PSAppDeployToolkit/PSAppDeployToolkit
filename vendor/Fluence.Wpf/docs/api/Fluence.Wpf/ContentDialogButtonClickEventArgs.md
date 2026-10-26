# ContentDialogButtonClickEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public class ContentDialogButtonClickEventArgs : EventArgs
```

Provides data for the [PrimaryButtonClick](../Fluence.Wpf.Controls/ContentDialog.md#api-e548fff3243f), [SecondaryButtonClick](../Fluence.Wpf.Controls/ContentDialog.md#api-2853a2ed535e), and [CloseButtonClick](../Fluence.Wpf.Controls/ContentDialog.md#api-7b12b3276bc9) events.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/ContentDialogButtonClickEventArgs.cs)

## Constructors

<a id="api-af596ca40df6"></a>

### ContentDialogButtonClickEventArgs

```csharp
public ContentDialogButtonClickEventArgs()
```

Creates a new `ContentDialogButtonClickEventArgs` instance.

## Properties

<a id="api-bd49d9c01b88"></a>

### Cancel

```csharp
public bool Cancel { get; set; }
```

Gets or sets a value indicating whether the button click should be canceled. Set to `true` to keep the [ContentDialog](../Fluence.Wpf.Controls/ContentDialog.md) open and skip the associated button command.
