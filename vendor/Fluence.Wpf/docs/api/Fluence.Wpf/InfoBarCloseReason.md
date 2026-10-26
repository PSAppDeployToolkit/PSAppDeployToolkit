# InfoBarCloseReason

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public enum InfoBarCloseReason
```

Describes why a [InfoBar](../Fluence.Wpf.Controls/InfoBar.md) closed. Mirrors WinUI's `InfoBarCloseReason`.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/InfoBarCloseReason.cs)

## Values

<a id="api-dde777cee7ed"></a>

### CloseButton

```csharp
CloseButton = 0
```

The user clicked the close button.

<a id="api-72290b7d899f"></a>

### Programmatic

```csharp
Programmatic = 1
```

Code set [IsOpen](../Fluence.Wpf.Controls/InfoBar.md#api-8afd9c710efd) to `false`.
