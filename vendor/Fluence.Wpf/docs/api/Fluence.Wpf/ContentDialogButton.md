# ContentDialogButton

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public enum ContentDialogButton
```

Identifies a command button of a [ContentDialog](../Fluence.Wpf.Controls/ContentDialog.md), mirroring the WinUI 3 `ContentDialogButton` enumeration. Used by [DefaultButton](../Fluence.Wpf.Controls/ContentDialog.md#api-595f77184284) to select which button receives initial focus and responds to the Enter key.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/ContentDialogButton.cs)

## Values

<a id="api-9c786d6ee39f"></a>

### Close

```csharp
Close = 3
```

The close button is the default.

<a id="api-88b3a8062d6a"></a>

### None

```csharp
None = 0
```

No button is treated as the default.

<a id="api-3d9901e627e6"></a>

### Primary

```csharp
Primary = 1
```

The primary button is the default.

<a id="api-0d5cb7298225"></a>

### Secondary

```csharp
Secondary = 2
```

The secondary button is the default.
