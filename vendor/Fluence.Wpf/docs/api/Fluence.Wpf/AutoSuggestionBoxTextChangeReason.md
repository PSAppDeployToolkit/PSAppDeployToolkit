# AutoSuggestionBoxTextChangeReason

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public enum AutoSuggestionBoxTextChangeReason
```

Specifies the reason the text changed in an [AutoSuggestBox](../Fluence.Wpf.Controls/AutoSuggestBox.md), mirroring the WinUI 3 `AutoSuggestionBoxTextChangeReason` enumeration.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/AutoSuggestionBoxTextChangeReason.cs)

## Values

<a id="api-95adfffba47a"></a>

### ProgrammaticChange

```csharp
ProgrammaticChange = 1
```

The text was changed programmatically.

<a id="api-7b211a96769f"></a>

### SuggestionChosen

```csharp
SuggestionChosen = 2
```

A suggestion was chosen and the text was updated from it.

<a id="api-85159bbb421c"></a>

### UserInput

```csharp
UserInput = 0
```

The user edited the text.
