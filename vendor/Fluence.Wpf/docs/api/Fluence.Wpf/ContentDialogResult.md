# ContentDialogResult

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public enum ContentDialogResult
```

Specifies the result of a [ContentDialog](../Fluence.Wpf.Controls/ContentDialog.md) interaction, mirroring the WinUI 3 `ContentDialogResult` enumeration.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/ContentDialogResult.cs)

## Values

<a id="api-ffdcc3d4bda8"></a>

### None

```csharp
None = 0
```

The dialog was dismissed without the user selecting the primary or secondary button (close button, Escape key, or a programmatic hide).

<a id="api-e9316f0d09fc"></a>

### Primary

```csharp
Primary = 1
```

The user invoked the primary button.

<a id="api-0854f28fa873"></a>

### Secondary

```csharp
Secondary = 2
```

The user invoked the secondary button.
