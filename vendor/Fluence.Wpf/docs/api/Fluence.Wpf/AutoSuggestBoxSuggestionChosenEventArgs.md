# AutoSuggestBoxSuggestionChosenEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public class AutoSuggestBoxSuggestionChosenEventArgs : EventArgs
```

Provides data for the [SuggestionChosen](../Fluence.Wpf.Controls/AutoSuggestBox.md#api-ad5b21807785) event.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/AutoSuggestBoxSuggestionChosenEventArgs.cs)

## Constructors

<a id="api-69a09585d9b3"></a>

### AutoSuggestBoxSuggestionChosenEventArgs

```csharp
public AutoSuggestBoxSuggestionChosenEventArgs()
```

Creates a new `AutoSuggestBoxSuggestionChosenEventArgs` instance.

## Properties

<a id="api-8bb8b91dc8c5"></a>

### SelectedItem

```csharp
public object? SelectedItem { get; set; }
```

Gets or sets the suggestion that was chosen from the suggestion list.
