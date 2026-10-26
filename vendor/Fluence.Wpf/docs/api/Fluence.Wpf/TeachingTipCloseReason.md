# TeachingTipCloseReason

[C# API](../index.md) / [Fluence.Wpf](index.md)

- **Assembly:** `Fluence.Wpf`
- **Namespace:** `Fluence.Wpf`

```csharp
public enum TeachingTipCloseReason
```

Describes why a [TeachingTip](../Fluence.Wpf.Controls/TeachingTip.md) closed. Mirrors WinUI's `TeachingTipCloseReason`.

[C# source](https://github.com/sintaxasn/Fluence.Wpf/blob/main/Fluence.Wpf/TeachingTipCloseReason.cs)

## Values

<a id="api-032aa181d369"></a>

### CloseButton

```csharp
CloseButton = 0
```

The user clicked the close button.

<a id="api-a10999473a78"></a>

### LightDismiss

```csharp
LightDismiss = 1
```

The user dismissed the tip by clicking away from it.

<a id="api-e066cb2a0f70"></a>

### Programmatic

```csharp
Programmatic = 2
```

Code set [IsOpen](../Fluence.Wpf.Controls/TeachingTip.md#api-7199b282e925) to `false`.
