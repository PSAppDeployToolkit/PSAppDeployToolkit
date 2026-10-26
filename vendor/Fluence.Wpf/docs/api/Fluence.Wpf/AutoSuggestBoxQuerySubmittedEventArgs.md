# AutoSuggestBoxQuerySubmittedEventArgs

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public class AutoSuggestBoxQuerySubmittedEventArgs : EventArgs
```

Provides data for the [QuerySubmitted](../Fluence.Wpf.Controls/AutoSuggestBox.md#api-3eca4afc52ef) event.

**Base type:** [`EventArgs`](https://learn.microsoft.com/dotnet/api/system.eventargs) (including inherited WPF and .NET members)

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/AutoSuggestBoxQuerySubmittedEventArgs.cs)

## Constructors

<a id="api-50967831c71c"></a>

### AutoSuggestBoxQuerySubmittedEventArgs

```csharp
public AutoSuggestBoxQuerySubmittedEventArgs()
```

Creates a new `AutoSuggestBoxQuerySubmittedEventArgs` instance.

## Properties

<a id="api-3ffc45d31122"></a>

### ChosenSuggestion

```csharp
public object? ChosenSuggestion { get; set; }
```

Gets or sets the suggestion that was chosen to submit the query, or `null` when the query was submitted without choosing a suggestion.

<a id="api-b4432294f953"></a>

### QueryText

```csharp
public string QueryText { get; set; }
```

Gets or sets the query text at the time the query was submitted.
